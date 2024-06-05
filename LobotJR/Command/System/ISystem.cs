﻿using System.Threading.Tasks;

namespace LobotJR.Command.System
{
    /// <summary>
    /// Describes a system that processes the logic of a module continuously,
    /// not just in response to commands from users.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// Called once per frame to process the logic of a system.
        /// </summary>
        Task Process();
    }
}
