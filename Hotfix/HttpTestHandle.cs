using Core;
using Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix
{
    [HttpCommond("test")]
    public class HttpTestHandle : HttpHandle
    {
        public override Task<string> Excute(string id, string url, Dictionary<string, string> paramMap)
        {
            Logger.Info("测试test");
            return Task.FromResult("这是一串http测试文本");
        }
    }
}
