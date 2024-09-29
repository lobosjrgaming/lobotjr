using Autofac;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using static LobotJR.Command.Controller.Fishing.FishingController;

namespace LobotJR.Test.Controllers.Fishing
{
    [TestClass]
    public class FishingControllerTests
    {
        private IConnectionManager ConnectionManager;
        private FishingController FishingController;
        private SettingsManager SettingsManager;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            FishingController = AutofacMockSetup.Container.Resolve<FishingController>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            FishingController.Initialize();
        }

        [TestCleanup]
        public void Cleanup()
        {
            AutofacMockSetup.ResetFishingRecords();
        }

        [TestMethod]
        public void GetsFisherById()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var retrieved = FishingController.GetFisherByUser(user);
            Assert.AreEqual(user.TwitchId, retrieved.User.TwitchId);
        }

        [TestMethod]
        public void CreatesFisherWhenNoneExist()
        {
            var invalidUser = new User() { TwitchId = "InvalidId", Username = "Invalid User" };
            var retrieved = FishingController.GetFisherByUser(invalidUser);
            Assert.IsNotNull(retrieved);
        }

        [TestMethod]
        public void CalculatesFishSizes()
        {
            var db = ConnectionManager.CurrentConnection;
            var fish = db.FishData.Read().First();
            var fisher = new Fisher
            {
                User = new User("", ""),
                HookedId = fish.Id
            };
            var catchData = FishingController.CalculateFishSizes(fisher, false);
            Assert.IsTrue(fish.MinimumWeight <= catchData.Weight);
            Assert.IsTrue(fish.MaximumWeight >= catchData.Weight);
            Assert.IsTrue(fish.MinimumLength <= catchData.Length);
            Assert.IsTrue(fish.MaximumLength >= catchData.Length);
        }

        [TestMethod]
        public void CalculateFishSizesRandomizesWithSteppedWeights()
        {
            var db = ConnectionManager.CurrentConnection;
            var fish = db.FishData.Read().First();
            var fisher = new Fisher
            {
                User = new User("", ""),
                HookedId = fish.Id
            };
            var minWeightGroup = (fish.MaximumWeight - fish.MinimumWeight) / 5 + fish.MinimumWeight;
            var minLengthGroup = (fish.MaximumLength - fish.MinimumLength) / 5 + fish.MinimumLength;
            var maxWeightGroup = (fish.MaximumWeight - fish.MinimumWeight) / 5 * 4 + fish.MinimumWeight;
            var maxLengthGroup = (fish.MaximumLength - fish.MinimumLength) / 5 * 4 + fish.MinimumLength;
            var sampleSize = 10000;
            var samples = new List<Catch>();
            for (var i = 0; i < sampleSize; i++)
            {
                samples.Add(FishingController.CalculateFishSizes(fisher, false));
            }
            var minGroupSize = samples.Count(x => x.Length <= minLengthGroup && x.Weight <= minWeightGroup);
            var maxGroupSize = samples.Count(x => x.Length >= maxLengthGroup && x.Weight >= maxWeightGroup);
            Assert.IsTrue(minGroupSize > sampleSize * 0.39 && minGroupSize < sampleSize * 0.41);
            Assert.IsTrue(maxGroupSize > 0 && maxGroupSize < sampleSize * 0.02);
        }

        [TestMethod]
        public void CalculateFishSizesRandomizesWithNormalDistribution()
        {
            var db = ConnectionManager.CurrentConnection;
            var fish = db.FishData.Read().First();
            var fisher = new Fisher
            {
                User = new User("", ""),
                HookedId = fish.Id
            };

            var weightRange = fish.MaximumWeight - fish.MinimumWeight;
            var lengthRange = fish.MaximumLength - fish.MinimumLength;
            var weightMean = (weightRange) / 2 + fish.MinimumWeight;
            var lengthMean = (lengthRange) / 2 + fish.MinimumLength;
            var weightStdMin = weightMean - weightRange / 6;
            var weightStdMax = weightMean + weightRange / 6;
            var lengthStdMin = lengthMean - lengthRange / 6;
            var lengthStdMax = lengthMean + lengthRange / 6;

            var sampleSize = 10000;
            var samples = new List<Catch>();
            for (var i = 0; i < sampleSize; i++)
            {
                samples.Add(FishingController.CalculateFishSizes(fisher, true));
            }
            var oneStdGroupSizeWeight = samples.Count(x => x.Weight >= weightStdMin && x.Weight <= weightStdMax);
            var oneStdGroupSizeLength = samples.Count(x => x.Length >= lengthStdMin && x.Length <= lengthStdMax);
            Assert.IsTrue(samples.Min(x => x.Weight) >= fish.MinimumWeight);
            Assert.IsTrue(samples.Max(x => x.Weight) <= fish.MaximumWeight);
            Assert.IsTrue(samples.Min(x => x.Length) >= fish.MinimumLength);
            Assert.IsTrue(samples.Max(x => x.Length) <= fish.MaximumLength);
            Assert.IsTrue(oneStdGroupSizeWeight > sampleSize * 0.66 && oneStdGroupSizeWeight < sampleSize * 0.70);
            Assert.IsTrue(oneStdGroupSizeLength > sampleSize * 0.66 && oneStdGroupSizeLength < sampleSize * 0.70);
        }

        [TestMethod]
        public void CastsLine()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            fisher.IsFishing = false;
            fisher.HookedId = -1;
            fisher.HookedTime = null;
            FishingController.Cast(fisher.User);
            var now = DateTime.Now;
            var appSettings = SettingsManager.GetGameSettings();
            var min = now.AddSeconds(appSettings.FishingCastMinimum);
            var max = now.AddSeconds(appSettings.FishingCastMaximum);
            Assert.IsTrue(fisher.IsFishing);
            Assert.IsTrue(fisher.HookedTime >= min);
            Assert.IsTrue(fisher.HookedTime <= max);
        }

        [TestMethod]
        public void CastCreatesNewFisherIfNoneExistsWithMatchingUserId()
        {
            var newUser = new User() { TwitchId = "NewId", Username = "NewUser" };
            FishingController.Cast(newUser);
            var newFisher = FishingController.GetFisherByUser(newUser);
            Assert.IsNotNull(newFisher);
            Assert.IsTrue(newFisher.IsFishing);
        }

        [TestMethod]
        public void HooksFish()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            var result = FishingController.HookFish(fisher, false);
            Assert.IsTrue(result);
            Assert.IsNotNull(fisher.HookedId);
        }

        [TestMethod]
        public void HooksFishWithNormalRarityDistribution()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            var rarities = db.FishData.Read().Select(x => x.Rarity).Distinct().ToArray();
            var sampleSize = 10000;
            var samples = new List<int>();
            for (var i = 0; i < sampleSize; i++)
            {
                FishingController.HookFish(fisher, true);
                samples.Add(fisher.HookedId);
            }
            var fish = db.FishData.Read().ToDictionary(x => x.Id, x => x);
            var groups = samples.GroupBy(x => fish[x].Rarity, x => x, (rarity, all) => new { Rarity = rarity, Count = all.Count() }).ToDictionary(x => x.Rarity, x => x.Count);
            var commonCount = groups[rarities[0]];
            var uncommonCount = groups[rarities[1]];
            var rareCount = groups[rarities[2]];
            Assert.IsTrue(commonCount >= sampleSize * 0.682 * 0.85 && commonCount <= sampleSize * 0.682 * 1.15);
            Assert.IsTrue(uncommonCount >= sampleSize * 0.272 * 0.85 && uncommonCount <= sampleSize * 0.272 * 1.15);
            Assert.IsTrue(rareCount > 0 && rareCount <= sampleSize * 0.046 * 1.15);
        }

        [TestMethod]
        public void HooksFishWithWeightedRarityDistribution()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            var rarities = db.FishData.Read().Select(x => x.Rarity).Distinct().ToArray();
            var sampleSize = 10000;
            var samples = new List<int>();
            for (var i = 0; i < sampleSize; i++)
            {
                FishingController.HookFish(fisher, false);
                samples.Add(fisher.HookedId);
            }
            var weightTotal = (float)rarities.Sum(x => x.Weight);
            var fish = db.FishData.Read().ToDictionary(x => x.Id, x => x);
            var groups = samples.GroupBy(x => fish[x].Rarity, x => x, (rarity, all) => new { Rarity = rarity, Count = all.Count() }).ToDictionary(x => x.Rarity, x => x.Count);
            var commonCount = groups[rarities[0]];
            var uncommonCount = groups[rarities[1]];
            var rareCount = groups[rarities[2]];
            var commonWeight = (float)rarities[0].Weight / weightTotal;
            var uncommonWeight = (float)rarities[1].Weight / weightTotal;
            var rareWeight = (float)rarities[2].Weight / weightTotal;
            Assert.IsTrue(commonCount >= sampleSize * (commonWeight / 1.1) && commonCount <= sampleSize * (commonWeight * 1.1));
            Assert.IsTrue(uncommonCount >= sampleSize * (uncommonWeight / 1.1) && uncommonCount <= sampleSize * (uncommonWeight * 1.1));
            Assert.IsTrue(rareCount >= sampleSize * (rareWeight / 1.1) && rareCount <= sampleSize * (rareWeight * 1.1));
        }

        [TestMethod]
        public void UnhooksFish()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            fisher.IsFishing = true;
            fisher.HookedId = 1;
            fisher.HookedTime = DateTime.Now;
            FishingController.UnhookFish(fisher);
            Assert.IsFalse(fisher.IsFishing);
            Assert.AreEqual(-1, fisher.HookedId);
            Assert.IsNull(fisher.HookedTime);
        }

        [TestMethod]
        public void CatchesFish()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            fisher.HookedId = db.FishData.Read().First().Id;
            var catchData = FishingController.CatchFish(fisher);
            Assert.IsNotNull(catchData);
            Assert.AreEqual(db.FishData.Read().First().Id, catchData.Fish.Id);
            Assert.AreEqual(fisher.User.TwitchId, catchData.UserId);
        }

        [TestMethod]
        public void CatchFishDoesNothingWhenFisherIsNull()
        {
            var catchData = FishingController.CatchFish(null);
            Assert.IsNull(catchData);
        }

        [TestMethod]
        public void CatchFishDoesNothingIfNoFishHooked()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            fisher.HookedId = -1;
            var catchData = FishingController.CatchFish(fisher);
            Assert.IsNull(catchData);
        }

        [TestMethod]
        public void ProcessHooksFish()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            fisher.IsFishing = true;
            fisher.HookedId = -1;
            fisher.HookedTime = DateTime.Now;
            var callbackMock = new Mock<FisherEventHandler>();
            FishingController.FishHooked += callbackMock.Object;
            FishingController.Process();
            Assert.IsNotNull(fisher.HookedId);
            callbackMock.Verify(x => x(fisher), Times.Once);
        }

        [TestMethod]
        public void ProcessReleasesFish()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var fisher = FishingController.GetFisherByUser(user);
            var settings = SettingsManager.GetGameSettings();
            fisher.IsFishing = true;
            fisher.HookedTime = DateTime.Now.AddSeconds(-settings.FishingHookLength);
            fisher.HookedId = db.FishData.Read().First().Id;
            var callbackMock = new Mock<FisherEventHandler>();
            FishingController.FishGotAway += callbackMock.Object;
            FishingController.Process();
            Assert.IsFalse(fisher.IsFishing);
            Assert.AreEqual(-1, fisher.HookedId);
            Assert.IsNull(fisher.HookedTime);
            callbackMock.Verify(x => x(fisher), Times.Once);
        }
    }
}

