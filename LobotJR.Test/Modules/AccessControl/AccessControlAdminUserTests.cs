using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.View.AccessControl;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Modules.AccessControl
{
    [TestClass]
    public class AccessControlAdminUserTests
    {
        private IConnectionManager ConnectionManager;
        private AccessControlAdmin View;
        private ICommandManager CommandManager;
        private UserController UserController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            View = AutofacMockSetup.Container.Resolve<AccessControlAdmin>();
            CommandManager = AutofacMockSetup.Container.Resolve<ICommandManager>();
            UserController = AutofacMockSetup.Container.Resolve<UserController>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            AutofacMockSetup.ResetAccessGroups();
        }

        [TestMethod]
        public void AddsUserToGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var enrollments = db.Enrollments.Read(x => x.GroupId == group.Id);
            var baseEnrollmentCount = enrollments.Count();
            var result = command.Executor.Execute(db.Users.Read().First(), "NotAuth TestGroup");
            db.Commit();
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("success", StringComparison.OrdinalIgnoreCase));
            enrollments = db.Enrollments.Read(x => x.GroupId == group.Id);
            Assert.AreEqual(baseEnrollmentCount + 1, enrollments.Count());
            var notAuth = UserController.GetUserByName("NotAuth");
            Assert.IsTrue(db.Enrollments.Read(x => x.Group.Equals(group) && x.UserId.Equals(notAuth.TwitchId)).Any());
        }

        [TestMethod]
        public void AddUserErrorsOnMissingParameters()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var user = db.Users.Read().First();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var wrongParameterCount = "BadInput";
            var userToAdd = "NotAuth";
            var groupToAdd = "TestGroup";
            var result = CommandManager.ProcessMessage($"{command.Name} {wrongParameterCount}", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(wrongParameterCount));
            result = CommandManager.ProcessMessage($"{command.Name}  {groupToAdd}", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(groupToAdd));
            result = CommandManager.ProcessMessage($"{command.Name} {userToAdd} ", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(result.Responses[0].Contains(userToAdd));
        }

        [TestMethod]
        public void AddUserErrorsOnInvalidGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var groupToAdd = "NotTestGroup";
            var result = command.Executor.Execute(db.Users.Read().First(), $"NotAuth {groupToAdd}");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(groupToAdd));
        }

        [TestMethod]
        public void AddUserErrorsOnExistingAssignment()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var userToAdd = "Auth";
            var result = command.Executor.Execute(db.Users.Read().First(), $"{userToAdd} TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(userToAdd));
        }

        [TestMethod]
        public void RemovesUserFromGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var userToRemove = UserController.GetUserByName("Auth").Username;
            var result = command.Executor.Execute(db.Users.Read().First(), $"{userToRemove} TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("success", StringComparison.OrdinalIgnoreCase));
            var enrollments = db.Enrollments.Read(x => x.UserId.Equals(userToRemove) && x.GroupId == group.Id);
            Assert.IsFalse(enrollments.Any());
        }

        [TestMethod]
        public void RemoveUserErrorsOnMissingParameters()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = db.Users.Read().First();
            var command = View.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var wrongParameterCount = "BadInput";
            var userToRemove = "NotAuth";
            var groupToRemove = "TestGroup";
            var result = CommandManager.ProcessMessage($"{command.Name} {wrongParameterCount}", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(wrongParameterCount));
            result = CommandManager.ProcessMessage($"{command.Name}  {userToRemove}", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(userToRemove));
            result = CommandManager.ProcessMessage($"{command.Name} {groupToRemove} ", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(groupToRemove));
        }

        [TestMethod]
        public void RemoveUserErrorsOnUserNotEnrolled()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var userToRemove = "NotAuth";
            var result = command.Executor.Execute(db.Users.Read().First(), $"{userToRemove} TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(userToRemove));
        }

        [TestMethod]
        public void RemoveUserErrorsOnGroupNotFound()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var groupToRemove = "NotTestGroup";
            var result = command.Executor.Execute(db.Users.Read().First(), $"Auth {groupToRemove}");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(groupToRemove));
        }

        [TestMethod]
        public void SetGroupFlagSetsModFlag()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var result = command.Executor.Execute(db.Users.Read().First(), "TestGroup mod true");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now includes", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(group.IncludeMods);
        }

        [TestMethod]
        public void SetGroupFlagUnsetsModFlag()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            group.IncludeMods = true;
            var result = command.Executor.Execute(db.Users.Read().First(), "TestGroup mod false");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now does not include", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(group.IncludeMods);
        }

        [TestMethod]
        public void SetGroupFlagSetsVipFlag()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var result = command.Executor.Execute(db.Users.Read().First(), "TestGroup vip true");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now includes", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(group.IncludeVips);
        }

        [TestMethod]
        public void SetGroupFlagUnsetsVipFlag()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            group.IncludeVips = true;
            var result = command.Executor.Execute(db.Users.Read().First(), "TestGroup vip false");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now does not include", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(group.IncludeVips);
        }

        [TestMethod]
        public void SetGroupFlagSetsSubFlag()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var result = command.Executor.Execute(db.Users.Read().First(), "TestGroup sub true");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now includes", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(group.IncludeSubs);
        }

        [TestMethod]
        public void SetGroupFlagUnsetsSubFlag()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            group.IncludeSubs = true;
            var result = command.Executor.Execute(db.Users.Read().First(), "TestGroup sub false");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now does not include", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(group.IncludeSubs);
        }

        [TestMethod]
        public void SetGroupFlagSetsAdminFlag()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var result = command.Executor.Execute(db.Users.Read().First(), "TestGroup admin true");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now includes", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(group.IncludeAdmins);
        }

        [TestMethod]
        public void SetGroupFlagUnsetsAdminFlag()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            group.IncludeAdmins = true;
            var result = command.Executor.Execute(db.Users.Read().First(), "TestGroup admin false");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now does not include", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(group.IncludeAdmins);
        }
    }
}