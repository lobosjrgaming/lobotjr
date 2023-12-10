using LobotJR.Data;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LobotJR.Command
{
    /// <summary>
    /// Represents a command restricted to an access group.
    /// </summary>
    public class Restriction : TableObject
    {
        private static Dictionary<string, Regex> RegexMap = new Dictionary<string, Regex>();

        public static Regex RegexFromCommand(string command)
        {
            var commandString = command.Replace(".", "\\.").Replace("*", ".*");
            return new Regex($"^{commandString}$");
        }

        /// <summary>
        /// Checks a command to see if it's covered by a given command pattern.
        /// </summary>
        /// <param name="commandPattern">The restricted command string.</param>
        /// <param name="commandId">The id of the command to check.</param>
        /// <returns>Whether or not the command is covered.</returns>
        public static bool CoversCommand(string commandPattern, string commandId)
        {
            if (commandPattern.IndexOf('*') > -1)
            {
                if (!RegexMap.ContainsKey(commandPattern))
                {
                    RegexMap.Add(commandPattern, RegexFromCommand(commandPattern));
                }
                var regex = RegexMap[commandPattern];
                return regex.IsMatch(commandId);
            }
            else
            {
                return commandId.Equals(commandPattern, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// The id of the group this command is restricted to.
        /// </summary>
        public int GroupId { get; set; }
        /// <summary>
        /// The command that is restricted. May include * as a wildcard match.
        /// </summary>
        public string Command { get; set; }

        public Restriction() { }

        public Restriction(int groupId, string command)
        {
            GroupId = groupId;
            Command = command;
        }
    }
}
