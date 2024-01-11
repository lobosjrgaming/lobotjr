using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Modules.AccessControl
{
    [TestClass]
    public class AccessControlAdminCommandTests : AccessControlAdminBase
    {
        [TestInitialize]
        public void Initialize()
        {
            InitializeAccessControlModule();
        }

        [TestMethod]
        public void AddsCommandToGroup()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("RestrictCommand")).FirstOrDefault();
            var testGroup = CommandManager.RepositoryManager.AccessGroups.Read(x => x.Name.Equals("TestGroup")).First();
            var restrictions = CommandManager.RepositoryManager.Restrictions.Read();
            var baseCount = restrictions.Count();
            var result = command.Executor.Execute(null, "CommandMock.Unrestricted TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(baseCount + 1, restrictions.Count());
            Assert.IsTrue(restrictions.Any(x => x.GroupId == testGroup.Id && x.Command.Equals("CommandMock.Unrestricted")));
        }

        [TestMethod]
        public void AddCommandErrorsOnMissingParameters()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("RestrictCommand")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage("UnrestrictCommand BadInput", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains("BadInput"));
            var groupToAddTo = "TestGroup";
            var commandToAdd = "CommandMock.Bar";
            result = CommandManager.ProcessMessage($"UnrestrictCommand  {groupToAddTo}", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(groupToAddTo));
            result = CommandManager.ProcessMessage($"UnrestrictCommand {commandToAdd} ", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(commandToAdd));
        }

        [TestMethod]
        public void AddCommandErrorsOnInvalidGroup()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("RestrictCommand")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var groupToAdd = "NotTestGroup";
            var result = command.Executor.Execute(null, $"CommandMock.Unrestricted {groupToAdd}");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(result.Responses[0].Contains(groupToAdd));
        }

        [TestMethod]
        public void AddCommandErrorsOnInvalidCommand()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("RestrictCommand")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var commandToAdd = "CommandMock.Invalid";
            var groupToAdd = "TestGroup";
            var result = command.Executor.Execute(null, $"{commandToAdd} {groupToAdd}");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            var response = result.Responses[0];
            Assert.IsTrue(response.StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(response.Contains(commandToAdd));
            Assert.IsFalse(response.Contains(groupToAdd));
        }

        [TestMethod]
        public void AddCommandErrorsOnExistingAssignment()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("RestrictCommand")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var commandToAdd = "CommandMock.Foo";
            var groupToAddTo = "TestGroup";
            var result = command.Executor.Execute(null, $"{commandToAdd} {groupToAddTo}");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            var response = result.Responses[0];
            Assert.IsTrue(response.StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(response.Contains(commandToAdd));
            Assert.IsTrue(response.Contains(groupToAddTo));
        }

        [TestMethod]
        public void ListsCommands()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("ListCommands")).FirstOrDefault();

            var result = command.Executor.Execute(null, "");

            var commandModule = CommandModuleMock;
            var subCommandModule = SubCommandModuleMock;
            var commandCount = commandModule.Commands.Count() + subCommandModule.Commands.Count() + Module.Commands.Count();
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(4, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains($"{commandCount} commands", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(result.Responses.Any(x => x.Contains("3 modules", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(result.Responses.Any(
                x => x.Contains(commandModule.Name) &&
                commandModule.Commands.All(y => x.Contains(y.Name))));
            Assert.IsTrue(result.Responses.Any(
                x => x.Contains(subCommandModule.Name) &&
                subCommandModule.Commands.All(y => x.Contains(y.Name))));
            Assert.IsTrue(result.Responses.Any(
                x => x.Contains(Module.Name) &&
                Module.Commands.All(y => x.Contains(y.Name))));
        }

        [TestMethod]
        public void RemovesCommandFromGroup()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnrestrictCommand")).FirstOrDefault();
            var restrictions = CommandManager.RepositoryManager.Restrictions.Read();
            var testGroup = CommandManager.RepositoryManager.AccessGroups.Read(x => x.Name.Equals("TestGroup")).First();
            var commandToRemove = "CommandMock.Foo";
            var result = command.Executor.Execute(null, $"{commandToRemove} TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(restrictions.Any(x => x.GroupId == testGroup.Id && x.Command.Equals(commandToRemove)));
        }

        [TestMethod]
        public void RemoveCommandErrorsOnMissingParameters()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnrestrictCommand")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage("UnrestrictCommand BadInput", user, true);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            var groupToRemove = "TestGroup";
            var commandToRemove = "CommandMock.Foo";
            result = CommandManager.ProcessMessage($"UnrestrictCommand  {groupToRemove}", user, true);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(groupToRemove));
            result = CommandManager.ProcessMessage($"UnrestrictCommand {commandToRemove} ", user, true);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(commandToRemove));
        }

        [TestMethod]
        public void RemoveCommandErrorsOnCommandNotAssigned()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnrestrictCommand")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var commandToRemove = "CommandMock.Unrestricted";
            var result = command.Executor.Execute(null, $"{commandToRemove} TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(commandToRemove));
        }

        [TestMethod]
        public void RemoveCommandErrorsOnGroupNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnrestrictCommand")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var groupToRemove = "NotTestGroup";
            var result = command.Executor.Execute(null, $"CommandMock.Unrestricted {groupToRemove}");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(groupToRemove));
        }

        [TestMethod]
        public void RemoveCommandErrorsOnCommandNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnrestrictCommand")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var commandToRemove = "CommandMock.None";
            var result = command.Executor.Execute(null, $"{commandToRemove} TestGroup");
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(commandToRemove));
        }

    }
}