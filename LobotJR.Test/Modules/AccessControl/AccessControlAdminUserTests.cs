using LobotJR.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Modules.AccessControl
{
    [TestClass]
    public class AccessControlAdminUserTests : AccessControlAdminBase
    {
        [TestInitialize]
        public void Initialize()
        {
            InitializeAccessControlModule();
        }


        [TestMethod]
        public void AddsUserToRole()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.UserRoles.Read().FirstOrDefault();
            var baseIdCount = role.UserIds.Count;
            var result = command.Executor("NotAuth TestRole", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("success", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(baseIdCount + 1, role.UserIds.Count);
            Assert.IsTrue(role.UserIds.Contains(CommandManager.UserSystem.GetUserByName("NotAuth").TwitchId));
        }

        [TestMethod]
        public void AddUserErrorsOnMissingParameters()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.UserRoles.Read().FirstOrDefault();
            var wrongParameterCount = "BadInput";
            var userToAdd = "NotAuth";
            var roleToAdd = "TestRole";
            var result = command.Executor(wrongParameterCount, null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(wrongParameterCount));
            result = command.Executor($" {roleToAdd}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(roleToAdd));
            result = command.Executor($"{userToAdd} ", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(result.Responses[0].Contains(userToAdd));
        }

        [TestMethod]
        public void AddUserErrorsOnInvalidRole()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.UserRoles.Read().FirstOrDefault();
            var roleToAdd = "NotTestRole";
            var result = command.Executor($"NotAuth {roleToAdd}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(roleToAdd));
        }

        [TestMethod]
        public void AddUserErrorsOnExistingAssignment()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.UserRoles.Read().FirstOrDefault();
            var userToAdd = "Auth";
            var result = command.Executor($"{userToAdd} TestRole", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(userToAdd));
        }

        [TestMethod]
        public void RemovesUserFromRole()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.UserRoles.Read().FirstOrDefault();
            var userToRemove = "Auth";
            var result = command.Executor($"{userToRemove} TestRole", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("success", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(role.UserIds.Contains(CommandManager.UserSystem.GetUserByName("Foo").TwitchId));
        }

        [TestMethod]
        public void RemoveUserErrorsOnMissingParameters()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.UserRoles.Read().FirstOrDefault();
            var wrongParameterCount = "BadInput";
            var userToRemove = "NotAuth";
            var roleToRemove = "TestRole";
            var result = command.Executor(wrongParameterCount, null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(wrongParameterCount));
            result = command.Executor($" {userToRemove}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(userToRemove));
            result = command.Executor($"{roleToRemove} ", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(roleToRemove));
        }

        [TestMethod]
        public void RemoveUserErrorsOnUserNotEnrolled()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.UserRoles.Read().FirstOrDefault();
            var userToRemove = "NotAuth";
            var result = command.Executor($"{userToRemove} TestRole", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(userToRemove));
        }

        [TestMethod]
        public void RemoveUserErrorsOnRoleNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.UserRoles.Read().FirstOrDefault();
            var roleToRemove = "NotTestRole";
            var result = command.Executor($"Auth {roleToRemove}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(roleToRemove));
        }
    }
}