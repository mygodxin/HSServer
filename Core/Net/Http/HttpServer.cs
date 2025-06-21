using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Core.Net.Http
{
    public class HttpServer
    {
        private WebApplication _http { get; set; }
        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="port"></param>
        public async Task Start(int port, bool isDebug = true)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel(options =>
            {
                if (isDebug)
                {
                    options.ListenAnyIP(port);
                }
                else
                {
                    options.ListenAnyIP(port, builder =>
                    {
                        builder.UseHttps();
                    });
                }
            })
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Error);
            }).UseNLog();

            _http = builder.Build();
            _http.MapGet("/game/{text}", HandleRequest);
            _http.MapPost("/game/{text}", HandleRequest);
            await _http.StartAsync();
            Logger.Info("[HttpServer] start!");
            return;
        }

        private async Task HandleRequest(HttpContext context)
        {
            try
            {
                string ip = context.Connection.RemoteIpAddress.ToString();
                string url = context.Request.PathBase + context.Request.Path;
                Logger.Info($"收到来自[{ip}]的HTTP请求. 请求url:[{url}]");
                Dictionary<string, string> paramMap = new Dictionary<string, string>();

                foreach (var keyValuePair in context.Request.Query)
                    paramMap.Add(keyValuePair.Key, keyValuePair.Value[0]);

                context.Response.Headers.Append("content-type", "text/html;charset=utf-8");

                if (context.Request.Method.Equals("POST"))
                {
                    var headCType = context.Request.ContentType;
                    if (string.IsNullOrEmpty(headCType))
                    {
                        await context.Response.WriteAsync("http header content type is null");
                        return;
                    }
                    var isJson = context.Request.HasJsonContentType();
                    var isForm = context.Request.HasFormContentType;
                    if (isJson)
                    {
                        JsonElement json = await context.Request.ReadFromJsonAsync<JsonElement>();
                        foreach (var keyValuePair in json.EnumerateObject())
                        {
                            if (paramMap.ContainsKey(keyValuePair.Name))
                            {
                                await context.Response.WriteAsync(new HttpReturn(HttpStatus.ParamErr, "参数重复了:" + keyValuePair.Name));
                                return;
                            }
                            var key = keyValuePair.Name;
                            var val = keyValuePair.Value.GetString();
                            paramMap.Add(keyValuePair.Name, keyValuePair.Value.GetString());
                        }
                    }
                    else if (isForm)
                    {
                        foreach (var keyValuePair in context.Request.Form)
                        {
                            if (paramMap.ContainsKey(keyValuePair.Key))
                            {
                                await context.Response.WriteAsync(new HttpReturn(HttpStatus.ParamErr, "参数重复了:" + keyValuePair.Key));
                                return;
                            }
                            paramMap.Add(keyValuePair.Key, keyValuePair.Value[0]);
                        }
                    }
                }

                var str = new StringBuilder();
                str.Append("请求参数:");
                foreach (var parameter in paramMap)
                {
                    if (parameter.Key.Equals(""))
                        continue;
                    str.Append("'").Append(parameter.Key).Append("'='").Append(parameter.Value).Append("'  ");
                }
                Logger.Info(str.ToString());

                if (!paramMap.TryGetValue("command", out var cmd))
                {
                    await context.Response.WriteAsync(new HttpReturn(HttpStatus.CommondErr));
                    return;
                }

                var handler = HandleManager.Instance.GetHttpHandle(cmd);
                if (handler == null)
                {
                    await context.Response.WriteAsync(new HttpReturn(HttpStatus.ParamErr, "commond 不存在:" + cmd));
                    return;
                }

                var ret = await handler.Excute(ip, url, paramMap);
                await context.Response.WriteAsync(ret);
            }
            catch (Exception e)
            {
                Logger.Error($"执行http异常. {e.Message} {e.StackTrace}");
                await context.Response.WriteAsync(e.Message);
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public Task Stop()
        {
            if (_http != null)
            {
                Logger.Info("[HttpServer] stop!");
                var task = _http.StopAsync();
                _http = null;
                return task;
            }
            return Task.CompletedTask;
        }
    }
}
