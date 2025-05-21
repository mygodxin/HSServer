


using Core;

namespace HSServer
{
    public static class Program
    {

        public static void Main(string[] args)
        {
            // 初始化日志
            Logger.Init();

            Logger.Info("打印info");
            Logger.Warn("打印warn");
            Logger.Debug("打印debug");
            Logger.Error("打印error");
            // 初始化代码加载
            var controller = new CodeLoader();
            controller.Init();

            Console.WriteLine("输入'reload'触发热更新，'exit'退出");

            ActorTest.Test();

            while (true)
            {
                var input = Console.ReadLine();
                if (input == "reload")
                {
                    controller.Reload();
                }
                else if (input == "exit")
                {
                    Environment.Exit(0);
                }
            }
        }
    }
}