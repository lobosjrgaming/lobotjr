using LobotJR.Command.Module.AccessControl;
using LobotJR.Test.Command;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Modules.AccessControl
{
    [TestClass]
    public class AccessControlTests : CommandManagerTestBase
    {
        protected AccessControlModule Module;

        [TestInitialize]
        public void Setup()
        {
            InitializeCommandManager();
            Module = new AccessControlModule(Manager);
        }

        [TestMethod]
        public void ChecksUsersAccessOfSpecificRole()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var result = command.Executor("TestRole", CommandManager.UserSystem.GetUserByName("Auth"));
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsFalse(result.Responses[0].Contains("not", StringComparison.OrdinalIgnoreCase));
            var user = new User() { TwitchId = "999", Username = "NewUser" };
            result = command.Executor("TestRole", user);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("not", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void CheckAccessGivesNoRoleMessage()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var user = new User() { TwitchId = "999", Username = "NewUser" };
            var result = command.Executor(null, user);
            var roles = CommandManager.RepositoryManager.AccessGroups.Read().Select(x => x.Name);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsFalse(roles.Any(x => result.Responses.Any(y => y.Contains(x))));
        }

        [TestMethod]
        public void CheckAccessListsAllRoles()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var username = "Auth";
            var result = command.Executor(null, CommandManager.UserSystem.GetUserByName(username));
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(CommandManager.RepositoryManager.AccessGroups
                .Read(x => x.UserIds.Any(y => y.Equals(username)))
                .All(x => result.Responses.Any(y => y.Contains(x.Name))));
        }

        [TestMethod]
        public void ChecksAccessErrorsWithRoleNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var roleToCheck = "NotTestRole";
            var result = command.Executor(roleToCheck, CommandManager.UserSystem.GetUserByName("Auth"));
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(roleToCheck));
        }
    }
}
