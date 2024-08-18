using System.Threading.Tasks;

namespace LobotJR.Command.System
{
    public interface ISystemManager
    {
        /// <summary>
        /// Initializes all systems that require database access during
        /// initalization.
        /// </summary>
        void Initialize();
        /// <summary>
        /// Processes all loaded systems.
        /// </summary>
        Task Process();
    }
}
