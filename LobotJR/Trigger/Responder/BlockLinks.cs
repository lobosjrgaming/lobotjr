using LobotJR.Command.System.Player;
using LobotJR.Twitch.Model;
using System;
using System.Text.RegularExpressions;

namespace LobotJR.Trigger.Responder
{
    public class BlockLinks : ITriggerResponder
    {
        private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(30);

        public Regex Pattern { get; private set; } = new Regex(@"([A-Za-z0-9])\.([A-Za-z])([A-Za-z0-9])", RegexOptions.IgnoreCase);
        private readonly PlayerSystem PlayerSystem;
        private DateTime LastTrigger = DateTime.Now - Cooldown;

        public BlockLinks(PlayerSystem playerSystem)
        {
            PlayerSystem = playerSystem;
        }

        public TriggerResult Process(Match match, User user)
        {
            var player = PlayerSystem.GetPlayerByUser(user);
            if (!match.Groups[0].Value.Equals("d.va")
                && !user.IsSub
                && !user.IsMod
                && player.Level < 2
                && player.Prestige < 1
                && LastTrigger + Cooldown <= DateTime.Now)
            {
                LastTrigger = DateTime.Now;
                return new TriggerResult()
                {
                    Messages = new string[] { "Links may only be posted by viewers of Level 2 or above. (Message me '?' for more details)" },
                    TimeoutSender = true,
                    TimeoutMessage = $"@{user.Username} Links may only be posted by viewers of Level 2 or above. (Message me '?' for more details)"
                };
            }
            return null;
        }
    }
}
