using Microsoft.AspNetCore.Identity.Data;
using Proto;
using Proto.Remote;
using Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    internal class LoginHandler : IActor
    {
        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case ReqLogin req:
                    // 1. 认证用户
                    var token = GenerateToken(req.Account);

                    var gateServer = await context.System.Remote().SpawnNamedAsync("127.0.0.1:8001", "gate_server", "gate_server", TimeSpan.FromSeconds(3000));
                    //context.Send(gateServer.Pid, new UserLoginEvent { UserId = 111111 });

                    var gateServer1 = PID.FromAddress("127.0.0.1:8001", "gate_server");
                    //context.Send(gateServer1, new UserLoginEvent { UserId = 222222 });

                    break;
            }
        }

        private string GenerateToken(string username) => $"{username}_{DateTime.UtcNow.Ticks}";
    }
}
