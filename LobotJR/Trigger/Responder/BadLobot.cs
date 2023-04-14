using LobotJR.Shared.Authentication;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Wolfcoins;

namespace LobotJR.Trigger.Responder
{
    internal class BadLobot : ITriggerResponder
    {
        public Regex Pattern { get; private set; } = new Regex("^Bad lobot", RegexOptions.IgnoreCase);
        private Currency UserList;
        private TokenData TokenData;

        public BadLobot(Currency currency, TokenData tokenData)
        {
            UserList = currency;
            TokenData = tokenData;
        }

        public TriggerResult Process(Match match, string user)
        {
            if (user.Equals("empyrealhell", StringComparison.OrdinalIgnoreCase)
            || user.Equals("celesteenfer", StringComparison.OrdinalIgnoreCase))
            {
                return new TriggerResult()
                {
                    Messages = new string[] { "Sorry mistress! lobosS" }
                };
            }
            else if (user.Equals(TokenData.BroadcastUser, StringComparison.OrdinalIgnoreCase))
            {
                return new TriggerResult()
                {
                    Messages = new string[] { "Leave me alone, dad!" }
                };
            }
            else if (UserList.moderatorList.Contains(user, StringComparer.OrdinalIgnoreCase))
            {
                return new TriggerResult()
                {
                    Messages = new string[] { "Whatever..." }
                };
            }
            return null;
        }
    }
}
