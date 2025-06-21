using Core;
using Core.Net.Http;
using Proto;

using Proto.Remote;
using Proto.Remote.GrpcNet;
using Share;

namespace LoginServer
{
    /// <summary>
    /// 只负责账号验证
    /// </summary>
    public class LoginServer
    {
        private static HttpServer _http;

        public static async void StartAsync()
        {
            InitActorSystem();
            InitListener();
            await Task.Delay(-1);
        }

        private static async void InitActorSystem()
        {
            var remoteConfig = GrpcNetRemoteConfig
                .BindTo("127.0.0.1", 8000)
                .WithRemoteKind("login_server", Props.FromProducer(() => new LoginHandler()))
                .WithSerializer(10, 10, new MsgPackSerializer());
            var system = new ActorSystem()
                .WithRemote(remoteConfig);
            await system
               .Remote()
               .StartAsync();

            var prop = Props.FromProducer(() => new LoginHandler());
            var pid = system.Root.Spawn(prop);
            system.Root.Send(pid, new ReqLogin { Account = "mdx", Password = "123456", Platform = "Test" });
        }

        private static async void InitListener()
        {
            _http = new HttpServer();
            await _http.Start(20000);
        }

        public static async Task StopAsync()
        {
            await _http.Stop();
        }
    }
}
