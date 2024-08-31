using Autofac;
using LobotJR.Data;
using LobotJR.Test.Mocks;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LobotJR.Test.Twitch
{
    [TestClass]
    public class WhisperQueueTests
    {
        private IConnectionManager ConnectionManager;
        private SettingsManager SettingsManager;

        [TestInitialize]
        public void Initialize()
        {
            ConnectionManager = AutofacMockSetup.Container.Resolve<IConnectionManager>();
            SettingsManager = AutofacMockSetup.Container.Resolve<SettingsManager>();
        }

        [TestMethod]
        public void EnqueueAddsWhispersToQueue()
        {
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 1, 1);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            var canSend = queue.TryGetMessage(out var toSend);
            Assert.IsTrue(canSend);
            Assert.IsTrue(toSend.Message.Equals("test"));
        }

        [TestMethod]
        public void GetMessagesRemovesWhispersFromQueue()
        {
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 1, 1);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            queue.TryGetMessage(out var _);
            var canSend = queue.TryGetMessage(out var toSend);
            Assert.IsFalse(canSend);
            Assert.IsNull(toSend);
        }

        [TestMethod]
        public void GetMessagesRespectsPerSecondLimit()
        {
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 1, 10);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            queue.Enqueue(new User("Test", "0"), "fail", DateTime.Now + TimeSpan.FromMilliseconds(1));
            var canSendFirst = queue.TryGetMessage(out var toSendFirst);
            queue.ReportSuccess(toSendFirst);
            var canSendSecond = queue.TryGetMessage(out var toSendSecond);
            Assert.IsTrue(canSendFirst);
            Assert.IsTrue(toSendFirst.Message.Equals("test"));
            Assert.IsFalse(canSendSecond);
            Assert.IsNull(toSendSecond);
        }

        [TestMethod]
        public void GetMessagesRespectsPerMinuteLimit()
        {
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 10, 1);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            queue.Enqueue(new User("Test", "0"), "fail", DateTime.Now + TimeSpan.FromMilliseconds(1));
            var canSendFirst = queue.TryGetMessage(out var toSendFirst);
            queue.ReportSuccess(toSendFirst);
            var canSendSecond = queue.TryGetMessage(out var toSendSecond);
            Assert.IsTrue(canSendFirst);
            Assert.IsTrue(toSendFirst.Message.Equals("test"));
            Assert.IsFalse(canSendSecond);
            Assert.IsNull(toSendSecond);
        }

        [TestMethod]
        public void GetMessagesRespectsMaxRecipientLimit()
        {
            SettingsManager.GetAppSettings().MaxWhisperRecipients = 1;
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 10, 10);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            queue.Enqueue(new User("Second", "1"), "fail", DateTime.Now + TimeSpan.FromMilliseconds(1));
            var canSendFirst = queue.TryGetMessage(out var toSendFirst);
            queue.ReportSuccess(toSendFirst);
            var canSendSecond = queue.TryGetMessage(out var toSendSecond);
            Assert.IsTrue(canSendFirst);
            Assert.IsTrue(toSendFirst.Message.Equals("test"));
            Assert.IsFalse(canSendSecond);
            Assert.IsNull(toSendSecond);
        }

        [TestMethod]
        public void GetMessagesAllowsExistUsersWhenAtLimit()
        {
            SettingsManager.GetAppSettings().MaxWhisperRecipients = 1;
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 10, 10);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            queue.Enqueue(new User("Second", "1"), "fail", DateTime.Now + TimeSpan.FromMilliseconds(1));
            var canSendFirst = queue.TryGetMessage(out var toSendFirst);
            queue.ReportSuccess(toSendFirst);
            var canSendSecond = queue.TryGetMessage(out var toSendSecond);
            Assert.IsTrue(canSendFirst);
            Assert.IsTrue(toSendFirst.Message.Equals("test"));
            Assert.IsFalse(canSendSecond);
            Assert.IsNull(toSendSecond);
            queue.Enqueue(new User("Test", "0"), "test two", DateTime.Now + TimeSpan.FromMilliseconds(2));
            var canSendAgain = queue.TryGetMessage(out var toSendAgain);
            Assert.IsTrue(canSendAgain);
            Assert.IsTrue(toSendAgain.Message.Equals("test two"));
        }

        [TestMethod]
        public void GetMessagesExcludesMessagesWithNoUserId()
        {
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 1, 1);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", null), "test", DateTime.Now);
            var canSend = queue.TryGetMessage(out var toSend);
            Assert.IsFalse(canSend);
            Assert.IsNull(toSend);
        }

        [TestMethod]
        public void ReportSuccessAddsWhisperTimer()
        {
            var db = ConnectionManager.CurrentConnection;
            var timer = db.DataTimers.Read(x => x.Name.Equals("WhisperQueue")).First();
            db.DataTimers.Delete(timer);
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 1, 1);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            var canSend = queue.TryGetMessage(out var toSend);
            queue.ReportSuccess(toSend);
            timer = db.DataTimers.Read(x => x.Name.Equals("WhisperQueue")).First();
            Assert.IsNotNull(timer);
            Assert.IsTrue(timer.Timestamp <= DateTime.Now);
        }

        [TestMethod]
        public void ReportSuccessUpdatesWhisperTimer()
        {
            var db = ConnectionManager.CurrentConnection;
            var timer = db.DataTimers.Read(x => x.Name.Equals("WhisperQueue")).First();
            timer.Timestamp = DateTime.Now - TimeSpan.FromDays(2);
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 1, 1);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            var canSend = queue.TryGetMessage(out var toSend);
            queue.ReportSuccess(toSend);
            timer = db.DataTimers.Read(x => x.Name.Equals("WhisperQueue")).First();
            Assert.IsNotNull(timer);
            Assert.IsTrue(DateTime.Now - timer.Timestamp < TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void ReportSuccessAddsToRecipients()
        {
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 1, 1);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            queue.TryGetMessage(out var toSend);
            queue.ReportSuccess(toSend);
            Assert.IsTrue(queue.WhisperRecipients.Contains("0"));
            Assert.AreEqual(1, queue.WhisperRecipients.Count);
        }

        [TestMethod]
        public void ReportSuccessDoesNotAddDuplicateRecipients()
        {
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 10, 10);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            queue.TryGetMessage(out var toSend);
            queue.ReportSuccess(toSend);
            queue.Enqueue(new User("Test", "0"), "test 2", DateTime.Now);
            queue.TryGetMessage(out toSend);
            queue.ReportSuccess(toSend);
            Assert.IsTrue(queue.WhisperRecipients.Contains("0"));
            Assert.AreEqual(1, queue.WhisperRecipients.Count);
        }

        [TestMethod]
        public void ReportSuccessAddsToRollingTimers()
        {
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 2, 2);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            queue.TryGetMessage(out var toSend);
            queue.ReportSuccess(toSend);
            var secondAvailable = queue.SecondTimer.AvailableOccurrences();
            var minuteAvailable = queue.SecondTimer.AvailableOccurrences();
            Assert.AreEqual(1, secondAvailable);
            Assert.AreEqual(1, minuteAvailable);
        }

        [TestMethod]
        public void ReportSuccessClearsWhisperRecipientsOnRollover()
        {
            var db = ConnectionManager.CurrentConnection;
            var settings = SettingsManager.GetAppSettings();
            settings.MaxWhisperRecipients = 2;
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 10, 10);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "0"), "test", DateTime.Now);
            var canSend = queue.TryGetMessage(out var toSend);
            queue.ReportSuccess(toSend);
            queue.Enqueue(new User("Second", "1"), "test", DateTime.Now);
            canSend = queue.TryGetMessage(out toSend);
            var timer = db.DataTimers.Read(x => x.Name.Equals("WhisperQueue")).First();
            timer.Timestamp = DateTime.Now - TimeSpan.FromDays(2);
            db.DataTimers.Update(timer);
            db.DataTimers.Commit();
            queue.ReportSuccess(toSend);
            queue.Enqueue(new User("Third", "2"), "test", DateTime.Now);
            canSend = queue.TryGetMessage(out toSend);
            Assert.IsTrue(canSend);
            Assert.IsTrue(toSend.User.Username.Equals("Third"));
        }

        [TestMethod]
        public void FreezeQueuePreventsNewMessages()
        {
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 100, 100);
            queue.UpdateMaxRecipients();
            queue.FreezeQueue();
            queue.Enqueue(new User("Test", null), "test", DateTime.Now);
            var canSend = queue.TryGetMessage(out var toSend);
            Assert.IsFalse(canSend);
            Assert.IsNull(toSend);
        }

        [TestMethod]
        public void FreezeQueueUpdatesMaxRecipients()
        {
            var queue = new WhisperQueue(ConnectionManager, SettingsManager, 100, 100);
            queue.UpdateMaxRecipients();
            queue.Enqueue(new User("Test", "01"), "test", DateTime.Now);
            queue.TryGetMessage(out var toSend);
            queue.ReportSuccess(toSend);
            queue.FreezeQueue();
            var settings = SettingsManager.GetAppSettings();
            Assert.AreEqual(1, settings.MaxWhisperRecipients);
        }
    }
}
