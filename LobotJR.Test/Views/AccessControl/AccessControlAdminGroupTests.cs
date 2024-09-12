using Autofac;
using LobotJR.Command.View.AccessControl;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Views.AccessControl
{
    [TestClass]
    public class AccessControlAdminGroupTests
    {
        private IConnectionManager ConnectionManager;
        private AccessControlAdmin View;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            View = AutofacMockSetup.Container.Resolve<AccessControlAdmin>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            AutofacMockSetup.ResetAccessGroups();
        }

        [TestMethod]
        public void ListsGroups()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("ListGroups")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = command.Executor.Execute(user, "");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains(db.AccessGroups.Read().Count().ToString()));
            Assert.IsTrue(result.Responses.Any(x => x.Contains("TestGroup")));
        }

        [TestMethod]
        public void CreatesANewGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var initialCount = db.AccessGroups.Read().Count();
            var command = View.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = command.Executor.Execute(user, "NewTestGroup");
            db.Commit();
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(initialCount + 1, db.AccessGroups.Read().Count());
            Assert.IsTrue(db.AccessGroups.Read().Any(x => x.Name.Equals("NewTestGroup")));
        }

        [TestMethod]
        public void CreateGroupErrorsOnDuplicateGroupName()
        {
            var db = ConnectionManager.CurrentConnection;
            var initialCount = db.AccessGroups.Read().Count();
            var command = View.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = command.Executor.Execute(user, "TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Error", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(initialCount, db.AccessGroups.Read().Count());
        }

        [TestMethod]
        public void DescribesGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("DescribeGroup")).FirstOrDefault();
            var group = db.AccessGroups.Read().FirstOrDefault();
            var user = db.Users.Read().First();
            var result = command.Executor.Execute(user, "TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(2, result.Responses.Count());
            Assert.IsTrue(result.Responses.All(x => x.Contains("TestGroup")));
            var restrictions = db.Restrictions.Read(x => x.GroupId == group.Id);
            var enrollments = db.Enrollments.Read(x => x.GroupId == group.Id);
            foreach (var commandString in restrictions)
            {
                Assert.IsTrue(result.Responses.Any(x => x.Contains(commandString.Command)));
            }
            foreach (var enrollment in enrollments)
            {
                var username = db.Users.Read(x => x.TwitchId.Equals(enrollment.UserId)).First().Username;
                Assert.IsTrue(result.Responses.Any(x => x.Contains(username, StringComparison.OrdinalIgnoreCase)));
            }
        }

        [TestMethod]
        public void DescribeGroupErrorsOnGroupNotFound()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("DescribeGroup")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = command.Executor.Execute(user, "NotTestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void DeletesAGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var initialCount = db.AccessGroups.Read().Count();
            var user = db.Users.Read().First();
            var add = View.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
            add.Executor.Execute(user, "NewTestGroup");
            db.Commit();
            Assert.AreEqual(initialCount + 1, db.AccessGroups.Read().Count());
            var command = View.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
            var result = command.Executor.Execute(user, "NewTestGroup");
            db.Commit();
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.AreEqual(initialCount, db.AccessGroups.Read().Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void DeleteGroupErrorsOnDeleteNonEmptyGroup()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = command.Executor.Execute(user, "TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void DeleteGroupErrorsOnGroupNotFound()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = View.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = command.Executor.Execute(user, "NotTestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
        }
    }
}