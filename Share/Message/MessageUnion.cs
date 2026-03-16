using MessagePack;

namespace Share.Message
{
    [Union(0, typeof(LoginRequest))]
    [Union(1, typeof(LoginResponse))]
    [Union(2, typeof(ClientMessage))]
    [Union(3, typeof(BroadcastMessage))]
    public partial class MessageUnion
    {
    }
}
