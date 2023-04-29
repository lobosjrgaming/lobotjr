using System;

namespace LobotJR.Twitch
{
    /// <summary>
    /// A record of a whisper to be sent.
    /// </summary>
    public class WhisperRecord
    {
        /// <summary>
        /// The name of the user to send the whisper to.
        /// </summary>
        public string Username { get; private set; }
        /// <summary>
        /// The Twitch id of the user.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The content of the whisper.
        /// </summary>
        public string Message { get; private set; }
        /// <summary>
        /// The time the message was queued.
        /// </summary>
        public DateTime QueueTime { get; private set; }

        public WhisperRecord(string userName, string userId, string message, DateTime queueTime)
        {
            Username = userName;
            UserId = userId;
            Message = message;
            QueueTime = queueTime;
        }

        public override bool Equals(object obj)
        {
            var other = obj as WhisperRecord;
            return other != null
                && (string.Equals(Username, other.Username, StringComparison.OrdinalIgnoreCase))
                && (string.Equals(Message, other.Message, StringComparison.OrdinalIgnoreCase))
                && QueueTime.Equals(other.QueueTime);
        }

        private int GetStringHash(string str)
        {
            return str == null ? 0 : str.GetHashCode();
        }

        public override int GetHashCode()
        {
            var hash = GetStringHash(Username) * 17;
            hash = (hash + GetStringHash(Message)) * 17;
            hash = (hash + QueueTime.GetHashCode()) * 17;
            return hash;
        }
    }
}
