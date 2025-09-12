using cfg;
using Core;
using Luban;
using Proto;
using Share;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix
{
	public class ConfigLoader:Singleton<ConfigLoader>
	{
		public Tables Tables;

		public void Load()
		{
			Tables = new Tables(file => {
				var url = $"{Environment.CurrentDirectory}/GameConfig/Bin/{file}.bytes";
				return new ByteBuf(File.ReadAllBytes(url));
			});
		}
	}
}
