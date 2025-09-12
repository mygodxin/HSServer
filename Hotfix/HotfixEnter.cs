
using Core;
using HSServer;
using Proto;

namespace Hotfix
{
    [Hotfix]
    public class HotfixEnter : IHotfixRun
    {
        public async Task Run(params string[] args)
        {
            Console.WriteLine("[HotfixEnter] Run");

            // �������ñ�
            ConfigLoader.Instance.Load();

            Logger.Info($"{ConfigLoader.Instance.Tables.Tbitem[1001].Name}1111111");

            LoginServer.LoginServer.StartAsync();
        }
    }
}