using Core;
//using Core.Net.WS;
using Proto;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Share;

namespace GateServer
{
    public class GateServer
    {
        //private static WSServer _ws;

        public static async void StartAsync()
        {
            InitActorSystem();
            InitListener();
            // 保持运行
            await Task.Delay(-1);
        }

        private static async void InitActorSystem()
        {
            var remoteConfig = GrpcNetRemoteConfig
                .BindTo("127.0.0.1", 8001)
                .WithRemoteKind("gate_server", Props.FromProducer(() => new GateManager())) // 和 system.Root.SpawnNamed(Props.FromProducer(() => new GateManager()), "gate_server"); 作用一致
                .WithSerializer(10, 10, new MsgPackSerializer());

            var system = new ActorSystem()
                .WithRemote(remoteConfig);

            await system
               .Remote()
               .StartAsync();
        }

        private static async void InitListener()
        {
            //_ws = new WSServer();
            //await _ws.StartAsync("http://127.0.0.1:3000");
        }

        public static async Task StopAsync()
        {
            //await _ws.StopAsync();
        }
    }
}
