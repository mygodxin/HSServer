
using Core.Net;
using Core.Protocol;
using Core.Timers;
using System.Reflection;

namespace Core
{
    public class HandleManager : Singleton<HandleManager>
    {
        private Dictionary<int, Type> _msgHandles = new Dictionary<int, Type>();
        private Dictionary<string, Type> _httpHandles = new Dictionary<string, Type>();
        private Dictionary<string, Type> _timeHandles = new Dictionary<string, Type>();

        /// <summary>
        /// 获取message唯一ID
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetID(Type type)
        {
            return (int)MurmurHash3.Hash(type.FullName);
        }

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
            var id = GetID(msgType);
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
                Logger.Error($"add olready has fullName:{commond}");
            }
            return true;
        }

        public ITimerHandler GetTimerHandle(string commond)
        {
            if (_timeHandles.TryGetValue(commond, out var type))
            {
                var inst = Activator.CreateInstance(type);
                if (inst is ITimerHandler handle)
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

        public bool AddTimerHandle(Type type)
        {
            var handler = type.GetInterface(typeof(ITimerHandler).FullName);
            if (handler == null) return false;
            var fullName = type.FullName;

            if (!_timeHandles.ContainsKey(fullName))
                _timeHandles.Add(fullName, type);
            else
            {
                Logger.Error($"add olready has timer:{fullName}");
            }
            return true;
        }
    }

}