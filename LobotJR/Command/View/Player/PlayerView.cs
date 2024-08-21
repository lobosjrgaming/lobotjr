﻿using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Controller.General;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Model.Player;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Command.View.Player
{
    /// <summary>
    /// View containing commands for player class selection and retrieving
    /// information about experience and currency.
    /// </summary>
    public class PlayerView : ICommandView, IPushNotifier
    {
        private readonly PlayerController PlayerController;
        private readonly PartyController PartyController;
        private readonly GroupFinderController GroupFinderController;

        /// <summary>
        /// Prefix applied to names of commands within this view.
        /// </summary>
        public string Name => "Player";
        /// <summary>
        /// Invoked to notify users when they level up.
        /// </summary>
        public event PushNotificationHandler PushNotification;
        /// <summary>
        /// A collection of commands this view provides.
        /// </summary>
        public IEnumerable<CommandHandler> Commands { get; private set; }

        public PlayerView(PlayerController playerController, PartyController partyController, GroupFinderController groupFinderController, ConfirmationController confirmationController)
        {
            PlayerController = playerController;
            PartyController = partyController;
            GroupFinderController = groupFinderController;
            PlayerController.LevelUp += PlayerController_LevelUp;
            PlayerController.ExperienceAwarded += PlayerController_ExperienceAwarded;
            confirmationController.Canceled += ConfirmationController_Canceled;
            var commands = new List<CommandHandler>()
            {
                new CommandHandler("Coins", this, CommandMethod.GetInfo(GetCoins), "coins"),
                new CommandHandler("Experience", this, CommandMethod.GetInfo(GetExperience), "xp", "level", "lvl"),
                new CommandHandler("Stats", this, CommandMethod.GetInfo<string>(GetStats), CommandMethod.GetInfo(GetStatsCompact), "stats"),
                new CommandHandler("ClassDistribution", this, CommandMethod.GetInfo(GetClassStats), "classes"),
                new CommandHandler("ClassSelect", this, CommandMethod.GetInfo<string>(SelectClass), "c", "class"),
                new CommandHandler("ClassHelp", this, CommandMethod.GetInfo(ClassHelp), "classhelp"),
                new CommandHandler("Respec", this, CommandMethod.GetInfo(Respec), "respec"),
            };
            //This adds aliases for each class in the database to allow for class selection in the form of "!c1", instead of "!c 1"
            var classes = PlayerController.GetPlayableClasses();
            foreach (var playerClass in classes)
            {
                commands.Add(new CommandHandler($"ClassSelect{playerClass.Name}", this, CommandMethod.GetInfo((User user) => { return SelectClass(user, playerClass.Id.ToString()); }), $"c{playerClass.Id}"));
            }
            Commands = commands;
        }

        private void ConfirmationController_Canceled(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            if (PlayerController.IsFlaggedForRespec(player))
            {
                PlayerController.UnflagForRespec(player);
                PushNotification?.Invoke(user, new CommandResult("Respec cancelled. No Wolfcoins deducted from your balance."));
            }
        }

        private void PlayerController_LevelUp(User user, PlayerCharacter player)
        {
            var messages = new List<string>();
            if (player.Level == 3 && player.Prestige > 0)
            {
                messages.Add($"You have earned a Prestige level! You are now Prestige {player.Prestige} and your level has been set to 3.");
            }
            else
            {
                messages.Add($"DING! You just reached level {player.Level}!");
                if (player.Prestige > 0)
                {
                    messages.Add($"You are Prestige {player.Prestige}.");
                }
            }
            messages.Add($"XP to next level: {PlayerController.GetExperienceToNextLevel(player.Experience)}.");
            var baseMessage = string.Join(" ", messages);
            messages.Clear();
            messages.Add(baseMessage);
            if (player.Level >= 3 && !player.CharacterClass.CanPlay)
            {
                if (player.Level == 3)
                {
                    messages.Add("You've reached LEVEL 3! You get to choose a class for your character! Choose by whispering me one of the following:");
                }
                else
                {
                    messages.Add("It looks like you are elligible to choose a class but haven't yet done so. Choose by whispering me one of the following:");
                }
                var classes = PlayerController.GetPlayableClasses().Select(x => $"!C{x.Id} ({x.Name})");
                messages.Add(string.Join(", ", classes));
            }
            PushNotification?.Invoke(user, new CommandResult(user, messages.ToArray()));
        }

        private void PlayerController_ExperienceAwarded(int experience, int currency, int multiplier)
        {
            PushNotification?.Invoke(null, new CommandResult(false, $"Thanks for watching! Viewers awarded {experience} XP & {currency} Wolfcoins. Subscribers earn {multiplier}x that amount!"));
        }

        public CommandResult GetExperience(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            if (player.Experience == 0)
            {
                return new CommandResult("You don't have any XP yet! Hang out in chat during the livestream to earn XP & coins.");
            }
            var messages = new List<string>();
            if (player.CharacterClass.CanPlay)
            {
                if (player.Prestige > 0)
                {
                    messages.Add($"You are a level {player.Level} {player.CharacterClass.Name}, and you are Prestige level {player.Prestige}.");
                }
                else
                {
                    messages.Add($"You are a level {player.Level} {player.CharacterClass.Name}.");
                }
            }
            var xpToNext = PlayerController.GetExperienceToNextLevel(player.Experience);
            messages.Add($"(Total XP: {player.Experience} | XP To Next Level: {xpToNext})");
            return new CommandResult(string.Join(" ", messages));
        }

        public CommandResult GetCoins(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            if (player.Currency == 0)
            {
                return new CommandResult("You don't have any coins yet! Stick around during the livestream to earn coins.");
            }
            return new CommandResult($"You have: {player.Currency} coins.");
        }

        public CompactCollection<PlayerCharacter> GetStatsCompact(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            return new CompactCollection<PlayerCharacter>(new List<PlayerCharacter>() { player }, x => $"{x.Level}|{x.CharacterClass.Name}|{x.Experience}|{PlayerController.GetExperienceToNextLevel(x.Experience)}|{x.Currency};");
        }

        public CommandResult GetStats(User user, string target = null)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                var coins = GetCoins(user);
                var xp = GetExperience(user);
                coins.Responses.Add(xp.Responses.First());
                return coins;
            }
            else
            {
                var pryingPlayer = PlayerController.GetPlayerByUser(user);
                var cost = PlayerController.GetPryCost();
                if (PlayerController.CanPry(pryingPlayer))
                {
                    var targetPlayer = PlayerController.Pry(pryingPlayer, target);
                    if (targetPlayer != null)
                    {
                        string message;
                        if (targetPlayer.CharacterClass.CanPlay)
                        {
                            message = $"{target} is a Level {targetPlayer.Level} {targetPlayer.CharacterClass.Name} ({targetPlayer.Experience} XP), "
                                + $"Prestige level {targetPlayer.Prestige}, and has {targetPlayer.Currency} Wolfcoins.";
                        }
                        else
                        {
                            message = $"{target} is Level {targetPlayer.Level} ({targetPlayer.Experience} XP), and has {targetPlayer.Currency} Wolfcoins.";
                        }
                        return new CommandResult(message, $"It cost you {cost} Wolfcoins to discover this information.");
                    }
                    return new CommandResult($"User {target} does not exist in the database. You were not charged any Wolfcoins.");
                }
                return new CommandResult($"It costs {cost} to pry. You have {pryingPlayer.Currency} Wolfcoins.");
            }
        }

        public CommandResult GetClassStats()
        {
            var messages = new List<string>() { "Class distribution for the Wolfpack RPG: " };
            var distribution = PlayerController.GetClassDistribution();
            var total = distribution.Select(x => x.Value).Sum();
            foreach (var pair in distribution)
            {
                messages.Add($"{pair.Key.Name}s: {Math.Round((pair.Value / total) * 100d, 1)}%");
            }
            return new CommandResult(messages.ToArray());
        }

        private CommandResult SetClass(PlayerCharacter player, CharacterClass characterClass)
        {
            if (PlayerController.IsFlaggedForRespec(player))
            {
                var cost = PlayerController.GetRespecCost(player.Level);
                if (PlayerController.Respec(player, characterClass, cost))
                {
                    return new CommandResult($"Class successfully updated to {characterClass.Name}! {cost} deducted from your Wolfcoin balance.");
                }
                return new CommandResult($"It costs {cost} Wolfcoins to respec at your level. You have {player.Currency} coins.");
            }
            PlayerController.SetClass(player, characterClass);
            return new CommandResult($"You successfully selected the {characterClass.Name} class!");
        }

        public CommandResult SelectClass(User user, string choice)
        {
            var player = PlayerController.GetPlayerByUser(user);
            if (PlayerController.CanSelectClass(player))
            {
                var classes = PlayerController.GetPlayableClasses();
                var output = new CommandResult($"Invalid selection. Please whisper me one of the following: ", string.Join(", ", classes.Select(x => $"!C{x.Id} ({x.Name})")));
                if (int.TryParse(choice, out var choiceInt))
                {
                    var classChoice = classes.FirstOrDefault(x => x.Id == choiceInt);
                    if (classChoice != null)
                    {
                        output = SetClass(player, classChoice);
                    }
                }
                else
                {
                    var classChoice = classes.FirstOrDefault(x => x.Name.Equals(choice));
                    if (classChoice != null)
                    {
                        output = SetClass(player, classChoice);
                    }
                }
                return output;
            }
            if (player.CharacterClass.CanPlay)
            {
                return new CommandResult($"Unable to choose a class. If you want to change your class, use the !respec command.");
            }
            return new CommandResult("You are not high enough level to choose a class. Continue watching the stream to gain experience.");
        }

        public CommandResult ClassHelp(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            if (player.Level >= PlayerController.MinLevel)
            {
                var classes = PlayerController.GetPlayableClasses().Select(x => $"!C{x.Id} ({x.Name})");
                return new CommandResult("It looks like you are elligible to choose a class but haven't yet done so. Choose by whispering me one of the following:",
                    string.Join(", ", classes));
            }
            return new CommandResult("You are not high enough level to choose a class. Continue watching the stream to gain experience.");
        }

        public CommandResult Respec(User user)
        {
            var player = PlayerController.GetPlayerByUser(user);
            if (player.CharacterClass.CanPlay)
            {
                var party = PartyController.GetCurrentGroup(player);
                if (party == null)
                {
                    if (GroupFinderController.IsPlayerQueued(player))
                    {
                        var cost = PlayerController.GetRespecCost(player.Level);
                        if (player.Currency >= cost)
                        {
                            PlayerController.FlagForRespec(player);
                            var classes = PlayerController.GetPlayableClasses();
                            return new CommandResult(
                                $"You've chosen to respec your class! It will cost you {cost} coins to respec and you will lose all your items. Reply 'Nevermind' to cancel or one of the following codes to select your new class: ",
                                string.Join(", ", classes.Select(x => $"!C{x.Id} ({x.Name})")));
                        }
                        return new CommandResult($"It costs {cost} Wolfcoins to respec at your level. You have {player.Currency} coins.");
                    }
                    return new CommandResult("You can't respec while in the dungeon queue!");
                }
                return new CommandResult("You can't respec while in a party!");
            }
            return new CommandResult("You are not high enough level to choose a class. Continue watching the stream to gain experience.");
        }
    }
}
