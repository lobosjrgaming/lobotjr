using LobotJR.Command;
using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Model.AccessControl;
using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Fishing;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Test.Mocks
{
    public class MockConnectionManager : IConnectionManager
    {
        private readonly Random random = new Random();
        private MockContext Context;
        public IDatabase CurrentConnection { get; private set; }

        public Task<IDatabase> OpenConnection()
        {
            Context = MockContext.Create();
            CurrentConnection = new SqliteRepositoryManager(Context);
            return Task.FromResult(CurrentConnection);
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
                FishingUseNormalSizes = false,
                LevelGloatCost = 20,
                PetGloatCost = 30,
            };
            context.GameSettings.Create(gameSettings);
        }

        private void InitializeTimers(IDatabase context)
        {
            context.DataTimers.Create(new DataTimer() { Name = "WhisperQueue", Timestamp = DateTime.Now });
        }

        private void InitializeClasses(IDatabase context)
        {
            context.CharacterClassData.Create(new CharacterClass() { CanPlay = false, Name = "NonPlayable" });
            context.CharacterClassData.Create(new CharacterClass() { CanPlay = true, Name = "Playable" });
            context.CharacterClassData.Create(new CharacterClass() { CanPlay = true, Name = "PlayableAlternate" });
            context.CharacterClassData.Create(new CharacterClass() { CanPlay = true, Name = "PlayableThird" });
        }

        private void InitializePlayers(IDatabase context)
        {
            var canPlay = context.CharacterClassData.Read(x => x.CanPlay);
            var noPlay = context.CharacterClassData.Read(x => !x.CanPlay);
            foreach (var user in context.Users.Read())
            {
                context.PlayerCharacters.Create(new PlayerCharacter() { UserId = user.TwitchId, CharacterClass = user.IsSub ? canPlay.First() : noPlay.First() });
            }
            context.Stables.Create(new Stable()
            {
                Pet = context.PetData.Read().First(),
                UserId = context.Users.Read().First().TwitchId,
                Name = "FirstPet"
            });
            context.Stables.Create(new Stable()
            {
                Pet = context.PetData.Read().Last(),
                UserId = context.Users.Read().First().TwitchId,
                Name = "LastPet"
            });
        }

        private void InitializeItems(IDatabase context)
        {
            var qualities = new List<ItemQuality>()
            {
                new ItemQuality() { Name = "Normal", DropRatePenalty = 0 },
                new ItemQuality() { Name = "Rare", DropRatePenalty = 10 }
            };
            var types = new List<ItemType>()
            {
                new ItemType() { Name = "Wood" },
                new ItemType() { Name = "Metal" }
            };
            var slots = new List<ItemSlot>()
            {
                new ItemSlot() { Name = "Weapon" },
                new ItemSlot() { Name = "Armor" }
            };
            var index = 1;
            foreach (var quality in qualities)
            {
                context.ItemQualityData.Create(quality);
                foreach (var type in types)
                {
                    context.ItemTypeData.Create(type);
                    foreach (var slot in slots)
                    {
                        context.ItemSlotData.Create(slot);
                        context.ItemData.Create(new Item()
                        {
                            Name = $"{quality.Name} {type.Name} {slot.Name}",
                            Max = 1,
                            Description = $"A {slot.Name} made of {quality.Name} {type.Name}",
                            Quality = quality,
                            Type = type,
                            Slot = slot,
                            CoinBonus = index++ / 100f,
                            ItemFind = index++ / 100f,
                            SuccessChance = index++ / 100f,
                            XpBonus = index++ / 100f,
                            PreventDeathBonus = index++ / 100f
                        });
                    }
                }
            }
            context.EquippableData.Create(new Equippables()
            {
                CharacterClass = context.CharacterClassData.Read(x => x.CanPlay).First(),
                ItemType = types.First(),
            });
            var consumableType = new ItemType() { Name = "Consumable" };
            var nonSlot = new ItemSlot() { Name = "Unequippable" };
            context.ItemTypeData.Create(consumableType);
            context.ItemSlotData.Create(nonSlot);
            context.ItemData.Create(new Item()
            {
                Name = "Potion",
                Max = 5,
                Quality = qualities.First(),
                Type = consumableType,
                Slot = nonSlot
            });
        }

        private void InitializePets(IDatabase context)
        {
            var basic = new PetRarity()
            {
                Name = "Common",
                DropRate = 1
            };
            var rare = new PetRarity()
            {
                Name = "Rare",
                DropRate = 0.25f
            };
            context.PetRarityData.Create(basic);
            context.PetRarityData.Create(rare);
            var petSnake = new Pet()
            {
                Name = "Snake",
                Description = "A common garter snake",
                Rarity = basic
            };
            context.PetData.Create(petSnake);
            var petDragon = new Pet()
            {
                Name = "Dragon",
                Description = "A mighty dragon",
                Rarity = rare
            };
            context.PetData.Create(petDragon);
        }

        private void InitializeDungeons(IDatabase context)
        {
            var modes = new List<DungeonMode>()
            {
                new DungeonMode()
                {
                    Name = "Normal",
                    Flag = "N",
                    IsDefault = true
                },
                new DungeonMode()
                {
                    Name = "Heroic",
                    Flag = "H",
                }
            };

            context.DungeonTimerData.Create(new DungeonTimer()
            {
                Name = GroupFinderController.DailyTimerName,
                Length = 5
            });

            var dungeon = new Dungeon()
            {
                Description = "A dungeon.",
                Name = "Dungeon",
                FailureText = "You died!",
                Introduction = "Welcome to the dungeon, we've got mobs and loot",
                Encounters = new List<Encounter>()
                {
                    new Encounter()
                    {
                        Levels = new List<EncounterLevel>()
                        {
                            new EncounterLevel()
                            {
                                Difficulty = 1,
                                Mode = modes.First()
                            },
                            new EncounterLevel()
                            {
                                Difficulty = 1,
                                Mode = modes.Last()
                            }
                        },
                        Enemy = "Mob",
                        SetupText = "You encounter a basic mob",
                        CompleteText = "You easily defeated it!"
                    },
                    new Encounter()
                    {
                        Levels = new List<EncounterLevel>()
                        {
                            new EncounterLevel()
                            {
                                Difficulty = 1,
                                Mode = modes.First()
                            },
                            new EncounterLevel()
                            {
                                Difficulty = .5f,
                                Mode = modes.Last()
                            }
                        },
                        Enemy = "Boss",
                        SetupText = "You encounter the dungeon boss",
                        CompleteText = "You barely managed to eke out a victory!"
                    }
                },
                LevelRanges = new List<LevelRange>()
                {
                    new LevelRange()
                    {
                        Minimum = 3,
                        Maximum = 10,
                        Mode = modes.First()
                    },
                    new LevelRange()
                    {
                        Minimum = 10,
                        Maximum = 20,
                        Mode = modes.Last()
                    }
                },
                Loot = new List<Loot>()
                {
                    new Loot()
                    {
                        Mode = modes.First(),
                        Item = context.ItemData.Read().First(),
                        DropChance = 0
                    },
                    new Loot()
                    {
                        Mode = modes.Last(),
                        Item = context.ItemData.Read().ElementAt(2),
                        DropChance = 0
                    }
                },
            };
            context.DungeonData.Create(dungeon);
            var dungeon2 = new Dungeon()
            {
                Name = "Dungeon 2",
                Description = "A different dungeon.",
                FailureText = "You died!",
                Introduction = "Welcome to the second dungeon.",
                LevelRanges = new List<LevelRange>() { new LevelRange() { Mode = modes.First() }, new LevelRange() { Mode = modes.Last() } }
            };
            context.DungeonData.Create(dungeon2);
        }

        public void SeedData()
        {
            InitializeSettings(CurrentConnection);
            InitializeUsers(CurrentConnection);
            CurrentConnection.Commit();
            InitializeUserRoles(CurrentConnection);
            InitializeFish(CurrentConnection);
            CurrentConnection.Commit();
            InitializePersonalLeaderboards(CurrentConnection);
            CurrentConnection.Commit();
            InitializeGlobalLeaderboard(CurrentConnection);
            InitializeTournaments(CurrentConnection);
            InitializeTimers(CurrentConnection);
            InitializeClasses(CurrentConnection);
            InitializePets(CurrentConnection);
            CurrentConnection.Commit();
            InitializePlayers(CurrentConnection);
            InitializeItems(CurrentConnection);
            CurrentConnection.Commit();
            InitializeDungeons(CurrentConnection);
        }

        public void ResetUsers()
        {
            CurrentConnection.Commit();
            CurrentConnection.Users.Delete();
            CurrentConnection.Commit();
            InitializeUsers(CurrentConnection);
            CurrentConnection.Commit();
        }

        public void ResetAccessGroups()
        {
            CurrentConnection.Commit();
            CurrentConnection.Enrollments.Delete();
            CurrentConnection.Restrictions.Delete();
            CurrentConnection.AccessGroups.Delete();
            CurrentConnection.Commit();
            InitializeUserRoles(CurrentConnection);
            CurrentConnection.Commit();
        }

        public void ResetFishingData()
        {
            CurrentConnection.Commit();
            CurrentConnection.Catches.Delete();
            CurrentConnection.TournamentEntries.Delete();
            CurrentConnection.TournamentResults.Delete();
            CurrentConnection.FishingLeaderboard.Delete();
            CurrentConnection.Commit();
            InitializePersonalLeaderboards(CurrentConnection);
            CurrentConnection.Commit();
            InitializeGlobalLeaderboard(CurrentConnection);
            InitializeTournaments(CurrentConnection);
            CurrentConnection.Commit();
        }

        public void ResetDungeons()
        {
            CurrentConnection.DungeonData.Delete();
            CurrentConnection.EncounterData.Delete();
            CurrentConnection.LevelRangeData.Delete();
            CurrentConnection.LootData.Delete();
            CurrentConnection.DungeonModeData.Delete();
            CurrentConnection.Commit();
            InitializeDungeons(CurrentConnection);
            CurrentConnection.Commit();
        }

        public void ResetPlayers()
        {
            CurrentConnection.PlayerCharacters.Delete();
            CurrentConnection.Inventories.Delete();
            CurrentConnection.Stables.Delete();
            CurrentConnection.DungeonHistories.Delete();
            CurrentConnection.DungeonLockouts.Delete();
            CurrentConnection.Commit();
            InitializePlayers(CurrentConnection);
            CurrentConnection.Commit();
        }
    }
}
