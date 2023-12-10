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
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var enrollments = CommandManager.RepositoryManager.Enrollments.Read(x => x.GroupId == group.Id);
            var baseEnrollmentCount = enrollments.Count();
            var result = command.Executor("NotAuth TestRole", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("success", StringComparison.OrdinalIgnoreCase));
            enrollments = CommandManager.RepositoryManager.Enrollments.Read(x => x.GroupId == group.Id);
            Assert.AreEqual(baseEnrollmentCount + 1, enrollments.Count());
            var notAuth = CommandManager.UserSystem.GetUserByName("NotAuth");
            Assert.IsTrue(CommandManager.RepositoryManager.Enrollments.Read(x => x.GroupId == group.Id && x.UserId.Equals(notAuth.TwitchId)).Any());
        }

        [TestMethod]
        public void AddUserErrorsOnMissingParameters()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
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
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
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
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
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
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var userToRemove = CommandManager.UserSystem.GetUserByName("Auth").Username;
            var result = command.Executor($"{userToRemove} TestRole", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("success", StringComparison.OrdinalIgnoreCase));
            var enrollments = CommandManager.RepositoryManager.Enrollments.Read(x => x.UserId.Equals(userToRemove) && x.GroupId == group.Id);
            Assert.IsFalse(enrollments.Any());
        }

        [TestMethod]
        public void RemoveUserErrorsOnMissingParameters()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
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
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
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
            var role = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var roleToRemove = "NotTestRole";
            var result = command.Executor($"Auth {roleToRemove}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(roleToRemove));
        }
    }
}