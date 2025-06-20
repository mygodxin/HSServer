
using Core.Net;
using System.Reflection;

namespace Core
{
    public class HotfixManager : Singleton<HotfixManager>
    {
        private Dictionary<int, Type> _msgHandles = new Dictionary<int, Type>();
        private Dictionary<string, Type> _httpHandles = new Dictionary<string, Type>();

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
            var msgIdField = attribute.MessageType.GetField("GID", BindingFlags.Static | BindingFlags.Public);
            if (msgIdField == null) return false;
            int msgId = (int)msgIdField.GetValue(null);

            if (!_msgHandles.ContainsKey(msgId))
                _msgHandles.Add(msgId, type);
            else
            {
                Logger.Error($"add olready has msg:{msgId}");
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