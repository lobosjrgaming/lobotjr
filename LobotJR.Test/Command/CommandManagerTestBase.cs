using LobotJR.Command;
using LobotJR.Command.Module;
using LobotJR.Command.System.Twitch;
using LobotJR.Data;
using LobotJR.Test.Mocks;
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
    public abstract class CommandManagerTestBase : ICommandModule
    {
        protected List<AccessGroup> AccessGroups;
        protected List<Enrollment> Enrollments;
        protected List<Restriction> Restrictions;
        protected List<User> IdCache;
        protected CommandManager CommandManager;
        protected IRepositoryManager Manager;

        protected MockCommandModule CommandModuleMock;
        protected MockCommandSubModule SubCommandModuleMock;
        protected Mock<IRepositoryManager> RepositoryManagerMock;
        protected Mock<IRepository<User>> UserMock;
        protected Mock<IRepository<AccessGroup>> AccessGroupMock;
        protected Mock<IRepository<Enrollment>> EnrollmentMock;
        protected Mock<IRepository<Restriction>> RestrictionMock;
        protected Mock<IRepository<AppSettings>> AppSettingsMock;

        public string Name => "CommandManagerTest";

        public IEnumerable<CommandHandler> Commands => new List<CommandHandler>();

        public event PushNotificationHandler PushNotification;

        private Mock<IRepository<T>> CreateListRepositoryMock<T>(IList<T> list) where T : TableObject
        {
            var listMock = new Mock<IRepository<T>>();
            listMock.Setup(x => x.Read()).Returns(list);
            listMock.Setup(x => x.Read(It.IsAny<Func<T, bool>>()))
                .Returns((Func<T, bool> param) => list.Where(param));
            listMock.Setup(x => x.Create(It.IsAny<T>()))
                .Returns((T param) => { list.Add(param); return param; });
            listMock.Setup(x => x.Update(It.IsAny<T>()))
                .Returns((T param) => { list.Remove(list.Where(x => x.Id == param.Id).FirstOrDefault()); list.Add(param); return param; });
            listMock.Setup(x => x.Delete(It.IsAny<T>()))
                .Returns((T param) => { list.Remove(list.Where(x => x.Id == param.Id).FirstOrDefault()); return param; });
            return listMock;
        }

        /// <summary>
        /// Initializes a command manager object with all internals mocked out.
        /// This allows for testing without regard to the commands actually
        /// implemented, and without any need for sql connections or static
        /// data.
        /// </summary>
        public void InitializeCommandManager()
        {
            SubCommandModuleMock = new MockCommandSubModule();
            CommandModuleMock = new MockCommandModule();

            AccessGroups = new List<AccessGroup>(new AccessGroup[] { new AccessGroup(1, "TestGroup"),
                new AccessGroup(2, "ModGroup") { IncludeMods = true },
                new AccessGroup(3, "VipGroup") { IncludeVips = true },
                new AccessGroup(4, "SubGroup") { IncludeSubs = true },
                new AccessGroup(5, "AdminGroup") { IncludeAdmins = true },
            });
            Enrollments = new List<Enrollment>(new Enrollment[] { new Enrollment(1, "12345") });
            Restrictions = new List<Restriction>(new Restriction[] {
                new Restriction(1, "CommandMock.Foo"),
                new Restriction(2, "CommandMock.ModFoo"),
                new Restriction(3, "CommandMock.VipFoo"),
                new Restriction(4, "CommandMock.SubFoo"),
                new Restriction(5, "CommandMock.AdminFoo"),
            });
            IdCache = new List<User>(new User[]
            {
                new User() { TwitchId = "12345", Username = "Auth" },
                new User() { TwitchId = "67890", Username = "NotAuth" },
                new User() { TwitchId = "1", Username = "Mod", IsMod = true },
                new User() { TwitchId = "2", Username = "Vip", IsVip = true },
                new User() { TwitchId = "3", Username = "Sub", IsSub = true },
                new User() { TwitchId = "4", Username = "Admin", IsAdmin = true }
            });
            UserMock = CreateListRepositoryMock(IdCache);
            AccessGroupMock = CreateListRepositoryMock(AccessGroups);
            EnrollmentMock = CreateListRepositoryMock(Enrollments);
            RestrictionMock = CreateListRepositoryMock(Restrictions);
            var Settings = new List<AppSettings>(new AppSettings[] { new AppSettings() });
            AppSettingsMock = new Mock<IRepository<AppSettings>>();
            AppSettingsMock.Setup(x => x.Read()).Returns(Settings);
            RepositoryManagerMock = new Mock<IRepositoryManager>();
            RepositoryManagerMock.Setup(x => x.Users).Returns(UserMock.Object);
            RepositoryManagerMock.Setup(x => x.AccessGroups).Returns(AccessGroupMock.Object);
            RepositoryManagerMock.Setup(x => x.Enrollments).Returns(EnrollmentMock.Object);
            RepositoryManagerMock.Setup(x => x.Restrictions).Returns(RestrictionMock.Object);
            RepositoryManagerMock.Setup(x => x.AppSettings).Returns(AppSettingsMock.Object);
            Manager = RepositoryManagerMock.Object;


            var userLookup = new UserSystem(RepositoryManagerMock.Object, null);
            CommandManager = new CommandManager(new ICommandModule[] { CommandModuleMock, SubCommandModuleMock }, RepositoryManagerMock.Object, userLookup);
            CommandManager.InitializeModules();
        }
    }
}
