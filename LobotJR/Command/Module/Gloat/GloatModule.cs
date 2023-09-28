using LobotJR.Command.System.Gloat;
using LobotJR.Twitch.Model;
using System.Collections.Generic;

namespace LobotJR.Command.Module.Gloat
{
    /// <summary>
    /// Module of access control commands.
    /// </summary>
    public class GloatModule : ICommandModule
    {
        private readonly GloatSystem GloatSystem;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Gloat";

        /// <summary>
        /// Notifications when a tournament starts or ends.
        /// </summary>
        public event PushNotificationHandler PushNotification;

        /// <summary>
        /// A collection of commands for managing access to commands.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public GloatModule(GloatSystem gloatSystem)
        {
            GloatSystem = gloatSystem;
            Commands = new CommandHandler[]
            {
                new CommandHandler("GloatFish", GloatFish, "gloatfish", "fishgloat", "gloat-fish")
            };
        }

        public CommandResult GloatFish(string data, User user)
        {
            if (int.TryParse(data, out var index))
            {
                if (GloatSystem.CanGloatFishing(user))
                {
                    var record = GloatSystem.FishingGloat(user, index - 1);
                    if (record != null)
                    {
                        return new CommandResult(user, $"You spent {GloatSystem.FishingGloatCost} wolfcoins to brag about your biggest {record.Fish.Name}.")
                        {
                            Messages = new string[] { $"{user.Username} gloats about the time they caught a {record.Length} in. long, {record.Weight} pound {record.Fish.Name} lobosSmug" }
                        };
                    }
                    return new CommandResult(user, "You don't have any fish! Type !cast to try and fish for some!");
                }
                return new CommandResult(user, "You don't have enough coins to gloat!");
            }
            return new CommandResult(user, "Invalid request. Syntax: !gloatfish <Fish #>");
        }
    }
}
