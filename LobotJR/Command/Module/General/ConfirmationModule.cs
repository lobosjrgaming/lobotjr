﻿using LobotJR.Command.Controller.General;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;

namespace LobotJR.Command.Module.General
{
    /// <summary>
    /// Module containing commands for responding to events that require
    /// confirmation.
    /// </summary>
    public class ConfirmationModule : ICommandModule
    {
        private readonly ConfirmationController ConfirmationSystem;

        /// <summary>
        /// Prefix applied to names of commands within this module.
        /// </summary>
        public string Name => "Confirmation";
        /// <summary>
        /// Triggered when the player confirms or cancels something.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this module provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public ConfirmationModule(ConfirmationController confirmationSystem)
        {
            ConfirmationSystem = confirmationSystem;
            Commands = new List<CommandHandler>()
            {
                new CommandHandler("Confirm", this, CommandMethod.GetInfo(Confirm), "y", "yes", "accept", "confirm"),
                new CommandHandler("Cancel", this, CommandMethod.GetInfo(Cancel), "n", "no", "decline", "cancel", "nevermind"),
            };
        }

        public CommandResult Confirm(User user)
        {
            ConfirmationSystem.Confirm(user);
            return new CommandResult(true);
        }

        public CommandResult Cancel(User user)
        {
            ConfirmationSystem.Cancel(user);
            return new CommandResult(true);
        }
    }
}
