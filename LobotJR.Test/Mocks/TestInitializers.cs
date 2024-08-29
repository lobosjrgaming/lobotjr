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
    public static class TestInitializers
    {
        private static readonly Random random = new Random();

        private static Fish CreateFish(int id, string name, string flavorText, int minLength, int maxLength, int minWeight, int maxWeight, int sizeId, string sizeName, string sizeMessage, int rarityId, string rarityName, float rarityWeight)
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

        public static void InitializeUsers(MockContext context)
        {
            context.Users.Add(new User() { TwitchId = "01", Username = "Streamer", IsAdmin = true });
            context.Users.Add(new User() { TwitchId = "02", Username = "Bot", IsAdmin = true });
            context.Users.Add(new User() { TwitchId = "03", Username = "Dev" });
            context.Users.Add(new User() { TwitchId = "04", Username = "Mod", IsMod = true });
            context.Users.Add(new User() { TwitchId = "05", Username = "Sub", IsSub = true });
            context.Users.Add(new User() { TwitchId = "06", Username = "Vip", IsVip = true });
            context.Users.Add(new User() { TwitchId = "10", Username = "Foo" });
            context.Users.Add(new User() { TwitchId = "11", Username = "Bar" });
            context.Users.Add(new User() { TwitchId = "12", Username = "Fizz" });
            context.Users.Add(new User() { TwitchId = "13", Username = "Buzz" });
            context.Users.Add(new User() { TwitchId = "20", Username = "Super", IsMod = true, IsSub = true, IsVip = true });
        }

        public static void InitializeUserRoles(MockContext context)
        {
            var streamer = context.Users.First(x => x.Username.Equals("Streamer"));
            var bot = context.Users.First(x => x.Username.Equals("Bot"));
            var dev = context.Users.First(x => x.Username.Equals("Dev"));
            var group1 = new AccessGroup(1, "Streamer") { IncludeAdmins = true };
            context.AccessGroups.Add(group1);
            context.Restrictions.Add(new Restriction(group1, "*.Admin.*"));
            var group2 = new AccessGroup(2, "UIDev");
            context.AccessGroups.Add(group2);
            context.Restrictions.Add(new Restriction(group2, dev.TwitchId));
        }

        public static void InitializeFish(MockContext context)
        {
            context.FishData.Add(CreateFish(1, "SmallTestFish", "It's a small fish.", 10, 20, 50, 60, 1, "Small", "Light tug", 1, "Common", 3.5f));
            context.FishData.Add(CreateFish(2, "BigTestFish", "It's a big fish.", 100, 200, 500, 600, 2, "Big", "Heavy tug", 2, "Uncommon", 2.5f));
            context.FishData.Add(CreateFish(3, "RareTestFish", "It's a rare fish.", 1000, 2000, 5000, 6000, 3, "Rare", "Mystical tug", 3, "Rare", 1.5f));
        }

        public static void InitializePersonalLeaderboards(MockContext context)
        {
            var userData = context.Users.ToList();
            var fishData = context.FishData.ToList();
            for (var i = 0; i < userData.Count - 1; i++)
            {
                var user = userData[i];
                foreach (var fish in fishData)
                {
                    context.Catches.Add(new Catch()
                    {
                        UserId = user.TwitchId,
                        Fish = fish,
                        Length = (float)random.NextDouble() * (fish.MaximumLength - fish.MinimumLength) + fish.MinimumLength,
                        Weight = (float)random.NextDouble() * (fish.MaximumWeight - fish.MinimumWeight) + fish.MinimumWeight
                    });
                }
            }
        }

        public static void InitializeGlobalLeaderboard(MockContext context)
        {
            var fishData = context.FishData.ToList();
            var catchData = context.Catches.ToList();
            foreach (var fish in fishData)
            {
                var best = catchData.Where(x => x.Fish.Id == fish.Id).OrderByDescending(x => x.Weight).FirstOrDefault();
                if (best != null)
                {
                    context.FishingLeaderboard.Add(new LeaderboardEntry()
                    {
                        Fish = best.Fish,
                        Length = best.Length,
                        Weight = best.Weight,
                        UserId = best.UserId
                    });
                }
            }
        }

        private static TournamentResult CreateTournamentResult(int hours, int minutes, int seconds, params TournamentEntry[] entries)
        {
            return new TournamentResult(DateTime.Now - new TimeSpan(hours, minutes, seconds), entries);
        }

        public static void InitializeTournaments(MockContext context)
        {
            context.FishingTournaments.Add(CreateTournamentResult(0, 0, 30, new TournamentEntry("10", 10), new TournamentEntry("11", 20), new TournamentEntry("12", 30)));
            context.FishingTournaments.Add(CreateTournamentResult(0, 30, 30, new TournamentEntry("10", 30), new TournamentEntry("11", 20), new TournamentEntry("12", 10)));
            context.FishingTournaments.Add(CreateTournamentResult(1, 0, 30, new TournamentEntry("10", 40), new TournamentEntry("11", 20), new TournamentEntry("12", 50)));
            context.FishingTournaments.Add(CreateTournamentResult(1, 30, 30, new TournamentEntry("10", 35), new TournamentEntry("11", 20), new TournamentEntry("12", 10)));
            context.FishingTournaments.Add(CreateTournamentResult(2, 0, 30, new TournamentEntry("10", 40), new TournamentEntry("11", 60), new TournamentEntry("12", 50)));
        }

        public static void InitializeSettings(MockContext context)
        {
            var appSettings = new AppSettings
            {
                UserDatabaseUpdateTime = 2,
                MaxWhisperRecipients = 10,
                UserLookupBatchTime = 0
            };
            context.AppSettings.Add(appSettings);

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
            context.GameSettings.Add(gameSettings);
        }

        public static void InitializeTimers(MockContext context)
        {
            context.DataTimers.Add(new DataTimer() { Name = "WhisperQueue", Timestamp = DateTime.Now });
        }

        public static void SetupDatabase()
        {
            var dbContext = MockContext.CreateAndSeed(
                InitializeSettings,
                InitializeUsers,
                InitializeUserRoles,
                InitializeFish,
                InitializePersonalLeaderboards,
                InitializeGlobalLeaderboard,
                InitializeTournaments,
                InitializeTimers);
            dbContext.Database.Initialize(true);
            var manager = new SqliteRepositoryManager(dbContext);
            manager.Dispose();
        }
    }
}
