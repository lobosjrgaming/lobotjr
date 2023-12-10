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
        public void ChecksUsersAccessOfSpecificGroup()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var result = command.Executor("TestGroup", CommandManager.UserSystem.GetUserByName("Auth"));
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsFalse(result.Responses[0].Contains("not", StringComparison.OrdinalIgnoreCase));
            var user = new User() { TwitchId = "999", Username = "NewUser" };
            result = command.Executor("TestGroup", user);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("not", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void CheckAccessGivesNoGroupMessage()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var user = new User() { TwitchId = "999", Username = "NewUser" };
            var result = command.Executor(null, user);
            var groups = CommandManager.RepositoryManager.AccessGroups.Read().Select(x => x.Name);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsFalse(groups.Any(x => result.Responses.Any(y => y.Contains(x))));
        }

        [TestMethod]
        public void CheckAccessListsAllGroups()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var username = "Auth";
            var user = CommandManager.UserSystem.GetUserByName(username);
            var result = command.Executor(null, CommandManager.UserSystem.GetUserByName(username));
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            var enrollments = CommandManager.RepositoryManager.Enrollments.Read(x => x.UserId.Equals(user.TwitchId));
            var enrolledGroups = enrollments.Select(x => x.GroupId).Distinct();
            var groups = CommandManager.RepositoryManager.AccessGroups.Read(x => enrolledGroups.Contains(x.Id));
            Assert.IsTrue(groups.All(x => result.Responses.Any(y => y.Contains(x.Name))));
        }

        [TestMethod]
        public void ChecksAccessErrorsWithGroupNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("CheckAccess")).FirstOrDefault();
            var groupToCheck = "NotTestGroup";
            var result = command.Executor(groupToCheck, CommandManager.UserSystem.GetUserByName("Auth"));
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(groupToCheck));
        }
    }
}
