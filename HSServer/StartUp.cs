using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSServer
{
    public class StartUp
    {
        public void Init()
        {
			// 初始化代码加载
			var controller = new CodeLoader();
			controller.Init();

			// 监听Ctrl+C、Ctrl+Break和关闭事件（通过CancelKeyPress）
			Console.CancelKeyPress += (sender, e) =>
			{
				Exit();
			};

			// 监听进程退出事件（包括右上角关闭按钮、环境退出等）
			AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
			{
				Exit();
			};

			// 如果是.NET Core，还可以监听UnhandledException事件来处理未处理的异常导致的退出
			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				Exit();
			};

			// 模拟工作循环
			while (true)
			{
				Thread.Sleep(1000);
			}
		}
        private void Exit()
		{
			var shutdown = Task.Run(()=>
			{
				// 在这里执行你的清理和关闭逻辑
				// 例如，关闭数据库连接、保存状态等
				Console.WriteLine("正在执行清理操作...");
				Thread.Sleep(2000); // 模拟清理操作的时间
				Console.WriteLine("清理操作完成，程序即将退出。");
			});
			shutdown.Wait();
		}
    }

    
}
