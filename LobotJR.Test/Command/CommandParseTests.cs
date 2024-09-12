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
        private MockCommandView CommandViewMock;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            CommandManager = AutofacMockSetup.Container.Resolve<ICommandManager>();
            CommandViewMock = AutofacMockSetup.Container.Resolve<MockCommandView>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            CommandViewMock.ResetCounts();
        }

        [TestMethod]
        public void ProcessMessageParsesSingleParameter()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} Foo", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageParsesMultipleParameters()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("MultiParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} Foo Bar", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo") && x.Contains("Bar")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.MultiParamCount);
        }

        [TestMethod]
        public void ProcessMessageParsesIntParameter()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("IntParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} 10", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("10")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.IntParamCount);
        }

        [TestMethod]
        public void ProcessMessageParsesBoolParameter()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("BoolParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} true", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("True")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.BoolParamCount);
        }

        [TestMethod]
        public void ProcessMessageIgnoresExtraWhitespace()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name}  Foo  ", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsQuotedSpaces()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} \"Foo Bar\"", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo Bar") && !x.Contains("\"")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsEscapedQuotes()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} \"Foo \\\"Bar\\\"\"", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo \"Bar\"")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageIgnoresEscapesOutsideQuotedStrings()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} Foo\\Bar", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo\\Bar")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageErrorsOnTooFewParameters()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Syntax")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(0, CommandViewMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageErrorsOnTooManyParameters()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} Foo Bar", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Syntax")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(0, CommandViewMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessagePassesUserObject()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("UserParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains(user.Username)));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.UserParamCount);
        }

        [TestMethod]
        public void ProcessMessagePassesParamsWithUserObject()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("UserAndStringParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} Foo", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains(user.Username) && x.Contains("Foo")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.UserAndStringParamCount);
        }

        [TestMethod]
        public void ProcessMessageDefaultsOptionalParameters()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("OptionalParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("default")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.OptionalParamCount);
        }

        [TestMethod]
        public void ProcessMessageDefaultsOptionalParametersWithUser()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("UserAndOptionalParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("default")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.OptionalParamCount);
        }

        [TestMethod]
        public void ProcessMessageErrorsOnInvalidType()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("IntParam")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} one", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Invalid")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(0, CommandViewMock.IntParamCount);
        }

        [TestMethod]
        public void ProcessMessagePassesEmptyStringOnIgnoreParseWithNoParams()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("NoParse")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("\"\"")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.NoParseCount);
        }

        [TestMethod]
        public void ProcessMessagePassesAllOnIgnoreParse()
        {
            var db = ConnectionManager.CurrentConnection;
            var command = CommandViewMock.Commands.Where(x => x.Name.Equals("NoParse")).FirstOrDefault();
            var user = db.Users.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} params with spaces", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("\"params with spaces\"")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandViewMock.NoParseCount);
        }
    }
}
