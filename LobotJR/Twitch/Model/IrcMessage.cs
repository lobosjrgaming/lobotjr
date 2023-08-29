using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LobotJR.Twitch.Model
{
    /// <summary>
    /// An object describing an incoming message from the Twitch IRC server.
    /// </summary>
    public class IrcMessage
    {
        private static readonly Regex MessagePattern = new Regex("^(?<tags>(?:[^=]+=[^;]*;)*(?:[^=]+=[^;]*))?:(?:(?<user>[^.!]+)(?:!\\k<user>@\\k<user>\\.|\\.)?)?tmi\\.twitch\\.tv (?<command>[^:#]+?)(?: #?(?<channel>[^ :]+))?(?: :(?<message>.*))?$");
        // private static readonly Regex MessagePattern = new Regex("^(?<tags>.*):(?<user>[^!]+)!\\k<user>@\\k<user>\\.tmi\\.twitch\\.tv (?<command>[A-Z]+) #?(?<channel>[^ ]+)(?: :(?<message>.*))?$");
        private static readonly Regex PingPattern = new Regex("^(?<command>PING|PONG) :(?<message>.*)$");

        /// <summary>
        /// A collection of tags sent along with the message.
        /// </summary>
        public Dictionary<string, string> Tags { get; private set; }
        /// <summary>
        /// The name of the user who sent the message.
        /// </summary>
        public string UserName { get; private set; }
        /// <summary>
        /// The id of the user who sent the message.
        /// </summary>
        public string UserId { get; private set; }
        /// <summary>
        /// The IRC command that was sent.
        /// </summary>
        public string Command { get; private set; }
        /// <summary>
        /// The channel the message was sent to.
        /// </summary>
        public string Channel { get; private set; }
        /// <summary>
        /// The content of the message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// True if the message is an incoming whisper.
        /// </summary>
        public bool IsWhisper { get { return "whisper".Equals(Command, StringComparison.OrdinalIgnoreCase); } }
        /// <summary>
        /// True if the message is an incoming message in the broadcaster's chat.
        /// </summary>
        public bool IsChat { get { return "privmsg".Equals(Command, StringComparison.OrdinalIgnoreCase); } }
        /// <summary>
        /// True if the message is a usernotice alert.
        /// </summary>
        public bool IsUserNotice { get { return "usernotice".Equals(Command, StringComparison.OrdinalIgnoreCase); } }

        public static IrcMessage Parse(string message)
        {
            var content = MessagePattern.Match(message);
            if (content.Success)
            {
                var output = new IrcMessage();
                output.Tags = content.Groups["tags"].Value
                    .Split(';')
                    .Select(x => x.Split('='))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x[0], x => x[1]);
                string id = null;
                output.Tags.TryGetValue("user-id", out id);
                output.UserId = id;
                output.UserName = content.Groups["user"].Value;
                output.Command = content.Groups["command"].Value;
                output.Channel = content.Groups["channel"].Value;
                output.Message = content.Groups["message"].Value;
                return output;
            }
            else
            {
                content = PingPattern.Match(message);
                if (content.Success)
                {
                    var output = new IrcMessage();
                    output.Command = content.Groups["command"].Value;
                    output.Message = content.Groups["message"].Value;
                    return output;
                }
            }
            return null;
        }
    }
}
