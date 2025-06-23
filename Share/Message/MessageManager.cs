using Core;
using Core.MessageExtension.PolymorphicMessagePack;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share
{
    public class MessageManager
    {
        /// <summary>
        /// 注册message子类
        /// </summary>
        public static void Register()
        {
            Logger.Info("[注册Message]");

            PolyTypeMapper.Register<ReqLogin>();
            PolyTypeMapper.Register<ResLogin>();

            PolyResolver.Instance.Init();
        }
    }
}
