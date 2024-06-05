namespace LobotJR.Command
{
    /// <summary>
    /// Interface for modules that act on the command system itself.
    /// </summary>
    public interface IMetaModule
    {
        /// <summary>
        /// Entry point to inject fully resolved command manager into the
        /// module.
        /// </summary>
        ICommandManager CommandManager { set; }
    }
}
