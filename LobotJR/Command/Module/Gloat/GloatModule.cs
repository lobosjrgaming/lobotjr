using LobotJR.Command.System.Gloat;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;

namespace LobotJR.Command.Module.Gloat
{
    /// <summary>
    /// Module containing commands that allow players to gloat about their
    /// achievements.
    /// </summary>
    public class GloatModule : ICommandModule
    {
        private readonly GloatSystem GloatSystem;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Gloat";
        /// <summary>
        /// Invoked to push gloat messages to public chat.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public GloatModule(GloatSystem gloatSystem)
        {
            GloatSystem = gloatSystem;
            Commands = new CommandHandler[]
            {
                new CommandHandler("GloatFish", this, CommandMethod.GetInfo<int>(GloatFish), "gloatfish", "fishgloat", "gloat-fish")
            };
        }

        public CommandResult GloatFish(User user, int index)
        {
            if (GloatSystem.CanGloatFishing(user))
            {
                var max = GloatSystem.GetFishCount(user);
                if (max == 0)
                {
                    return new CommandResult("You don't have any fish! Type !cast to try and fish for some!");
                }
                else if (index < 1 || index >= max)
                {
                    return new CommandResult($"Invalid index. Please use a number between 1 and {max}");
                }
                else
                {
                    var record = GloatSystem.FishingGloat(user, index - 1);
                    if (record != null)
                    {
                        return new CommandResult($"You spent {GloatSystem.FishingGloatCost} wolfcoins to brag about your biggest {record.Fish.Name}.")
                        {
                            Messages = new string[] { $"{user.Username} gloats about the time they caught a {record.Length} in. long, {record.Weight} pound {record.Fish.Name} lobosSmug" }
                        };
                    }
                }
                return new CommandResult("Uh oh! Something went wrong trying to gloat, please check your inputs and try again.");
            }
            return new CommandResult("You don't have enough coins to gloat!");
        }
    }
}
