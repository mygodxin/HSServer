using Share;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LoginServer
{
    public class LoginServer
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<string, Session> _activeSessions;
        private readonly ConcurrentDictionary<string, DateTime> _loginAttempts;
        private readonly List<ServerInfo> _gameServers;
        private readonly int _maxLoginAttempts = 5;
        private readonly TimeSpan _loginCooldown = TimeSpan.FromMinutes(1);

        public LoginServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _activeSessions = new ConcurrentDictionary<string, Session>();
            _loginAttempts = new ConcurrentDictionary<string, DateTime>();
            _gameServers = new List<ServerInfo>();
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine($"LoginServer started on port {((IPEndPoint)_listener.LocalEndpoint).Port}");

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client); // 不等待，处理多个客户端
            }
        }

        public void AddGameServer(ServerInfo serverInfo)
        {
            _gameServers.Add(serverInfo);
            Console.WriteLine($"Added GameServer: {serverInfo.Name} (IP: {serverInfo.Ip}, Port: {serverInfo.Port})");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[1024];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var requestJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    var loginRequest = JsonSerializer.Deserialize<C2S_Login>(requestJson);
                    var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    var response = await ProcessLoginAsync(loginRequest, clientIp);

                    var responseJson = JsonSerializer.Serialize(response);
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
        }

        private async Task<S2C_Login> ProcessLoginAsync(C2S_Login request, string clientIp)
        {
            // 1. 验证账号密码 (简化版，实际应该查数据库)
            if (!ValidateCredentials(request.Account, request.Password))
            {
                return new S2C_Login { Success = false, Message = "Invalid account or password" };
            }

            // 2. 检查登录频率限制
            if (IsLoginFlooding(request.Account))
            {
                return new S2C_Login
                {
                    Success = false,
                    Message = "Too many login attempts. Please wait before trying again."
                };
            }

            // 3. 处理顶号逻辑
            if (_activeSessions.TryGetValue(request.Account, out var existingSession))
            {
                // 通知游戏服务器踢出原有连接
                await NotifyGameServerForKick(existingSession.ConnectedGameServer, existingSession.SessionToken);
                _activeSessions.TryRemove(request.Account, out _);
            }

            // 4. 选择负载最低的游戏服务器
            var selectedServer = SelectBestGameServer();
            if (selectedServer == null)
            {
                return new S2C_Login { Success = false, Message = "No available game servers" };
            }

            // 5. 创建新会话
            var newSession = new Session
            {
                AccountId = request.Account,
                SessionToken = GenerateSessionToken(),
                LoginTime = DateTime.UtcNow,
                ConnectedGameServer = selectedServer.Name,
                ClientIp = clientIp
            };

            _activeSessions[request.Account] = newSession;
            UpdateLoginAttempts(request.Account);

            // 6. 返回成功响应
            return new S2C_Login
            {
                Success = true,
                Message = "Login successful",
                GameServerIp = selectedServer.Ip,
                GameServerPort = selectedServer.Port,
                SessionToken = newSession.SessionToken
            };
        }

        // 验证账号密码 (简化版)
        private bool ValidateCredentials(string account, string password)
        {
            // 实际项目中应该查询数据库或调用认证服务
            return !string.IsNullOrEmpty(account) && !string.IsNullOrEmpty(password);
        }

        // 检查是否频繁登录
        private bool IsLoginFlooding(string account)
        {
            if (_loginAttempts.TryGetValue(account, out var lastAttempt))
            {
                var attemptCount = _loginAttempts.Count(kv =>
                    kv.Key == account &&
                    (DateTime.UtcNow - kv.Value) < _loginCooldown);

                return attemptCount >= _maxLoginAttempts;
            }
            return false;
        }

        // 更新登录尝试记录
        private void UpdateLoginAttempts(string account)
        {
            _loginAttempts.AddOrUpdate(account,
                DateTime.UtcNow,
                (key, oldValue) => DateTime.UtcNow);

            // 清理过期的记录
            foreach (var kvp in _loginAttempts)
            {
                if ((DateTime.UtcNow - kvp.Value) > _loginCooldown)
                {
                    _loginAttempts.TryRemove(kvp.Key, out _);
                }
            }
        }

        // 选择负载最低的游戏服务器
        private ServerInfo SelectBestGameServer()
        {
            if (_gameServers.Count == 0) return null;

            // 先移除不活跃的服务器(超过30秒未更新状态)
            _gameServers.RemoveAll(server =>
                (DateTime.UtcNow - server.LastUpdateTime) > TimeSpan.FromSeconds(30));

            // 按负载因子排序并选择最低负载的服务器
            return _gameServers
                .OrderBy(server => server.LoadFactor)
                .FirstOrDefault(server => server.CurrentPlayers < server.MaxPlayers);
        }

        // 生成会话令牌
        private string GenerateSessionToken()
        {
            return Guid.NewGuid().ToString("N") + DateTime.UtcNow.Ticks.ToString("x8");
        }

        // 通知游戏服务器踢出玩家(顶号处理)
        private async Task NotifyGameServerForKick(string gameServerName, string sessionToken)
        {
            var gameServer = _gameServers.FirstOrDefault(s => s.Name == gameServerName);
            if (gameServer == null) return;

            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(gameServer.Ip, gameServer.Port + 1); // 假设管理端口是游戏端口+1

                    var kickCommand = new
                    {
                        Action = "KickPlayer",
                        SessionToken = sessionToken,
                        Reason = "Logged in from another location"
                    };

                    var json = JsonSerializer.Serialize(kickCommand);
                    var bytes = Encoding.UTF8.GetBytes(json);

                    using (var stream = client.GetStream())
                    {
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to notify game server for kick: {ex.Message}");
            }
        }

        // 添加一个方法来处理游戏服务器的心跳更新
        public void UpdateGameServerStatus(string serverName, int playerCount, float cpuUsage)
        {
            var server = _gameServers.FirstOrDefault(s => s.Name == serverName);
            if (server != null)
            {
                server.CurrentPlayers = playerCount;
                server.CpuUsage = cpuUsage;
                server.LastUpdateTime = DateTime.UtcNow;
            }
        }

        // 可以单独开一个TCP端口来接收游戏服务器的心跳
        public async Task StartGameServerMonitorAsync(int monitorPort)
        {
            var monitorListener = new TcpListener(IPAddress.Any, monitorPort);
            monitorListener.Start();

            while (true)
            {
                var client = await monitorListener.AcceptTcpClientAsync();
                _ = HandleGameServerHeartbeatAsync(client);
            }
        }

        private async Task HandleGameServerHeartbeatAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[1024];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var heartbeatJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    var heartbeat = JsonSerializer.Deserialize<ServerHeartbeat>(heartbeatJson);
                    UpdateGameServerStatus(heartbeat.ServerName, heartbeat.PlayerCount, heartbeat.CpuUsage);

                    // 返回确认
                    var response = Encoding.UTF8.GetBytes("ACK");
                    await stream.WriteAsync(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling game server heartbeat: {ex.Message}");
            }
        }
    }
}
