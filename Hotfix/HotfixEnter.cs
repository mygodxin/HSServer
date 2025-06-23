
using HSServer;
using NLog;
using Proto;

namespace Hotfix
{
    [Hotfix]
    public class HotfixEnter : IHotfixRun
    {
        public Task Run(params string[] args)
        {
            Console.WriteLine("[HotfixEnter] Run");
            return Task.CompletedTask;
        }
    }
}