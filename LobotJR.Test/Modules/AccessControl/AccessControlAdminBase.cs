using LobotJR.Command;
using LobotJR.Command.Module;
using LobotJR.Command.Module.AccessControl;
using LobotJR.Command.System.Twitch;
using LobotJR.Test.Command;

namespace LobotJR.Test.Modules.AccessControl
{
    public abstract class AccessControlAdminBase : CommandManagerTestBase
    {
        protected AccessControlAdmin Module;

        public void InitializeAccessControlModule()
        {
            InitializeCommandManager();
            var userSystem = new UserSystem(RepositoryManagerMock.Object, null);
            Module = new AccessControlAdmin(Manager, userSystem);
            CommandManager = new CommandManager(new ICommandModule[] { CommandModuleMock, SubCommandModuleMock, Module }, RepositoryManagerMock.Object, userSystem);
            CommandManager.InitializeModules();
        }
    }
}
