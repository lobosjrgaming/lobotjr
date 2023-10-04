using LobotJR.Twitch.Model;
using System.Text.RegularExpressions;

namespace LobotJR.Trigger
{
    public interface ITriggerResponder
    {
        Regex Pattern { get; }
        TriggerResult Process(Match match, User user);
    }
}
