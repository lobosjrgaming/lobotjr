using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LobotJR.Test.Command
{
    delegate void NoParamDelegate();
    delegate void StringDelegate(string arg1);
    delegate void IntDelegate(int arg1);

    [TestClass]
    public class CommandParseTests : CommandManagerTestBase
    {
        [TestInitialize]
        public void Initialize()
        {
            InitializeCommandManager();
        }

        [TestMethod]
        public void ProcessMessageParsesSingleParameter()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} Foo", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageParsesMultipleParameters()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("MultiParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} Foo Bar", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo") && x.Contains("Bar")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.MultiParamCount);
        }

        [TestMethod]
        public void ProcessMessageParsesIntParameter()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("IntParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} 10", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("10")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.IntParamCount);
        }

        [TestMethod]
        public void ProcessMessageParsesBoolParameter()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("BoolParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} true", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("True")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.BoolParamCount);
        }

        [TestMethod]
        public void ProcessMessageIgnoresExtraWhitespace()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name}  Foo  ", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsQuotedSpaces()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} \"Foo Bar\"", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo Bar") && !x.Contains("\"")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageAllowsEscapedQuotes()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} \"Foo \\\"Bar\\\"\"", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo \"Bar\"")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageIgnoresEscapesOutsideQuotedStrings()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} Foo\\Bar", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Foo\\Bar")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageErrorsOnTooFewParameters()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Syntax")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(0, CommandModuleMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessageErrorsOnTooManyParameters()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("SingleParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} Foo Bar", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Syntax")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(0, CommandModuleMock.SingleParamCount);
        }

        [TestMethod]
        public void ProcessMessagePassesUserObject()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("UserParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains(user.Username)));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.UserParamCount);
        }

        [TestMethod]
        public void ProcessMessagePassesParamsWithUserObject()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("UserAndStringParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} Foo", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains(user.Username) && x.Contains("Foo")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.UserAndStringParamCount);
        }

        [TestMethod]
        public void ProcessMessageDefaultsOptionalParameters()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("OptionalParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name}", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("default")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(1, CommandModuleMock.OptionalParamCount);
        }

        [TestMethod]
        public void ProcessMessageErrorsOnInvalidType()
        {
            var command = CommandModuleMock.Commands.Where(x => x.Name.Equals("IntParam")).FirstOrDefault();
            var user = UserMock.Object.Read().First();
            var result = CommandManager.ProcessMessage($"{command.Name} one", user, true);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Responses.Any(x => x.Contains("Invalid")));
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(0, CommandModuleMock.IntParamCount);
        }

        //TODO: Add tests for dynamic invoke parse system
        //Check error handling for missing params, too many params, and invalid type cast params
    }
}
