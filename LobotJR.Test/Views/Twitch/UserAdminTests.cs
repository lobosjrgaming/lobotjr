using Autofac;
using LobotJR.Command.View.Twitch;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.Twitch
{
    [TestClass]
    public class UserAdminTests
    {
        private UserAdmin View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<UserAdmin>();
        }

        public void UpdateViewersTriggersViewerUpdate() { }
    }
}
