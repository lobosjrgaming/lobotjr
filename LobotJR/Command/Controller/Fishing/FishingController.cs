using LobotJR.Command.Model.Fishing;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LobotJR.Command.Controller.Fishing
{
    /// <summary>
    /// Runs the logic for the fishing controller.
    /// </summary>
    public class FishingController : IProcessor, IDatabaseInitialize
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IConnectionManager ConnectionManager;
        private readonly SettingsManager SettingsManager;

        private readonly Random Random = new Random();
        private readonly int[] Chances = new int[] { 40, 70, 95, 99, 100 };

        private readonly List<Fisher> Fishers = new List<Fisher>();

        /// <summary>
        /// Event handler for events related to a specific user.
        /// </summary>
        /// <param name="database">A connection to the database.</param>
        /// <param name="fisher">The fisher object for the user.</param>
        public delegate void FisherEventHandler(Fisher fisher);
        /// <summary>
        /// Event handler for when a fisher catches a fish.
        /// </summary>
        /// <param name="database">A connection to the database.</param>
        /// <param name="fisher">The fisher object for the user.</param>
        /// <param name="catchData">The catch data for the fish caught.</param>
        public delegate void FishCatchEventHandler(Fisher fisher, Catch catchData);

        /// <summary>
        /// Event fired when a user hooks a fish.
        /// </summary>
        public event FisherEventHandler FishHooked;
        /// <summary>
        /// Event fired when a user's hooked fish gets away.
        /// </summary>
        public event FisherEventHandler FishGotAway;
        /// <summary>
        /// Event fired when a user catches a fish.
        /// </summary>
        public event FishCatchEventHandler FishCaught;

        public int CastTimeMinimum { get; set; }
        public int CastTimeMaximum { get; set; }

        public FishingController(IConnectionManager connectionManager, SettingsManager settingsManager)
        {
            ConnectionManager = connectionManager;
            SettingsManager = settingsManager;
        }

        public void Initialize()
        {
            var settings = SettingsManager.GetGameSettings();
            CastTimeMinimum = settings.FishingCastMinimum;
            CastTimeMaximum = settings.FishingCastMaximum;
        }

        public IEnumerable<FishRarity> GetRarities()
        {
            return ConnectionManager.CurrentConnection.FishRarityData.Read().ToList();
        }

        public IEnumerable<FishSize> GetSizes()
        {
            return ConnectionManager.CurrentConnection.FishSizeData.Read().ToList();
        }

        public IEnumerable<Fish> GetAllFish()
        {
            return ConnectionManager.CurrentConnection.FishData.Read().ToList();
        }

        /// <summary>
        /// Gets or creates a fisher object for a given user id.
        /// </summary>
        /// <param name="user">The Twitch user object for the user.</param>
        /// <returns>The fisher object for the user.</returns>
        public Fisher GetFisherByUser(User user)
        {
            var fisher = Fishers.FirstOrDefault(x => x.User.TwitchId.Equals(user.TwitchId));
            if (fisher == null)
            {
                fisher = new Fisher() { User = user };
                Fishers.Add(fisher);
            }
            return fisher;
        }

        /// <summary>
        /// Gets the fish record for the fish a fisher current has hooked.
        /// </summary>
        /// <param name="id">The fisher to get the hooked fish for.</param>
        /// <returns>The fish data that fisher has hooked, or null if no fish
        /// is hooked.</returns>
        public Fish GetHookedFish(Fisher fisher)
        {
            if (fisher.HookedId != -1)
            {
                return ConnectionManager.CurrentConnection.FishData.ReadById(fisher.HookedId);
            }
            return null;
        }

        /// <summary>
        /// Resets all fishers by clearing any cast lines or hooked fish.
        /// </summary>
        public void ResetFishers()
        {
            foreach (var fisher in Fishers)
            {
                fisher.IsFishing = false;
                fisher.HookedId = -1;
                fisher.HookedTime = null;
            }
        }

        /// <summary>
        /// Calculates the exact length, weight, and point value of a fish
        /// being caught.
        /// </summary>
        /// <param name="fisher">The fisher object for the user catching the
        /// fish.</param>
        /// <param name="useNormalSizes">Whether to use the legacy algorithm or
        /// a normal distribution for calculating fish size.</param>
        /// <returns>The catch object with the calculated data values.</returns>
        public Catch CalculateFishSizes(Fisher fisher, bool useNormalSizes)
        {
            if (fisher == null || fisher.HookedId == -1)
            {
                return null;
            }

            var fish = GetHookedFish(fisher);
            var catchData = new Catch
            {
                UserId = fisher.User.TwitchId,
                Fish = fish
            };

            if (useNormalSizes)
            {
                catchData.Weight = (float)Random.NextNormalBounded(fish.MinimumWeight, fish.MaximumWeight);
                catchData.Length = (float)Random.NextNormalBounded(fish.MinimumLength, fish.MaximumLength);

                var weightRange = fish.MaximumWeight - fish.MinimumWeight;
                var lengthRange = fish.MaximumLength - fish.MinimumLength;
                catchData.Points = (int)Math.Round(
                    (catchData.Weight - fish.MinimumWeight) / weightRange * 50f +
                    (catchData.Length - fish.MinimumLength) / lengthRange * 50f);
            }
            else
            {
                var weightRange = (fish.MaximumWeight - fish.MinimumWeight) / 5;
                var lengthRange = (fish.MaximumLength - fish.MinimumLength) / 5;
                var weightVariance = Random.NextDouble() * weightRange;
                var lengthVariance = Random.NextDouble() * lengthRange;

                var size = Random.NextDouble() * 100;
                var category = Chances.Where(x => size >= x).Count();
                catchData.Length = (float)Math.Round(fish.MinimumLength + lengthRange * category + lengthVariance, 2);
                catchData.Weight = (float)Math.Round(fish.MinimumWeight + weightRange * category + weightVariance, 2);
                catchData.Points = (int)Math.Max(Math.Round(size), 1);
            }

            return catchData;
        }

        /// <summary>
        /// Casts the line out for a user, starting the fishing process.
        /// </summary>
        /// <param name="user">The Twitch user object of the user to begin
        /// fishing for.</param>
        public void Cast(User user)
        {
            var fisher = GetFisherByUser(user);
            var hookTime = DateTime.Now;
            hookTime = hookTime.AddSeconds(Random.Next(CastTimeMinimum, CastTimeMaximum + 1));
            if (fisher == null)
            {
                Fishers.Add(new Fisher()
                {
                    User = user,
                    IsFishing = true,
                    HookedTime = hookTime
                });
            }
            else
            {
                fisher.IsFishing = true;
                fisher.HookedTime = hookTime;
            }
        }

        /// <summary>
        /// Attempts to hook a fish.
        /// </summary>
        /// <param name="database">A connection to the database.</param>
        /// <param name="fisher">The fisher to update.</param>
        /// <param name="useNormalRarity">Whether to use the legacy formula or
        /// a normal distribution when determining the rarity of the hooked
        /// fish.</param>
        /// <returns>True if a fish was hooked.</returns>
        public bool HookFish(Fisher fisher, bool useNormalRarity)
        {
            var index = -1;
            var rarities = ConnectionManager.CurrentConnection.FishData.Read().Select(x => x.Rarity).Distinct().ToList();
            if (useNormalRarity)
            {
                index = Random.NextNormalIndex(rarities.Count);
            }
            else
            {
                index = Random.WeightedRandom(rarities.Select(x => (double)x.Weight).ToList());
            }
            if (index >= 0)
            {
                var rarityId = rarities[index].Id;
                var fishList = ConnectionManager.CurrentConnection.FishData.Read(x => x.Rarity.Id == rarityId).ToList();
                var fish = Random.RandomElement(fishList);
                fisher.HookedId = fish.Id;
                Logger.Debug("Fish {fish} hooked for user {userId}.", fish?.Name, fisher.User.TwitchId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to have the hooked fish escape.
        /// </summary>
        /// <param name="fisher">The fisher to update.</param>
        public void UnhookFish(Fisher fisher)
        {
            fisher.HookedId = -1;
            fisher.HookedTime = null;
            fisher.IsFishing = false;
        }

        /// <summary>
        /// Catches the fish a user has hooked. If no fish is hooked, the user
        /// will reel in the empty line.
        /// </summary>
        /// <param name="fisher">The fisher that is trying to catch.</param>
        /// <returns>The catch data for this fish.</returns>
        public Catch CatchFish(Fisher fisher)
        {
            var catchData = default(Catch);
            var settings = SettingsManager.GetGameSettings();
            if (fisher != null)
            {
                catchData = CalculateFishSizes(fisher, settings.FishingUseNormalSizes);
                fisher.IsFishing = false;
                fisher.HookedId = -1;
                fisher.HookedTime = null;
                Logger.Debug("User id {userId} catching fish {fish}", fisher.User.TwitchId, catchData?.Fish?.Name);
                if (catchData != null)
                {
                    FishCaught?.Invoke(fisher, catchData);
                }
            }
            return catchData;
        }

        /// <summary>
        /// Runs all active fishers to process hooking and releasing events.
        /// </summary>
        public Task Process()
        {
            var messages = new Dictionary<string, IEnumerable<string>>();
            var settings = SettingsManager.GetGameSettings();
            foreach (var fisher in Fishers.Where(x => x.IsFishing))
            {
                if (fisher.IsFishing
                    && fisher.HookedId == -1
                    && DateTime.Now >= fisher.HookedTime)
                {
                    Logger.Debug("Hooking fish for user {userId}.", fisher.User.TwitchId);
                    if (HookFish(fisher, settings.FishingUseNormalRarity))
                    {
                        FishHooked?.Invoke(fisher);
                    }
                }
                if (fisher.HookedId != -1
                    && fisher.HookedTime.HasValue
                    && DateTime.Now >= fisher.HookedTime.Value.AddSeconds(settings.FishingHookLength))
                {
                    Logger.Debug("Fish got away for user {userId}.", fisher.User.TwitchId);
                    UnhookFish(fisher);
                    FishGotAway?.Invoke(fisher);
                }
            }
            return Task.CompletedTask;
        }
    }
}
