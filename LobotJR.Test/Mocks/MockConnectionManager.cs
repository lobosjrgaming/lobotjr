using LobotJR.Command;
using LobotJR.Command.Model.AccessControl;
using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using System;
using System.Linq;

namespace LobotJR.Test.Mocks
{
    public class MockConnectionManager : IConnectionManager
    {
        private readonly Random random = new Random();
        private MockContext Context;
        public IDatabase CurrentConnection { get; private set; }

        public IDatabase OpenConnection()
        {
            Context = MockContext.Create();
            CurrentConnection = new SqliteRepositoryManager(Context);
            return CurrentConnection;
        }

        private Fish CreateFish(int id, string name, string flavorText, int minLength, int maxLength, int minWeight, int maxWeight, int sizeId, string sizeName, string sizeMessage, int rarityId, string rarityName, float rarityWeight)
        {
            return new Fish()
            {
                Id = id,
                Name = name,
                FlavorText = flavorText,
                MinimumLength = minLength,
                MaximumLength = maxLength,
                MinimumWeight = minWeight,
                MaximumWeight = maxWeight,
                SizeCategory = new FishSize()
                {
                    Id = sizeId,
                    Name = sizeName,
                    Message = sizeMessage
                },
                Rarity = new FishRarity()
                {
                    Id = rarityId,
                    Name = rarityName,
                    Weight = rarityWeight
                }
            };
        }

        private void InitializeUsers(IDatabase context)
        {
            context.Users.Create(new User() { TwitchId = "01", Username = "Streamer", IsAdmin = true });
            context.Users.Create(new User() { TwitchId = "02", Username = "Bot", IsAdmin = true });
            context.Users.Create(new User() { TwitchId = "03", Username = "Dev" });
            context.Users.Create(new User() { TwitchId = "04", Username = "Mod", IsMod = true });
            context.Users.Create(new User() { TwitchId = "05", Username = "Sub", IsSub = true });
            context.Users.Create(new User() { TwitchId = "06", Username = "Vip", IsVip = true });
            context.Users.Create(new User() { TwitchId = "10", Username = "Foo" });
            context.Users.Create(new User() { TwitchId = "11", Username = "Bar" });
            context.Users.Create(new User() { TwitchId = "12", Username = "Fizz" });
            context.Users.Create(new User() { TwitchId = "13", Username = "Buzz" });
            context.Users.Create(new User() { TwitchId = "20", Username = "Super", IsMod = true, IsSub = true, IsVip = true });
            context.Users.Create(new User() { TwitchId = "12345", Username = "Auth" });
            context.Users.Create(new User() { TwitchId = "67890", Username = "NotAuth" });
        }

        private void CreateAccessGroup(IDatabase context, string name, string username, string commands, bool includeMods = false, bool includeVips = false, bool includeSubs = false, bool includeAdmins = false)
        {
            var group = new AccessGroup() { Name = name, IncludeMods = includeMods, IncludeSubs = includeSubs, IncludeVips = includeVips, IncludeAdmins = includeAdmins };
            context.AccessGroups.Create(group);
            if (!string.IsNullOrWhiteSpace(username))
            {
                var user = context.Users.Read(x => x.Username.Equals(username)).FirstOrDefault();
                if (user != null)
                {
                    context.Enrollments.Create(new Enrollment(group, user.TwitchId));
                }
            }
            context.Restrictions.Create(new Restriction(group, commands));
        }

        private void InitializeUserRoles(IDatabase context)
        {
            // OG access groups, no idea why these exist
            /*
            var streamer = context.Users.First(x => x.Username.Equals("Streamer"));
            var bot = context.Users.First(x => x.Username.Equals("Bot"));
            var dev = context.Users.First(x => x.Username.Equals("Dev"));
            var group1 = new AccessGroup(1, "Streamer") { IncludeAdmins = true };
            context.AccessGroups.Create(group1);
            context.Restrictions.Create(new Restriction(group1, "*.Admin.*"));
            var group2 = new AccessGroup(2, "UIDev");
            context.AccessGroups.Create(group2);
            context.Restrictions.Create(new Restriction(group2, dev.TwitchId));
            //*/

            CreateAccessGroup(context, "TestGroup", "Auth", "CommandMock.Foo");
            CreateAccessGroup(context, "ModGroup", null, "CommandMock.ModFoo", includeMods: true);
            CreateAccessGroup(context, "VipGroup", null, "CommandMock.VipFoo", includeVips: true);
            CreateAccessGroup(context, "SubGroup", null, "CommandMock.SubFoo", includeSubs: true);
            CreateAccessGroup(context, "AdminGroup", null, "CommandMock.AdminFoo", includeAdmins: true);
        }

        private void InitializeFish(IDatabase context)
        {
            context.FishData.Create(CreateFish(1, "SmallTestFish", "It's a small fish.", 10, 20, 50, 60, 1, "Small", "Light tug", 1, "Common", 3.5f));
            context.FishData.Create(CreateFish(2, "BigTestFish", "It's a big fish.", 100, 200, 500, 600, 2, "Big", "Heavy tug", 2, "Uncommon", 2.5f));
            context.FishData.Create(CreateFish(3, "RareTestFish", "It's a rare fish.", 1000, 2000, 5000, 6000, 3, "Rare", "Mystical tug", 3, "Rare", 1.5f));
        }

        private void InitializePersonalLeaderboards(IDatabase context)
        {
            var userData = context.Users.Read().ToList();
            var fishData = context.FishData.Read().ToList();
            for (var i = 0; i < userData.Count - 1; i++)
            {
                var user = userData[i];
                foreach (var fish in fishData)
                {
                    context.Catches.Create(new Catch()
                    {
                        UserId = user.TwitchId,
                        Fish = fish,
                        Length = (float)random.NextDouble() * (fish.MaximumLength - fish.MinimumLength) + fish.MinimumLength,
                        Weight = (float)random.NextDouble() * (fish.MaximumWeight - fish.MinimumWeight) + fish.MinimumWeight
                    });
                }
            }
        }

        private void InitializeGlobalLeaderboard(IDatabase context)
        {
            var fishData = context.FishData.Read().ToList();
            var catchData = context.Catches.Read().ToList();
            foreach (var fish in fishData)
            {
                var best = catchData.Where(x => x.Fish.Id == fish.Id).OrderByDescending(x => x.Weight).FirstOrDefault();
                if (best != null)
                {
                    context.FishingLeaderboard.Create(new LeaderboardEntry()
                    {
                        Fish = best.Fish,
                        Length = best.Length,
                        Weight = best.Weight,
                        UserId = best.UserId
                    });
                }
            }
        }

        private TournamentResult CreateTournamentResult(int hours, int minutes, int seconds, params TournamentEntry[] entries)
        {
            return new TournamentResult(DateTime.Now - new TimeSpan(hours, minutes, seconds), entries);
        }

        private void InitializeTournaments(IDatabase context)
        {
            context.TournamentResults.Create(CreateTournamentResult(0, 0, 30, new TournamentEntry("10", 10), new TournamentEntry("11", 20), new TournamentEntry("12", 30)));
            context.TournamentResults.Create(CreateTournamentResult(0, 30, 30, new TournamentEntry("10", 30), new TournamentEntry("11", 20), new TournamentEntry("12", 10)));
            context.TournamentResults.Create(CreateTournamentResult(1, 0, 30, new TournamentEntry("10", 40), new TournamentEntry("11", 20), new TournamentEntry("12", 50)));
            context.TournamentResults.Create(CreateTournamentResult(1, 30, 30, new TournamentEntry("10", 35), new TournamentEntry("11", 20), new TournamentEntry("12", 10)));
            context.TournamentResults.Create(CreateTournamentResult(2, 0, 30, new TournamentEntry("10", 40), new TournamentEntry("11", 60), new TournamentEntry("12", 50)));
        }

        private void InitializeSettings(IDatabase context)
        {
            var appSettings = new AppSettings
            {
                UserDatabaseUpdateTime = 2,
                MaxWhisperRecipients = 10,
                UserLookupBatchTime = 0
            };
            context.AppSettings.Create(appSettings);

            var gameSettings = new GameSettings
            {
                FishingCastMaximum = 20,
                FishingCastMinimum = 10,
                FishingGloatCost = 10,
                FishingHookLength = 10,
                FishingTournamentCastMaximum = 2,
                FishingTournamentCastMinimum = 1,
                FishingTournamentDuration = 5,
                FishingTournamentInterval = 10,
                FishingUseNormalRarity = false,
                FishingUseNormalSizes = false
            };
            context.GameSettings.Create(gameSettings);
        }

        private void InitializeTimers(IDatabase context)
        {
            context.DataTimers.Create(new DataTimer() { Name = "WhisperQueue", Timestamp = DateTime.Now });
        }

        public void SeedData()
        {
            InitializeSettings(CurrentConnection);
            InitializeUsers(CurrentConnection);
            InitializeUserRoles(CurrentConnection);
            InitializeFish(CurrentConnection);
            InitializePersonalLeaderboards(CurrentConnection);
            InitializeGlobalLeaderboard(CurrentConnection);
            InitializeTournaments(CurrentConnection);
            InitializeTimers(CurrentConnection);
        }
    }
}
