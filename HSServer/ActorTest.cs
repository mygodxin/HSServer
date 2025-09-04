using Core;
using Proto;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace HSServer
{
    public class TestActor : IActor
    {
        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case string msg:
                    if (msg == "Hello")
                    {
                        Console.WriteLine($"收到消息: {msg}self: {context.Self} sender:{context.Sender}");

                        context.Respond("OK");


                        var props2 = Props.FromProducer(() => new TestActor1());
                        var pid2 = context.Spawn(props2);
                        //context.Write(pid2, "NIHAO");
                        var response = await context.RequestAsync<string>(pid2, "NIHAO");
                        Console.WriteLine(response);
                    }
                    else
                    {
                        Console.WriteLine($"收到消息: {msg}self: {context.Self} sender:{context.Sender}");
                        context.Respond("OK1111");
                    }
                    break;
            }
            //return default;//Task.CompletedTask;
        }
    }
    public class TestActor1 : IActor
    {
        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case string msg:
                    if (msg == "NIHAO")
                    {
                        Console.WriteLine($"收到消息1: {msg}self: {context.Self} sender:{context.Sender}");

                        context.Respond("OK1");
                        //context.Write(context.Sender, "NIHAO2");
                        //context.

                    }
                    break;
            }
            return Task.CompletedTask;
        }
    }

    public class ActorTest
    {
        public static async void Test()
        {
            var config = GrpcNetRemoteConfig.BindToLocalhost()
                .WithRemoteKind("echo", Props.FromProducer(() => new TestActor()))
                .WithSerializer(10, 10, new MsgPackSerializer())
                ;

            var actorSystem = new ActorSystem();

            var system = actorSystem.Root;

            var props = Props.FromProducer(() => new TestActor());
            var pid1 = system.Spawn(props);

            system.Send(pid1, "Hello");

            var response = await system.RequestAsync<string>(pid1, "Request");
            Console.WriteLine(response);
        }
    }
}
