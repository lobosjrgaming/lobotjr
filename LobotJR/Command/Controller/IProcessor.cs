using System.Threading.Tasks;

namespace LobotJR.Command.Controller
{
    /// <summary>
    /// Describes a controller that processes the logic of a module
    /// continuously, not just in response to commands from users.
    /// </summary>
    public interface IProcessor
    {
        /// <summary>
        /// Called once per frame to process the logic of a controller.
        /// </summary>
        Task Process();
    }
}
