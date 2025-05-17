
using HSServer;
using NLog;

namespace HotUpdate
{
    [Hotfix]
    public class PlayerMatchModule : IHotfixStart
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Task Run(params string[] args)
        {
            Console.WriteLine("你好啊2222");
            return Task.CompletedTask;
        }

    }
}