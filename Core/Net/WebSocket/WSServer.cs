using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using NLog.Web;

namespace Core.Net.WS
{
    public class WSServer : NetChannel
    {
        WebApplication app { get; set; }

        public Task Start(string url)
        {
            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseUrls(url).UseNLog();
            app = builder.Build();

            app.UseWebSockets();

            app.Map("/ws", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var conn = new WSChannel(webSocket, OnMessage);
                    await conn.StartAsync();
                    await conn.DisconnectAsync();
                }
                else
                {
                    context.Response.StatusCode = 404;
                }
            });
            Logger.Info("[WSServer] started");
            return app.StartAsync();
        }

        public void OnMessage(NetChannel channel, Message message)
        {
            var handle = HotfixManager.Instance.GetMessageHandle(message.ID);
            handle.Channel = channel;
            handle.Message = message;
            handle.Excute();
        }

        /// <summary>
        /// 停止
        /// </summary>
        public Task Stop()
        {
            if (app != null)
            {
                Logger.Info("[WSServer] stop");
                var task = app.StopAsync();
                app = null;
                return task;
            }
            return Task.CompletedTask;
        }
    }
}