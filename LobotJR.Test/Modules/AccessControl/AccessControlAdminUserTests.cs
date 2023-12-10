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
        public void AddsUserToGroup()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var enrollments = CommandManager.RepositoryManager.Enrollments.Read(x => x.GroupId == group.Id);
            var baseEnrollmentCount = enrollments.Count();
            var result = command.Executor("NotAuth TestGroup", null);
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
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var wrongParameterCount = "BadInput";
            var userToAdd = "NotAuth";
            var groupToAdd = "TestGroup";
            var result = command.Executor(wrongParameterCount, null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(wrongParameterCount));
            result = command.Executor($" {groupToAdd}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(groupToAdd));
            result = command.Executor($"{userToAdd} ", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses.Any(x => x.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(result.Responses[0].Contains(userToAdd));
        }

        [TestMethod]
        public void AddUserErrorsOnInvalidGroup()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var groupToAdd = "NotTestGroup";
            var result = command.Executor($"NotAuth {groupToAdd}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(groupToAdd));
        }

        [TestMethod]
        public void AddUserErrorsOnExistingAssignment()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("EnrollUser")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var userToAdd = "Auth";
            var result = command.Executor($"{userToAdd} TestGroup", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(userToAdd));
        }

        [TestMethod]
        public void RemovesUserFromGroup()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var userToRemove = CommandManager.UserSystem.GetUserByName("Auth").Username;
            var result = command.Executor($"{userToRemove} TestGroup", null);
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
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var wrongParameterCount = "BadInput";
            var userToRemove = "NotAuth";
            var groupToRemove = "TestGroup";
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
            result = command.Executor($"{groupToRemove} ", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Responses[0].Contains(groupToRemove));
        }

        [TestMethod]
        public void RemoveUserErrorsOnUserNotEnrolled()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var userToRemove = "NotAuth";
            var result = command.Executor($"{userToRemove} TestGroup", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(userToRemove));
        }

        [TestMethod]
        public void RemoveUserErrorsOnGroupNotFound()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("UnenrollUser")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var groupToRemove = "NotTestGroup";
            var result = command.Executor($"Auth {groupToRemove}", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].StartsWith("Error:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Responses[0].Contains(groupToRemove));
        }

        [TestMethod]
        public void SetGroupFlagSetsModFlag()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var result = command.Executor("mod true TestGroup", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now includes", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(group.IncludeMods);
        }

        [TestMethod]
        public void SetGroupFlagUnsetsModFlag()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            group.IncludeMods = true;
            var result = command.Executor("mod false TestGroup", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now does not include", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(group.IncludeMods);
        }

        [TestMethod]
        public void SetGroupFlagSetsVipFlag()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var result = command.Executor("vip true TestGroup", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now includes", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(group.IncludeVips);
        }

        [TestMethod]
        public void SetGroupFlagUnsetsVipFlag()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            group.IncludeVips = true;
            var result = command.Executor("vip false TestGroup", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now does not include", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(group.IncludeVips);
        }

        [TestMethod]
        public void SetGroupFlagSetsSubFlag()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var result = command.Executor("sub true TestGroup", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now includes", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(group.IncludeSubs);
        }

        [TestMethod]
        public void SetGroupFlagUnsetsSubFlag()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            group.IncludeSubs = true;
            var result = command.Executor("sub false TestGroup", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now does not include", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(group.IncludeSubs);
        }

        [TestMethod]
        public void SetGroupFlagSetsAdminFlag()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            var result = command.Executor("admin true TestGroup", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now includes", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(group.IncludeAdmins);
        }

        [TestMethod]
        public void SetGroupFlagUnsetsAdminFlag()
        {
            var command = Module.Commands.Where(x => x.Name.Equals("SetGroupFlag")).FirstOrDefault();
            var group = CommandManager.RepositoryManager.AccessGroups.Read().FirstOrDefault();
            group.IncludeAdmins = true;
            var result = command.Executor("admin false TestGroup", null);
            Assert.IsTrue(result.Processed);
            Assert.AreEqual(1, result.Responses.Count());
            Assert.IsTrue(result.Responses[0].Contains("now does not include", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(group.IncludeAdmins);
        }
    }
}