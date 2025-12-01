using Core;
using MemoryPack;

namespace Share.Message
{
    [MemoryPackUnionFormatter(typeof(IMessage))]
    [MemoryPackUnion(0, typeof(LoginRequest))]
    [MemoryPackUnion(1, typeof(LoginResponse))]
    [MemoryPackUnion(2, typeof(ClientMessage))]
    [MemoryPackUnion(3, typeof(BroadcastMessage))]
    public partial class MessageUnion
    {
    }
}
