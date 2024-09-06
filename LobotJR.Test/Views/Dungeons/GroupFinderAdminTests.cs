using Autofac;
using LobotJR.Command.View.Dungeons;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.Dungeons
{
    [TestClass]
    public class GroupFinderAdminTests
    {
        private GroupFinderAdmin View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<GroupFinderAdmin>();
        }

        [TestMethod]
        public void QueueStatusGetsGroupFinderQueueDetails() { }
    }
}
