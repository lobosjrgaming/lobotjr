using Autofac;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Test.Controllers.Twitch
{
    [TestClass]
    public class UserControllerTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;
        private UserController Controller;
        private MockTwitchClient Client;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            Controller = AutofacMockSetup.Container.Resolve<UserController>();
            Client = AutofacMockSetup.Container.Resolve<MockTwitchClient>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            AutofacMockSetup.ResetUsers();
        }

        [TestMethod]
        public async Task ProcessesUpdatesAfterSetTime()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetAppSettings();
            Controller.LastUpdate = DateTime.Now - TimeSpan.FromMinutes(settings.UserDatabaseUpdateTime + 1);
            Client.Mock.Invocations.Clear();
            await Controller.Process();
            Client.Mock.Verify(x => x.GetModeratorListAsync(), Times.Once());
            Client.Mock.Verify(x => x.GetVipListAsync(), Times.Once());
            Client.Mock.Verify(x => x.GetSubscriberListAsync(), Times.Once());
        }

        [TestMethod]
        public async Task ProcessesUpdatesSyncsModList()
        {
            var db = ConnectionManager.CurrentConnection;
            var mod = db.Users.Read(x => x.IsMod).First();
            var notMod = db.Users.Read(x => !x.IsMod).First();
            mod.IsMod = false;
            notMod.IsMod = true;
            db.Users.Commit();
            Assert.IsFalse(mod.IsMod);
            Assert.IsTrue(notMod.IsMod);
            var settings = SettingsManager.GetAppSettings();
            Controller.LastUpdate = DateTime.Now - TimeSpan.FromMinutes(settings.UserDatabaseUpdateTime + 1);
            var allUsers = db.Users.Read().ToList();
            await Controller.Process();
            db.Commit();
            Assert.IsTrue(mod.IsMod);
            Assert.IsFalse(notMod.IsMod);
        }

        [TestMethod]
        public async Task ProcessesUpdatesPreservesExistingMods()
        {
            var db = ConnectionManager.CurrentConnection;
            var mod = db.Users.Read(x => x.IsMod).First();
            Assert.IsTrue(mod.IsMod);
            await Controller.Process();
            db.Commit();
            Assert.IsTrue(mod.IsMod);
        }

        [TestMethod]
        public async Task ProcessesUpdatesSyncsVipList()
        {
            var db = ConnectionManager.CurrentConnection;
            var vip = db.Users.Read(x => x.IsVip).First();
            var notVip = db.Users.Read(x => !x.IsVip).First();
            vip.IsVip = false;
            notVip.IsVip = true;
            db.Users.Commit();
            Assert.IsFalse(vip.IsVip);
            Assert.IsTrue(notVip.IsVip);
            var settings = SettingsManager.GetAppSettings();
            Controller.LastUpdate = DateTime.Now - TimeSpan.FromMinutes(settings.UserDatabaseUpdateTime + 1);
            await Controller.Process();
            db.Commit();
            Assert.IsTrue(vip.IsVip);
            Assert.IsFalse(notVip.IsVip);
        }

        [TestMethod]
        public async Task ProcessesUpdatesSyncsSubList()
        {
            var db = ConnectionManager.CurrentConnection;
            var sub = db.Users.Read(x => x.IsSub).First();
            var notSub = db.Users.Read(x => !x.IsSub).First();
            sub.IsSub = false;
            notSub.IsSub = true;
            db.Users.Commit();
            Assert.IsFalse(sub.IsSub);
            Assert.IsTrue(notSub.IsSub);
            var settings = SettingsManager.GetAppSettings();
            Controller.LastUpdate = DateTime.Now - TimeSpan.FromMinutes(settings.UserDatabaseUpdateTime + 1);
            await Controller.Process();
            db.Commit();
            Assert.IsTrue(sub.IsSub);
            Assert.IsFalse(notSub.IsSub);
        }

        [TestMethod]
        public async Task UpdateViewerListGetsChatterList()
        {
            var db = ConnectionManager.CurrentConnection;
            var allUsers = db.Users.Read().ToList();
            var settings = SettingsManager.GetAppSettings();
            Controller.LastUpdate = DateTime.Now - TimeSpan.FromMinutes(settings.UserDatabaseUpdateTime + 1);
            var viewers = await Controller.GetViewerList();
            db.Commit();
            Assert.AreEqual(allUsers.Count, viewers.Count());
            var missing = allUsers.Select(x => x.Username).Except(viewers.Select(x => x.Username)).ToList();
            Assert.AreEqual(0, missing.Count);
        }

        [TestMethod]
        public async Task ProcessDoesNotUpdateUntilSetTime()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = db.AppSettings.Read().First();
            settings.UserDatabaseUpdateTime = 10;
            db.AppSettings.Update(settings);
            db.AppSettings.Commit();
            Client.Mock.Invocations.Clear();
            await Controller.Process();
            Client.Mock.Verify(x => x.GetModeratorListAsync(), Times.Never());
            Client.Mock.Verify(x => x.GetVipListAsync(), Times.Never());
            Client.Mock.Verify(x => x.GetSubscriberListAsync(), Times.Never());
            Client.Mock.Verify(x => x.GetChatterListAsync(), Times.Never());
        }

        [TestMethod]
        public void SetBotUsersUpdatesAdminStatus()
        {
            var db = ConnectionManager.CurrentConnection;
            var bot = db.Users.Read(x => x.IsAdmin).First();
            bot.IsAdmin = false;
            db.Users.Update(bot);
            db.Users.Commit();
            Controller.SetBotUsers(bot, bot);
            Assert.IsTrue(bot.IsAdmin);
        }

        [TestMethod]
        public void GetOrCreateUserGetsExistingUser()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var newUser = Controller.GetOrCreateUser(user.TwitchId, user.Username);
            Assert.AreEqual(user, newUser);
        }

        [TestMethod]
        public void GetOrCreateUserCreatesNewUser()
        {
            var user = Controller.GetOrCreateUser("500", "NewUser");
            Assert.IsNotNull(user);
            Assert.AreEqual("500", user.TwitchId);
            Assert.AreEqual("NewUser", user.Username);
        }

        [TestMethod]
        public void GetOrCreateUserUpdatesUserWithNewName()
        {
            var db = ConnectionManager.CurrentConnection;
            var existingUser = db.Users.Read().First();
            var newName = $"New{existingUser.Username}";
            var user = Controller.GetOrCreateUser(existingUser.TwitchId, newName);
            Assert.IsNotNull(user);
            Assert.AreEqual(existingUser.TwitchId, user.TwitchId);
            Assert.AreEqual(newName, user.Username);
        }

        [TestMethod]
        public void GetUserByIdGetsUsers()
        {
            var user = Controller.GetUserById("10");
            Assert.IsNotNull(user);
            Assert.AreEqual("10", user.TwitchId);
        }

        [TestMethod]
        public void GetUserByNameAsyncExecutesIfUserExists()
        {
            var db = ConnectionManager.CurrentConnection;
            var executed = false;
            User foundUser = null;
            Controller.GetUserByNameAsync("Foo", (User user) => { executed = true; foundUser = user; });
            Assert.IsTrue(executed);
            Assert.AreEqual("Foo", foundUser.Username);
        }

        [TestMethod]
        public async Task GetUserByNameAsyncSchedulesExecuteIfUserDoesNotExist()
        {
            var db = ConnectionManager.CurrentConnection;
            var executed = false;
            User foundUser = null;
            Controller.GetUserByNameAsync("NewUser", (User user) => { executed = true; foundUser = user; });
            Assert.IsFalse(executed);
            Assert.IsNull(foundUser);
            await Controller.Process();
            Assert.IsTrue(executed);
            Assert.AreEqual("NewUser", foundUser.Username);
        }

        [TestMethod]
        public async Task GetUsersByNamesGetsAllUsers()
        {
            var db = ConnectionManager.CurrentConnection;
            var allUsers = await Controller.GetUsersByNames("NewUser", "TwoUser", "ThreeUser");
            Assert.IsNotNull(allUsers);
            Assert.AreEqual(3, allUsers.Count());
            Assert.IsTrue(allUsers.Any(x => x.Username.Equals("NewUser") && !string.IsNullOrWhiteSpace(x.TwitchId)));
            Assert.IsTrue(allUsers.Any(x => x.Username.Equals("TwoUser") && !string.IsNullOrWhiteSpace(x.TwitchId)));
            Assert.IsTrue(allUsers.Any(x => x.Username.Equals("ThreeUser") && !string.IsNullOrWhiteSpace(x.TwitchId)));
        }

        [TestMethod]
        public async Task GetUsersByNamesCreatesMissingUsers()
        {
            var db = ConnectionManager.CurrentConnection;
            var allUsers = (await Controller.GetUsersByNames("NewUser", "Foo")).ToList();
            Assert.AreEqual(2, allUsers.Count);
            var newUser = allUsers.FirstOrDefault(x => x.Username.Equals("NewUser"));
            var existingUser = allUsers.FirstOrDefault(x => x.Username.Equals("Foo"));
            Assert.IsNotNull(newUser);
            Assert.IsNotNull(newUser.TwitchId);
            Assert.AreEqual("NewUser", newUser.Username);
            Assert.AreEqual("Foo", existingUser.Username);
        }

        [TestMethod]
        public void GetUserByNameGetsUser()
        {
            var foo = Controller.GetUserByName("Foo");
            Assert.IsNotNull(foo);
            Assert.IsNotNull(foo.TwitchId);
            Assert.AreEqual("Foo", foo.Username);
        }

        [TestMethod]
        public void GetUserByNameReturnsNullForNonExistingUser()
        {
            var nullUser = Controller.GetUserByName("NewUser");
            Assert.IsNull(nullUser);
        }
    }
}
