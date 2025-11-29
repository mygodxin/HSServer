using cfg;
using Core;
using Luban;
using System;
using System.IO;

namespace Hotfix
{
    public class ConfigLoader : Singleton<ConfigLoader>
    {
        public Tables Tables;

        public void Load()
        {
            Tables = new Tables(file =>
            {
                var url = $"{Environment.CurrentDirectory}/GameConfig/Bin/{file}.bytes";
                return new ByteBuf(File.ReadAllBytes(url));
            });
        }
    }
}
