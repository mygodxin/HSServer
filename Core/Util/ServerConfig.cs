using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Util
{
    public class ServerConfig
	{
        /// <summary>
        /// 服务器ID
        /// </summary>
        public int ServerID;

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerURL;

        /// <summary>
        /// mongo地址
        /// </summary>
        public string MongoURL;

		/// <summary>
		/// mongo库名
		/// </summary>
		public string MongoName;
	}
}
