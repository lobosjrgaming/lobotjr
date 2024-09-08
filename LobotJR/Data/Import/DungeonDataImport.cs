using LobotJR.Command.Model.Dungeons;
using LobotJR.Command.Model.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LobotJR.Data.Import
{
    public class DungeonDataImport
    {
        public static readonly string ContentFolderName = "content";
        public static readonly string DungeonListPath = "dungeonlist.ini";
        public static readonly string DungeonFolder = "dungeons";
        public static IFileSystem FileSystem = new FileSystem();

        public static void SeedDungeonTimers(IRepository<DungeonTimer> timerRepository)
        {
            var now = DateTime.Now;
            timerRepository.Create(new DungeonTimer()
            {
                BaseTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0),
                Name = "Daily Dungeon",
                Length = 60 * 24
            });
            timerRepository.Commit();
        }

        public static Dictionary<string, DungeonMode> SeedDungeonModes(IRepository<DungeonMode> modeRepository)
        {
            var output = new Dictionary<string, DungeonMode>();
            var normalMode = new DungeonMode()
            {
                Name = "Normal",
                Flag = "N",
                IsDefault = true,
            };
            var heroicMode = new DungeonMode()
            {
                Name = "Heroic",
                Flag = "H",
                IsDefault = false
            };
            modeRepository.Create(normalMode);
            modeRepository.Create(heroicMode);
            modeRepository.Commit();
            output.Add("N", normalMode);
            output.Add("H", heroicMode);
            return output;
        }

        public static IEnumerable<Dungeon> LoadDungeonData(string contentFolder, string dungeonListPath, string dungeonFolder, Dictionary<int, Item> itemMap, Dictionary<string, DungeonMode> modeMap)
        {
            try
            {
                var entries = FileSystem.ReadAllLines($"{contentFolder}/{dungeonListPath}")
                    .Select(x => x.Split(','))
                    .Where(x => x.Length == 2 && !x[1].EndsWith("_h.txt"))
                    .ToDictionary(x => int.Parse(x[0]), x => x[1]);

                var output = new List<Dungeon>();
                foreach (var entry in entries)
                {
                    var path = $"{contentFolder}/{dungeonFolder}/{entry.Value}";
                    var dungeonData = FileSystem.ReadAllLines(path);
                    var heroicData = FileSystem.ReadAllLines($"{path.Replace(".txt", "_h.txt")}");
                    output.Add(CreateDungeonFromFile(dungeonData, heroicData, itemMap, modeMap));
                }
                return output;
            }
            catch
            {
                return new List<Dungeon>();
            }
        }

        private static Dungeon CreateDungeonFromFile(IEnumerable<string> fileData, IEnumerable<string> heroicData, Dictionary<int, Item> itemMap, Dictionary<string, DungeonMode> modeMap)
        {
            var metadata = fileData.ElementAt(1).Split(',');
            var encounterList = fileData.ElementAt(2).Split(',');
            var loot = fileData.FirstOrDefault(x => x.StartsWith("Loot="))?.Substring(5).Split(',').Select(x => int.Parse(x)) ?? new List<int>();
            var lines = fileData.Skip(loot.Any() ? 7 : 6).ToList();
            lines.Add(fileData.ElementAt(4));
            var heroicMetadata = heroicData.ElementAt(1).Split(',');
            var heroicEncounterList = heroicData.ElementAt(2).Split(',');
            var heroicLoot = heroicData.FirstOrDefault(x => x.StartsWith("Loot="))?.Substring(5).Split(',').Select(x => int.Parse(x)) ?? new List<int>();
            var encounterCount = metadata[1];
            var successRate = float.Parse(metadata[2]) / 100f;
            var heroicSuccessRate = float.Parse(heroicMetadata[2]) / 100f;
            var encounters = new List<Encounter>();
            for (var i = 0; i < encounterList.Length / 2; i++)
            {
                var name = encounterList[i * 2];
                var difficulty = encounterList[i * 2 + 1];
                var heroicDifficulty = heroicEncounterList[i * 2 + 1];
                encounters.Add(new Encounter()
                {
                    Enemy = name,
                    Levels = new List<EncounterLevel>()
                    {
                        new EncounterLevel()
                        {
                            Difficulty = successRate - float.Parse(difficulty) / 100f,
                            Mode = modeMap["N"]
                        },
                        new EncounterLevel()
                        {
                            Difficulty = heroicSuccessRate - float.Parse(heroicDifficulty) / 100f,
                            Mode = modeMap["H"]
                        },
                    },
                    SetupText = lines.ElementAt(i * 2),
                    CompleteText = lines.ElementAt(i * 2 + 1)
                });
            }
            var lootTable = new List<Loot>();
            foreach (var lootEntry in loot)
            {
                var item = itemMap[lootEntry];
                lootTable.Add(new Loot()
                {
                    Item = item,
                    DropChance = item.Quality.DropRatePenalty,
                    Mode = modeMap["N"]
                });
            }
            foreach (var lootEntry in heroicLoot)
            {
                var item = itemMap[lootEntry];
                lootTable.Add(new Loot()
                {
                    Item = item,
                    DropChance = item.Quality.DropRatePenalty,
                    Mode = modeMap["H"]
                });
            }

            return new Dungeon()
            {
                Name = metadata[0],
                Description = fileData.ElementAt(3),
                Introduction = lines.ElementAt(0),
                FailureText = fileData.ElementAt(5),
                LevelRanges = new List<LevelRange>()
                {
                    new LevelRange()
                    {
                        Minimum = int.Parse(metadata[3]),
                        Maximum = int.Parse(metadata[4]),
                        Mode = modeMap["N"]
                    },
                    new LevelRange()
                    {
                        Minimum = int.Parse(heroicMetadata[3]),
                        Maximum = int.Parse(heroicMetadata[4]),
                        Mode = modeMap["H"]
                    }
                },
                Encounters = encounters,
                Loot = lootTable
            };
        }

        public static void ImportDungeonDataIntoSql(string contentFolder, string dungeonDataPath, string dungeonFolder, IRepository<Dungeon> dungeonRepository, IRepository<DungeonTimer> timerRepository, IRepository<DungeonMode> modeRepository, Dictionary<int, Item> itemMap)
        {
            SeedDungeonTimers(timerRepository);
            var modeMap = SeedDungeonModes(modeRepository);

            var dungeons = LoadDungeonData(contentFolder, dungeonDataPath, dungeonFolder, itemMap, modeMap);
            foreach (var dungeon in dungeons)
            {
                dungeonRepository.Create(dungeon);
            }
            dungeonRepository.Commit();
        }
    }
}
