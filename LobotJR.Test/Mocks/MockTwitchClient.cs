using LobotJR.Data;
using LobotJR.Shared.Channel;
using LobotJR.Shared.User;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Test.Mocks
{
    public class MockTwitchClient : ITwitchClient
    {
        private readonly IConnectionManager ConnectionManager;
        public Mock<ITwitchClient> Mock { get; private set; }

        public MockTwitchClient(IConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
            Mock = new Mock<ITwitchClient>();
            Mock.Setup(x => x.GetChatterListAsync())
                .Returns(() => { return Task.FromResult(TwitchDataFromUser(GetUsers(x => true))); });
            Mock.Setup(x => x.GetModeratorListAsync())
                .Returns(() => { return Task.FromResult(TwitchDataFromUser(GetUsers(x => x.Username.Equals("Mod")))); });
            Mock.Setup(x => x.GetSubscriberListAsync())
                .Returns(() => { return Task.FromResult(SubscriberDataFromUser(GetUsers(x => x.Username.Equals("Vip")))); });
            Mock.Setup(x => x.GetTwitchUsers(It.IsAny<IEnumerable<string>>()))
                .Returns((IEnumerable<string> users) =>
                {
                    var userObjects = GetUsers(x => users.Contains(x.Username, StringComparer.OrdinalIgnoreCase));
                    var userResponse = userObjects.Select(x => new UserResponseData() { DisplayName = x.Username, Login = x.Username, Id = x.TwitchId }).ToList();
                    var toCreate = users.Except(userObjects.Select(x => x.Username)).ToList();
                    for (var i = 0; i < toCreate.Count(); i++)
                    {
                        var creating = toCreate[i];
                        userResponse.Add(new UserResponseData() { DisplayName = creating, Login = creating, Id = (500 + i).ToString() });
                    }
                    return Task.FromResult<IEnumerable<UserResponseData>>(userResponse);
                });
        }

        private IEnumerable<User> GetUsers(Func<User, bool> predicate)
        {
            return ConnectionManager.CurrentConnection.Users.Read(predicate);
        }

        private IEnumerable<TwitchUserData> TwitchDataFromUser(IEnumerable<User> users)
        {
            return users.Select(x => new TwitchUserData() { UserId = x.TwitchId, UserLogin = x.Username, UserName = x.Username });
        }

        private IEnumerable<SubscriptionResponseData> SubscriberDataFromUser(IEnumerable<User> users)
        {
            return users.Select(x => new SubscriptionResponseData() { UserId = x.TwitchId, UserLogin = x.Username, UserName = x.Username });
        }

        public Task<IEnumerable<TwitchUserData>> GetChatterListAsync()
        {
            return Mock.Object.GetChatterListAsync();
        }

        public Task<IEnumerable<TwitchUserData>> GetModeratorListAsync()
        {
            return Mock.Object.GetModeratorListAsync();
        }

        public Task<IEnumerable<SubscriptionResponseData>> GetSubscriberListAsync()
        {
            return Mock.Object.GetSubscriberListAsync();
        }

        public Task<IEnumerable<UserResponseData>> GetTwitchUsers(IEnumerable<string> usernames)
        {
            return Mock.Object.GetTwitchUsers(usernames);
        }

        public Task<IEnumerable<TwitchUserData>> GetVipListAsync()
        {
            throw new System.NotImplementedException();
        }

        public void Initialize()
        {
            throw new System.NotImplementedException();
        }

        public Task ProcessQueue()
        {
            throw new System.NotImplementedException();
        }

        public void QueueWhisper(User user, string message)
        {
            throw new System.NotImplementedException();
        }

        public void QueueWhisper(IEnumerable<User> users, string message)
        {
            throw new System.NotImplementedException();
        }

        public Task RefreshTokens()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> TimeoutAsync(User user, int? duration, string message)
        {
            throw new System.NotImplementedException();
        }
    }
}
