using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.View.AccessControl;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Views.AccessControl
{
    [TestClass]
    public class AccessControlTests
    {
        private IConnectionManager ConnectionManager;
        protected AccessControlView View;
        protected ICommandManager CommandManager;
        protected UserController UserController;

        [TestInitialize]
        public void Setup()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            CommandManager = AutofacMockSetup.Container.Resolve<ICommandManager>();
            View = AutofacMockSetup.Container.Resolve<AccessControlView>();
            UserController = AutofacMockSetup.Container.Resolve<UserController>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            AutofacMockSetup.ResetAccessGroups();
        }

        [TestMethod]
        public void ChecksUsersAccessOfSpecificGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var result = command.Executor.Execute(UserController.GetUserByName("Auth"), "TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsFalse(result.Responses[0].Contains("not", StringComparison.OrdinalIgnoreCase));
            var user = new User() { TwitchId = "999", Username = "NewUser" };
            result = command.Executor.Execute(user, "TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("not", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void CheckAccessGivesNoGroupMessage()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var user = new User() { TwitchId = "999", Username = "NewUser" };
            var result = command.Executor.Execute(user, null);
            var groups = db.AccessGroups.Read().Select(x => x.Name);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsFalse(groups.Any(x => result.Responses.Any(y => y.Contains(x))));
        }

        [TestMethod]
        public void CheckAccessListsAllGroups()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var username = "Auth";
            var user = UserController.GetUserByName(username);
            var result = command.Executor.Execute(UserController.GetUserByName(username), null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            var enrollments = db.Enrollments.Read(x => x.UserId.Equals(user.TwitchId));
            var enrolledGroups = enrollments.Select(x => x.GroupId).Distinct();
            var groups = db.AccessGroups.Read(x => enrolledGroups.Contains(x.Id));
            Assert.IsTrue(groups.All(x => result.Responses.Any(y => y.Contains(x.Name))));
        }

        [TestMethod]
        public void ChecksAccessErrorsWithGroupNotFound()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var groupToCheck = "NotTestGroup";
            var result = command.Executor.Execute(UserController.GetUserByName("Auth"), groupToCheck);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(groupToCheck));
        }
    }
}
