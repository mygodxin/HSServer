using Proto;
using Share;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GateServer
{
    public class UserSession : IActor
    {
        private readonly int _userId;
        private PID _gamePid;

        public UserSession(int userId)
        {
            _userId = userId;
        }

        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case GateForward msg:
                    break;

                case byte[] gameData: // 从 GameServer 返回的响应
                                      // 发送回客户端（伪代码）
                                      // clientSocket.Write(gameData);
                    break;
            }
        }
    }
}
