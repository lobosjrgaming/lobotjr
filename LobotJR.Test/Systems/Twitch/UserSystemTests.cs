using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Shared.Channel;
using LobotJR.Shared.User;
using LobotJR.Test.Mocks;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Test.Systems.Twitch
{
    [TestClass]
    public class UserSystemTests
    {
        private SqliteRepositoryManager Manager;
        private Mock<ITwitchClient> Client;
        private UserSystem System;

        [TestInitialize]
        public void Initialize()
        {
            Manager = new SqliteRepositoryManager(MockContext.Create());
            Client = new Mock<ITwitchClient>();
            Client.Setup(x => x.GetTwitchUsers(It.IsAny<IEnumerable<string>>()))
                .Returns((IEnumerable<string> users) =>
                {
                    var userObjects = Manager.Users.Read(x => users.Contains(x.Username, StringComparer.OrdinalIgnoreCase));
                    var userResponse = userObjects.Select(x => new UserResponseData() { DisplayName = x.Username, Login = x.Username, Id = x.TwitchId }).ToList();
                    var toCreate = users.Except(userObjects.Select(x => x.Username)).ToList();
                    for (var i = 0; i < toCreate.Count(); i++)
                    {
                        var creating = toCreate[i];
                        userResponse.Add(new UserResponseData() { DisplayName = creating, Login = creating, Id = (500 + i).ToString() });
                    }
                    var task = new Task<IEnumerable<UserResponseData>>(() => userResponse);
                    task.Start();
                    return task;
                });
            Client.Setup(x => x.GetModeratorListAsync())
                .Returns(() =>
                {
                    var userObjects = Manager.Users.Read(x => x.Username.Equals("Mod"));
                    var task = new Task<IEnumerable<TwitchUserData>>(() => userObjects.Select(x => new TwitchUserData() { UserId = x.TwitchId, UserLogin = x.Username, UserName = x.Username }));
                    task.Start();
                    return task;
                });
            Client.Setup(x => x.GetVipListAsync())
                .Returns(() =>
                {
                    var userObjects = Manager.Users.Read(x => x.Username.Equals("Vip"));
                    var task = new Task<IEnumerable<TwitchUserData>>(() => userObjects.Select(x => new TwitchUserData() { UserId = x.TwitchId, UserLogin = x.Username, UserName = x.Username }));
                    task.Start();
                    return task;
                });
            Client.Setup(x => x.GetSubscriberListAsync())
                .Returns(() =>
                {
                    var userObjects = Manager.Users.Read(x => x.Username.Equals("Sub"));
                    var task = new Task<IEnumerable<SubscriptionResponseData>>(() => userObjects.Select(x => new SubscriptionResponseData() { UserId = x.TwitchId, UserLogin = x.Username, UserName = x.Username }));
                    task.Start();
                    return task;
                });
            Client.Setup(x => x.GetChatterListAsync())
                .Returns(() =>
                {
                    var userObjects = Manager.Users.Read();
                    var task = new Task<IEnumerable<TwitchUserData>>(() => userObjects.Select(x => new TwitchUserData() { UserId = x.TwitchId, UserLogin = x.Username, UserName = x.Username }));
                    task.Start();
                    return task;
                });
            System = new UserSystem(Manager, Client.Object);
            var settings = Manager.AppSettings.Read().First();
            settings.UserDatabaseUpdateTime = 0;
            Manager.AppSettings.Update(settings);
            Manager.AppSettings.Commit();
        }

        [TestMethod]
        public async Task ProcessesUpdatesAfterSetTime()
        {
            await System.Process(true);
            Client.Verify(x => x.GetModeratorListAsync(), Times.Once());
            Client.Verify(x => x.GetVipListAsync(), Times.Once());
            Client.Verify(x => x.GetSubscriberListAsync(), Times.Once());
            Client.Verify(x => x.GetChatterListAsync(), Times.Once());
        }

        [TestMethod]
        public async Task ProcessesUpdatesSyncsModList()
        {
            var mod = Manager.Users.Read(x => x.IsMod).First();
            var notMod = Manager.Users.Read(x => !x.IsMod).First();
            mod.IsMod = false;
            Manager.Users.Update(mod);
            notMod.IsMod = true;
            Manager.Users.Update(notMod);
            Manager.Users.Commit();
            Assert.IsFalse(mod.IsMod);
            Assert.IsTrue(notMod.IsMod);
            await System.Process(true);
            Assert.IsTrue(mod.IsMod);
            Assert.IsFalse(notMod.IsMod);
        }

        [TestMethod]
        public async Task ProcessesUpdatesPreservesExistingMods()
        {
            var mod = Manager.Users.Read(x => x.IsMod).First();
            Assert.IsTrue(mod.IsMod);
            await System.Process(true);
            Assert.IsTrue(mod.IsMod);
        }

        [TestMethod]
        public async Task ProcessesUpdatesSyncsVipList()
        {
            var vip = Manager.Users.Read(x => x.IsVip).First();
            var notVip = Manager.Users.Read(x => !x.IsVip).First();
            vip.IsVip = false;
            Manager.Users.Update(vip);
            notVip.IsVip = true;
            Manager.Users.Update(notVip);
            Manager.Users.Commit();
            Assert.IsFalse(vip.IsVip);
            Assert.IsTrue(notVip.IsVip);
            await System.Process(true);
            Assert.IsTrue(vip.IsVip);
            Assert.IsFalse(notVip.IsVip);
        }

        [TestMethod]
        public async Task ProcessesUpdatesSyncsSubList()
        {
            var sub = Manager.Users.Read(x => x.IsSub).First();
            var notSub = Manager.Users.Read(x => !x.IsSub).First();
            sub.IsSub = false;
            Manager.Users.Update(sub);
            notSub.IsSub = true;
            Manager.Users.Update(notSub);
            Manager.Users.Commit();
            Assert.IsFalse(sub.IsSub);
            Assert.IsTrue(notSub.IsSub);
            await System.Process(true);
            Assert.IsTrue(sub.IsSub);
            Assert.IsFalse(notSub.IsSub);
        }

        [TestMethod]
        public async Task ProcessGetsChatterList()
        {
            var allUsers = Manager.Users.Read().ToList();
            await System.Process(true);
            Assert.AreEqual(allUsers.Count, System.Viewers.Count());
            var missing = allUsers.Select(x => x.Username).Except(System.Viewers.Select(x => x.Username)).ToList();
            Assert.AreEqual(0, missing.Count);
        }

        [TestMethod]
        public async Task ProcessDoesNotUpdateIfNotBroadcasting()
        {
            await System.Process(false);
            Client.Verify(x => x.GetModeratorListAsync(), Times.Never());
            Client.Verify(x => x.GetVipListAsync(), Times.Never());
            Client.Verify(x => x.GetSubscriberListAsync(), Times.Never());
            Client.Verify(x => x.GetChatterListAsync(), Times.Never());
        }

        [TestMethod]
        public async Task ProcessDoesNotUpdateUntilSetTime()
        {
            var settings = Manager.AppSettings.Read().First();
            settings.UserDatabaseUpdateTime = 10;
            Manager.AppSettings.Update(settings);
            Manager.AppSettings.Commit();
            await System.Process(true);
            Client.Verify(x => x.GetModeratorListAsync(), Times.Never());
            Client.Verify(x => x.GetVipListAsync(), Times.Never());
            Client.Verify(x => x.GetSubscriberListAsync(), Times.Never());
            Client.Verify(x => x.GetChatterListAsync(), Times.Never());
        }

        [TestMethod]
        public void SetBotUsersUpdatesAdminStatus()
        {
            var bot = Manager.Users.Read(x => x.IsAdmin).First();
            bot.IsAdmin = false;
            Manager.Users.Update(bot);
            Manager.Users.Commit();
            System.SetBotUsers(bot, bot);
            Assert.IsTrue(bot.IsAdmin);
        }

        [TestMethod]
        public void GetOrCreateUserGetsExistingUser()
        {
            var user = Manager.Users.Read().First();
            var newUser = System.GetOrCreateUser(user.TwitchId, user.Username);
            Assert.AreEqual(user, newUser);
        }

        [TestMethod]
        public void GetOrCreateUserCreatesNewUser()
        {
            var user = System.GetOrCreateUser("500", "NewUser");
            Assert.IsNotNull(user);
            Assert.AreEqual("500", user.TwitchId);
            Assert.AreEqual("NewUser", user.Username);
        }

        [TestMethod]
        public void GetOrCreateUserUpdatesUserWithNewName()
        {
            var existingUser = Manager.Users.Read().First();
            var newName = $"New{existingUser.Username}";
            var user = System.GetOrCreateUser(existingUser.TwitchId, newName);
            Assert.IsNotNull(user);
            Assert.AreEqual(existingUser.TwitchId, user.TwitchId);
            Assert.AreEqual(newName, user.Username);
        }

        [TestMethod]
        public void GetUserByIdGetsUsers()
        {
            var user = System.GetUserById("10");
            Assert.IsNotNull(user);
            Assert.AreEqual("10", user.TwitchId);
        }

        [TestMethod]
        public void GetUserByNameAsyncExecutesIfUserExists()
        {
            var executed = false;
            User foundUser = null;
            System.GetUserByNameAsync("Foo", (User user) => { executed = true; foundUser = user; });
            Assert.IsTrue(executed);
            Assert.AreEqual("Foo", foundUser.Username);
        }

        [TestMethod]
        public async Task GetUserByNameAsyncSchedulesExecuteIfUserDoesNotExist()
        {
            var executed = false;
            User foundUser = null;
            System.GetUserByNameAsync("NewUser", (User user) => { executed = true; foundUser = user; });
            Assert.IsFalse(executed);
            Assert.IsNull(foundUser);
            await System.Process(true);
            Assert.IsTrue(executed);
            Assert.AreEqual("NewUser", foundUser.Username);
        }

        [TestMethod]
        public async Task GetUsersByNamesGetsAllUsers()
        {
            var allUsers = await System.GetUsersByNames("NewUser", "TwoUser", "ThreeUser");
            var listUsers = allUsers.ToList();
            Assert.IsNotNull(allUsers);
            Assert.AreEqual(3, allUsers.Count());
            Assert.IsTrue(allUsers.Any(x => x.Username.Equals("NewUser") && !string.IsNullOrWhiteSpace(x.TwitchId)));
            Assert.IsTrue(allUsers.Any(x => x.Username.Equals("TwoUser") && !string.IsNullOrWhiteSpace(x.TwitchId)));
            Assert.IsTrue(allUsers.Any(x => x.Username.Equals("ThreeUser") && !string.IsNullOrWhiteSpace(x.TwitchId)));
        }

        [TestMethod]
        public async Task GetUsersByNamesCreatesMissingUsers()
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

        [TestMethod]
        public void GetUserByNameGetsUser()
        {
            var foo = System.GetUserByName("Foo");
            Assert.IsNotNull(foo);
            Assert.IsNotNull(foo.TwitchId);
            Assert.AreEqual("Foo", foo.Username);
        }

        [TestMethod]
        public void GetUserByNameReturnsNullForNonExistingUser()
        {
            var nullUser = System.GetUserByName("NewUser");
            Assert.IsNull(nullUser);
        }
    }
}
