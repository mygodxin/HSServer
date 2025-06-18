
namespace Core
{
    public class HotfixManager : Singleton<HotfixManager>
    {
        private Dictionary<int, Type> _msgHandles = new Dictionary<int, Type>();

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
    }

}