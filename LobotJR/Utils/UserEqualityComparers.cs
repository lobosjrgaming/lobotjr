using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;

namespace LobotJR.Utils
{
    public class UserNameEqualityComparer : IEqualityComparer<User>
    {
        public bool Equals(User x, User y)
        {
            return x.Username != null && x.Username.Equals(y.Username, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(User obj)
        {
            return obj?.Username.ToUpperInvariant().GetHashCode() ?? 0;
        }
    }

    public class UserIdEqualityComparer : IEqualityComparer<User>
    {
        public bool Equals(User x, User y)
        {
            return x.TwitchId != null && x.TwitchId.Equals(y.TwitchId);
        }

        public int GetHashCode(User obj)
        {
            return obj?.TwitchId.GetHashCode() ?? 0;
        }
    }
}
