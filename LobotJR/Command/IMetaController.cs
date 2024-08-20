namespace LobotJR.Command
{
    /// <summary>
    /// Interface for controllers that act on the command system itself.
    /// </summary>
    public interface IMetaController
    {
        /// <summary>
        /// Entry point to inject fully resolved command manager into the
        /// controller.
        /// </summary>
        ICommandManager CommandManager { set; }
    }
}
