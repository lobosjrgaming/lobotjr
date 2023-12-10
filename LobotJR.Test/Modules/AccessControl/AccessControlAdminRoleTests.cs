using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Modules.AccessControl
{
    [TestClass]
    public class AccessControlAdminRoleTests : AccessControlAdminBase
    {
        [TestInitialize]
        public void Initialize()
        {
            InitializeAccessControlModule();
        }

        [TestMethod]
        public void ListsRoles()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("ListGroups")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor("", user);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains(AccessGroups.Count.ToString()));
            Assert.IsTrue(result.Responses.Any(x => x.Contains("TestRole")));
        }

        [TestMethod]
        public void CreatesANewRole()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor("NewTestRole", user);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(2, CommandManager.RepositoryManager.AccessGroups.Read().Count());
            Assert.IsTrue(CommandManager.RepositoryManager.AccessGroups.Read().Any(x => x.Name.Equals("NewTestRole")));
        }

        [TestMethod]
        public void CreateRoleErrorsOnDuplicateRoleName()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor("TestRole", user);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Error", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(1, CommandManager.RepositoryManager.AccessGroups.Read().Count());
        }

        [TestMethod]
        public void DescribesRole()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("DescribeGroup")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor("TestRole", user);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(2, result.Responses.Count());
            Assert.IsTrue(result.Responses.All(x => x.Contains("TestRole")));
            var restrictions = CommandManager.RepositoryManager.Restrictions.Read(x => x.GroupId == group.Id);
            var enrollments = CommandManager.RepositoryManager.Enrollments.Read(x => x.GroupId == group.Id);
            foreach (var commandString in restrictions)
            {
                Assert.IsTrue(result.Responses.Any(x => x.Contains(commandString.Command)));
            }
            foreach (var enrollment in enrollments)
            {
                var username = CommandManager.UserSystem.GetUserById(enrollment.UserId).Username;
                Assert.IsTrue(result.Responses.Any(x => x.Contains(username, StringComparison.OrdinalIgnoreCase)));
            }
        }

        [TestMethod]
        public void DescribeRoleErrorsOnRoleNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("DescribeGroup")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor("NotTestRole", user);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void DeletesARole()
        {
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var add = Module.Commands.Where(x => x.Name.Equals("CreateGroup")).FirstOrDefault();
            add.Executor("NewTestRole", user);
            Assert.AreEqual(2, CommandManager.RepositoryManager.AccessGroups.Read().Count());
            var command = Module.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
            var result = command.Executor("NewTestRole", user);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.AreEqual(1, CommandManager.RepositoryManager.AccessGroups.Read().Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void DeleteRoleErrorsOnDeleteNonEmptyRole()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor("TestRole", user);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void DeleteRoleErrorsOnRoleNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("DeleteGroup")).FirstOrDefault();
            var user = CommandManager.RepositoryManager.Users.Read().First();
            var result = command.Executor("NotTestRole", user);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
        }
    }
}