using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using NLog.Web;

namespace Core.Net.WS
{
    public class WSServer
    {
        WebApplication _ws { get; set; }

        public async Task StartAsync(string url)
        {
            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseUrls(url).UseNLog();
            _ws = builder.Build();

            _ws.UseWebSockets();

            _ws.Map("/ws", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var conn = new WSChannel(webSocket);
                    await conn.StartAsync();
                    await conn.DisconnectAsync();
                }
                else
                {
                    context.Response.StatusCode = 404;
                }
            });
            Logger.Info("[WSServer] started");
            await _ws.StartAsync();
        }

        /// <summary>
        /// 停止
        /// </summary>
        public async Task StopAsync()
        {
            if (_ws != null)
            {
                Logger.Info("[WSServer] stop");
                await _ws.StopAsync();
                _ws = null;
            }
        }
    }
}