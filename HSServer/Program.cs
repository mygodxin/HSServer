


using Core;
using Luban;
using Share;

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

            //var tables = new cfg.Tables(file => new ByteBuf(File.ReadAllBytes("../../../../GenerateDatas/bytes/" + file + ".bytes")));
            //Console.WriteLine("== load succ ==");

            // 初始化代码加载
            var controller = new CodeLoader();
            controller.Init();

            Console.WriteLine("输入'reload'触发热更新，'exit'退出");

            ActorTest.Test();

            var msg = new C2SLogin();
            msg.Account = 5555 + "";
            msg.Password = 6666 + "";
            //序列化测试
            var p = HSerializer.Serialize(msg);
            var o = HSerializer.Deserialize<C2SLogin>(p);
            Logger.Info($"序列化结果{o.Account},{o.Password}");

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