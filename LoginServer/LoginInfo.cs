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
        public string AccountId { get; set; }
        public string SessionToken { get; set; }
        public DateTime LoginTime { get; set; }
        public string ConnectedGameServer { get; set; }
        public string ClientIp { get; set; }
        public ISocket Socket { get; set; }
    }

    public class ServerInfo
    {
        public string Name { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public float CpuUsage { get; set; }
        public DateTime LastUpdateTime { get; set; }

        public float LoadFactor =>
            (CurrentPlayers / (float)MaxPlayers) * 0.7f + CpuUsage * 0.3f;
    }

    public class ServerHeartbeat
    {
        public string ServerName { get; set; }
        public int PlayerCount { get; set; }
        public float CpuUsage { get; set; }
    }
}
