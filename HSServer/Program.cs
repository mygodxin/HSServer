


using Core;
using LoginServer;
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

            // 初始化代码加载
            var controller = new CodeLoader();
            controller.Init();

            var loginServer = new LoginServer.LoginServer(3000);

            // 添加几个游戏服务器
            loginServer.AddGameServer(new ServerInfo
            {
                Name = "GameServer1",
                Ip = "192.168.1.101",
                Port = 4000,
                MaxPlayers = 1000
            });

            loginServer.AddGameServer(new ServerInfo
            {
                Name = "GameServer2",
                Ip = "192.168.1.102",
                Port = 4000,
                MaxPlayers = 1000
            });

            // 启动游戏服务器状态监控(在另一个端口)
            _ = loginServer.StartGameServerMonitorAsync(3001);

            // 启动登录服务器
            _ = loginServer.StartAsync();

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