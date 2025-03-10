﻿using System;
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
        public static string InternalMessageId { get; private set; } = new Guid().ToString();
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
        /// <summary>
        /// True if the user who sent the message is a moderator on the channel
        /// the message was sent to.
        /// </summary>
        public bool IsMod { get { if (Tags.TryGetValue("mod", out var value)) { return value == "1"; } return false; } }
        /// <summary>
        /// True if the user who sent the message is a VIP on the channel the
        /// message was sent to.
        /// </summary>
        public bool IsVip { get { if (Tags.TryGetValue("vip", out var value)) { return value == "1"; } return false; } }
        /// <summary>
        /// True if the user who sent the message is a subscriber on the
        /// channel the message was sent to.
        /// </summary>
        public bool IsSub { get { if (Tags.TryGetValue("subscriber", out var value)) { return value == "1"; } return false; } }
        /// <summary>
        /// True if this message was sent directly through the UI, instead of
        /// going through Twitch.
        /// </summary>
        public bool IsInternal { get { if (Tags.TryGetValue("internal", out var value)) { return value.Equals(InternalMessageId); } return false; } }
        /// <summary>
        /// Indicates whether the message was sent on the local chat channel,
        /// or if it came across as part of a shared chat channel.
        /// </summary>
        public bool IsShared { get; private set; }

        public static IrcMessage Parse(string message)
        {
            var content = MessagePattern.Match(message);
            if (content.Success)
            {
                var output = new IrcMessage
                {
                    Tags = content.Groups["tags"].Value
                    .Split(';')
                    .Select(x => x.Split('='))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x[0], x => x[1])
                };
                output.Tags.TryGetValue("user-id", out string id);
                output.UserId = id;
                output.UserName = output.Tags.ContainsKey("display-name") ? output.Tags["display-name"] : content.Groups["user"].Value;
                if (output.Tags.ContainsKey("source-room-id") && output.Tags.ContainsKey("room-id"))
                {
                    output.IsShared = !output.Tags["source-room-id"].Equals(output.Tags["room-id"]);
                }
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
                    var output = new IrcMessage
                    {
                        Command = content.Groups["command"].Value,
                        Message = content.Groups["message"].Value
                    };
                    return output;
                }
            }
            return null;
        }

        public static IrcMessage Create(string message, string username, string userid)
        {
            return new IrcMessage
            {
                Message = message,
                UserName = username,
                UserId = userid,
                Command = "whisper",
                Channel = username,
                Tags = new Dictionary<string, string>() { { "internal", InternalMessageId } }
            };
        }
    }
}
