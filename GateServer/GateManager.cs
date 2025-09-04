using Core;
using Proto;
using Share;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GateServer
{
    public class GateManager : IActor
    {
        private readonly Dictionary<int, PID> _sessions = new();

        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                //case UserLoginEvent msg:
                //    // 为新用户创建 Session Actor
                //    Logger.Info($"收到消息={msg.UserId}");
                //    var sessionProps = Props.FromProducer(() => new UserSession(msg.UserId));
                //    _sessions[msg.UserId] = context.Spawn(sessionProps);
                //    break;

                //case int userId when _sessions.TryGetValue(userId, out var pid):
                //    // 踢出用户
                //    context.Stop(pid);
                //    _sessions.Remove(userId);
                //    break;
            }
            return Task.CompletedTask;
        }
    }
}
