using System.Threading.Tasks;

namespace LobotJR.Command.Controller
{
    public interface IControllerManager
    {
        /// <summary>
        /// Initializes all controllers that require database access during
        /// initalization.
        /// </summary>
        void Initialize();
        /// <summary>
        /// Processes all loaded controllers.
        /// </summary>
        Task Process();
    }
}
