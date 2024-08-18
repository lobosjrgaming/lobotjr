namespace LobotJR.Command.System
{
    public interface IDatabaseInitialize
    {
        /// <summary>
        /// Code to initialize the system. Any initialization code that
        /// requires database access should be put here, since there is no open
        /// connection to the database when the constructor is called.
        /// </summary>
        void Initialize();
    }
}
