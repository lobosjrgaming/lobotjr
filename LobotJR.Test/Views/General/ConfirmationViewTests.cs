using Autofac;
using LobotJR.Command.Controller.General;
using LobotJR.Command.View.General;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;

namespace LobotJR.Test.Views.General
{
    [TestClass]
    public class ConfirmationViewTests
    {
        private ConfirmationController Controller;
        private ConfirmationView View;
        private User User;

        [TestInitialize]
        public void Initialize()
        {
            Controller = AutofacMockSetup.Container.Resolve<ConfirmationController>();
            View = AutofacMockSetup.Container.Resolve<ConfirmationView>();
            User = AutofacMockSetup.ConnectionManager.CurrentConnection.Users.Read().First();
        }

        [TestMethod]
        public void ConfirmRaisesConfirmationEvent()
        {
            var listener = new Mock<ConfirmationController.ConfirmationHandler>();
            Controller.Confirmed += listener.Object;
            View.Confirm(User);
            listener.Verify(x => x(User), Times.Once);
        }

        [TestMethod]
        public void CancelRaisesCancelEvent()
        {
            var listener = new Mock<ConfirmationController.ConfirmationHandler>();
            Controller.Canceled += listener.Object;
            View.Cancel(User);
            listener.Verify(x => x(User), Times.Once);
        }
    }
}
