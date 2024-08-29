using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LobotJR.Trigger.Responder
{
    public class BadLobot : ITriggerResponder
    {
        public Regex Pattern { get; private set; } = new Regex("^.*(?:[Bb]ad|[Dd]amn it|[Ss]tupid) lobot.*$", RegexOptions.IgnoreCase);

        private readonly Random Random = new Random();
        private readonly IEnumerable<string> DevResponses = new List<string>()
        {
            "Sorry mistress! lobosS",
            "I'm sorry lobosCry",
            "Uh oh... lobosVanish"
        };
        private readonly IEnumerable<string> ModResponses = new List<string>()
        {
            "Whatever...",
            "lobosK",
            "What are you gonna do about it? lobosLaugh"
        };
        private readonly IEnumerable<string> AdminResponses = new List<string>()
        {
            "Leave me alone, dad!",
            "But mom said I could!",
        };

        public TriggerResult Process(Match match, User user)
        {
            if (user.Username.Equals("empyrealhell", StringComparison.OrdinalIgnoreCase)
            || user.Username.Equals("celesteenfer", StringComparison.OrdinalIgnoreCase))
            {
                return new TriggerResult()
                {
                    Messages = new string[] { Random.RandomElement(DevResponses) }
                };
            }
            else if (user.IsAdmin)
            {
                return new TriggerResult()
                {
                    Messages = new string[] { Random.RandomElement(AdminResponses) }
                };
            }
            else if (user.IsMod)
            {
                return new TriggerResult()
                {
                    Messages = new string[] { Random.RandomElement(ModResponses) }
                };
            }
            return null;
        }
    }
}
