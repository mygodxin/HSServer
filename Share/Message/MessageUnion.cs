using Core;
using MemoryPack;

namespace Share.Message
{
    [MemoryPackUnionFormatter(typeof(IMessage))]
    [MemoryPackUnion(0, typeof(LoginRequest))]
    [MemoryPackUnion(1, typeof(LoginResponse))]
    [MemoryPackUnion(2, typeof(ClientMessage))]
    public partial class MessageUnion
    {
    }
}
