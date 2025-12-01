// SessionManagerActor.cs
using Proto;
using Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
namespace Hotfix.Login;

public class UserSession
{
    public string Username { get; set; }
    public string ConnectionId { get; set; }
    public string Token { get; set; }
    public DateTime LoginTime { get; set; }
}

public class SendMessage
{
    public string Data { get; set; }
}

public class SessionManagerActor : IActor
{
    private readonly Dictionary<string, PID> _connectedClients = new();
    private readonly Dictionary<string, UserSession> _authenticatedSessions = new();
    private IContext _context;

    public Task ReceiveAsync(IContext context)
    {
        _context = context; // 保存context引用

        switch (context.Message)
        {
            case ClientConnected connected:
                HandleClientConnected(connected);
                break;

            case ClientDisconnected disconnected:
                HandleClientDisconnected(disconnected);
                break;

            case LoginRequest loginRequest:
                HandleLoginRequest(loginRequest);
                break;

            case BroadcastMessage broadcastMessage:
                HandleBroadcastMessage(broadcastMessage);
                break;
        }
        return Task.CompletedTask;
    }

    private void HandleClientConnected(ClientConnected connected)
    {
        _connectedClients[connected.ConnectionId] = connected.ClientActor;
        Console.WriteLine($"Client connected: {connected.ConnectionId}");
    }

    private void HandleClientDisconnected(ClientDisconnected disconnected)
    {
        if (_connectedClients.ContainsKey(disconnected.ConnectionId))
        {
            _connectedClients.Remove(disconnected.ConnectionId);

            // 同时从认证会话中移除
            var sessionToRemove = _authenticatedSessions
                .FirstOrDefault(x => x.Value.ConnectionId == disconnected.ConnectionId);
            if (!string.IsNullOrEmpty(sessionToRemove.Key))
            {
                _authenticatedSessions.Remove(sessionToRemove.Key);
            }
        }
        Console.WriteLine($"Client disconnected: {disconnected.ConnectionId}");
    }

    private void HandleLoginRequest(LoginRequest loginRequest)
    {
        // 简单的认证逻辑
        bool isAuthenticated = AuthenticateUser(loginRequest.Username, loginRequest.Password);

        var response = new LoginResponse();

        if (isAuthenticated)
        {
            var token = Guid.NewGuid().ToString();
            response.Success = true;
            response.Message = "Login successful";
            response.Token = token;

            // 保存会话信息
            _authenticatedSessions[loginRequest.Username] = new UserSession
            {
                Username = loginRequest.Username,
                ConnectionId = loginRequest.ConnectionId,
                Token = token,
                LoginTime = DateTime.Now
            };

            Console.WriteLine($"User {loginRequest.Username} logged in successfully");
        }
        else
        {
            response.Success = false;
            response.Message = "Invalid username or password";
            Console.WriteLine($"Failed login attempt for user: {loginRequest.Username}");
        }

        // 发送响应回客户端
        if (_connectedClients.TryGetValue(loginRequest.ConnectionId, out var clientActor))
        {
            var responseJson = JsonSerializer.Serialize(new
            {
                type = "login_response",
                success = response.Success,
                message = response.Message,
                token = response.Token
            });
            _context.Send(clientActor, new SendMessage { Data = responseJson });
        }
    }

    private void HandleBroadcastMessage(BroadcastMessage message)
    {
        var broadcastJson = JsonSerializer.Serialize(new
        {
            type = "broadcast",
            sender = message.Sender,
            content = message.Content,
            timestamp = message.Timestamp
        });

        // 向所有认证的客户端广播消息
        foreach (var session in _authenticatedSessions.Values)
        {
            if (_connectedClients.TryGetValue(session.ConnectionId, out var clientActor))
            {
                // 使用_context.Send而不是SendSystemMessage
                _context.Send(clientActor, new SendMessage { Data = broadcastJson });
            }
        }

        Console.WriteLine($"Broadcast message from {message.Sender}: {message.Content}");
    }

    private bool AuthenticateUser(string username, string password)
    {
        // 简单的演示认证逻辑
        return (username == "admin" && password == "123456") ||
               (username == "user" && password == "password");
    }
}