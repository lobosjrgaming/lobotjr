﻿using Adventures;
using Classes;
using Equipment;
using LobotJR.Command.System.Twitch;
using LobotJR.Twitch;
using LobotJR.Twitch.Model;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Wolfcoins
{
    public class Party
    {
        public Dungeon myDungeon;
        public HashSet<CharClass> members = new HashSet<CharClass>();
        public string partyLeader;
        public int status = 0;
        public int myID = -1;
        public DateTime lastTime = DateTime.Now;
        public bool usedDungeonFinder = false;

        public void PostDungeon(Currency wolfcoins)
        {
            foreach (var member in members)
            {
                wolfcoins.classList[member.name].xpEarned = 0;
                wolfcoins.classList[member.name].coinsEarned = 0;

                wolfcoins.classList[member.name].numInvitesSent = 0;
                wolfcoins.classList[member.name].pendingInvite = false;
            }
        }

        public void ResetTime()
        {
            lastTime = DateTime.Now;
        }

        public void AddMember(CharClass member)
        {
            members.Add(member);
        }

        public bool RemoveMember(string user)
        {
            for (int i = 0; i < members.Count(); i++)
            {
                if (members.ElementAt(i).name == user)
                {
                    members.Remove(members.ElementAt(i));
                    return true;
                }
            }
            return false;
        }

        public int NumMembers()
        {
            int num = 0;
            for (int i = 0; i < members.Count(); i++)
            {
                if (!members.ElementAt(i).pendingInvite)
                    num++;
            }
            return num;
        }
    }

    public class Currency
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Dictionary<string, int> coinList = new Dictionary<string, int>();
        public Dictionary<string, int> xpList = new Dictionary<string, int>();
        public Dictionary<string, CharClass> classList = new Dictionary<string, CharClass>();

        private readonly string path = "wolfcoins.json";
        private readonly string xpPath = "XP.json";
        private readonly string classPath = "classData.json";

        private const int COINMAX = int.MaxValue;

        public const int MAX_XP = 37094;
        public const int MAX_LEVEL = 20;

        public const int WARRIOR = 1;
        public const int MAGE = 2;
        public const int ROGUE = 3;
        public const int RANGER = 4;
        public const int CLERIC = 5;

        public const string clientID = "c95v57t6nfrpts7dqk2urruyc8d0ln1";
        public const int baseRespecCost = 250;

        public Currency()
        {
            Init();
        }

        public int XPForLevel(int level)
        {
            int xp = (int)(4 * (Math.Pow(level, 3)) + 50);
            return xp;
        }

        // algorithm is XP = 4 * (level^3) + 50
        public int determineLevel(string user)
        {
            if (Exists(xpList, user))
            {


                float xp = (float)xpList[user];

                if (xp <= 81)
                    return 1;

                float level = (float)Math.Pow((xp - 50.0f) / 4.0f, (1.0f / 3.0f));

                return (int)level;
            }
            return 0;
        }

        public int determinePrestige(string user)
        {
            if (Exists(classList, user))
            {
                return classList[user].prestige;
            }
            return 0;
        }

        public string gloatWithPrestige(string user)
        {

            if (Exists(xpList, user) && Exists(classList, user))
            {


                float xp = (float)xpList[user];

                if (xp <= 81)
                    return "1";

                float level = (float)Math.Pow((xp - 50.0f) / 4.0f, (1.0f / 3.0f));

                string ret = " Level " + (int)level;

                if (classList[user].prestige > -1)
                {
                    int prestigeLevel = classList[user].prestige;
                    ret += ", Prestige Level " + (int)prestigeLevel;
                }

                return ret;
            }
            return "0";
        }

        public int determineLevel(int xp)
        {
            if (xp <= 54)
                return 1;

            float level = (float)Math.Pow((float)(xp - 50.0f) / 4.0f, (1.0f / 3.0f));
            return (int)level;
        }

        public string determineClass(string user)
        {
            if (classList != null)
            {
                if (classList.Keys.Contains(user))
                {
                    if (classList[user].classType != -1)
                    {
                        int userClass = classList[user].classType;
                        switch (userClass)
                        {
                            case 1:
                                {
                                    return "Warrior";
                                }

                            case 2:
                                {
                                    return "Mage";
                                }

                            case 3:
                                {
                                    return "Rogue";
                                }

                            case 4:
                                {
                                    return "Ranger";
                                }

                            case 5:
                                {
                                    return "Cleric";
                                }

                            default: break;
                        }
                    }
                }
            }
            return "INVALID CLASS";
        }

        public void AwardCoins(int coins, IEnumerable<User> users)
        {

            foreach (var user in users)
            {
                AddCoins(user, coins);
            }
            Logger.Info("Added {coins} coins to current viewers.", coins);
        }

        public void AwardCoins(int coins, string user)
        {
            if (Exists(coinList, user))
            {

                if (coinList.ContainsKey(user) && int.TryParse(coins.ToString(), out int value))
                {
                    try
                    {
                        int prevCoins = coinList[user];
                        checked
                        {
                            coinList[user] += value;
                        }
                        if (coinList[user] > COINMAX)
                        {
                            coinList[user] = COINMAX;
                        }

                        if (coinList[user] < 0)
                            coinList[user] = 0;


                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error adding coins.");
                        Logger.Error(e);
                    }

                }
            }
        }

        public void AwardXP(int xp, User user, ITwitchClient twitchClient)
        {
            var userName = user?.Username;
            //int value = 0;
            if (Exists(xpList, userName))
            {
                int prevLevel = determineLevel(userName);
                if (xpList == null)
                    return;

                if (xpList.ContainsKey(userName) && int.TryParse(xp.ToString(), out int value))
                {
                    try
                    {
                        int prevXP = xpList[userName];
                        checked
                        {
                            xpList[userName] += value;
                        }


                        if (xpList[userName] < 0)
                            xpList[userName] = 0;

                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error adding xp.");
                        Logger.Error(e);
                    }

                    int newLevel = determineLevel(userName);


                    if (newLevel > MAX_LEVEL)
                    {
                        newLevel = 3;
                        classList[userName].prestige++; //prestige code
                        xpList[userName] = 200;
                        twitchClient.QueueWhisper(user, " You have earned a Prestige level! You are now Prestige " + classList[userName].prestige + " and your level has been set to 3. XP to next level: " + XpToNextLevel(userName) + ".");
                        return;
                    }

                    int myPrestige = classList[userName].prestige; //prestige code

                    if (newLevel > prevLevel && newLevel != 3 && newLevel > 1)
                    {
                        //prestige code
                        if (myPrestige > 0)
                        {
                            twitchClient.QueueWhisper(user, " DING! You just reached Level " + newLevel + "! You are Prestige " + myPrestige + ". XP to next level: " + XpToNextLevel(userName) + ".");
                        } //prestige code
                        else
                        {
                            twitchClient.QueueWhisper(user, " DING! You just reached Level " + newLevel + "! XP to next level: " + XpToNextLevel(userName) + ".");
                        }

                        if (newLevel > 5)
                        {
                            classList[userName].level = newLevel;
                        }
                    }

                    if (!(classList.ContainsKey(userName)) && newLevel > prevLevel && newLevel == 3)
                    {
                        CharClass newClass = new CharClass();
                        newClass.classType = -1;
                        classList.Add(userName.ToLower(), newClass);
                        twitchClient.QueueWhisper(user, " You've reached LEVEL 3! You get to choose a class for your character! Choose by whispering me one of the following: ");
                        twitchClient.QueueWhisper(user, " 'C1' (Warrior), 'C2' (Mage), 'C3' (Rogue), 'C4' (Ranger), or 'C5' (Cleric)");
                    }

                }
            }
        }

        public void AwardXP(int xp, IEnumerable<User> users, ITwitchClient twitchClient)
        {
            foreach (var user in users)
            {
                //int value = 0;
                if (xpList != null)
                {
                    var username = user.Username;
                    int prevLevel = determineLevel(username);
                    AddXP(user, xp.ToString());
                    int newLevel = determineLevel(username);
                    if (newLevel > MAX_LEVEL)
                    {
                        newLevel = 3;
                        classList[username].prestige++; //prestige code
                        xpList[username] = 0;
                        twitchClient.QueueWhisper(user, " You have earned a Prestige level! You are now Prestige " + classList[username].prestige + " and your level has been reset to 1. XP to next level: " + XpToNextLevel(username) + ".");
                        return;
                    }
                    int myPrestige = 0;
                    if (classList.ContainsKey(username))
                        myPrestige = classList[username].prestige; //prestige code

                    if (newLevel > prevLevel && newLevel != 3 && newLevel > 1)
                    {
                        //prestige code
                        if (myPrestige > 0)
                        {
                            twitchClient.QueueWhisper(user, " DING! You just reached Level " + newLevel + "! You are Prestige " + myPrestige + ". XP to next level: " + XpToNextLevel(username) + ".");
                        } //prestige code
                        else
                        {
                            twitchClient.QueueWhisper(user, " DING! You just reached Level " + newLevel + "! XP to next level: " + XpToNextLevel(username) + ".");
                        }
                        if (newLevel > 5)
                        {
                            if (classList.ContainsKey(username))
                                classList[username].level = newLevel;
                        }
                    }

                    if (newLevel > prevLevel && newLevel == 3)
                    {

                        CharClass newClass = new CharClass();
                        newClass.classType = -1;
                        if (classList.ContainsKey(username.ToLower()))
                            continue;

                        classList.Add(username.ToLower(), newClass);
                        twitchClient.QueueWhisper(user, " You've reached LEVEL 3! You get to choose a class for your character! Choose by whispering me one of the following: ");
                        twitchClient.QueueWhisper(user, " 'C1' (Warrior), 'C2' (Mage), 'C3' (Rogue), 'C4' (Ranger), or 'C5' (Cleric)");
                    }
                }

            }
            Logger.Info("Granted {xp} xp to current viewers.", xp);
        }

        public void SetClass(User user, string choice, ITwitchClient twitchClient)
        {
            string username = user.Username;
            switch (choice.ToLower())
            {
                case "c1":
                    {
                        classList[username] = new Warrior();
                        classList[username].name = username;
                        classList[username].level = determineLevel(username);
                        classList[username].itemEarned = -1;
                        SaveClassData();
                        twitchClient.QueueWhisper(user, " You successfully selected the Warrior class!");
                    }
                    break;

                case "c2":
                    {
                        classList[username] = new Mage();
                        classList[username].name = username;
                        classList[username].level = determineLevel(username);
                        classList[username].itemEarned = -1;
                        SaveClassData();
                        twitchClient.QueueWhisper(user, " You successfully selected the Mage class!");
                    }
                    break;

                case "c3":
                    {
                        classList[username] = new Rogue();
                        classList[username].name = username;
                        classList[username].level = determineLevel(username);
                        classList[username].itemEarned = -1;
                        SaveClassData();
                        twitchClient.QueueWhisper(user, " You successfully selected the Rogue class!");
                    }
                    break;

                case "c4":
                    {
                        classList[username] = new Ranger();
                        classList[username].name = username;
                        classList[username].level = determineLevel(username);
                        classList[username].itemEarned = -1;
                        SaveClassData();
                        twitchClient.QueueWhisper(user, " You successfully selected the Ranger class!");
                    }
                    break;

                case "c5":
                    {
                        classList[username] = new Cleric();
                        classList[username].name = username;
                        classList[username].level = determineLevel(username);
                        classList[username].itemEarned = -1;
                        SaveClassData();
                        twitchClient.QueueWhisper(user, " You successfully selected the Cleric class!");
                    }
                    break;

                default: break;
            }
        }

        public int SetXP(int xp, User user, ITwitchClient twitchClient)
        {
            var username = user.Username;
            if (xpList != null)
            {
                if (xp > MAX_XP)
                    xp = MAX_XP - 1;

                if (xp < 0)
                    xp = 0;

                if (xpList.Keys.Contains(username))
                {
                    int prevLevel = determineLevel(username);
                    xpList[username] = xp;
                    int newLevel = determineLevel(username);

                    if (newLevel > MAX_LEVEL)
                    {
                        newLevel = MAX_LEVEL;
                    }

                    if (newLevel > prevLevel && newLevel != 3 && Exists(classList, username))
                    {
                        twitchClient.QueueWhisper(user, " DING! You just reached Level " + newLevel + "!  XP to next level: " + XpToNextLevel(username) + ".");
                        if (newLevel > 3)
                        {
                            if (Exists(classList, username))
                            {
                                classList[username].level = newLevel;
                                SaveClassData();
                            }
                        }
                    }

                    if (newLevel > prevLevel && newLevel >= 3 && classList != null & !classList.ContainsKey(username))
                    {
                        CharClass newChar = new CharClass();
                        newChar.classType = -1;
                        newChar.level = newLevel;
                        classList.Add(username.ToLower(), newChar);
                        twitchClient.QueueWhisper(user, " You've reached LEVEL " + newLevel + "! You get to choose a class for your character! Choose by whispering me one of the following: ");
                        twitchClient.QueueWhisper(user, " 'C1' (Warrior), 'C2' (Mage), 'C3' (Rogue), 'C4' (Ranger), or 'C5' (Cleric)");
                        SaveClassData();
                    }

                    if (newLevel < prevLevel)
                    {
                        twitchClient.QueueWhisper(user, " You lost a level! :( You're now level: " + newLevel);
                        if (Exists(classList, username))
                        {
                            classList[username].level = newLevel;
                            SaveClassData();
                        }
                    }
                }
                else
                {
                    xpList.Add(username, xp);
                    Logger.Info("Added user {user} and set their XP to {xp}.", username, xp);
                }
                SaveXP();

                return xp;
            }
            return -1;
        }

        public int SetCoins(int coins, string user)
        {

            if (coinList != null)
            {
                if (coins > COINMAX)
                    coins = COINMAX - 1;

                if (coins < 0)
                    coins = 0;

                if (coinList.Keys.Contains(user))
                {
                    coinList[user] = coins;
                    Logger.Info("Set {user}'s coins to {coins}.", user, coins);
                }
                else
                {
                    coinList.Add(user, coins);
                    Logger.Info("Added user {user} and set their coins to {coins}.", user, coins);
                }
                SaveCoins();

                return coins;
            }
            return -1;
        }

        public bool Exists(Dictionary<string, int> dic)
        {
            return (dic != null);
        }

        public bool Exists(Dictionary<string, int> dic, string user)
        {
            if (dic != null)
            {
                return dic.Keys.Contains(user);
            }

            return false;
        }

        public bool Exists(Dictionary<string, CharClass> dic, string user)
        {
            if (dic != null)
            {
                return dic.ContainsKey(user);
            }

            return false;
        }

        public bool CheckCoins(string user, int amount)
        {
            if (Exists(coinList, user))
            {
                if (amount <= coinList[user])
                {
                    return true;
                }
            }
            return false;
        }

        public bool AddCoins(User user, int coins)
        {
            if (coinList == null)
                return false;

            if (coinList.ContainsKey(user.Username))
            {
                try
                {
                    int prevCoins = coinList[user.Username];
                    checked
                    {
                        if (user.IsSub)
                        {
                            coins *= 2;
                        }
                        coinList[user.Username] += coins;
                    }
                    if (coinList[user.Username] > COINMAX)
                    {
                        coinList[user.Username] = COINMAX;
                    }

                    if (coinList[user.Username] < 0)
                        coinList[user.Username] = 0;


                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error("Error adding coins.");
                    Logger.Error(e);
                }

            }
            {
                if (!coinList.ContainsKey(user.Username))
                {
                    coinList.Add(user.Username, coins);

                }
                else
                {
                    return false;
                }

            }
            return false;

        }

        public int XpToNextLevel(string user)
        {
            if (Exists(xpList, user))
            {
                int myXP = xpList[user];
                int myLevel = determineLevel(myXP);
                int xpNextLevel = (int)(4 * (Math.Pow(myLevel + 1, 3)) + 50);
                if (myLevel == 1)
                    return (82 - myXP);

                return (xpNextLevel - myXP);
            }
            else
            {
                return -1;
            }
        }

        public bool AddXP(User user, string xp)
        {
            if (xpList == null)
                return false;

            if (xpList.ContainsKey(user.Username) && int.TryParse(xp, out int value))
            {
                try
                {
                    int prevXP = xpList[user.Username];
                    if (user.IsSub)
                    {
                        value *= 2;
                    }
                    checked
                    {
                        xpList[user.Username] += value;
                    }
                    //if (xpList[user] > MAX_XP)
                    //{
                    //    xpList[user] = MAX_XP - 1;
                    //}

                    if (xpList[user.Username] < 0)
                        xpList[user.Username] = 0;

                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error("Error adding xp.");
                    Logger.Error(e);
                }


            }
            if (!xpList.ContainsKey(user.Username) && int.TryParse(xp, out value))
            {
                if (value > MAX_XP)
                    value = MAX_XP - 1;

                if (value < 0)
                    value = 0;

                xpList.Add(user.Username, value);

            }
            else
            {
                return false;
            }

            return false;

        }

        public bool RemoveCoins(string user, int coins)
        {
            if (coinList == null)
                return false;

            if (coinList.ContainsKey(user))
            {
                if (coins > 0)
                {
                    try
                    {
                        int prevCoins = coinList[user];
                        checked
                        {
                            coinList[user] -= coins;
                        }
                        if (coinList[user] > COINMAX)
                            coinList[user] = COINMAX;

                        if (coinList[user] < 0)
                            coinList[user] = 0;

                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error removing coins.");
                        Logger.Error(e);
                    }
                }
            }
            return false;
        }

        public bool SaveCoins()
        {
            var json = JsonConvert.SerializeObject(coinList);
            var bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                File.WriteAllBytes(path, bytes);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Error saving coins file: ");
                Logger.Error(e);
                return false;
            }

        }

        public void ChangeClass(string user, int newClass, UserSystem userSystem, ITwitchClient twitchClient)
        {
            if (classList != null && coinList != null)
            {
                if (classList.Keys.Contains(user) && coinList.Keys.Contains(user))
                {
                    int respecCost = (baseRespecCost * (classList[user].level - 4));
                    if (respecCost < baseRespecCost)
                        respecCost = baseRespecCost;

                    if (classList[user].classType != -1 && coinList[user] >= respecCost)
                    {
                        classList[user].myItems = new List<Item>();
                        classList[user].classType = newClass;
                        RemoveCoins(user, respecCost);

                        string myClass = determineClass(user);
                        classList[user].className = myClass;
                        userSystem.GetUserByNameAsync(user, (userObj) =>
                        {
                            twitchClient.QueueWhisper(userObj, " Class successfully updated to " + myClass + "! " + respecCost + " deducted from your Wolfcoin balance.");
                        });

                        SaveClassData();
                        SaveCoins();
                    }
                    else if (coinList[user] < respecCost)
                    {
                        userSystem.GetUserByNameAsync(user, (userObj) =>
                        {
                            twitchClient.QueueWhisper(userObj, " It costs " + respecCost + " Wolfcoins to respec at your level. You have " + coinList[user] + " coins.");
                        });
                    }
                }
            }
        }

        public bool BackupData()
        {
            var json = JsonConvert.SerializeObject(xpList);
            var bytes = Encoding.UTF8.GetBytes(json);
            string backupPath = "backup/XP";
            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }
            var json2 = JsonConvert.SerializeObject(classList);
            var bytes2 = Encoding.UTF8.GetBytes(json2);
            string backupPath2 = "backup/ClassData";
            if (!Directory.Exists(backupPath2))
            {
                Directory.CreateDirectory(backupPath2);
            }
            var json3 = JsonConvert.SerializeObject(coinList);
            var bytes3 = Encoding.UTF8.GetBytes(json3);
            string backupPath3 = "backup/Coins";
            if (!Directory.Exists(backupPath3))
            {
                Directory.CreateDirectory(backupPath3);
            }
            DateTime now = DateTime.Now;
            backupPath = backupPath + now.Day + now.Month + now.Year + now.Hour + now.Minute + now.Second;
            backupPath2 = backupPath2 + now.Day + now.Month + now.Year + now.Hour + now.Minute + now.Second;
            backupPath3 = backupPath3 + now.Day + now.Month + now.Year + now.Hour + now.Minute + now.Second;
            try
            {
                File.WriteAllBytes(backupPath, bytes);
                File.WriteAllBytes(backupPath2, bytes2);
                File.WriteAllBytes(backupPath3, bytes3);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Error saving backup files");
                Logger.Error(e);
                return false;
            }

        }
        public bool SaveXP()
        {
            var json = JsonConvert.SerializeObject(xpList);
            var bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                File.WriteAllBytes(xpPath, bytes);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Error saving XP file");
                Logger.Error(e);
                return false;
            }

        }

        public bool SaveClassData()
        {
            var json = JsonConvert.SerializeObject(classList);
            var bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                File.WriteAllBytes(classPath, bytes);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Error saving Class data file");
                Logger.Error(e);
                return false;
            }

        }

        public void Init()
        {
            if (File.Exists(path))
            {
                coinList = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(path));
                Logger.Info("Wolfcoins collection loaded.");
            }
            else
            {
                coinList = new Dictionary<string, int>();
                Logger.Warn("Path not found. Coins initialized to default.");
            }

            if (File.Exists(xpPath))
            {
                xpList = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(xpPath));
                Logger.Info("Viewer XP loaded.");
            }
            else
            {
                xpList = new Dictionary<string, int>();
                Logger.Warn("Path not found. XP file initialized to default.");
            }

            if (File.Exists(classPath))
            {
                classList = JsonConvert.DeserializeObject<Dictionary<string, CharClass>>(File.ReadAllText(classPath));
                foreach (var player in classList)
                {
                    player.Value.groupID = -1;
                    player.Value.isPartyLeader = false;
                    player.Value.numInvitesSent = 0;
                    player.Value.pendingInvite = false;
                }
                Logger.Info("Class data loaded.");
            }
            else
            {
                classList = new Dictionary<string, CharClass>();
                Logger.Warn("Path not found. Class data file initialized to default.");
            }
        }


    }
}
