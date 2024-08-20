namespace LobotJR.Command.Controller
{
    public interface IDatabaseInitialize
    {
        /// <summary>
        /// Code to initialize the controller. Any initialization code that
        /// requires database access should be put here, since there is no open
        /// connection to the database when the constructor is called.
        /// </summary>
        void Initialize();
    }
}
