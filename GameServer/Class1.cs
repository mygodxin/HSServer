// GameServer.cs
using Proto;

namespace GameServer
{

    public class GameInstance : IActor
    {
        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case byte[] gameData:
                    // 处理游戏逻辑
                    var response = ProcessGameLogic(gameData);
                    context.Respond(response);
                    break;
            }
            return Task.CompletedTask;
        }

        private byte[] ProcessGameLogic(byte[] data) => new byte[] { 0x01 }; // 示例响应
    }

    public static class Program
    {
        public static async Task Main()
        {

            await Task.Delay(-1); // 保持运行
        }
    }
}
