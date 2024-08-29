using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.AccessControl;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Command
{
    [TestClass]
    public class CommandManagerTests
    {
        private IConnectionManager ConnectionManager;
        private ICommandManager CommandManager;
        private MockCommandView CommandModuleMock;
        private MockCommandSubView SubCommandModuleMock;
        private UserController UserController;


        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            CommandManager = AutofacMockSetup.Container.Resolve<ICommandManager>();
            CommandModuleMock = AutofacMockSetup.Container.Resolve<MockCommandView>();
            SubCommandModuleMock = AutofacMockSetup.Container.Resolve<MockCommandSubView>();
            UserController = AutofacMockSetup.Container.Resolve<UserController>();
        }

        [TestMethod]
        public void LoadModulesLoadsModules()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var commands = CommandManager.Commands;
                var module = CommandModuleMock;
                var firstCommand = module.Commands.First().Name;
                Assert.IsTrue(commands.Count() >= module.Commands.Count());
                Assert.IsFalse(commands.Any(x => x.Equals(firstCommand)));
                Assert.IsTrue(commands.Any(x => x.Equals($"{module.Name}.{firstCommand}")));
            }
        }

        [TestMethod]
        public void IsValidCommandMatchesFullId()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var module = CommandModuleMock;
                var firstCommand = module.Commands.First();
                Assert.IsTrue(CommandManager.IsValidCommand($"{module.Name}.{firstCommand.Name}"));
            }
        }

        [TestMethod]
        public void IsValidCommandMatchesWildcardAtEnd()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var module = CommandModuleMock;
                Assert.IsTrue(CommandManager.IsValidCommand($"{module.Name}.*"));
            }
        }

        [TestMethod]
        public void IsValidCommandMatchesWildcardAtStart()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var module = SubCommandModuleMock;
                var part = module.Name.Substring(module.Name.IndexOf('.') + 1);
                Assert.IsTrue(CommandManager.IsValidCommand($"*.{part}.*"));
            }
        }

        [TestMethod]
        public void ProcessMessageExecutesCommands()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var module = CommandModuleMock;
                var command = module.Commands.First();
                var commandStrings = command.CommandStrings;
                var user = UserController.GetUserByName("Auth");
                foreach (var commandString in commandStrings)
                {
                    CommandManager.ProcessMessage(commandString, user, true);
                }
                Assert.AreEqual(commandStrings.Count(), module.TotalCount);
            }
        }

        [TestMethod]
        public void ProcessMessageWildcardAllowsAccessToSubModules()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var module = CommandModuleMock;
                var group = db.AccessGroups.Read(x => x.Name.Equals("TestGroup")).First();
                var restriction = db.Restrictions.Read(x => x.GroupId.Equals(group.Id)).First();
                var old = restriction.Command;
                restriction.Command = "CommandMock.*";
                var user = UserController.GetUserByName("Auth");
                var result = CommandManager.ProcessMessage("Foobar", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(1, SubCommandModuleMock.FoobarCount);
                restriction.Command = old;
            }
        }

        [TestMethod]
        public void ProcessMessageSubModuleAccessDoesNotAllowParentAccess()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("NotAuth");
                var module = CommandModuleMock;
                var group = new AccessGroup() { Name = "NewGroup" };
                db.AccessGroups.Create(group);
                var restriction = new Restriction() { Group = group, Command = "CommandMock.SubMock.*" };
                db.Restrictions.Create(restriction);
                var enrollment = new Enrollment() { Group = group, UserId = user.TwitchId };
                db.Enrollments.Create(enrollment);
                var result = CommandManager.ProcessMessage("Foo", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsTrue(result.Errors.Any());
                Assert.AreEqual(0, module.FooCount);
                db.Enrollments.Delete(enrollment);
                db.Restrictions.Delete(restriction);
                db.AccessGroups.Delete(group);
            }
        }

        [TestMethod]
        public void ProcessMessageAllowsAuthorizedUserWhenWildcardIsRestricted()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("NotAuth");
                var module = CommandModuleMock;
                var group = new AccessGroup() { Name = "NewGroup" };
                db.AccessGroups.Create(group);
                var restriction = new Restriction() { Group = group, Command = "CommandMock.SubMock.*" };
                db.Restrictions.Create(restriction);
                var enrollment = new Enrollment() { Group = group, UserId = user.TwitchId };
                db.Enrollments.Create(enrollment);

                var otherGroup = db.AccessGroups.Read(x => x.Name.Equals("TestGroup")).First();
                var otherRestriction = db.Restrictions.Read(x => x.Group.Equals(otherGroup)).First();
                var old = otherRestriction.Command;
                otherRestriction.Command = "CommandMock.*";

                var result = CommandManager.ProcessMessage("Foobar", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(1, SubCommandModuleMock.FoobarCount);
                otherRestriction.Command = old;
                db.Enrollments.Delete(enrollment);
                db.Restrictions.Delete(restriction);
                db.AccessGroups.Delete(group);
            }
        }

        [TestMethod]
        public void ProcessMessageRestrictsAccessToUnauthorizedUsers()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("NotAuth");
                var result = CommandManager.ProcessMessage("Foo", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsTrue(result.Errors.Any());
                Assert.AreEqual(0, CommandModuleMock.FooCount);
            }
        }

        [TestMethod]
        public void ProcessMessageRestrictsAccessToUnauthorizedUsersByFlag()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("NotAuth");
                var result = CommandManager.ProcessMessage("ModFoo", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsTrue(result.Errors.Any());
                Assert.AreEqual(0, CommandModuleMock.ModFooCount);
            }
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToModUsersByFlag()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("Mod");
                var result = CommandManager.ProcessMessage("ModFoo", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(1, CommandModuleMock.ModFooCount);
            }
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToVipUsersByFlag()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("Vip");
                var result = CommandManager.ProcessMessage("VipFoo", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(1, CommandModuleMock.VipFooCount);
            }
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToSubUsersByFlag()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("Sub");
                var result = CommandManager.ProcessMessage("SubFoo", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(1, CommandModuleMock.SubFooCount);
            }
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToAdminUsersByFlag()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("Admin");
                var result = CommandManager.ProcessMessage("AdminFoo", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(1, CommandModuleMock.AdminFooCount);
            }
        }

        [TestMethod]
        public void ProcessMessageAllowsAccessToAuthorizedUsers()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("Auth");
                var result = CommandManager.ProcessMessage("Foo", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(1, CommandModuleMock.FooCount);
            }
        }

        [TestMethod]
        public void ProcessMessageRestrictsCommandsWithWildcards()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var group = db.AccessGroups.Read(x => x.Name.Equals("TestGroup")).First();
                var restriction = db.Restrictions.Read(x => x.GroupId.Equals(group.Id)).First();
                var old = restriction.Command;
                restriction.Command = "CommandMock.*";
                var user = UserController.GetUserByName("NotAuth");
                var result = CommandManager.ProcessMessage("Foo", user, true);
                Assert.IsTrue(result.Processed);
                Assert.IsTrue(result.Errors.Any());
                Assert.AreEqual(0, CommandModuleMock.FooCount);
                restriction.Command = old;
            }
        }

        [TestMethod]
        public void ProcessMessageProcessesCompactCommands()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("Auth");
                var result = CommandManager.ProcessMessage("Foo -c", user, true);
                Assert.IsTrue(result.Processed);
                Assert.AreEqual(@"Foo: Foo|Bar;", result.Responses.First());
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(1, CommandModuleMock.FooCountCompact);
            }
        }

        [TestMethod]
        public void ProcessMessageCompactCommandsPassParameters()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("Auth");
                var result = CommandManager.ProcessMessage("Foo -c value", user, true);
                Assert.IsTrue(result.Processed);
                Assert.AreEqual(@"Foo: Foo|value;", result.Responses.First());
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(1, CommandModuleMock.FooCountCompact);
            }
        }

        [TestMethod]
        public void ProcessMessageDoesNotAllowWhisperOnlyMessageInPublicChat()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("Auth");
                var result = CommandManager.ProcessMessage("Foo", user, false);
                Assert.IsTrue(result.TimeoutSender);
                Assert.IsTrue(result.Processed);
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(0, CommandModuleMock.FooCount);
            }
        }

        [TestMethod]
        public void ProcessMessageDoesAllowNonWhisperOnlyMessageInPublicChat()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName("Auth");
                var result = CommandManager.ProcessMessage("Public", user, false);
                Assert.IsTrue(result.Processed);
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(1, CommandModuleMock.PublicCount);
            }
        }
    }
}
