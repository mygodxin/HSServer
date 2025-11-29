
using Core;
using HSServer;
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

            LoginServer.LoginServer.StartAsync();
        }
    }
}