using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Net
{
    /// <summary>
    /// 消息标签
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class HttpCommondAttribute : Attribute
    {
        public string Commond;

        public HttpCommondAttribute(string commond)
        {
            Commond = commond;
        }
    }

    public abstract class HttpHandle
    {
        public abstract Task<string> Excute(string id, string url, Dictionary<string, string> paramMap);
    }
}
