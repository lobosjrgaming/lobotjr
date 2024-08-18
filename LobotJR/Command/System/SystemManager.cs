using System.Collections.Generic;
using System.Threading.Tasks;

namespace LobotJR.Command.System
{
    /// <summary>
    /// Loads and manages the various systems.
    /// </summary>
    public class SystemManager : ISystemManager
    {
        /// <summary>
        /// Collection of all loaded systems.
        /// </summary>
        private IEnumerable<ISystemProcess> Systems { get; set; }
        /// <summary>
        /// Collection of all loaded systems.
        /// </summary>
        private IEnumerable<IDatabaseInitialize> SystemsToInitialize { get; set; }

        public SystemManager(IEnumerable<ISystemProcess> systems, IEnumerable<IDatabaseInitialize> initializeSystems)
        {
            Systems = systems;
            SystemsToInitialize = initializeSystems;
        }

        /// <summary>
        /// Initializes all systems that require database access during
        /// initalization.
        /// </summary>
        public void Initialize()
        {
            foreach (var system in SystemsToInitialize)
            {
                system.Initialize();
            }
        }

        /// <summary>
        /// Processes all loaded systems.
        /// </summary>
        public async Task Process()
        {
            foreach (var system in Systems)
            {
                await system.Process();
            }
        }
    }
}
