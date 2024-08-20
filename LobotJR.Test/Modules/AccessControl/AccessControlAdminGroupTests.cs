using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Modules.AccessControl
{
    [TestClass]
    public class AccessControlAdminGroupTests : AccessControlAdminBase
    {
        [TestInitialize]
        public void Initialize()
        {
            InitializeAccessControlModule();
        }

        [TestMethod]
        public void ListsGroups()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("ListGroups")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor.Execute(user, "");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains(AccessGroups.Count.ToString()));
            Assert.IsTrue(result.Responses.Any(x => x.Contains("TestGroup")));
        }

        [TestMethod]
        public void CreatesANewGroup()
        {
            var initialCount = CommandManager.RepositoryManager.AccessGroups.Read().Count();
            var command = Module.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor.Execute(user, "NewTestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(initialCount + 1, CommandManager.RepositoryManager.AccessGroups.Read().Count());
            Assert.IsTrue(CommandManager.RepositoryManager.AccessGroups.Read().Any(x => x.Name.Equals("NewTestGroup")));
        }

        [TestMethod]
        public void CreateGroupErrorsOnDuplicateGroupName()
        {
            var initialCount = CommandManager.RepositoryManager.AccessGroups.Read().Count();
            var command = Module.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor.Execute(user, "TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Error", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(initialCount, CommandManager.RepositoryManager.AccessGroups.Read().Count());
        }

        [TestMethod]
        public void DescribesGroup()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("DescribeGroup")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor.Execute(user, "TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(2, result.Responses.Count());
            Assert.IsTrue(result.Responses.All(x => x.Contains("TestGroup")));
            var restrictions = CommandManager.RepositoryManager.Restrictions.Read(x => x.GroupId == group.Id);
            var enrollments = CommandManager.RepositoryManager.Enrollments.Read(x => x.GroupId == group.Id);
            foreach (var commandString in restrictions)
            {
                Assert.IsTrue(result.Responses.Any(x => x.Contains(commandString.Command)));
            }
            foreach (var enrollment in enrollments)
            {
                var username = CommandManager.UserController.GetUserById(enrollment.UserId).Username;
                Assert.IsTrue(result.Responses.Any(x => x.Contains(username, StringComparison.OrdinalIgnoreCase)));
            }
        }

        [TestMethod]
        public void DescribeGroupErrorsOnGroupNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("DescribeGroup")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor.Execute(user, "NotTestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void DeletesAGroup()
        {
            var initialCount = CommandManager.RepositoryManager.AccessGroups.Read().Count();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var add = Module.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
            add.Executor.Execute(user, "NewTestGroup");
            Assert.AreEqual(initialCount + 1, CommandManager.RepositoryManager.AccessGroups.Read().Count());
            var command = Module.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
            var result = command.Executor.Execute(user, "NewTestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.AreEqual(initialCount, CommandManager.RepositoryManager.AccessGroups.Read().Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void DeleteGroupErrorsOnDeleteNonEmptyGroup()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor.Execute(user, "TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void DeleteGroupErrorsOnGroupNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor.Execute(user, "NotTestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
        }
    }
}