using LobotJR.Command.System.Gloat;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Wolfcoins;

namespace LobotJR.Test.Systems.Gloat
{
    [TestClass]
    public class GloatSystemTests
    {
        private SqliteRepositoryManager Manager;
        private GloatSystem GloatSystem;
        private Dictionary<string, int> Wolfcoins;

        [TestInitialize]
        public void Initialize()
        {
            Manager = new SqliteRepositoryManager(MockContext.Create());
            var currency = new Currency();
            Wolfcoins = currency.coinList;

            GloatSystem = new GloatSystem(Manager, currency);
        }

        [TestMethod]
        public void CanGloatReturnsTrueWithEnoughCoins()
        {
            var user = Manager.Users.Read().First();
            Wolfcoins.Add(user.Username, Manager.AppSettings.Read().First().FishingGloatCost);
            var canGloat = GloatSystem.CanGloatFishing(user);
            Assert.IsTrue(canGloat);
        }

        [TestMethod]
        public void CanGloatReturnsFalseWithoutEnoughCoins()
        {
            var user = Manager.Users.Read().First();
            Wolfcoins.Add(user.Username, Manager.AppSettings.Read().First().FishingGloatCost - 1);
            var canGloat = GloatSystem.CanGloatFishing(user);
            Assert.IsFalse(canGloat);
        }

        [TestMethod]
        public void CanGloatReturnsFalseWithNoCoinEntry()
        {
            var user = Manager.Users.Read().First();
            var canGloat = GloatSystem.CanGloatFishing(user);
            Assert.IsFalse(canGloat);
        }

        [TestMethod]
        public void GloatReturnsCorrectRecordAndRemovesCoins()
        {
            var user = Manager.Users.Read().First();
            var userId = user.TwitchId;
            var expectedFish = Manager.Catches.Read(x => x.UserId.Equals(userId)).OrderBy(x => x.FishId).First();
            Wolfcoins.Add(user.Username, Manager.AppSettings.Read().First().FishingGloatCost);
            var gloat = GloatSystem.FishingGloat(user, 0);
            Assert.AreEqual(0, Wolfcoins[user.Username]);
            Assert.AreEqual(expectedFish.FishId, gloat.FishId);
        }

        [TestMethod]
        public void GloatReturnsNullWithNoRecords()
        {
            var user = Manager.Users.Read().First();
            var userId = user.TwitchId;
            var records = Manager.Catches.Read(x => x.UserId.Equals(userId)).ToList();
            foreach (var record in records)
            {
                Manager.Catches.Delete(record);
            }
            Manager.Catches.Commit();
            var cost = Manager.AppSettings.Read().First().FishingGloatCost;
            Wolfcoins.Add(user.Username, cost);
            var gloat = GloatSystem.FishingGloat(user, 0);
            Assert.AreEqual(cost, Wolfcoins[user.Username]);
            Assert.IsNull(gloat);
        }

        [TestMethod]
        public void GloatReturnsNullWithNegativeIndex()
        {
            var user = Manager.Users.Read().First();
            var userId = user.TwitchId;
            var cost = Manager.AppSettings.Read().First().FishingGloatCost;
            Wolfcoins.Add(user.Username, cost);
            var gloat = GloatSystem.FishingGloat(user, -1);
            Assert.AreEqual(cost, Wolfcoins[user.Username]);
            Assert.IsNull(gloat);
        }

        [TestMethod]
        public void GloatReturnsNullWithTooHighIndex()
        {
            var user = Manager.Users.Read().First();
            var userId = user.TwitchId;
            var cost = Manager.AppSettings.Read().First().FishingGloatCost;
            var recordCount = Manager.Catches.Read(x => x.UserId.Equals(userId)).Count();
            Wolfcoins.Add(user.Username, cost);
            var gloat = GloatSystem.FishingGloat(user, recordCount);
            Assert.AreEqual(cost, Wolfcoins[user.Username]);
            Assert.IsNull(gloat);
        }
    }
}
