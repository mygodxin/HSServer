using Core;
using Proto;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Share;

namespace GateServer
{
    public class GateServer
    {
        public static async void StartAsync()
        {
            InitActorSystem();
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
    }
}
