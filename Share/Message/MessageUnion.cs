using Core;
using MemoryPack;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Message
{
    [MemoryPackUnionFormatter(typeof(IMessage))]
    [MemoryPackUnion(0, typeof(ReqLogin))]
    [MemoryPackUnion(1, typeof(ResLogin))]
    [MemoryPackUnion(2, typeof(LoginRequest))]
    [MemoryPackUnion(3, typeof(LoginResponse))]
    [MemoryPackUnion(4, typeof(PlayerMoveRequest))]
    [MemoryPackUnion(5, typeof(PlayerMoveResponse))]
    [MemoryPackUnion(6, typeof(SessionValidationRequest))]
    [MemoryPackUnion(7, typeof(SessionValidationResponse))]
    [MemoryPackUnion(8, typeof(RegisterServer))]
    [MemoryPackUnion(9, typeof(ServerAddress))]
    [MemoryPackUnion(10, typeof(GetServer))]
    [MemoryPackUnion(11, typeof(ServerResponse))]
    public partial class MessageUnion
    {
    }
}
