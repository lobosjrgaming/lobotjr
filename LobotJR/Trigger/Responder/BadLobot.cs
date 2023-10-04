using LobotJR.Twitch.Model;
using System;
using System.Text.RegularExpressions;

namespace LobotJR.Trigger.Responder
{
    internal class BadLobot : ITriggerResponder
    {
        public Regex Pattern { get; private set; } = new Regex("^Bad lobot", RegexOptions.IgnoreCase);

        public BadLobot()
        {
        }

        public TriggerResult Process(Match match, User user)
        {
            if (user.Username.Equals("empyrealhell", StringComparison.OrdinalIgnoreCase)
            || user.Username.Equals("celesteenfer", StringComparison.OrdinalIgnoreCase))
            {
                return new TriggerResult()
                {
                    Messages = new string[] { "Sorry mistress! lobosS" }
                };
            }
            else if (user.IsAdmin)
            {
                return new TriggerResult()
                {
                    Messages = new string[] { "Leave me alone, dad!" }
                };
            }
            else if (user.IsMod)
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
