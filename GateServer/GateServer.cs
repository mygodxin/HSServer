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
            var remoteConfig = GrpcNetRemoteConfig
                .BindTo("127.0.0.1", 8001)
                .WithRemoteKind("gate_server", Props.FromProducer(() => new GateManager()))
                .WithSerializer(10, 10, new MsgPackSerializer());

            var system = new ActorSystem()
                .WithRemote(remoteConfig);
            //.WithCluster();

            await system
               .Remote()
               .StartAsync();

            //system.Root.SpawnNamed(Props.FromProducer(() => new GateManager()), "gate_server");

            // 保持运行
            await Task.Delay(-1);
        }
    }
}
