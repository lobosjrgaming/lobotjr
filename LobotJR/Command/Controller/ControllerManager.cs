using System.Collections.Generic;
using System.Threading.Tasks;

namespace LobotJR.Command.Controller
{
    /// <summary>
    /// Loads and manages the various controllers.
    /// </summary>
    public class ControllerManager : IControllerManager
    {
        /// <summary>
        /// Collection of all loaded controllers.
        /// </summary>
        private IEnumerable<IProcessor> Controllers { get; set; }
        /// <summary>
        /// Collection of all loaded controllers.
        /// </summary>
        private IEnumerable<IDatabaseInitialize> ControllersToInitialize { get; set; }

        public ControllerManager(IEnumerable<IProcessor> controllers, IEnumerable<IDatabaseInitialize> initializeControllers)
        {
            Controllers = controllers;
            ControllersToInitialize = initializeControllers;
        }

        /// <summary>
        /// Initializes all controllers that require database access during
        /// initalization.
        /// </summary>
        public void Initialize()
        {
            foreach (var controller in ControllersToInitialize)
            {
                controller.Initialize();
            }
        }

        /// <summary>
        /// Processes all loaded controllers.
        /// </summary>
        public async Task Process()
        {
            foreach (var controller in Controllers)
            {
                await controller.Process();
            }
        }
    }
}
