using LobotJR.Command;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.View;
using LobotJR.Command.View.AccessControl;
using LobotJR.Test.Command;

namespace LobotJR.Test.Modules.AccessControl
{
    public abstract class AccessControlAdminBase : CommandManagerTestBase
    {
        protected AccessControlAdmin Module;

        public void InitializeAccessControlModule()
        {
            InitializeCommandManager();
            var userController = new UserController(RepositoryManagerMock.Object, null);
            Module = new AccessControlAdmin(Manager, userController);
            CommandManager = new CommandManager(new ICommandView[] { CommandModuleMock, SubCommandModuleMock, Module }, RepositoryManagerMock.Object, userController);
            CommandManager.InitializeViews();
        }
    }
}
