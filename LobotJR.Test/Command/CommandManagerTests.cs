using LobotJR.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var module = CommandModuleMock;
            var firstCommand = module.Commands.First().Name;
            Assert.IsTrue(commands.Count() >= module.Commands.Count());
            Assert.IsFalse(commands.Any(x => x.Equals(firstCommand)));
            Assert.IsTrue(commands.Any(x => x.Equals($"{module.Name}.{firstCommand}")));
        }

        [TestMethod]
        public void InitializeLoadsRoleData()
        {
            var accessGroupJson = JsonConvert.SerializeObject(AccessGroups);
            var loadedGroupsJson = JsonConvert.SerializeObject(CommandManager.RepositoryManager.AccessGroups.Read());
            Assert.AreEqual(accessGroupJson, loadedGroupsJson);
        }

        [TestMethod]
        public void IsValidCommandMatchesFullId()
        {
            var module = CommandModuleMock;
            var firstCommand = module.Commands.First();
            Assert.IsTrue(CommandManager.IsValidCommand($"{module.Name}.{firstCommand.Name}"));
        }

        [TestMethod]
        public void IsValidCommandMatchesWildcardAtEnd()
        {
            var module = CommandModuleMock;
            Assert.IsTrue(CommandManager.IsValidCommand($"{module.Name}.*"));
        }

        [TestMethod]
        public void IsValidCommandMatchesWildcardAtStart()
        {
            var module = SubCommandModuleMock;
            var part = module.Name.Substring(module.Name.IndexOf('.') + 1);
            Assert.IsTrue(CommandManager.IsValidCommand($"*.{part}.*"));
        }

        [TestMethod]
        public void ProcessMessageExecutesCommands()
        {
            var module = CommandModuleMock;
            var command = module.Commands.First();
            var commandStrings = command.CommandStrings;
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            foreach (var commandString in commandStrings)
            {
                CommandManager.ProcessMessage(commandString, user, true);
            }
            Assert.AreEqual(commandStrings.Count(), module.TotalCount);
        }

        [TestMethod]
        public void ProcessMessageWildcardAllowsAccessToSubModules()
        {
            var module = CommandModuleMock;
            var group = AccessGroups.First();
            var restrictionIds = Enrollments.Where(x => x.GroupId == group.Id).Select(x => x.Id);
            Restrictions.RemoveAll(x => restrictionIds.Contains(x.Id));
            Restrictions.Add(new Restriction(group, "CommandMock.*"));
            var enrollmentIds = Enrollments.Where(x => x.GroupId == group.Id).Select(x => x.Id);
            Enrollments.RemoveAll(x => enrollmentIds.Contains(x.Id));
            Enrollments.Add(new Enrollment(group, "12345"));
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foobar", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, SubCommandModuleMock.FoobarCount);
        }

        [TestMethod]
        public void ProcessMessageSubModuleAccessDoesNotAllowParentAccess()
        {
            var module = CommandModuleMock;
            var group = AccessGroups.First();
            var restrictionIds = Enrollments.Where(x => x.GroupId == group.Id).Select(x => x.Id);
            Restrictions.RemoveAll(x => restrictionIds.Contains(x.Id));
            Restrictions.Add(new Restriction(group, "CommandMock.SubMock.*"));
            var last = AccessGroups.Last();
            var newGroup = new AccessGroup(last.Id + 1, "OtherGroup");
            AccessGroups.Add(newGroup);
            Restrictions.Add(new Restriction(newGroup, "CommandMock.*"));
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            Assert.AreEqual(0, module.FooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAuthorizedUserWhenWildcardIsRestricted()
        {
            var module = CommandModuleMock;
            var group = AccessGroups.First();
            var restrictionIds = Enrollments.Where(x => x.GroupId == group.Id).Select(x => x.Id);
            Restrictions.RemoveAll(x => restrictionIds.Contains(x.Id));
            Restrictions.Add(new Restriction(group, "CommandMock.SubMock.*"));
            var last = AccessGroups.Last();
            var newGroup = new AccessGroup(last.Id + 1, "OtherGroup");
            AccessGroups.Add(newGroup);
            Restrictions.Add(new Restriction(newGroup, "CommandMock.*"));
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foobar", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, SubCommandModuleMock.FoobarCount);
        }

        [TestMethod]
        public void ProcessMessageRestrictsAccessToUnauthorizedUsers()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("NotAuth"));
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            Assert.AreEqual(0, CommandModuleMock.FooCount);
        }

        [TestMethod]
        public void ProcessMessageRestrictsAccessToUnauthorizedUsersByFlag()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("NotAuth"));
            var result = CommandManager.ProcessMessage("ModFoo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            Assert.AreEqual(0, CommandModuleMock.ModFooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToModUsersByFlag()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Mod"));
            var result = CommandManager.ProcessMessage("ModFoo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandModuleMock.ModFooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToVipUsersByFlag()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Vip"));
            var result = CommandManager.ProcessMessage("VipFoo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandModuleMock.VipFooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToSubUsersByFlag()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Sub"));
            var result = CommandManager.ProcessMessage("SubFoo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandModuleMock.SubFooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToAdminUsersByFlag()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Admin"));
            var result = CommandManager.ProcessMessage("AdminFoo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandModuleMock.AdminFooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToAuthorizedUsers()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandModuleMock.FooCount);
        }

        [TestMethod]
        public void ProcessMessageRestrictsCommandsWithWildcards()
        {
            var group = AccessGroups.First();
            var restrictionIds = Enrollments.Where(x => x.GroupId == group.Id).Select(x => x.Id);
            Restrictions.RemoveAll(x => restrictionIds.Contains(x.Id));
            Restrictions.Add(new Restriction(group, "CommandMock.*"));
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("NotAuth"));
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            Assert.AreEqual(0, CommandModuleMock.FooCount);
        }

        [TestMethod]
        public void ProcessMessageProcessesCompactCommands()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foo -c", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(@"Foo: Foo|Bar;", result.Responses.First());
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandModuleMock.FooCountCompact);
        }

        [TestMethod]
        public void ProcessMessageCompactCommandsPassParameters()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foo -c value", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(@"Foo: Foo|value;", result.Responses.First());
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandModuleMock.FooCountCompact);
        }

        [TestMethod]
        public void ProcessMessageDoesNotAllowWhisperOnlyMessageInPublicChat()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Foo", user, false);
            Assert.IsTrue(result.TimeoutSender);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(0, CommandModuleMock.FooCount);
        }

        [TestMethod]
        public void ProcessMessageDoesAllowNonWhisperOnlyMessageInPublicChat()
        {
            var user = IdCache.FirstOrDefault(x => x.Username.Equals("Auth"));
            var result = CommandManager.ProcessMessage("Public", user, false);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandModuleMock.PublicCount);
        }
    }
}
