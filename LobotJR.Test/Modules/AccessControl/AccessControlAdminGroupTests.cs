using Autofac;
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
    public class AccessControlAdminGroupTests
    {
        private IConnectionManager ConnectionManager;
        private AccessControlView Module;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            Module = AutofacMockSetup.Container.Resolve<AccessControlView>();
        }

        [TestMethod]
        public void ListsGroups()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = Module.Commands.Where(x => x.Name.Equals("ListGroups")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = command.Executor.Execute(user, "");
                Assert.IsTrue(result.Processed);
                Assert.AreEqual(1, result.Responses.Count());
                Assert.IsTrue(result.Responses[0].Contains(db.AccessGroups.Read().Count().ToString()));
                Assert.IsTrue(result.Responses.Any(x => x.Contains("TestGroup")));
            }
        }

        [TestMethod]
        public void CreatesANewGroup()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var initialCount = db.AccessGroups.Read().Count();
                var command = Module.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = command.Executor.Execute(user, "NewTestGroup");
                Assert.IsTrue(result.Processed);
                Assert.AreEqual(1, result.Responses.Count());
                Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
                Assert.AreEqual(initialCount + 1, db.AccessGroups.Read().Count());
                Assert.IsTrue(db.AccessGroups.Read().Any(x => x.Name.Equals("NewTestGroup")));
            }
        }

        [TestMethod]
        public void CreateGroupErrorsOnDuplicateGroupName()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var initialCount = db.AccessGroups.Read().Count();
                var command = Module.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = command.Executor.Execute(user, "TestGroup");
                Assert.IsTrue(result.Processed);
                Assert.AreEqual(1, result.Responses.Count());
                Assert.IsTrue(result.Responses.Any(x => x.Contains("Error", StringComparison.OrdinalIgnoreCase)));
                Assert.AreEqual(initialCount, db.AccessGroups.Read().Count());
            }
        }

        [TestMethod]
        public void DescribesGroup()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = Module.Commands.Where(x => x.Name.Equals("DescribeGroup")).FirstOrDefault();
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
        }

        [TestMethod]
        public void DescribeGroupErrorsOnGroupNotFound()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = Module.Commands.Where(x => x.Name.Equals("DescribeGroup")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = command.Executor.Execute(user, "NotTestGroup");
                Assert.IsTrue(result.Processed);
                Assert.AreEqual(1, result.Responses.Count());
                Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
            }
        }

        [TestMethod]
        public void DeletesAGroup()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var initialCount = db.AccessGroups.Read().Count();
                var user = db.Users.Read().First();
                var add = Module.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
                add.Executor.Execute(user, "NewTestGroup");
                Assert.AreEqual(initialCount + 1, db.AccessGroups.Read().Count());
                var command = Module.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
                var result = command.Executor.Execute(user, "NewTestGroup");
                Assert.IsTrue(result.Processed);
                Assert.AreEqual(1, result.Responses.Count());
                Assert.AreEqual(initialCount, db.AccessGroups.Read().Count());
                Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
            }
        }

        [TestMethod]
        public void DeleteGroupErrorsOnDeleteNonEmptyGroup()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = Module.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = command.Executor.Execute(user, "TestGroup");
                Assert.IsTrue(result.Processed);
                Assert.AreEqual(1, result.Responses.Count());
                Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
            }
        }

        [TestMethod]
        public void DeleteGroupErrorsOnGroupNotFound()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = Module.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = command.Executor.Execute(user, "NotTestGroup");
                Assert.IsTrue(result.Processed);
                Assert.AreEqual(1, result.Responses.Count());
                Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}