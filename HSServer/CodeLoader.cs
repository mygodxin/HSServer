using Core;
using Share;
using System.Reflection;
using System.Runtime.Loader;

namespace HSServer
{
    /// <summary>
    /// 热更Attribute
    /// </summary>
    public class HotfixAttribute : Attribute
    {
    }

    /// <summary>
    /// 热更完成后立刻运行
    /// </summary>
    public interface IHotfixRun
    {
        public Task Run(params string[] args);
    }

    /// <summary>
    /// 代码加载，HotUpdate程序集中所有需要热更的类都需要添加HotUpdateAttribute标签
    /// </summary>
    public class CodeLoader
    {
        private readonly string[] Assemblys = ["Hotfix"];
        private Dictionary<string, AssemblyLoadContext> _assemblyLoadContexts;
        private List<Assembly> _assemblys;

        public void Init()
        {
            _assemblys = new List<Assembly>();
            _assemblyLoadContexts = new Dictionary<string, AssemblyLoadContext>();
            Reload();
        }

        private Assembly LoadHotUpdate(string assemblyName)
        {
            _assemblyLoadContexts.TryGetValue(assemblyName, out var oldContext);
            oldContext?.Unload();
            oldContext = new AssemblyLoadContext(assemblyName, true);
            byte[] dllBytes = File.ReadAllBytes($"./Hotfix/{assemblyName}.dll");
            byte[] pdbBytes = File.ReadAllBytes($"./Hotfix/{assemblyName}.pdb");
            var assembly = oldContext.LoadFromStream(new MemoryStream(dllBytes), new MemoryStream(pdbBytes));
            return assembly;
        }

        public void Reload()
        {
            _assemblys.Clear();
            IHotfixRun hotfixRun = null;
            for (int i = 0; i < Assemblys.Length; i++)
            {
                var assemblyName = Assemblys[i];
                var assembly = LoadHotUpdate(assemblyName);
                _assemblys.Add(assembly);
            }
            GC.Collect();

            for (int i = 0; i < _assemblys.Count; i++)
            {
                var assembly = _assemblys[i];
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (!HandleManager.Instance.AddMessageHandle(type) && !HandleManager.Instance.AddHttpHandle(type) && !HandleManager.Instance.AddTimerHandle(type))
                    {
                        var hotfixStart = type.GetInterface(typeof(IHotfixRun).FullName);
                        if (hotfixStart != null)
                        {
                            var hotfix = Activator.CreateInstance(type);
                            hotfixRun = ((IHotfixRun)hotfix);
                        }
                    }
                }
            }
            // 运行热更代码
            if (hotfixRun != null)
                hotfixRun.Run();
            else
                Logger.Error($"hotfix run error");
        }
    }
}