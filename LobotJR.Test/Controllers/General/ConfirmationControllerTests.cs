using Autofac;
using LobotJR.Command.Controller.General;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;

namespace LobotJR.Test.Controllers.General
{
    [TestClass]
    public class ConfirmationControllerTests
    {
        private IConnectionManager ConnectionManager;
        private ConfirmationController Controller;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            Controller = AutofacMockSetup.Container.Resolve<ConfirmationController>();
            AutofacMockSetup.ResetPlayers();
        }

        [TestMethod]
        public void ConfirmRaisesEvent()
        {
            var user = ConnectionManager.CurrentConnection.Users.Read().First();
            var listener = new Mock<ConfirmationController.ConfirmationHandler>();
            Controller.Confirmed += listener.Object;
            Controller.Confirm(user);
            listener.Verify(x => x(user), Times.Once());
        }

        [TestMethod]
        public void CancelRaisesEvent()
        {
            var user = ConnectionManager.CurrentConnection.Users.Read().First();
            var listener = new Mock<ConfirmationController.ConfirmationHandler>();
            Controller.Canceled += listener.Object;
            Controller.Cancel(user);
            listener.Verify(x => x(user), Times.Once());
        }
    }
}
