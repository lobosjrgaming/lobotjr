using Autofac;
using LobotJR.Command.Controller.Player;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Test.Controllers.Player
{
    [TestClass]
    public class PlayerControllerTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;
        private PlayerController PlayerController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            PlayerController = AutofacMockSetup.Container.Resolve<PlayerController>();
            AutofacMockSetup.ResetPlayers();
            PlayerController.ClearRespecs();
        }

        [TestMethod]
        public void GainExperienceIncreasesLevel()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var player = PlayerController.GetPlayerByUser(user);
            player.Experience = 0;
            player.Level = 0;
            PlayerController.GainExperience(user, player, 300);
            Assert.AreEqual(3, player.Level);
            Assert.AreEqual(300, player.Experience);
        }

        [TestMethod]
        public void GainExperienceIncreasesPrestige()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var player = PlayerController.GetPlayerByUser(user);
            player.Experience = 0;
            player.Level = 0;
            PlayerController.GainExperience(user, player, int.MaxValue / 2);
            Assert.AreEqual(3, player.Level);
            Assert.AreEqual(200, player.Experience);
            Assert.AreEqual(1, player.Prestige);
        }

        [TestMethod]
        public void GainExperienceDoesNotDecreaseLevel()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var player = PlayerController.GetPlayerByUser(user);
            player.Experience = 200;
            player.Level = 3;
            PlayerController.GainExperience(user, player, -100);
            var toNext = PlayerController.GetExperienceToNextLevel(player.Experience - 1);
            Assert.AreEqual(3, player.Level);
            Assert.AreEqual(toNext, 1);
        }

        [TestMethod]
        public void GetPlayerByUserGetsExistingPlayer()
        {
            var db = ConnectionManager.CurrentConnection;
            var existingPlayer = db.PlayerCharacters.Read().First();
            var user = db.Users.Read(x => x.TwitchId.Equals(existingPlayer.UserId)).First();
            var player = PlayerController.GetPlayerByUser(user);
            Assert.AreEqual(existingPlayer, player);
            Assert.IsTrue(existingPlayer == player);
        }

        [TestMethod]
        public void GetPlayerByUserCreatesNewPlayer()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var existingPlayer = db.PlayerCharacters.Read(x => x.UserId.Equals(user.TwitchId)).First();
            db.PlayerCharacters.Delete(existingPlayer);
            db.Commit();
            var player = PlayerController.GetPlayerByUser(user);
            Assert.IsNotNull(player);
            Assert.AreEqual(user.TwitchId, player.UserId);
            Assert.AreEqual(1, player.Level);
            Assert.AreEqual(0, player.Experience);
            Assert.AreEqual(0, player.Currency);
        }

        [TestMethod]
        public void GetUserByPlayerGetsUserObject()
        {
            var db = ConnectionManager.CurrentConnection;
            var existingPlayer = db.PlayerCharacters.Read().First();
            var user = db.Users.Read(x => x.TwitchId.Equals(existingPlayer.UserId)).First();
            var playerUser = PlayerController.GetUserByPlayer(existingPlayer);
            Assert.AreEqual(user, playerUser);
            Assert.IsTrue(user == playerUser);
        }

        [TestMethod]
        public void GetExperienceToNextLevelGetsCorrectValue()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var player = PlayerController.GetPlayerByUser(user);
            var oldLevel = player.Level;
            var toNextLevel = PlayerController.GetExperienceToNextLevel(player.Experience);
            PlayerController.GainExperience(player, toNextLevel);
            Assert.AreEqual(oldLevel + 1, player.Level);
            oldLevel = player.Level;
            toNextLevel = PlayerController.GetExperienceToNextLevel(player.Experience);
            PlayerController.GainExperience(player, toNextLevel - 1);
            Assert.AreEqual(oldLevel, player.Level);
        }

        [TestMethod]
        public void GetPlayableClassesGetsClassList()
        {
            var db = ConnectionManager.CurrentConnection;
            var classes = db.CharacterClassData.Read(x => x.CanPlay).ToList();
            var nonPlayableClasses = db.CharacterClassData.Read(x => !x.CanPlay).ToList();
            var retrievedClasses = PlayerController.GetPlayableClasses();
            Assert.AreEqual(classes.Count, retrievedClasses.Count());
            foreach (var c in classes)
            {
                Assert.IsTrue(retrievedClasses.Any(x => x.Id == c.Id));
            }
            foreach (var c in nonPlayableClasses)
            {
                Assert.IsFalse(retrievedClasses.Any(x => x.Id == c.Id));
            }
        }

        [TestMethod]
        public void GetClassDistributionGetsCorrectValues()
        {
            var db = ConnectionManager.CurrentConnection;
            var classes = db.CharacterClassData.Read(x => x.CanPlay);
            var distribution = new Dictionary<int, int>();
            foreach (var c in classes)
            {
                distribution.Add(c.Id, db.PlayerCharacters.Read(x => x.CharacterClassId == c.Id).Count());
            }
            var retrievedDistribution = PlayerController.GetClassDistribution();
            Assert.AreEqual(distribution.Count, retrievedDistribution.Count());
            foreach (var count in distribution)
            {
                var retrieved = retrievedDistribution.Where(x => x.Key.Id == count.Key).First().Value;
                Assert.AreEqual(count.Value, retrieved);
            }
        }

        [TestMethod]
        public void GetRespecCostGetsCorrectValue()
        {
            var expectedLevel3 = SettingsManager.GetGameSettings().RespecCost;
            var expectedLevel20 = SettingsManager.GetGameSettings().RespecCost * 16;
            var actualLevel1 = PlayerController.GetRespecCost(1);
            var actualLevel3 = PlayerController.GetRespecCost(3);
            var actualLevel20 = PlayerController.GetRespecCost(20);
            Assert.AreEqual(expectedLevel3, actualLevel1);
            Assert.AreEqual(expectedLevel3, actualLevel3);
            Assert.AreEqual(expectedLevel20, actualLevel20);
        }

        [TestMethod]
        public void GetPryCostGetsCorrectValue()
        {
            var expected = SettingsManager.GetGameSettings().PryCost;
            var actual = PlayerController.GetPryCost();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ClearClassSetsClassToNonPlayable()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => x.CharacterClass.CanPlay).First();
            PlayerController.ClearClass(player);
            Assert.IsFalse(player.CharacterClass.CanPlay);
        }

        [TestMethod]
        public void SetClassUpdatesCharacterClass()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => !x.CharacterClass.CanPlay).First();
            var targetClass = db.CharacterClassData.Read(x => x.CanPlay).First();
            PlayerController.SetClass(player, targetClass);
            Assert.AreEqual(player.CharacterClass, targetClass);
            Assert.IsTrue(player.CharacterClass.CanPlay);
        }

        [TestMethod]
        public void RespecChangesClassAndRemovesCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => x.CharacterClass.CanPlay).First();
            var currentClass = player.CharacterClass;
            var targetClass = db.CharacterClassData.Read(x => x.CanPlay).Last();
            player.Currency = 10;
            PlayerController.Respec(player, targetClass, 10);
            Assert.AreEqual(targetClass, player.CharacterClass);
            Assert.AreNotEqual(currentClass, player.CharacterClass);
            Assert.AreEqual(0, player.Currency);
        }

        [TestMethod]
        public void IsFlaggedForRespecReturnsTrueForFlaggedUsers()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => x.CharacterClass.CanPlay).First();
            var flagged = PlayerController.IsFlaggedForRespec(player);
            Assert.IsFalse(flagged);
            var result = PlayerController.FlagForRespec(player);
            flagged = PlayerController.IsFlaggedForRespec(player);
            Assert.IsTrue(result);
            Assert.IsTrue(flagged);
        }

        [TestMethod]
        public void IsFlaggedForRespecReturnsFalseForNonFlaggedUsers()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => x.CharacterClass.CanPlay).First();
            var flagged = PlayerController.IsFlaggedForRespec(player);
            Assert.IsFalse(flagged);
        }

        [TestMethod]
        public void FlagForRespecReturnsFalseForAlreadyFlaggedCharacters()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => x.CharacterClass.CanPlay).First();
            var result = PlayerController.FlagForRespec(player);
            var flagged = PlayerController.IsFlaggedForRespec(player);
            Assert.IsTrue(result);
            Assert.IsTrue(flagged);
            result = PlayerController.FlagForRespec(player);
            flagged = PlayerController.IsFlaggedForRespec(player);
            Assert.IsFalse(result);
            Assert.IsTrue(flagged);
        }

        [TestMethod]
        public void UnflagForRespecRemovesRespecFlag()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => x.CharacterClass.CanPlay).First();
            var result = PlayerController.FlagForRespec(player);
            var flagged = PlayerController.IsFlaggedForRespec(player);
            Assert.IsTrue(result);
            Assert.IsTrue(flagged);
            PlayerController.UnflagForRespec(player);
            flagged = PlayerController.IsFlaggedForRespec(player);
            Assert.IsFalse(flagged);
        }

        [TestMethod]
        public void CanSelectClassReturnsTrueForNewPlayers()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => !x.CharacterClass.CanPlay).First();
            var result = PlayerController.CanSelectClass(player);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanSelectClassReturnsTrueForRespeccingCharacters()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => x.CharacterClass.CanPlay).First();
            PlayerController.GainExperience(player, 500);
            PlayerController.FlagForRespec(player);
            var result = PlayerController.CanSelectClass(player);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanSelectClassReturnsFalseForLowLevelCharacters()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => !x.CharacterClass.CanPlay).First();
            PlayerController.GainExperience(player, 100);
            var result = PlayerController.CanSelectClass(player);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanSelectClassReturnsFalseCharactersWithClassesNotFlaggedForRespec()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read(x => x.CharacterClass.CanPlay).First();
            PlayerController.GainExperience(player, 500);
            var result = PlayerController.CanSelectClass(player);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanPryReturnsTrueIfPlayerHasFunds()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            player.Currency = SettingsManager.GetGameSettings().PryCost;
            var result = PlayerController.CanPry(player);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanPryReturnsFalseIfPlayerHasNoFunds()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            player.Currency = SettingsManager.GetGameSettings().PryCost - 1;
            var result = PlayerController.CanPry(player);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void PryReturnsTargetInfoAndRemovesCoins()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            var target = db.PlayerCharacters.Read().Last();
            var targetUser = db.Users.Read(x => x.TwitchId.EndsWith(target.UserId)).First();
            player.Currency = SettingsManager.GetGameSettings().PryCost;
            var result = PlayerController.Pry(player, targetUser.Username);
            Assert.AreEqual(0, player.Currency);
            Assert.AreEqual(target, result);
        }

        [TestMethod]
        public void PryReturnsNullOnMissingUsername()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            player.Currency = SettingsManager.GetGameSettings().PryCost;
            var result = PlayerController.Pry(player, "InvalidUserName");
            Assert.AreEqual(SettingsManager.GetGameSettings().PryCost, player.Currency);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void PryReturnsNullOnNullUsername()
        {
            var db = ConnectionManager.CurrentConnection;
            var player = db.PlayerCharacters.Read().First();
            player.Currency = SettingsManager.GetGameSettings().PryCost;
            var result = PlayerController.Pry(player, null);
            Assert.AreEqual(SettingsManager.GetGameSettings().PryCost, player.Currency);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void EnableAwardsSetsAwardsFlagAndSettingUser()
        {
            var db = ConnectionManager.CurrentConnection;
            Assert.IsFalse(PlayerController.AwardsEnabled);
            var user = db.Users.Read().First();
            PlayerController.EnableAwards(user);
            Assert.IsTrue(PlayerController.AwardsEnabled);
            Assert.AreEqual(user, PlayerController.AwardSetter);
        }

        [TestMethod]
        public void DisableAwardsRemovesAwardsFlag()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            PlayerController.EnableAwards(user);
            Assert.IsTrue(PlayerController.AwardsEnabled);
            Assert.AreEqual(user, PlayerController.AwardSetter);
            PlayerController.DisableAwards();
            Assert.IsFalse(PlayerController.AwardsEnabled);
            Assert.IsNull(PlayerController.AwardSetter);
        }

        [TestMethod]
        public async Task ProcessAwardsExperienceAndCurrency()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var listener = new Mock<PlayerController.ExperienceAwardHandler>();
            PlayerController.ExperienceAwarded += listener.Object;
            PlayerController.SetMultiplier(1);
            var user = db.Users.Read(x => !x.IsSub).First();
            var sub = db.Users.Read(x => x.IsSub).First();
            var player = PlayerController.GetPlayerByUser(user);
            var subPlayer = PlayerController.GetPlayerByUser(sub);
            var playerXp = player.Experience;
            var playerCoins = player.Currency;
            var subXp = subPlayer.Experience;
            var subCoins = subPlayer.Currency;
            PlayerController.AwardsEnabled = true;
            PlayerController.LastAward = DateTime.Now - TimeSpan.FromMinutes(settings.ExperienceFrequency);
            await PlayerController.Process();
            Assert.AreEqual(playerXp + settings.ExperienceValue, player.Experience);
            Assert.AreEqual(playerCoins + settings.CoinValue, player.Currency);
            Assert.AreEqual(subXp + settings.ExperienceValue * settings.SubRewardMultiplier, subPlayer.Experience);
            Assert.AreEqual(subCoins + settings.CoinValue * settings.SubRewardMultiplier, subPlayer.Currency);
            listener.Verify(x => x(settings.ExperienceValue, settings.CoinValue, settings.SubRewardMultiplier), Times.Once());
        }

        [TestMethod]
        public async Task ProcessWaitsForInterval()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var listener = new Mock<PlayerController.ExperienceAwardHandler>();
            PlayerController.ExperienceAwarded += listener.Object;
            PlayerController.SetMultiplier(1);
            var user = db.Users.Read(x => !x.IsSub).First();
            var sub = db.Users.Read(x => x.IsSub).First();
            var player = PlayerController.GetPlayerByUser(user);
            var subPlayer = PlayerController.GetPlayerByUser(sub);
            var playerXp = player.Experience;
            var playerCoins = player.Currency;
            var subXp = subPlayer.Experience;
            var subCoins = subPlayer.Currency;
            PlayerController.AwardsEnabled = true;
            PlayerController.LastAward = DateTime.Now - TimeSpan.FromMinutes(settings.ExperienceFrequency) + TimeSpan.FromSeconds(1);
            await PlayerController.Process();
            Assert.AreEqual(playerXp, player.Experience);
            Assert.AreEqual(playerCoins, player.Currency);
            Assert.AreEqual(subXp, subPlayer.Experience);
            Assert.AreEqual(subCoins, subPlayer.Currency);
            listener.Verify(x => x(settings.ExperienceValue, settings.CoinValue, settings.SubRewardMultiplier), Times.Never());
        }

        [TestMethod]
        public async Task ProcessUpdatesAwardTime()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var listener = new Mock<PlayerController.ExperienceAwardHandler>();
            PlayerController.ExperienceAwarded += listener.Object;
            PlayerController.SetMultiplier(1);
            var user = db.Users.Read(x => !x.IsSub).First();
            var sub = db.Users.Read(x => x.IsSub).First();
            var player = PlayerController.GetPlayerByUser(user);
            var subPlayer = PlayerController.GetPlayerByUser(sub);
            var playerXp = player.Experience;
            var playerCoins = player.Currency;
            var subXp = subPlayer.Experience;
            var subCoins = subPlayer.Currency;
            PlayerController.AwardsEnabled = true;
            PlayerController.LastAward = DateTime.Now - TimeSpan.FromMinutes(settings.ExperienceFrequency);
            await PlayerController.Process();
            Assert.AreEqual(playerXp + settings.ExperienceValue, player.Experience);
            Assert.AreEqual(playerCoins + settings.CoinValue, player.Currency);
            Assert.AreEqual(subXp + settings.ExperienceValue * settings.SubRewardMultiplier, subPlayer.Experience);
            Assert.AreEqual(subCoins + settings.CoinValue * settings.SubRewardMultiplier, subPlayer.Currency);
            Assert.IsTrue(DateTime.Now - PlayerController.LastAward < TimeSpan.FromMilliseconds(10));
            listener.Verify(x => x(settings.ExperienceValue, settings.CoinValue, settings.SubRewardMultiplier), Times.Once());
        }

        [TestMethod]
        public async Task ProcessAwardsAppliesMultiplier()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var listener = new Mock<PlayerController.ExperienceAwardHandler>();
            PlayerController.ExperienceAwarded += listener.Object;
            PlayerController.SetMultiplier(2);
            var user = db.Users.Read(x => !x.IsSub).First();
            var sub = db.Users.Read(x => x.IsSub).First();
            var player = PlayerController.GetPlayerByUser(user);
            var subPlayer = PlayerController.GetPlayerByUser(sub);
            var playerXp = player.Experience;
            var playerCoins = player.Currency;
            var subXp = subPlayer.Experience;
            var subCoins = subPlayer.Currency;
            PlayerController.AwardsEnabled = true;
            PlayerController.LastAward = DateTime.Now - TimeSpan.FromMinutes(settings.ExperienceFrequency);
            await PlayerController.Process();
            Assert.AreEqual(playerXp + settings.ExperienceValue * 2, player.Experience);
            Assert.AreEqual(playerCoins + settings.CoinValue * 2, player.Currency);
            Assert.AreEqual(subXp + settings.ExperienceValue * settings.SubRewardMultiplier * 2, subPlayer.Experience);
            Assert.AreEqual(subCoins + settings.CoinValue * settings.SubRewardMultiplier * 2, subPlayer.Currency);
            listener.Verify(x => x(settings.ExperienceValue * 2, settings.CoinValue * 2, settings.SubRewardMultiplier), Times.Once());
        }

        [TestMethod]
        public async Task ProcessDoesNotAwardExperienceIfAwardsAreDisabled()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var listener = new Mock<PlayerController.ExperienceAwardHandler>();
            PlayerController.ExperienceAwarded += listener.Object;
            var user = db.Users.Read(x => !x.IsMod).First();
            var player = PlayerController.GetPlayerByUser(user);
            var playerXp = player.Experience;
            var playerCoins = player.Currency;
            PlayerController.AwardsEnabled = false;
            PlayerController.LastAward = DateTime.Now - TimeSpan.FromMinutes(settings.ExperienceFrequency);
            await PlayerController.Process();
            Assert.AreEqual(playerXp, player.Experience);
            Assert.AreEqual(playerCoins, player.Currency);
            listener.Verify(x => x(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never());
        }

        [TestMethod]
        public async Task ProcessDoesNotAwardExperienceIfFrequencyNotElapsed()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetGameSettings();
            var listener = new Mock<PlayerController.ExperienceAwardHandler>();
            PlayerController.ExperienceAwarded += listener.Object;
            var user = db.Users.Read(x => !x.IsMod).First();
            var player = PlayerController.GetPlayerByUser(user);
            var playerXp = player.Experience;
            var playerCoins = player.Currency;
            PlayerController.AwardsEnabled = true;
            PlayerController.LastAward = DateTime.Now;
            await PlayerController.Process();
            Assert.AreEqual(playerXp, player.Experience);
            Assert.AreEqual(playerCoins, player.Currency);
            listener.Verify(x => x(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never());
        }
    }
}
