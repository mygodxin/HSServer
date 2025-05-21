using Core;

namespace HSServer
{
    public class TestActor : IActor
    {
        public Task ReceiveAsync(IContext context)
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
                        context.Send(pid2, "NIHAO");
                    }
                    else
                    {
                        Console.WriteLine($"收到消息: {msg}self: {context.Self} sender:{context.Sender}");
                    }
                    break;
            }
            return Task.CompletedTask;
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
                        context.Send(context.Sender, "NIHAO2");

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

            var system = new ActorSystem().Root;

            var props = Props.FromProducer(() => new TestActor());
            var pid1 = system.Spawn(props);

            system.Send(pid1, "Hello");

            var response = await system.RequestAsync<string>(pid1, "Request");
            Console.WriteLine(response);
        }
    }
}
