using System;

namespace LobotJR.Twitch
{
    [Flags]
    public enum TwitchRole
    {
        None = 0,
        Sub = 1,
        Vip = 2,
        Mod = 4,
        Streamer = 8
    }
}
