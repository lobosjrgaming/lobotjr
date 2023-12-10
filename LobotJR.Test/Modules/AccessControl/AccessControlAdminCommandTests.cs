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
        public void AddsCommandToRole()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("RestrictCommand")).FirstOrDefault();
            var testRole = CommandManager.RepositoryManager.AccessGroups.Read(x => x.Name.Equals("TestRole")).First();
            var restrictions = CommandManager.RepositoryManager.Restrictions.Read();
            var baseCount = restrictions.Count();
            var result = command.Executor("CommandMock.Unrestricted TestRole", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(baseCount + 1, restrictions.Count());
            Assert.IsTrue(restrictions.Any(x => x.GroupId == testRole.Id && x.Command.Equals("CommandMock.Unrestricted")));
        }

        [TestMethod]
        public void AddCommandErrorsOnMissingParameters()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("RestrictCommand")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var wrongParameterCount = "BadInput";
            var result = command.Executor(wrongParameterCount, null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(wrongParameterCount));
            var roleToAddTo = "TestRole";
            var commandToAdd = "CommandMock.Bar";
            result = command.Executor($" {roleToAddTo}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(roleToAddTo));
            result = command.Executor($"{commandToAdd} ", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(commandToAdd));
        }

        [TestMethod]
        public void AddCommandErrorsOnInvalidRole()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("RestrictCommand")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var roleToAdd = "NotTestRole";
            var result = command.Executor($"CommandMock.Unrestricted {roleToAdd}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(result.Responses[0].Contains(roleToAdd));
        }

        [TestMethod]
        public void AddCommandErrorsOnInvalidCommand()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("RestrictCommand")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var commandToAdd = "CommandMock.Invalid";
            var roleToAdd = "TestRole";
            var result = command.Executor($"{commandToAdd} {roleToAdd}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            var response = result.Responses[0];
            Assert.IsTrue(response.StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(response.Contains(commandToAdd));
            Assert.IsFalse(response.Contains(roleToAdd));
        }

        [TestMethod]
        public void AddCommandErrorsOnExistingAssignment()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("RestrictCommand")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var commandToAdd = "CommandMock.Foo";
            var roleToAddTo = "TestRole";
            var result = command.Executor($"{commandToAdd} {roleToAddTo}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            var response = result.Responses[0];
            Assert.IsTrue(response.StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(response.Contains(commandToAdd));
            Assert.IsTrue(response.Contains(roleToAddTo));
        }

        [TestMethod]
        public void ListsCommands()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("ListCommands")).FirstOrDefault();

            var result = command.Executor("", null);

            var commandModule = CommandModuleMock.Object;
            var subCommandModule = SubCommandModuleMock.Object;
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
        public void RemovesCommandFromRole()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnrestrictCommand")).FirstOrDefault();
            var restrictions = CommandManager.RepositoryManager.Restrictions.Read();
            var testRole = CommandManager.RepositoryManager.AccessGroups.Read(x => x.Name.Equals("TestRole")).First();
            var commandToRemove = "CommandMock.Foo";
            var result = command.Executor($"{commandToRemove} TestRole", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.Contains("success", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(restrictions.Any(x => x.GroupId == testRole.Id && x.Command.Equals(commandToRemove)));
        }

        [TestMethod]
        public void RemoveCommandErrorsOnMissingParameters()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnrestrictCommand")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var wrongParameterCount = "BadInput";
            var result = command.Executor(wrongParameterCount, null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            var roleToRemove = "TestRole";
            var commandToRemove = "CommandMock.Foo";
            result = command.Executor($" {roleToRemove}", null);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(roleToRemove));
            result = command.Executor($"{commandToRemove} ", null);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(commandToRemove));
        }

        [TestMethod]
        public void RemoveCommandErrorsOnCommandNotAssigned()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnrestrictCommand")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var commandToRemove = "CommandMock.Unrestricted";
            var result = command.Executor($"{commandToRemove} TestRole", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(commandToRemove));
        }

        [TestMethod]
        public void RemoveCommandErrorsOnRoleNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnrestrictCommand")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var roleToRemove = "NotTestRole";
            var result = command.Executor($"CommandMock.Unrestricted {roleToRemove}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(roleToRemove));
        }

        [TestMethod]
        public void RemoveCommandErrorsOnCommandNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnrestrictCommand")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var commandToRemove = "CommandMock.None";
            var result = command.Executor($"{commandToRemove} TestRole", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(commandToRemove));
        }

    }
}