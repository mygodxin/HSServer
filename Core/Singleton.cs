namespace Core
{
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        protected Singleton()
        {
        }

        private static T _instance = new T();

        public static T Instance => _instance;
    }
}
