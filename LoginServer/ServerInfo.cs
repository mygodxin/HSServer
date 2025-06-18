using System;

namespace LoginServer
{
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
}
