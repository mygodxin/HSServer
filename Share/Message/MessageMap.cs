using Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share
{
    public class MessageMap
    {
        public static Dictionary<Type, int> Messages = new Dictionary<Type, int>()
        {
            {typeof(ReqLogin),1 },
            {typeof(ResLogin) ,2},
        };
    }
}
