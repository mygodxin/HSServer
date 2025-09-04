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
    public partial class MessageUnion
    {
    }
}
