
using Core;
using HSServer;
using Proto;
using System;
using System.Threading.Tasks;

namespace Hotfix
{
    [Hotfix]
    public class HotfixEnter : IHotfixRun
    {
        public async Task Run(params string[] args)
        {
            Console.WriteLine("[HotfixEnter] Run");

            // º”‘ÿ≈‰÷√±Ì
            ConfigLoader.Instance.Load();

            Logger.Info($"{ConfigLoader.Instance.Tables.Tbitem[1001].Name}111111222221");

            var actorSystem = new ActorSystem();
            var server = new Hotfix.Login.TcpServer(actorSystem);

            try
            {
                await server.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
        }
    }
}