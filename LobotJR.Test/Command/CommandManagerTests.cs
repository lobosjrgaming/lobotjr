using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.AccessControl;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Command
{
    [TestClass]
    public class CommandManagerTests
    {
        private IConnectionManager ConnectionManager;
        private ICommandManager CommandManager;
        private MockCommandView CommandViewMock;
        private MockCommandSubView SubCommandViewMock;
        private UserController UserController;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            CommandManager = AutofacMockSetup.Container.Resolve<ICommandManager>();
            CommandViewMock = AutofacMockSetup.Container.Resolve<MockCommandView>();
            SubCommandViewMock = AutofacMockSetup.Container.Resolve<MockCommandSubView>();
            UserController = AutofacMockSetup.Container.Resolve<UserController>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            AutofacMockSetup.ResetAccessGroups();
            CommandViewMock.ResetCounts();
            SubCommandViewMock.ResetCounts();
            AutofacMockSetup.ResetUsers();
        }

        [TestMethod]
        public void LoadModulesLoadsModules()
        {
            var db = ConnectionManager.CurrentConnection;
            var commands = CommandManager.Commands;
            var module = CommandViewMock;
            var firstCommand = module.Commands.First().Name;
            Assert.IsTrue(commands.Count() >= module.Commands.Count());
            Assert.IsFalse(commands.Any(x => x.Equals(firstCommand)));
            Assert.IsTrue(commands.Any(x => x.Equals($"{module.Name}.{firstCommand}")));
        }

        [TestMethod]
        public void IsValidCommandMatchesFullId()
        {
            var module = CommandViewMock;
            var firstCommand = module.Commands.First();
            Assert.IsTrue(CommandManager.IsValidCommand($"{module.Name}.{firstCommand.Name}"));
        }

        [TestMethod]
        public void IsValidCommandMatchesWildcardAtEnd()
        {
            var module = CommandViewMock;
            Assert.IsTrue(CommandManager.IsValidCommand($"{module.Name}.*"));
        }

        [TestMethod]
        public void IsValidCommandMatchesWildcardAtStart()
        {
            var module = SubCommandViewMock;
            var part = module.Name.Substring(module.Name.IndexOf('.') + 1);
            Assert.IsTrue(CommandManager.IsValidCommand($"*.{part}.*"));
        }

        [TestMethod]
        public void ProcessMessageExecutesCommands()
        {
            var module = CommandViewMock;
            var command = module.Commands.First();
            var commandStrings = command.CommandStrings;
            var user = UserController.GetUserByName("Auth");
            foreach (var commandString in commandStrings)
            {
                CommandManager.ProcessMessage(commandString, user, true);
            }
            Assert.AreEqual(commandStrings.Count(), module.TotalCount);
        }

        [TestMethod]
        public void ProcessMessageIgnoresMessagesFromBannedUsers()
        {
            var user = UserController.GetUserByName("Auth");
            user.BanTime = DateTime.Now;
            var response = CommandManager.ProcessMessage("Foo", user, true);
            Assert.AreEqual(0, response.Responses.Count());
        }

        [TestMethod]
        public void ProcessMessageWildcardAllowsAccessToSubModules()
        {
            var db = ConnectionManager.CurrentConnection;
            var module = CommandViewMock;
            var group = db.AccessGroups.Read(x => x.Name.Equals("TestGroup")).First();
            var restriction = db.Restrictions.Read(x => x.GroupId.Equals(group.Id)).First();
            var old = restriction.Command;
            restriction.Command = "CommandMock.*";
            var user = UserController.GetUserByName("Auth");
            var result = CommandManager.ProcessMessage("Foobar", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, SubCommandViewMock.FoobarCount);
        }

        [TestMethod]
        public void ProcessMessageSubModuleAccessDoesNotAllowParentAccess()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = UserController.GetUserByName("NotAuth");
            var module = CommandViewMock;
            var group = new AccessGroup() { Name = "NewGroup" };
            db.AccessGroups.Create(group);
            var restriction = new Restriction() { Group = group, Command = "CommandMock.SubMock.*" };
            db.Restrictions.Create(restriction);
            var enrollment = new Enrollment() { Group = group, UserId = user.TwitchId };
            db.Enrollments.Create(enrollment);
            db.Commit();
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            Assert.AreEqual(0, module.FooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAuthorizedUserWhenWildcardIsRestricted()
        {
            var db = ConnectionManager.CurrentConnection;
            var user = UserController.GetUserByName("NotAuth");
            var module = CommandViewMock;
            var group = new AccessGroup() { Name = "NewGroup" };
            db.AccessGroups.Create(group);
            var restriction = new Restriction() { Group = group, Command = "CommandMock.SubMock.*" };
            db.Restrictions.Create(restriction);
            var enrollment = new Enrollment() { Group = group, UserId = user.TwitchId };
            db.Enrollments.Create(enrollment);
            db.Commit();

            var otherGroup = db.AccessGroups.Read(x => x.Name.Equals("TestGroup")).First();
            var otherRestriction = db.Restrictions.Read(x => x.Group.Equals(otherGroup)).First();
            var old = otherRestriction.Command;
            otherRestriction.Command = "CommandMock.*";

            var result = CommandManager.ProcessMessage("Foobar", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, SubCommandViewMock.FoobarCount);
        }

        [TestMethod]
        public void ProcessMessageRestrictsAccessToUnauthorizedUsers()
        {
            var user = UserController.GetUserByName("NotAuth");
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            Assert.AreEqual(0, CommandViewMock.FooCount);
        }

        [TestMethod]
        public void ProcessMessageRestrictsAccessToUnauthorizedUsersByFlag()
        {
            var user = UserController.GetUserByName("NotAuth");
            var result = CommandManager.ProcessMessage("ModFoo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            Assert.AreEqual(0, CommandViewMock.ModFooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToModUsersByFlag()
        {
            var user = UserController.GetUserByName("Mod");
            var result = CommandManager.ProcessMessage("ModFoo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandViewMock.ModFooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToVipUsersByFlag()
        {
            var user = UserController.GetUserByName("Vip");
            var result = CommandManager.ProcessMessage("VipFoo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandViewMock.VipFooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToSubUsersByFlag()
        {
            var user = UserController.GetUserByName("Sub");
            var result = CommandManager.ProcessMessage("SubFoo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandViewMock.SubFooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToAdminUsersByFlag()
        {
            var user = UserController.GetUserByName("Streamer");
            var result = CommandManager.ProcessMessage("AdminFoo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandViewMock.AdminFooCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToAuthorizedUsers()
        {
            var user = UserController.GetUserByName("Auth");
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandViewMock.FooCount);
        }

        [TestMethod]
        public void ProcessMessageRestrictsCommandsWithWildcards()
        {
            var db = ConnectionManager.CurrentConnection;
            var group = db.AccessGroups.Read(x => x.Name.Equals("TestGroup")).First();
            var restriction = db.Restrictions.Read(x => x.GroupId.Equals(group.Id)).First();
            var old = restriction.Command;
            restriction.Command = "CommandMock.*";
            var user = UserController.GetUserByName("NotAuth");
            var result = CommandManager.ProcessMessage("Foo", user, true);
            Assert.IsTrue(result.Processed);
            Assert.IsTrue(result.Errors.Any());
            Assert.AreEqual(0, CommandViewMock.FooCount);
        }

        [TestMethod]
        public void ProcessMessageProcessesCompactCommands()
        {
            var user = UserController.GetUserByName("Auth");
            var result = CommandManager.ProcessMessage("Foo -c", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(@"Foo: Foo|Bar;", result.Responses.First());
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandViewMock.FooCountCompact);
        }

        [TestMethod]
        public void ProcessMessageCompactCommandsPassParameters()
        {
            var user = UserController.GetUserByName("Auth");
            var result = CommandManager.ProcessMessage("Foo -c value", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(@"Foo: Foo|value;", result.Responses.First());
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandViewMock.FooCountCompact);
        }

        [TestMethod]
        public void ProcessMessageDoesNotAllowWhisperOnlyTimeoutMessageInPublicChat()
        {
            var user = UserController.GetUserByName("Auth");
            var result = CommandManager.ProcessMessage("Foo", user, false);
            Assert.IsTrue(result.TimeoutSender);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(0, CommandViewMock.FooCount);
        }

        [TestMethod]
        public void ProcessMessageDoesAllowNonWhisperOnlyMessageInPublicChat()
        {
            var user = UserController.GetUserByName("Auth");
            var result = CommandManager.ProcessMessage("Public", user, false);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(1, CommandViewMock.PublicCount);
        }

        [TestMethod]
        public void ProcessMessageIgnoresWhisperOnlyNonTimeoutMessageInPublicChat()
        {
            var user = UserController.GetUserByName("Auth");
            var result = CommandManager.ProcessMessage("Ignore", user, false);
            Assert.IsTrue(result.Processed);
            Assert.IsFalse(result.Errors.Any());
            Assert.IsFalse(result.Responses.Any());
            Assert.IsFalse(result.Messages.Any());
            Assert.AreEqual(0, CommandViewMock.IgnoreCount);
        }
    }
}
