
using Newtonsoft.Json;
using System;
using System.IO;

namespace Core.Util
{
    public class ServerManager
    {
        private static ServerConfig _config;

        public static void Init()
        {
            _config = Load<ServerConfig>("Config/ServerConfig.json");
        }

        /// <summary>
        /// 加载本地json文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T Load<T>(string path)
        {
            var configJson = File.ReadAllText(path);
            if (configJson == null)
            {
                throw new Exception($"Load config file error: {path}");
            }
            return (T)JsonConvert.DeserializeObject<T>(configJson);
        }
    }
}
