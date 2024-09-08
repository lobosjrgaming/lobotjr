using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.View;
using LobotJR.Command.View.Twitch;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace LobotJR.Test.Views.Twitch
{
    [TestClass]
    public class UserAdminTests
    {
        private SettingsManager SettingsManager;
        private UserController Controller;
        private UserAdmin View;

        [TestInitialize]
        public void Initialize()
        {
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
            Controller = AutofacMockSetup.Container.Resolve<UserController>();
            View = AutofacMockSetup.Container.Resolve<UserAdmin>();
            AutofacMockSetup.ResetUsers();
        }

        [TestMethod]
        public void UpdateViewersTriggersViewerUpdate()
        {
            Controller.LastUpdate = DateTime.Now;
            View.UpdateViewers();
            Assert.IsTrue(Controller.LastUpdate <= DateTime.Now - TimeSpan.FromMinutes(SettingsManager.GetAppSettings().UserDatabaseUpdateTime));
        }

        [TestMethod]
        public void RpgBanBansUser()
        {
            var user = Controller.GetUserByName("Sub");
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            var response = View.RpgBan(user.Username);
            var message = response.Responses.First();
            Assert.IsNotNull(user.BanTime);
            Assert.IsTrue(message.Contains("banned") && message.Contains(user.Username));
            listener.Verify(x => x(user, It.Is<CommandResult>(y => y.Responses.First().Contains("banned"))), Times.Once);
        }

        [TestMethod]
        public void RpgBanIncludesBanMessage()
        {
            var user = Controller.GetUserByName("Sub");
            var banMessage = "Ban Message";
            var response = View.RpgBan(user.Username, banMessage);
            var message = response.Responses.First();
            Assert.IsNotNull(user.BanTime);
            Assert.AreEqual(banMessage, user.BanMessage);
            Assert.IsTrue(message.Contains("banned") && message.Contains(user.Username));
        }

        [TestMethod]
        public void RpgBanReturnsErrorIfUserAlreadyBanned()
        {
            var user = Controller.GetUserByName("Sub");
            user.BanTime = DateTime.Now;
            var response = View.RpgBan(user.Username);
            var message = response.Responses.First();
            Assert.IsNotNull(user.BanTime);
            Assert.IsTrue(message.Contains(user.Username) && message.Contains("already banned"));
        }

        [TestMethod]
        public void RpgBanReturnsErrorIfUserNotFound()
        {
            var username = "InvalidUserName";
            var response = View.RpgBan(username);
            var message = response.Responses.First();
            Assert.IsTrue(message.Contains(username) && message.Contains("Unable"));
        }

        [TestMethod]
        public void RpgUnbanRemovesUserBan()
        {
            var user = Controller.GetUserByName("Sub");
            var listener = new Mock<PushNotificationHandler>();
            View.PushNotification += listener.Object;
            user.BanTime = DateTime.Now;
            var response = View.RpgUnban(user.Username);
            var message = response.Responses.First();
            Assert.IsNull(user.BanTime);
            Assert.IsTrue(message.Contains("lifted") && message.Contains(user.Username));
            listener.Verify(x => x(user, It.Is<CommandResult>(y => y.Responses.First().Contains("unbanned"))), Times.Once);
        }

        [TestMethod]
        public void RpgUnbanReturnsErrorIfUserNotBanned()
        {
            var user = Controller.GetUserByName("Sub");
            var response = View.RpgUnban(user.Username);
            var message = response.Responses.First();
            Assert.IsNull(user.BanTime);
            Assert.IsTrue(message.Contains(user.Username) && message.Contains("not banned"));
        }

        [TestMethod]
        public void RpgUnbanReturnsErrorIfUserNotFound()
        {
            var username = "InvalidUserName";
            var response = View.RpgUnban(username);
            var message = response.Responses.First();
            Assert.IsTrue(message.Contains(username) && message.Contains("Unable"));
        }
    }
}
