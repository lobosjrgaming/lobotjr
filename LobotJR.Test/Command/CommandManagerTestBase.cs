using LobotJR.Command;
using LobotJR.Command.Module;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Test.Command
{
    /// <summary>
    /// Class containing mocks for the command manager class. Due to the
    /// complexity of mocking the command manager, this class provides a simple
    /// base to add a mocked out command manager to tests.
    /// 
    /// Due to the number of internal variables exposed, this is presented as
    /// an abstract class to be extended. This removes the need to re-establish
    /// these variables in each test class.
    /// </summary>
    public abstract class CommandManagerTestBase
    {
        protected List<AccessGroup> UserRoles;
        protected List<User> IdCache;
        protected IEnumerable<CommandHandler> CommandHandlers;
        protected IEnumerable<CommandHandler> SubCommandHandlers;
        protected CommandManager CommandManager;
        protected IRepositoryManager Manager;

        protected Dictionary<string, Mock<CommandExecutor>> ExecutorMocks;
        protected Mock<ICommandModule> CommandModuleMock;
        protected Mock<ICommandModule> SubCommandModuleMock;
        protected Mock<IRepositoryManager> RepositoryManagerMock;
        protected Mock<IRepository<User>> UserMapMock;
        protected Mock<IRepository<AccessGroup>> UserRoleMock;
        protected Mock<IRepository<AppSettings>> AppSettingsMock;

        /// <summary>
        /// Initializes a command manager object with all internals mocked out.
        /// This allows for testing without regard to the commands actually
        /// implemented, and without any need for sql connections or static
        /// data.
        /// </summary>
        public void InitializeCommandManager()
        {
            ExecutorMocks = new Dictionary<string, Mock<CommandExecutor>>();
            var commands = new string[] { "Foobar", "Foo", "Bar", "Unrestricted", "Public" };
            foreach (var command in commands)
            {
                var executorMock = new Mock<CommandExecutor>();
                executorMock.Setup(x => x(It.IsAny<string>(), It.IsAny<User>())).Returns(new CommandResult(new User(), ""));
                ExecutorMocks.Add(command, executorMock);
            }
            SubCommandHandlers = new CommandHandler[]
            {
                new CommandHandler("Foobar", ExecutorMocks["Foobar"].Object, "Foobar"),
            };
            SubCommandModuleMock = new Mock<ICommandModule>();
            SubCommandModuleMock.Setup(x => x.Name).Returns("CommandMock.SubMock");
            SubCommandModuleMock.Setup(x => x.Commands).Returns(SubCommandHandlers);


            CommandHandlers = new CommandHandler[] {
                new CommandHandler("Foo", ExecutorMocks["Foo"].Object, (data, user) =>
                {
                    var items = new string[]
                    {
                        string.IsNullOrWhiteSpace(data) ? "Bar" : data
                    };
                    return new CompactCollection<string>(items, x => $"Foo|{x};");
                }, "Foo"),
                new CommandHandler("Bar", ExecutorMocks["Bar"].Object, "Bar"),
                new CommandHandler("Unrestricted", ExecutorMocks["Unrestricted"].Object, "Unrestricted"),
                new CommandHandler("Public", ExecutorMocks["Public"].Object, "Public") { WhisperOnly = false }
            };
            CommandModuleMock = new Mock<ICommandModule>();
            CommandModuleMock.Setup(x => x.Name).Returns("CommandMock");
            CommandModuleMock.Setup(x => x.Commands).Returns(CommandHandlers);
            UserRoles = new List<AccessGroup>(new AccessGroup[]
            {
                new AccessGroup("TestRole",
                    new List<string>(new string[] { "12345" }),
                    new List<string>(new string[] { "CommandMock.Foo" }))
            });
            IdCache = new List<User>(new User[]
            {
                new User() { TwitchId = "12345", Username = "Auth" },
                new User() { TwitchId = "67890", Username = "NotAuth" }
            });
            UserMapMock = new Mock<IRepository<User>>();
            UserMapMock.Setup(x => x.Read()).Returns(IdCache);
            UserMapMock.Setup(x => x.Read(It.IsAny<Func<User, bool>>()))
                .Returns((Func<User, bool> param) => IdCache.Where(param));
            UserMapMock.Setup(x => x.Create(It.IsAny<User>()))
                .Returns((User param) => { IdCache.Add(param); return param; });
            UserMapMock.Setup(x => x.Update(It.IsAny<User>()))
                .Returns((User param) => { IdCache.Remove(IdCache.Where(x => x.TwitchId == param.TwitchId).FirstOrDefault()); IdCache.Add(param); return param; });
            UserMapMock.Setup(x => x.Delete(It.IsAny<User>()))
                .Returns((User param) => { IdCache.Remove(IdCache.Where(x => x.TwitchId == param.TwitchId).FirstOrDefault()); return param; });
            UserRoleMock = new Mock<IRepository<AccessGroup>>();
            UserRoleMock.Setup(x => x.Read()).Returns(UserRoles);
            UserRoleMock.Setup(x => x.Read(It.IsAny<Func<AccessGroup, bool>>()))
                .Returns((Func<AccessGroup, bool> param) => UserRoles.Where(param));
            UserRoleMock.Setup(x => x.Create(It.IsAny<AccessGroup>()))
                .Returns((AccessGroup param) => { UserRoles.Add(param); return param; });
            UserRoleMock.Setup(x => x.Update(It.IsAny<AccessGroup>()))
                .Returns((AccessGroup param) => { UserRoles.Remove(UserRoles.Where(x => x.Id == param.Id).FirstOrDefault()); UserRoles.Add(param); return param; });
            UserRoleMock.Setup(x => x.Delete(It.IsAny<AccessGroup>()))
                .Returns((AccessGroup param) => { UserRoles.Remove(UserRoles.Where(x => x.Id == param.Id).FirstOrDefault()); return param; });
            var Settings = new List<AppSettings>(new AppSettings[] { new AppSettings() });
            AppSettingsMock = new Mock<IRepository<AppSettings>>();
            AppSettingsMock.Setup(x => x.Read()).Returns(Settings);
            RepositoryManagerMock = new Mock<IRepositoryManager>();
            RepositoryManagerMock.Setup(x => x.Users).Returns(UserMapMock.Object);
            RepositoryManagerMock.Setup(x => x.AccessGroups).Returns(UserRoleMock.Object);
            RepositoryManagerMock.Setup(x => x.AppSettings).Returns(AppSettingsMock.Object);
            Manager = RepositoryManagerMock.Object;


            var userLookup = new UserSystem(RepositoryManagerMock.Object, null);
            CommandManager = new CommandManager(new ICommandModule[] { CommandModuleMock.Object, SubCommandModuleMock.Object }, RepositoryManagerMock.Object, userLookup);
            CommandManager.InitializeModules();
        }
    }
}
