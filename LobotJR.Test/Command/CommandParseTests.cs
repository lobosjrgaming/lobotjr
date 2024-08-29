using Autofac;
using LobotJR.Command;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Command
{
    delegate void NoParamDelegate();
    delegate void StringDelegate(string arg1);
    delegate void IntDelegate(int arg1);

    [TestClass]
    public class CommandParseTests
    {
        private IConnectionManager ConnectionManager;
        private ICommandManager CommandManager;
        private MockCommandView CommandModuleMock;


        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            CommandManager = AutofacMockSetup.Container.Resolve<ICommandManager>();
            CommandModuleMock = AutofacMockSetup.Container.Resolve<MockCommandView>();
        }

        [TestMethod]
        public void ProcessMessageParsesSingleParameter()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name} Foo", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.SingleParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageParsesMultipleParameters()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("MultiParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name} Foo Bar", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo") && x.Contains("Bar")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.MultiParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageParsesIntParameter()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("IntParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name} 10", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("10")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.IntParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageParsesBoolParameter()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("BoolParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name} true", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("True")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.BoolParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageIgnoresExtraWhitespace()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name}  Foo  ", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.SingleParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageAllowsQuotedSpaces()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name} \"Foo Bar\"", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo Bar") && !x.Contains("\"")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.SingleParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageAllowsEscapedQuotes()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name} \"Foo \\\"Bar\\\"\"", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo \"Bar\"")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.SingleParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageIgnoresEscapesOutsideQuotedStrings()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name} Foo\\Bar", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo\\Bar")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.SingleParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageErrorsOnTooFewParameters()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("Syntax")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(0, CommandModuleMock.SingleParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageErrorsOnTooManyParameters()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name} Foo Bar", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("Syntax")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(0, CommandModuleMock.SingleParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessagePassesUserObject()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("UserParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains(user.Username)));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.UserParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessagePassesParamsWithUserObject()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("UserAndStringParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name} Foo", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains(user.Username) && x.Contains("Foo")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.UserAndStringParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageDefaultsOptionalParameters()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("OptionalParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("default")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.OptionalParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageDefaultsOptionalParametersWithUser()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("UserAndOptionalParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("default")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(1, CommandModuleMock.OptionalParamCount);
            }
        }

        [TestMethod]
        public void ProcessMessageErrorsOnInvalidType()
        {
            using (var db = ConnectionManager.OpenConnection())
            {
                var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("IntParam")).FirstOrDefault();
                var user = db.Users.Read().First();
                var result = CommandManager.ProcessMessage($"{command.Name} one", user, true);
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Responses.Any(x => x.Contains("Invalid")));
                Assert.AreEqual(0, result.Errors.Count());
                Assert.AreEqual(0, CommandModuleMock.IntParamCount);
            }
        }
    }
}
