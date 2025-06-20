


using Core;
using LoginServer;
using Luban;
using Share;
using System.Net;

namespace HSServer
{
    public static class Program
    {

        public static void Main(string[] args)
        {
            // 初始化日志
            Logger.Init();

            // 初始化代码加载
            var controller = new CodeLoader();
            controller.Init();

            LoginServer.LoginServer.StartAsync();

            GateServer.GateServer.StartAsync();

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