


using Core;
using Core.Util;

namespace HSServer
{
    public static class Program
    {

        public static void Main(string[] args)
        {
            // 加载服务器配置
            ServerManager.Init();

            // 初始化日志
            Logger.Init();

            var startUp = new StartUp();
            startUp.Init();
        }
    }
}