using System;

namespace LobotJR.Twitch.Model
{
    /// <summary>
    /// A record of a whisper to be sent.
    /// </summary>
    public class WhisperRecord
    {
        /// <summary>
        /// The user object of the user to send to.
        /// </summary>
        public User User { get; set; }
        /// <summary>
        /// The content of the whisper.
        /// </summary>
        public string Message { get; private set; }
        /// <summary>
        /// The time the message was queued.
        /// </summary>
        public DateTime QueueTime { get; private set; }

        public WhisperRecord(User user, string message, DateTime queueTime)
        {
            User = user;
            Message = message;
            QueueTime = queueTime;
        }

        public override bool Equals(object obj)
        {
            return obj is WhisperRecord other
                && string.Equals(User?.TwitchId, other.User?.TwitchId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Message, other.Message, StringComparison.OrdinalIgnoreCase)
                && QueueTime.Equals(other.QueueTime);
        }

        private int GetStringHash(string str)
        {
            return str == null ? 0 : str.GetHashCode();
        }

        public override int GetHashCode()
        {
            var hash = GetStringHash(User.TwitchId) * 17;
            hash = (hash + GetStringHash(Message)) * 17;
            hash = (hash + QueueTime.GetHashCode()) * 17;
            return hash;
        }
    }
}
