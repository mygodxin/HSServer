


namespace HSServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var controller = new CodeLoader();
            controller.Init();

            Console.WriteLine("输入'reload'触发热更新，'exit'退出");

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