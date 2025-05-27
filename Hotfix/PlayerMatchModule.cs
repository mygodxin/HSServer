
using HSServer;
using NLog;
using Proto;

namespace Hotfix
{
    [Hotfix]
    public class PlayerMatchModule : IHotfixRun
    {
        public Task Run(params string[] args)
        {
            Console.WriteLine("你好啊2222");
            return Task.CompletedTask;
        }
    }
}