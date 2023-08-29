﻿using LobotJR.Data;
using LobotJR.Data.User;
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
        private IRepository<DataTimer> DataTimers;
        private IRepository<AppSettings> AppSettings;
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

        public WhisperQueue(IRepositoryManager repositoryManager, int maxPerSecond, int maxPerMinute)
        {
            AppSettings = repositoryManager.AppSettings;
            var currentSettings = AppSettings.Read().First();
            DataTimers = repositoryManager.DataTimers;
            SecondTimer = new RollingTimer(TimeSpan.FromSeconds(1), maxPerSecond);
            MinuteTimer = new RollingTimer(TimeSpan.FromMinutes(1), maxPerMinute);
            MaxRecipients = currentSettings.MaxWhisperRecipients;
            if (MaxRecipients == 0)
            {
                MaxRecipients = int.MaxValue;
            }
        }

        /// <summary>
        /// Adds a message to the whisper queue.
        /// </summary>
        /// <param name="user">The name of the user to send to.</param>
        /// <param name="userId">The Twitch id of the user to send to.</param>
        /// <param name="message">The content of the message to send.</param>
        /// <param name="dateTime">The time the message was queued.</param>
        public void Enqueue(string user, string userId, string message, DateTime dateTime)
        {
            var allowed = WhisperRecipients.Contains(userId) || WhisperRecipients.Count < MaxRecipients;
            if (allowed)
            {
                Queue.Add(new WhisperRecord(user, userId, message, dateTime));
            }
            else
            {
                Logger.Warn("Failed to queue whisper. {details}", Debug());
            }
        }

        /// <summary>
        /// Updates all queued messages with no user id. Any messages from
        /// users that still have no id will be removed from the queue.
        /// </summary>
        /// <param name="userLookup">The UserLookup object to use to fetch new
        /// user ids.</param>
        public void UpdateUserIds(UserLookup userLookup)
        {
            var nullIds = Queue.Where(x => string.IsNullOrWhiteSpace(x.UserId)).ToList();
            nullIds.ForEach(x => x.UserId = userLookup.GetId(x.Username));
            nullIds = Queue.Where(x => string.IsNullOrWhiteSpace(x.UserId)).ToList();
            Queue = Queue.Except(nullIds).ToList();
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
                    record = Queue.Where(x => !string.IsNullOrWhiteSpace(x.UserId)).OrderBy(x => x.QueueTime).FirstOrDefault();
                }
                else
                {
                    record = Queue.Where(x => !string.IsNullOrWhiteSpace(x.UserId) && WhisperRecipients.Contains(x.UserId)).OrderBy(x => x.QueueTime).FirstOrDefault();
                }
                if (record != null)
                {
                    Queue.Remove(record);
                    return true;
                }
                else if (Queue.Any(x => !string.IsNullOrWhiteSpace(x.UserId)))
                {
                    Logger.Warn("Failed to fetch message from queue despite queue containing messages to send. Cleaning up whisper queue.");
                    Logger.Debug("Current whisper recipients: {recipients}", string.Join(", ", WhisperRecipients));
                    Logger.Debug("Current whisper queue: ");
                    foreach (var item in Queue)
                    {
                        Logger.Debug("  To {username} ({userid}): {message}", item.Username, item.UserId, item.Message);
                    }
                    var toRemove = Queue.Where(x => !WhisperRecipients.Contains(x.UserId));
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
            var dataTimer = DataTimers.Read(x => x.Name.Equals(TimerKey)).FirstOrDefault();
            var timerUpdated = false;
            if (dataTimer == null)
            {
                dataTimer = new DataTimer() { Name = TimerKey, Timestamp = DateTime.Now };
                DataTimers.Create(dataTimer);
                timerUpdated = true;
            }
            else if (DateTime.Now > dataTimer.Timestamp + UniqueWhisperTimer)
            {
                dataTimer.Timestamp = DateTime.Now;
                DataTimers.Update(dataTimer);
                timerUpdated = true;
            }
            if (timerUpdated)
            {
                DataTimers.Commit();
                WhisperRecipients.Clear();
            }

            MinuteTimer.AddOccurrence(DateTime.Now);
            SecondTimer.AddOccurrence(DateTime.Now);
            if (record.UserId != null)
            {
                WhisperRecipients.Add(record.UserId);
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
            var currentSettings = AppSettings.Read().First();
            currentSettings.MaxWhisperRecipients = MaxRecipients;
            AppSettings.Update(currentSettings);
            AppSettings.Commit();
            var toRemove = Queue.Where(x => !WhisperRecipients.Contains(x.UserId));
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
