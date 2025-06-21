


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

            // 登录服务启动
            LoginServer.LoginServer.StartAsync();

            // 网关服务启动
            GateServer.GateServer.StartAsync();

            // 游戏服务启动


            while (true)
            {
                var input = Console.ReadLine();
                if (input == "reload")
                {
                    controller.Reload();
                }
                else if (input == "exit")
                {
                    LoginServer.LoginServer.StopAsync();
                    Environment.Exit(0);
                }
            }
        }
    }
}