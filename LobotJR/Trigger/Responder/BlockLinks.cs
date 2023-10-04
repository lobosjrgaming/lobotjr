using LobotJR.Twitch.Model;
using System.Text.RegularExpressions;
using Wolfcoins;

namespace LobotJR.Trigger.Responder
{
    public class BlockLinks : ITriggerResponder
    {
        public Regex Pattern { get; private set; } = new Regex(@"([A-Za-z0-9])\.([A-Za-z])([A-Za-z0-9])", RegexOptions.IgnoreCase);
        private Currency UserList;

        public BlockLinks(Currency currency)
        {
            UserList = currency;
        }

        public TriggerResult Process(Match match, User user)
        {
            if (!match.Groups[0].Value.Equals("d.va")
                && !user.IsSub
                && !user.IsMod
                && UserList.determineLevel(user.Username) < 2
                && UserList.determinePrestige(user.Username) < 1)
            {
                return new TriggerResult()
                {
                    Messages = new string[] { "Links may only be posted by viewers of Level 2 or above. (Message me '?' for more details)" },
                    TimeoutSender = true,
                    TimeoutMessage = "Links may only be posted by viewers of Level 2 or above. (Message me '?' for more details)"
                };
            }
            return null;
        }
    }
}
