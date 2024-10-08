﻿using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Fishing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LobotJR.Data.Import
{
    /// <summary>
    /// Loads fisher data from legacy flat-file format and imports them into
    /// the new sql format.
    /// </summary>
    public class FisherDataImport
    {
        /// <summary>
        /// Default path to the legacy fisher data file.
        /// </summary>
        public static readonly string FisherDataPath = "fishing.json";
        /// <summary>
        /// Default path to the legacy fishing leaderboard file.
        /// </summary>
        public static readonly string FishingLeaderboardPath = "fishingLeaderboard.json";
        /// <summary>
        /// File system implementation to use for reading file data.
        /// </summary>
        public static IFileSystem FileSystem = new FileSystem();

        /// <summary>
        /// Loads the list of fishers and their personal leaderboards from
        /// legacy data files.
        /// </summary>
        /// <param name="fisherDataPath">The path to the file containing the fisher data.</param>
        /// <returns>A collection of fisher leaderboards mapped to twitch usernames.</returns>
        public static Dictionary<string, LegacyFisher> LoadLegacyFisherData(string fisherDataPath)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, LegacyFisher>>(FileSystem.ReadAllText(fisherDataPath));
            }
            catch
            {
                return new Dictionary<string, LegacyFisher>();
            }
        }

        /// <summary>
        /// Loads the global fishing leaderboard from legacy data files.
        /// </summary>
        /// <param name="fishingLeaderboardDataPath">The path to the file containing the leaderboard data.</param>
        /// <returns>A collection of leaderboard records for each fish.</returns>
        public static List<LegacyCatch> LoadLegacyFishingLeaderboardData(string fishingLeaderboardDataPath)
        {
            try
            {
                return JsonConvert.DeserializeObject<List<LegacyCatch>>(FileSystem.ReadAllText(fishingLeaderboardDataPath));
            }
            catch
            {
                return new List<LegacyCatch>();
            }
        }

        /// <summary>
        /// Read the data from the legacy flat-file format, add them to the new
        /// repository, and commit the new data.
        /// </summary>
        /// <param name="fisherList">A dictionary mapping legacy personal leaderboards to twitch usernames.</param>
        /// <param name="fishRepository">The repository containing the fish data.</param>
        /// <param name="leaderboardRepository">The repository containing the personal leaderboard data.</param>
        /// <param name="userController">The user lookup controller to convert the stored usernames into user ids.</param>
        /// <exception cref="DirectoryNotFoundException">If the path to fisherDataPath does not exist.</exception>
        /// <exception cref="IOException">If the attempt to access the file at fisherDataPath throws an IOException.</exception>
        /// <exception cref="FileNotFoundException">If the file at fisherDataPath does not exist.</exception>
        public static void ImportFisherDataIntoSql(Dictionary<string, LegacyFisher> fisherList, IRepository<Fish> fishRepository, IRepository<Catch> leaderboardRepository, UserController userController)
        {
            foreach (var fisher in fisherList)
            {
                var records = new List<Catch>();
                var fisherUserId = userController.GetUserByName(fisher.Key)?.TwitchId;
                if (fisherUserId != null)
                {
                    var existingRecords = leaderboardRepository.Read(x => x.UserId.Equals(fisherUserId)).ToList();

                    foreach (var fish in fisher.Value.biggestFish)
                    {
                        var existingRecord = existingRecords.FirstOrDefault(x => x.FishId == fish.ID && x.UserId.Equals(fisherUserId));
                        if (existingRecord == null)
                        {
                            leaderboardRepository.Create(new Catch()
                            {
                                Fish = fishRepository.ReadById(fish.ID),
                                UserId = fisherUserId,
                                Length = fish.length,
                                Weight = fish.weight
                            });
                        }
                        else if (existingRecord.Weight < fish.weight)
                        {
                            existingRecord.Length = fish.length;
                            existingRecord.Weight = fish.weight;
                            leaderboardRepository.Update(existingRecord);
                        }
                    }
                }
            }
            leaderboardRepository.Commit();
        }

        /// <summary>
        /// Reads the data for the global fishing leaderboard, converts them to
        /// the current format, and imports them into the SQLite database.
        /// </summary>
        /// <param name="fishingLeaderboard">A list of catch data containing leaderboard records for each fish.</param>
        /// <param name="leaderboardRepository">The repository to import the leaderboard data to.</param>
        /// <param name="fishRepository">The repository containing the fish data.</param>
        /// <param name="userController">The user lookup system to convert the stored usernames into user ids.</param>
        public static void ImportLeaderboardDataIntoSql(List<LegacyCatch> fishingLeaderboard, IRepository<LeaderboardEntry> leaderboardRepository, IRepository<Fish> fishRepository, UserController userController)
        {
            foreach (var record in fishingLeaderboard)
            {
                var existing = leaderboardRepository.Read(x => x.Fish.Id == record.ID).FirstOrDefault();
                var userId = userController.GetUserByName(record.caughtBy)?.TwitchId;
                if (existing == null && userId != null)
                {
                    leaderboardRepository.Create(new LeaderboardEntry()
                    {
                        Fish = fishRepository.ReadById(record.ID),
                        UserId = userId,
                        Length = record.length,
                        Weight = record.weight
                    });
                }
            }
            leaderboardRepository.Commit();
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Needed to load in legacy data for conversion to modern standards")]
    public class LegacyFisher
    {
        public string username { get; set; } = "";
        public int level { get; set; } = -1;
        public int XP { get; set; } = -1;
        public int tournamentPoints { get; set; } = 0;
        public List<LegacyCatch> biggestFish { get; set; } = new List<LegacyCatch>();
        public int lure { get; set; } = -1;
        public bool isFishing { get; set; } = false;
        public bool fishHooked { get; set; } = false;
        public int hookedFishID { get; set; } = -1;
        public DateTime timeOfCatch { get; set; }
        public DateTime timeSinceHook { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Needed to load in legacy data for conversion to modern standards")]
    public class LegacyCatch : LegacyFish
    {
        public float length { get; set; } = -1;
        public float weight { get; set; } = -1;
        public string caughtBy { get; set; } = "";
    }
}
