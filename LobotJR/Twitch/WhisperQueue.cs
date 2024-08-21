using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Twitch
{
    /// <summary>
    /// This class handles the whisper queue, ensuring messages can be sent as
    /// quickly as possible while still conforming to the twitch rate limits.
    /// </summary>
    public class WhisperQueue
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly string TimerKey = "WhisperQueue";
        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;
        private TimeSpan UniqueWhisperTimer = TimeSpan.FromDays(1);
        private int MaxRecipients;

        /// <summary>
        /// A collection of whisper records waiting to be sent.
        /// </summary>
        public List<WhisperRecord> Queue { get; set; } = new List<WhisperRecord>();
        /// <summary>
        /// The timer that ensures the queue doesn't send messages that exceed
        /// the twitch limit of 3 whispers per second.
        /// </summary>
        public RollingTimer SecondTimer { get; set; }
        /// <summary>
        /// The timer that ensure the queue doesn't send messages that exceed
        /// the twitch limit of 100 whispers per minute.
        /// </summary>
        public RollingTimer MinuteTimer { get; set; }
        /// <summary>
        /// The ids of every recipient of a whisper sent. This is used to ensure we do not exceed the limit on unique recipents of 40 per day.
        /// </summary>
        public HashSet<string> WhisperRecipients { get; set; } = new HashSet<string>();

        public WhisperQueue(IConnectionManager connectionManager, SettingsManager settingsManager, int maxPerSecond, int maxPerMinute)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
            SecondTimer = new RollingTimer(TimeSpan.FromSeconds(1), maxPerSecond);
            MinuteTimer = new RollingTimer(TimeSpan.FromMinutes(1), maxPerMinute);
        }

        /// <summary>
        /// Updates the max recipient setting. This requires an open database
        /// connection.
        /// </summary>
        public void UpdateMaxRecipients()
        {
            var currentSettings = SettingsManager.GetAppSettings();
            MaxRecipients = currentSettings.MaxWhisperRecipients;
            if (MaxRecipients == 0)
            {
                MaxRecipients = int.MaxValue;
            }
        }

        /// <summary>
        /// Adds a message to the whisper queue.
        /// </summary>
        /// <param name="user">The user object of the user to send to.</param>
        /// <param name="message">The content of the message to send.</param>
        /// <param name="dateTime">The time the message was queued.</param>
        public void Enqueue(User user, string message, DateTime dateTime)
        {
            var allowed = WhisperRecipients.Contains(user.TwitchId) || WhisperRecipients.Count < MaxRecipients;
            if (allowed)
            {
                Queue.Add(new WhisperRecord(user, message, dateTime));
            }
            else
            {
                Logger.Warn("Failed to queue whisper. {details}", Debug());
            }
        }

        /// <summary>
        /// Gets the messages from the queue that need to be sent, and removes
        /// them from the queue.
        /// </summary>
        /// <returns>A collection of records that should be sent.</returns>
        public bool TryGetMessage(out WhisperRecord record)
        {
            record = null;
            var canSend = SecondTimer.AvailableOccurrences() > 0 && MinuteTimer.AvailableOccurrences() > 0;
            if (canSend)
            {
                if (WhisperRecipients.Count < MaxRecipients)
                {
                    record = Queue.Where(x => !string.IsNullOrWhiteSpace(x.User?.TwitchId)).OrderBy(x => x.QueueTime).FirstOrDefault();
                }
                else
                {
                    record = Queue.Where(x => !string.IsNullOrWhiteSpace(x.User?.TwitchId) && WhisperRecipients.Contains(x.User?.TwitchId)).OrderBy(x => x.QueueTime).FirstOrDefault();
                }
                if (record != null)
                {
                    Queue.Remove(record);
                    return true;
                }
                else if (Queue.Any(x => !string.IsNullOrWhiteSpace(x.User?.TwitchId)))
                {
                    Logger.Warn("Failed to fetch message from queue despite queue containing messages to send. Cleaning up whisper queue.");
                    Logger.Debug("Current whisper recipients: {recipients}", string.Join(", ", WhisperRecipients));
                    Logger.Debug("Current whisper queue: ");
                    foreach (var item in Queue)
                    {
                        Logger.Debug("  To {username} ({userid}): {message}", item.User?.Username, item.User?.TwitchId, item.Message);
                    }
                    var toRemove = Queue.Where(x => !WhisperRecipients.Contains(x.User?.TwitchId));
                    Queue = Queue.Except(toRemove).ToList();
                }
            }
            return false;
        }

        /// <summary>
        /// Reports that a whisper was successfully sent and updates the
        /// various rate limiters.
        /// </summary>
        /// <param name="record">The record that was sent.</param>
        public void ReportSuccess(WhisperRecord record)
        {
            var dataTimer = ConnectionManager.CurrentConnection.DataTimers.Read(x => x.Name.Equals(TimerKey)).FirstOrDefault();
            var timerUpdated = false;
            if (dataTimer == null)
            {
                dataTimer = new DataTimer() { Name = TimerKey, Timestamp = DateTime.Now };
                ConnectionManager.CurrentConnection.DataTimers.Create(dataTimer);
                timerUpdated = true;
            }
            else if (DateTime.Now > dataTimer.Timestamp + UniqueWhisperTimer)
            {
                dataTimer.Timestamp = DateTime.Now;
                ConnectionManager.CurrentConnection.DataTimers.Update(dataTimer);
                timerUpdated = true;
            }
            if (timerUpdated)
            {
                ConnectionManager.CurrentConnection.DataTimers.Commit();
                WhisperRecipients.Clear();
            }

            MinuteTimer.AddOccurrence(DateTime.Now);
            SecondTimer.AddOccurrence(DateTime.Now);
            if (!string.IsNullOrWhiteSpace(record.User?.TwitchId))
            {
                WhisperRecipients.Add(record.User?.TwitchId);
            }
            else
            {
                Logger.Warn("Whisper sucessfully sent to null user id. This shouldn't be possible.");
            }
        }

        /// <summary>
        /// Floods the minute timer to freeze the queue for one minute.
        /// </summary>
        public void FreezeQueue()
        {
            var now = DateTime.Now;
            for (var i = 0; i < MinuteTimer.MaxHits; i++)
            {
                MinuteTimer.AddOccurrence(now);
            }
            MaxRecipients = WhisperRecipients.Count;
            var currentSettings = SettingsManager.GetAppSettings();
            currentSettings.MaxWhisperRecipients = MaxRecipients;
            var toRemove = Queue.Where(x => !WhisperRecipients.Contains(x.User?.TwitchId));
            Queue = Queue.Except(toRemove).ToList();
        }

        /// <summary>
        /// Returns debug information about the state of the queue.
        /// </summary>
        /// <returns>An output string containing debug information.</returns>
        public string Debug()
        {
            return $"The current user list contains {WhisperRecipients.Count} entries. The minute timer contains {MinuteTimer.CurrentHitCount()} hits, and the second timer contains {SecondTimer.CurrentHitCount()} hits. There are {Queue.Count} messages in the queue.";
        }
    }
}
