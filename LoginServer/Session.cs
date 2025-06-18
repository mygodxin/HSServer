using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    public class Session
    {
        public string Account { get; set; }
        public string SessionToken { get; set; }
        public DateTime LoginTime { get; set; }
        public string ConnectedGameServer { get; set; }
        public string RemoteAddress { get; set; }
        public NetChannel Channel { get; set; }
    }
    public class ServerHeartbeat
    {
        public string ServerName { get; set; }
        public int PlayerCount { get; set; }
        public float CpuUsage { get; set; }
    }
}
