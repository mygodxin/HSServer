
using Core.Net;
using System.Reflection;

namespace Core
{
    public class HandleManager : Singleton<HandleManager>
    {
        private Dictionary<int, Type> _msgHandles = new Dictionary<int, Type>();
        private Dictionary<string, Type> _httpHandles = new Dictionary<string, Type>();
        public Dictionary<Type, int> Messages;

        public MessageHandle GetMessageHandle(int msgID)
        {
            if (_msgHandles.TryGetValue(msgID, out var type))
            {
                var inst = Activator.CreateInstance(type);
                if (inst is MessageHandle handle)
                {
                    return handle;
                }
                else
                {
                    throw new Exception($"GetMessageHandle error:{inst.GetType().FullName}");
                }
            }
            return null;
        }

        public bool AddMessageHandle(Type type)
        {
            var attribute = (MessageTypeAttribute)type.GetCustomAttribute(typeof(MessageTypeAttribute), true);
            if (attribute == null) return false;
            var msgType = attribute.MessageType;
            var id = Messages[msgType];
            Logger.Warn($"[add] {id}");
            if (!_msgHandles.ContainsKey(id))
                _msgHandles.Add(id, type);
            else
            {
                Logger.Error($"add olready has msg:{msgType.Name}");
            }
            return true;
        }

        public HttpHandle GetHttpHandle(string commond)
        {
            if (_httpHandles.TryGetValue(commond, out var type))
            {
                var inst = Activator.CreateInstance(type);
                if (inst is HttpHandle handle)
                {
                    return handle;
                }
                else
                {
                    throw new Exception($"GetMessageHandle error:{inst.GetType().FullName}");
                }
            }
            return null;
        }

        public bool AddHttpHandle(Type type)
        {
            var attribute = (HttpCommondAttribute)type.GetCustomAttribute(typeof(HttpCommondAttribute));
            if (attribute == null) return false;
            var commond = attribute.Commond;

            if (!_httpHandles.ContainsKey(commond))
                _httpHandles.Add(commond, type);
            else
            {
                Logger.Error($"add olready has commond:{commond}");
            }
            return true;
        }
    }

}