using Autofac;
using LobotJR.Command.View.Player;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.Player
{
    [TestClass]
    public class PlayerViewTests
    {
        private PlayerView View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<PlayerView>();
        }

        [TestMethod]
        public void GetCoinsRetrievesPlayerCoinCount() { }

        [TestMethod]
        public void GetCoinsReturnsHelpMessageAtZeroCoins() { }

        [TestMethod]
        public void GetExperienceRetrievesLevel() { }

        [TestMethod]
        public void GetExperienceRetrievesLevelAndPrestige() { }

        [TestMethod]
        public void GetExperienceRetrievesExperienceForLowLevelUsers() { }

        [TestMethod]
        public void GetExperienceReturnsHelpMessageAtZeroExperience() { }

        [TestMethod]
        public void GetStatsCompactRetrievesExperienceAndCoins() { }

        [TestMethod]
        public void GetStatsRetrievesExperienceAndCoins() { }

        [TestMethod]
        public void PryRetrievesPlayerInfo() { }

        [TestMethod]
        public void PryReturnsErrorIfPlayerDoesNotExist() { }

        [TestMethod]
        public void PryReturnsErrorIfPlayerCannotAfford() { }

        [TestMethod]
        public void GetClassStatsRetrievesClassDistribution() { }

        [TestMethod]
        public void SelectClassSetsPlayerInitialClass() { }

        [TestMethod]
        public void SelectClassCompletesRespec() { }

        [TestMethod]
        public void SelectClassReturnsErrorOnInvalidChoice() { }

        [TestMethod]
        public void SelectClassReturnsErrorIfNotRespeccing() { }

        [TestMethod]
        public void SelectClassReturnsErrorIfNotHighEnoughLevel() { }

        [TestMethod]
        public void ClassHelpGetsHelpMessage() { }

        [TestMethod]
        public void ClassHelpGetsContinueWatchingMessageForLowLevelPlayers() { }

        [TestMethod]
        public void RespecFlagsForRespec() { }

        [TestMethod]
        public void RespecReturnsErrorIfCannotAfford() { }

        [TestMethod]
        public void RespecReturnsErrorIfInDungeonQueue() { }

        [TestMethod]
        public void RespecReturnsErrorIfInParty() { }

        [TestMethod]
        public void RespecReturnsErrorForLowLevelPlayers() { }

        [TestMethod]
        public void CancelEventCancelsRespec() { }

        [TestMethod]
        public void LevelUpEventSendsLevelUpMessage() { }

        [TestMethod]
        public void LevelUpEventSendsPrestigeMessage() { }

        [TestMethod]
        public void LevelUpEventSendsClassChoiceMessage() { }

        [TestMethod]
        public void LevelUpEventSendsClassChoiceReminder() { }
    }
}
