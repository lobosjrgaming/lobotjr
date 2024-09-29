using LobotJR.Command.Controller.General;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;

namespace LobotJR.Command.View.General
{
    /// <summary>
    /// View containing commands for responding to events that require
    /// confirmation.
    /// </summary>
    public class ConfirmationView : ICommandView
    {
        private readonly ConfirmationController ConfirmationController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Confirmation";
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public ConfirmationView(ConfirmationController confirmationController)
        {
            ConfirmationController = confirmationController;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("Confirm", this, CommandMethod.GetInfo(Confirm), "y", "yes", "accept", "confirm") { TimeoutInChat = false },
                new CommandHandler("Cancel", this, CommandMethod.GetInfo(Cancel), "n", "no", "decline", "cancel", "nevermind") { TimeoutInChat = false },
            };
        }

        public CommandResult Confirm(User user)
        {
            ConfirmationController.Confirm(user);
            return new CommandResult(true);
        }

        public CommandResult Cancel(User user)
        {
            ConfirmationController.Cancel(user);
            return new CommandResult(true);
        }
    }
}
