using Autofac;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Test.Systems.Twitch
{
    [TestClass]
    public class UserSystemTests
    {
        private IConnectionManager ConnectionManager;
        private UserController System;
        private MockTwitchClient Client;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            System = AutofacMockSetup.Container.Resolve<UserController>();
            Client = AutofacMockSetup.Container.Resolve<MockTwitchClient>();
        }

        [TestMethod]
        public async Task ProcessesUpdatesAfterSetTime()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                await System.Process();
                Client.Mock.Verify(x => x.GetModeratorListAsync(), Times.Once());
                Client.Mock.Verify(x => x.GetVipListAsync(), Times.Once());
                Client.Mock.Verify(x => x.GetSubscriberListAsync(), Times.Once());
                Client.Mock.Verify(x => x.GetChatterListAsync(), Times.Once());
            }
        }

        [TestMethod]
        public async Task ProcessesUpdatesSyncsModList()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var mod = db.Users.Read(x => x.IsMod).First();
                var notMod = db.Users.Read(x => !x.IsMod).First();
                mod.IsMod = false;
                db.Users.Update(mod);
                notMod.IsMod = true;
                db.Users.Update(notMod);
                db.Users.Commit();
                Assert.IsFalse(mod.IsMod);
                Assert.IsTrue(notMod.IsMod);
                await System.Process();
                Assert.IsTrue(mod.IsMod);
                Assert.IsFalse(notMod.IsMod);
            }
        }

        [TestMethod]
        public async Task ProcessesUpdatesPreservesExistingMods()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var mod = db.Users.Read(x => x.IsMod).First();
                Assert.IsTrue(mod.IsMod);
                await System.Process();
                Assert.IsTrue(mod.IsMod);
            }
        }

        [TestMethod]
        public async Task ProcessesUpdatesSyncsVipList()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var vip = db.Users.Read(x => x.IsVip).First();
                var notVip = db.Users.Read(x => !x.IsVip).First();
                vip.IsVip = false;
                db.Users.Update(vip);
                notVip.IsVip = true;
                db.Users.Update(notVip);
                db.Users.Commit();
                Assert.IsFalse(vip.IsVip);
                Assert.IsTrue(notVip.IsVip);
                await System.Process();
                Assert.IsTrue(vip.IsVip);
                Assert.IsFalse(notVip.IsVip);
            }
        }

        [TestMethod]
        public async Task ProcessesUpdatesSyncsSubList()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var sub = db.Users.Read(x => x.IsSub).First();
                var notSub = db.Users.Read(x => !x.IsSub).First();
                sub.IsSub = false;
                db.Users.Update(sub);
                notSub.IsSub = true;
                db.Users.Update(notSub);
                db.Users.Commit();
                Assert.IsFalse(sub.IsSub);
                Assert.IsTrue(notSub.IsSub);
                await System.Process();
                Assert.IsTrue(sub.IsSub);
                Assert.IsFalse(notSub.IsSub);
            }
        }

        [TestMethod]
        public async Task ProcessGetsChatterList()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var allUsers = db.Users.Read().ToList();
                await System.Process();
                Assert.AreEqual(allUsers.Count, System.Viewers.Count());
                var missing = allUsers.Select(x => x.Username).Except(System.Viewers.Select(x => x.Username)).ToList();
                Assert.AreEqual(0, missing.Count);
            }
        }

        [TestMethod]
        public async Task ProcessDoesNotUpdateUntilSetTime()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var settings = db.AppSettings.Read().First();
                settings.UserDatabaseUpdateTime = 10;
                db.AppSettings.Update(settings);
                db.AppSettings.Commit();
                await System.Process();
                Client.Mock.Verify(x => x.GetModeratorListAsync(), Times.Never());
                Client.Mock.Verify(x => x.GetVipListAsync(), Times.Never());
                Client.Mock.Verify(x => x.GetSubscriberListAsync(), Times.Never());
                Client.Mock.Verify(x => x.GetChatterListAsync(), Times.Never());
            }
        }

        [TestMethod]
        public void SetBotUsersUpdatesAdminStatus()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var bot = db.Users.Read(x => x.IsAdmin).First();
                bot.IsAdmin = false;
                db.Users.Update(bot);
                db.Users.Commit();
                System.SetBotUsers(bot, bot);
                Assert.IsTrue(bot.IsAdmin);
            }
        }

        [TestMethod]
        public void GetOrCreateUserGetsExistingUser()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = db.Users.Read().First();
                var newUser = System.GetOrCreateUser(user.TwitchId, user.Username);
                Assert.AreEqual(user, newUser);
            }
        }

        [TestMethod]
        public void GetOrCreateUserCreatesNewUser()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = System.GetOrCreateUser("500", "NewUser");
                Assert.IsNotNull(user);
                Assert.AreEqual("500", user.TwitchId);
                Assert.AreEqual("NewUser", user.Username);
            }
        }

        [TestMethod]
        public void GetOrCreateUserUpdatesUserWithNewName()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var existingUser = db.Users.Read().First();
                var newName = $"New{existingUser.Username}";
                var user = System.GetOrCreateUser(existingUser.TwitchId, newName);
                Assert.IsNotNull(user);
                Assert.AreEqual(existingUser.TwitchId, user.TwitchId);
                Assert.AreEqual(newName, user.Username);
            }
        }

        [TestMethod]
        public void GetUserByIdGetsUsers()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = System.GetUserById("10");
                Assert.IsNotNull(user);
                Assert.AreEqual("10", user.TwitchId);
            }
        }

        [TestMethod]
        public void GetUserByNameAsyncExecutesIfUserExists()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var executed = false;
                User foundUser = null;
                System.GetUserByNameAsync("Foo", (User user) => { executed = true; foundUser = user; });
                Assert.IsTrue(executed);
                Assert.AreEqual("Foo", foundUser.Username);
            }
        }

        [TestMethod]
        public async Task GetUserByNameAsyncSchedulesExecuteIfUserDoesNotExist()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var executed = false;
                User foundUser = null;
                System.GetUserByNameAsync("NewUser", (User user) => { executed = true; foundUser = user; });
                Assert.IsFalse(executed);
                Assert.IsNull(foundUser);
                await System.Process();
                Assert.IsTrue(executed);
                Assert.AreEqual("NewUser", foundUser.Username);
            }
        }

        [TestMethod]
        public async Task GetUsersByNamesGetsAllUsers()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var allUsers = await System.GetUsersByNames("NewUser", "TwoUser", "ThreeUser");
                var listUsers = allUsers.ToList();
                Assert.IsNotNull(allUsers);
                Assert.AreEqual(3, allUsers.Count());
                Assert.IsTrue(allUsers.Any(x => x.Username.Equals("NewUser") && !string.IsNullOrWhiteSpace(x.TwitchId)));
                Assert.IsTrue(allUsers.Any(x => x.Username.Equals("TwoUser") && !string.IsNullOrWhiteSpace(x.TwitchId)));
                Assert.IsTrue(allUsers.Any(x => x.Username.Equals("ThreeUser") && !string.IsNullOrWhiteSpace(x.TwitchId)));
            }
        }

        [TestMethod]
        public async Task GetUsersByNamesCreatesMissingUsers()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var allUsers = (await System.GetUsersByNames("NewUser", "Foo")).ToList();
                Assert.AreEqual(2, allUsers.Count);
                var newUser = allUsers.FirstOrDefault(x => x.Username.Equals("NewUser"));
                var existingUser = allUsers.FirstOrDefault(x => x.Username.Equals("Foo"));
                Assert.IsNotNull(newUser);
                Assert.IsNotNull(newUser.TwitchId);
                Assert.AreEqual("NewUser", newUser.Username);
                Assert.AreEqual("Foo", existingUser.Username);
            }
        }

        [TestMethod]
        public void GetUserByNameGetsUser()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var foo = System.GetUserByName("Foo");
                Assert.IsNotNull(foo);
                Assert.IsNotNull(foo.TwitchId);
                Assert.AreEqual("Foo", foo.Username);
            }
        }

        [TestMethod]
        public void GetUserByNameReturnsNullForNonExistingUser()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var nullUser = System.GetUserByName("NewUser");
                Assert.IsNull(nullUser);
            }
        }
    }
}
