using Autofac;
using LobotJR.Command.View.General;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LobotJR.Test.Views.General
{
    [TestClass]
    public class ConfirmationViewTests
    {
        private ConfirmationView View;

        [TestInitialize]
        public void Initialize()
        {
            View = AutofacMockSetup.Container.Resolve<ConfirmationView>();
        }

        [TestMethod]
        public void ConfirmRaisesConfirmationEvent() { }

        [TestMethod]
        public void CancelRaisesCancelEvent() { }
    }
}
