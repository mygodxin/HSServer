using Core.Protocol;
using NetCoreServer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

// 安全配置类
public class SecurityConfig
{
    public int MaxConnectionsPerIp { get; set; } = 5;
    public int MaxMessagesPerMinute { get; set; } = 100;
    public int MessageSizeLimit { get; set; } = 16384; // 16KB
    public TimeSpan IpBlacklistDuration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);
}

// IP黑名单管理
public class IpBlacklist
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklistedIps = new();
    private readonly ConcurrentDictionary<string, DateTime> _temporaryBlacklist = new();
    public TimeSpan IpBlacklistDuration { get; set; } = TimeSpan.FromMinutes(30);

    public bool IsBlacklisted(string ip)
    {
        // 检查永久黑名单
        if (_blacklistedIps.ContainsKey(ip))
            return true;

        // 检查临时黑名单
        if (_temporaryBlacklist.TryGetValue(ip, out var expiry) && DateTime.UtcNow < expiry)
            return true;

        return false;
    }

    public void AddToBlacklist(string ip, bool permanent = false)
    {
        if (permanent)
            _blacklistedIps[ip] = DateTime.UtcNow;
        else
            _temporaryBlacklist[ip] = DateTime.UtcNow.Add(IpBlacklistDuration);
    }

    public void RemoveFromBlacklist(string ip)
    {
        _blacklistedIps.TryRemove(ip, out _);
        _temporaryBlacklist.TryRemove(ip, out _);
    }

    public void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in _temporaryBlacklist.ToArray())
        {
            if (now > entry.Value)
                _temporaryBlacklist.TryRemove(entry.Key, out _);
        }
    }
}

// 速率限制器
public class RateLimiter
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _requestTimestamps = new();
    private readonly ConcurrentDictionary<string, int> _connectionCounts = new();
    private readonly SecurityConfig _config;

    public RateLimiter(SecurityConfig config)
    {
        _config = config;
    }

    public bool CheckConnectionLimit(string ip)
    {
        var count = _connectionCounts.GetOrAdd(ip, 0);
        if (count >= _config.MaxConnectionsPerIp)
            return false;

        _connectionCounts[ip] = count + 1;
        return true;
    }

    public bool CheckMessageRate(string ip)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-1);

        var timestamps = _requestTimestamps.GetOrAdd(ip, _ => new List<DateTime>());

        // 清理过期的时间戳
        timestamps.RemoveAll(t => t < windowStart);

        if (timestamps.Count >= _config.MaxMessagesPerMinute)
            return false;

        timestamps.Add(now);
        return true;
    }

    public void DecrementConnectionCount(string ip)
    {
        if (_connectionCounts.TryGetValue(ip, out var count) && count > 0)
            _connectionCounts[ip] = count - 1;
    }

    public void CleanupOldEntries()
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-5); // 清理5分钟前的记录

        foreach (var entry in _requestTimestamps.ToArray())
        {
            entry.Value.RemoveAll(t => t < windowStart);
            if (entry.Value.Count == 0)
                _requestTimestamps.TryRemove(entry.Key, out _);
        }

        // 清理连接数为0的IP
        foreach (var entry in _connectionCounts.ToArray())
        {
            if (entry.Value == 0)
                _connectionCounts.TryRemove(entry.Key, out _);
        }
    }
}

// 安全WebSocket会话
public class SecureWebSocketSession : WsSession
{
    private readonly SecureWebSocketServer _server;
    private string _clientIp;
    private DateTime _lastActivity;
    private bool _authenticated;

    public SecureWebSocketSession(SecureWebSocketServer server) : base(server)
    {
        _server = server;
        _clientIp = "";
        _lastActivity = DateTime.UtcNow;
        _authenticated = false;
    }

    public override void OnWsConnected(HttpRequest request)
    {
        // 获取客户端IP
        _clientIp = request.Url.ToString() ?? "unknown";

        Console.WriteLine($"WebSocket session connected from {_clientIp}");

        // 安全检查
        if (_server.IpBlacklist.IsBlacklisted(_clientIp))
        {
            Console.WriteLine($"Blocked blacklisted IP: {_clientIp}");
            Close(1000); // Normal closure
            return;
        }

        if (!_server.RateLimiter.CheckConnectionLimit(_clientIp))
        {
            Console.WriteLine($"Connection limit exceeded for IP: {_clientIp}");
            _server.IpBlacklist.AddToBlacklist(_clientIp, false);
            Close(1008); // Policy violation
            return;
        }

        // 发送欢迎消息
        SendText("Connected to secure WebSocket server");

        // 启动会话超时检查
        _server.AddSession(this);
    }

    public override void OnWsDisconnected()
    {
        Console.WriteLine($"WebSocket session disconnected from {_clientIp}");
        _server.RateLimiter.DecrementConnectionCount(_clientIp);
        _server.RemoveSession(this);
    }

    public override void OnWsReceived(byte[] buffer, long offset, long size)
    {
        _lastActivity = DateTime.UtcNow;

        // 检查消息大小
        if (size > _server.SecurityConfig.MessageSizeLimit)
        {
            Console.WriteLine($"Message too large from {_clientIp}");
            Close(1009); // Message too big
            return;
        }

        // 检查消息频率
        if (!_server.RateLimiter.CheckMessageRate(_clientIp))
        {
            Console.WriteLine($"Message rate exceeded for IP: {_clientIp}");
            _server.IpBlacklist.AddToBlacklist(_clientIp, false);
            Close(1008); // Policy violation
            return;
        }

        // 处理消息
        try
        {
            //MessageHandle.Read(new ReadOnlySpan<byte>(buffer, (int)offset, (int)size));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            SendText($"ERROR: {ex.Message}");
        }
    }

    public bool IsTimedOut()
    {
        return DateTime.UtcNow - _lastActivity > _server.SecurityConfig.SessionTimeout;
    }

    public string GetClientIp()
    {
        return _clientIp;
    }
}

// 安全WebSocket服务器
public class SecureWebSocketServer : WsServer
{
    private readonly ConcurrentDictionary<string, int> _failedAuthCounts = new();
    private readonly List<SecureWebSocketSession> _sessions = new();
    private readonly object _sessionsLock = new object();
    private readonly System.Timers.Timer _cleanupTimer;

    public SecurityConfig SecurityConfig { get; }
    public IpBlacklist IpBlacklist { get; }
    public RateLimiter RateLimiter { get; }
    public string[] AllowedOrigins { get; set; } = { "https://yourdomain.com", "http://localhost:3000", "null" };

    public SecureWebSocketServer(IPAddress address, int port) : base(address, port)
    {
        SecurityConfig = new SecurityConfig();
        IpBlacklist = new IpBlacklist();
        RateLimiter = new RateLimiter(SecurityConfig);

        // 设置清理定时器
        _cleanupTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        _cleanupTimer.Elapsed += CleanupTimerElapsed;
        _cleanupTimer.AutoReset = true;
        _cleanupTimer.Start();
    }

    protected override TcpSession CreateSession()
    {
        return new SecureWebSocketSession(this);
    }

    public bool IsAllowedOrigin(string origin)
    {
        // 允许null origin（某些浏览器可能会发送null）
        if (origin == "null")
            return true;

        return Array.Exists(AllowedOrigins, o => o.Equals(origin, StringComparison.OrdinalIgnoreCase));
    }

    public void IncrementFailedAuthCount(string ip)
    {
        var count = _failedAuthCounts.AddOrUpdate(ip, 1, (key, oldValue) => oldValue + 1);

        // 如果认证失败次数过多，加入黑名单
        if (count >= 5)
        {
            IpBlacklist.AddToBlacklist(ip, false);
            _failedAuthCounts.TryRemove(ip, out _);
            Console.WriteLine($"IP {ip} temporarily blacklisted due to multiple auth failures");
        }
    }

    public void AddSession(SecureWebSocketSession session)
    {
        lock (_sessionsLock)
        {
            _sessions.Add(session);
        }
    }

    public void RemoveSession(SecureWebSocketSession session)
    {
        lock (_sessionsLock)
        {
            _sessions.Remove(session);
        }
    }

    private void CleanupTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // 清理过期会话
        List<SecureWebSocketSession> sessionsToRemove = new List<SecureWebSocketSession>();

        lock (_sessionsLock)
        {
            foreach (var session in _sessions)
            {
                if (session.IsTimedOut())
                {
                    Console.WriteLine($"Session from {session.GetClientIp()} timed out");
                    sessionsToRemove.Add(session);
                }
            }

            foreach (var session in sessionsToRemove)
            {
                session.Close(1000); // Normal closure
                _sessions.Remove(session);
            }
        }

        // 清理黑名单和速率限制器的过期条目
        IpBlacklist.CleanupExpired();
        RateLimiter.CleanupOldEntries();

        // 清理失败认证计数
        var now = DateTime.UtcNow;
        foreach (var entry in _failedAuthCounts.ToArray())
        {
            // 假设失败计数在1小时后重置
            if (now.Hour != DateTime.UtcNow.Hour)
                _failedAuthCounts.TryRemove(entry.Key, out _);
        }
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"WebSocket server caught an error: {error}");
    }

    public override bool Start()
    {
        Console.WriteLine($"Starting WebSocket server on {Address}:{Port}");
        return base.Start();
    }

    public override bool Stop()
    {
        _cleanupTimer.Stop();
        _cleanupTimer.Dispose();
        return base.Stop();
    }
}

// 监控和统计
public class SecurityMonitor
{
    private readonly SecureWebSocketServer _server;

    public SecurityMonitor(SecureWebSocketServer server)
    {
        _server = server;
    }

    public void PrintStats()
    {
        Console.WriteLine("=== Security Stats ===");
        Console.WriteLine($"Active connections: {_server.ConnectedSessions}");
        // 可以添加更多统计信息
    }
}