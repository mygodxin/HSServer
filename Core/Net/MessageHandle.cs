using System;

namespace Core
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageHandle
    {
        public NetChannel Channel;
        public Message Message;

        public virtual void Excute()
        {

        }
    }
}
