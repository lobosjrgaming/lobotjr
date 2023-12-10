using LobotJR.Command;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System.Linq;

namespace LobotJR.Test.Command
{
    [TestClass]
    public class CommandManagerTests : CommandManagerTestBase
    {
        [TestInitialize]
        public void Initialize()
        {
            InitializeCommandManager();
        }

        [TestMethod]
        public void LoadModulesLoadsModules()
        {
            var commands = CommandManager.Commands;
            var module = CommandModuleMock.Object;
            var firstCommand = module.Commands.First().Name;
            Assert.IsTrue(commands.Count() >= module.Commands.Count());
            Assert.IsFalse(commands.Any(x => x.Equals(firstCommand)));
            Assert.IsTrue(commands.Any(x => x.Equals($"{module.Name}.{firstCommand}")));
        }

        [TestMethod]
        public void InitializeLoadsRoleData()
        {
            var userRolesJson = JsonConvert.SerializeObject(AccessGroups);
            var loadedRolesJson = JsonConvert.SerializeObject(CommandManager.RepositoryManager.AccessGroups.Read());
            Assert.AreEqual(userRolesJson, loadedRolesJson);
        }

        [TestMethod]
        public void IsValidCommandMatchesFullId()
        {
            var module = CommandModuleMock.Object;
            var firstCommand = module.Commands.First();
            Assert.IsTrue(CommandManager.IsValidCommand($"{module.Name}.{firstCommand.Name}"));
        }

        [TestMethod]
        public void IsValidCommandMatchesWildcardAtEnd()
        {
            var module = CommandModuleMock.Object;
            Assert.IsTrue(CommandManager.IsValidCommand($"{module.Name}.*"));
        }

        [TestMethod]
        public void IsValidCommandMatchesWildcardAtStart()
        {
            var module = SubCommandModuleMock.Object;
            var part = module.Name.Substring(module.Name.IndexOf('.') + 1);
            Assert.IsTrue(CommandManager.IsValidCommand($"*.{part}.*"));
        }

        [TestMethod]
        public void ProcessMessageExecutesCommands()
        {
            var module = CommandModuleMock.Object;
            var command = module.Commands.First();
            var commandStrings = command.CommandStrings;
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            foreach (var commandString in commandStrings)
            {
                CommandManager.ProcessMessage(commandString, user, true);
            }
            ExecutorMocks[command.Name].Verify(x => x(It.IsAny<string>(), It.IsAny<User>()),
                Times.Exactly(commandStrings.Count()));
        }

        [TestMethod]
        public void ProcessMessageWildcardAllowsAccessToSubModules()
        {
            var module = CommandModuleMock.Object;
            var group = AccessGroups.First();
            var restrictionIds = Enrollments.Where(x => x.GroupId == group.Id).Select(x => x.Id);
            Restrictions.RemoveAll(x => restrictionIds.Contains(x.Id));
            Restrictions.Add(new Restriction(group.Id, "CommandMock.*"));
            var enrollmentIds = Enrollments.Where(x => x.GroupId == group.Id).Select(x => x.Id);
            Enrollments.RemoveAll(x => enrollmentIds.Contains(x.Id));
            Enrollments.Add(new Enrollment(group.Id, "12345"));
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foobar", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsNull(result.Errors);
            ExecutorMocks["Foobar"].Verify(x => x(It.IsAny<string>(), It.IsAny<User>()),
                Times.Once());
        }

        [TestMethod]
        public void ProcessMessageSubModuleAccessDoesNotAllowParentAccess()
        {
            var module = CommandModuleMock.Object;
            var group = AccessGroups.First();
            var restrictionIds = Enrollments.Where(x => x.GroupId == group.Id).Select(x => x.Id);
            Restrictions.RemoveAll(x => restrictionIds.Contains(x.Id));
            Restrictions.Add(new Restriction(group.Id, "CommandMock.SubMock.*"));
            var lastId = AccessGroups.Last().Id;
            AccessGroups.Add(new AccessGroup(lastId + 1, "OtherRole"));
            Restrictions.Add(new Restriction(lastId + 1, "CommandMock.*"));
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            ExecutorMocks["Foo"].Verify(x => x(It.IsAny<string>(), It.IsAny<User>()), Times.Never);
        }

        [TestMethod]
        public void ProcessMessageAllowsAuthorizedUserWhenWildcardIsRestricted()
        {
            var module = CommandModuleMock.Object;
            var group = AccessGroups.First();
            var restrictionIds = Enrollments.Where(x => x.GroupId == group.Id).Select(x => x.Id);
            Restrictions.RemoveAll(x => restrictionIds.Contains(x.Id));
            Restrictions.Add(new Restriction(group.Id, "CommandMock.SubMock.*"));
            var lastId = AccessGroups.Last().Id;
            AccessGroups.Add(new AccessGroup(lastId + 1, "OtherRole"));
            Restrictions.Add(new Restriction(lastId + 1, "CommandMock.*"));
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foobar", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsNull(result.Errors);
            ExecutorMocks["Foobar"].Verify(x => x(It.IsAny<string>(), It.IsAny<User>()), Times.Once);
        }

        [TestMethod]
        public void ProcessMessageRestrictsAccessToUnauthorizedUsers()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("NotAuth"));
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            ExecutorMocks["Foo"].Verify(x => x(It.IsAny<string>(), It.IsAny<User>()), Times.Never());
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToAuthorizedUsers()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsNull(result.Errors);
            ExecutorMocks["Foo"].Verify(x => x(It.IsAny<string>(), It.IsAny<User>()), Times.Once());
        }

        [TestMethod]
        public void ProcessMessageRestrictsCommandsWithWildcardRoles()
        {
            var group = AccessGroups.First();
            var restrictionIds = Enrollments.Where(x => x.GroupId == group.Id).Select(x => x.Id);
            Restrictions.RemoveAll(x => restrictionIds.Contains(x.Id));
            Restrictions.Add(new Restriction(group.Id, "CommandMock.*"));
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("NotAuth"));
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            ExecutorMocks["Foo"].Verify(x => x(It.IsAny<string>(), It.IsAny<User>()), Times.Never());
        }

        [TestMethod]
        public void ProcessMessageProcessesCompactCommands()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foo -c", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(@"Foo: Foo|Bar;", result.Responses.First());
            Assert.IsNull(result.Errors);
        }

        [TestMethod]
        public void ProcessMessageCompactCommandsPassParameters()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foo -c value", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(@"Foo: Foo|value;", result.Responses.First());
            Assert.IsNull(result.Errors);
        }

        [TestMethod]
        public void ProcessMessageDoesNotAllowWhisperOnlyMessageInPublicChat()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foo", user, false);
            Assert.IsTrue(result.TimeoutSender);
            Assert.IsTrue(result.Processed);
            Assert.IsNull(result.Errors);
            ExecutorMocks["Foo"].Verify(x => x(It.IsAny<string>(), It.IsAny<User>()), Times.Never());
        }

        [TestMethod]
        public void ProcessMessageDoesAllowNonWhisperOnlyMessageInPublicChat()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Public", user, false);
            Assert.IsTrue(result.Processed);
            Assert.IsNull(result.Errors);
            ExecutorMocks["Public"].Verify(x => x(It.IsAny<string>(), It.IsAny<User>()), Times.Once());
        }
    }
}
