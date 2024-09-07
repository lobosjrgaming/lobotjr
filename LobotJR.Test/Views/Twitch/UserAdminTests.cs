using Autofac;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.View.Twitch;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
        }

        public void UpdateViewersTriggersViewerUpdate()
        {
            Controller.LastUpdate = DateTime.Now;
            View.UpdateViewers();
            Assert.IsTrue(Controller.LastUpdate <= DateTime.Now - TimeSpan.FromMinutes(SettingsManager.GetAppSettings().UserDatabaseUpdateTime));
        }
    }
}
