using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using GooglePlayGames.BasicApi;
using GooglePlayGames;
using UnityEngine.SocialPlatforms;
using UnityEngine;

[Serializable]
public class ShipUpgradesInfo
{
    public List<int> upgradeLevels;
}

[Serializable]
public class UpgradesInfo
{
    public List<ShipUpgradesInfo> shipUpgradeLevels;
}

[Serializable]
public class UnlockedSkinsInfo
{
    public List<int> unlockedSkins;
}

public static class AccountManager
{
    private static FirebaseAuth auth;
    private static DatabaseReference mDatabaseRef;
    private static DatabaseReference mNicknamesDB;
    private static DatabaseReference mQueueValueRef;

    private static UpgradesInfo m_upgradesInfo;
    private static UnlockedSkinsInfo m_skinsInfo;

    private static int m_lastDailyRewardDebugTime;
    private static string m_lastDailyRewardTime;
    private static int m_lastDailyReward;
    private static string m_currNicknameIdx = "";

    private static List<IAchievement> m_achievementsState;
    private static List<IAchievementDescription> m_achievementsDesc;
    private static bool m_achievementsLoaded = false;

    public static int LastDailyRewardDebugTime => m_lastDailyRewardDebugTime;
    public static string LastDailyRewardTime => m_lastDailyRewardTime;
    public static int LastDailyReward => m_lastDailyReward;

    private static bool m_tutorialDone = false;
    private static bool m_arenaTutorialDone = false;
    private static bool m_missionTutorialDone = false;
    private static bool m_controlTutorialDone = false;
    private static int m_tutorialStep = 0;

    private static string m_userName = "Player";
    private static int m_selectedShip = 0;
    private static int m_queueLength = 0;
    private static List<string> m_usedNicknamesList = new List<string>();

    private static int m_experience = 0;
    private static int m_level = 1;
    private static int m_currency = 1000;
    private static int m_arenaRating = 0;
    private static bool m_nameChanged = false;

    public static Action<int> OnQueueChanged;
    public static bool IsLoaded = false;
    public static bool IsNicknamesListLoaded = false;

    public static string UserName => m_userName;
    public static int SelectedShip { get { return m_selectedShip; } set { m_selectedShip = value; } }
    public static int CurrentRating { get { return m_arenaRating; } set { m_arenaRating = value; } }

    public static int FirstTutorialStep => m_tutorialStep;
    public static bool IsArenaTutorialDone => m_arenaTutorialDone;
    public static bool IsMissionTutorialDone => m_missionTutorialDone;

    public static bool IsControlTutorialDone => m_controlTutorialDone;

    public static bool IsFirstTutorialDone => m_tutorialDone;

    public static bool IsNameChanged => m_nameChanged;

    public static int Currency { get { return m_currency; } set { m_currency = value; Launcher.instance.OnCurrencyChanged(); } }
    public static int Level { get { return m_level; } set { m_level = value; Launcher.instance.OnLevelChanged(); } }
    public static int Exp { get { return m_experience; } set { m_experience = value; CheckExp(); Launcher.instance.OnExpChanged(); } }

    public static int LoginQueueLength => m_queueLength;

    private static void OnQueueLengthChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args == null || args.Snapshot == null || args.Snapshot.Value == null)
        {
            return;
        }

        m_queueLength = int.Parse(args.Snapshot.Value.ToString());
        OnQueueChanged?.Invoke(m_queueLength);
    }

    private static void GetLoginQueueLengthFromSnapshot(DataSnapshot snapshot)
    {
        m_queueLength = int.Parse(snapshot.Value.ToString());
    }

    private static void LoadNicknamesFromSnapshot(DataSnapshot snapshot)
    {
        m_usedNicknamesList.Clear();

        foreach (var nicknameData in snapshot.Children)
        {
            string name = nicknameData.Value.ToString();

            m_usedNicknamesList.Add(name);
        }

        IsNicknamesListLoaded = true;
    }

    private static void CheckUsedNicknames(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args == null || args.Snapshot == null || args.Snapshot.Value == null)
        {
            return;
        }

        LoadNicknamesFromSnapshot(args.Snapshot);
    }

    private static void OnApplicationPaused(bool pause)
    {
        if (Launcher.instance.CloseGameOnError || mDatabaseRef == null || !AuthController.IsAuthorized) return;

        if (pause)
            mDatabaseRef.Child(AuthController.UserID).Child("online").SetValueAsync(false);
        else
            mDatabaseRef.Child(AuthController.UserID).Child("online").SetValueAsync(true);
    }

    private static void OnApplicationQuit()
    {
        if (!Launcher.instance.CloseGameOnError && AuthController.IsAuthorized)
            mDatabaseRef.Child(AuthController.UserID).Child("online").SetValueAsync(false);

        m_queueLength--;
        mQueueValueRef.SetValueAsync(m_queueLength);

        AuthController.SignOut();
    }

    private static void LoadProfileFromSnapshot(DataSnapshot snapshot)
    {
        bool notEmptyProfile = snapshot != null && snapshot.ChildrenCount > 1;

        bool isOnline = false;
        string deviceId = AuthController.DeviceID;

        snapshot.GetValueFromSnapshot("online", false, out isOnline);
        snapshot.GetValueFromSnapshot("deviceId", AuthController.DeviceID, out deviceId);

        if (isOnline && AuthController.DeviceID != deviceId)
        {
            AuthController.OnSecondLoginRegistered();
            return;
        }

        snapshot.GetValueFromSnapshot("username", m_userName, out m_userName);
        snapshot.GetValueFromSnapshot("selectedShip", 0, out m_selectedShip);
        snapshot.GetValueFromSnapshot("nameChanged", false, out m_nameChanged);
        snapshot.GetValueFromSnapshot("arenaRating", BalanceProvider.Balance.initArenaRating, out m_arenaRating);
        snapshot.GetValueFromSnapshot("currency", BalanceProvider.Balance.initCurrency, out m_currency);
        snapshot.GetValueFromSnapshot("experience", 0, out m_experience);
        snapshot.GetValueFromSnapshot("level", 1, out m_level);

        AuthController.SetEmailIfEmpty(notEmptyProfile ? snapshot.Child("email").Value.ToString() : "");

        snapshot.GetValueFromSnapshot("lastDailyRewardDebugTime", 0, out m_lastDailyRewardDebugTime);
        snapshot.GetValueFromSnapshot("lastDailyRewardTime", "", out m_lastDailyRewardTime);
        snapshot.GetValueFromSnapshot("lastDailyReward", 0, out m_lastDailyReward);

        snapshot.GetValueFromSnapshot("tutorialDone", false, out m_tutorialDone);
        snapshot.GetValueFromSnapshot("arenaTutorialDone", false, out m_arenaTutorialDone);
        snapshot.GetValueFromSnapshot("missionTutorialDone", false, out m_missionTutorialDone);
        snapshot.GetValueFromSnapshot("controlTutorialDone", false, out m_controlTutorialDone);
        snapshot.GetValueFromSnapshot("tutorialStep", 0, out m_tutorialStep);

        snapshot.GetRawValueFromSnapshot("UpgradesInfo", out m_upgradesInfo);
        snapshot.GetRawValueFromSnapshot("SkinsInfo", out m_skinsInfo);

        IsLoaded = true;
    }

    public static bool IsAchievementUnlocked(string id)
    {
        foreach (var a in m_achievementsState)
        {
            if (a.id == id && a.completed) return true;
        }

        return false;
    }

    public static void UnlockAchievement(string id)
    {
        #if UNITY_ANDROID
        if (Social.localUser.authenticated && !IsAchievementUnlocked(id))
            (Social.Active as PlayGamesPlatform).UnlockAchievement(id, achievementUpdated);
        #endif
    }

    public static int GetNeededExpForCurrLevel()
    {
        return Mathf.FloorToInt(BalanceProvider.Balance.expForLevelBase * Mathf.Pow(BalanceProvider.Balance.expProgressionModifier, (m_level - 1) * BalanceProvider.Balance.expProgressionPowModifier));
    }

    private static void CheckExp()
    {
        int neededExp = GetNeededExpForCurrLevel();

        int expTotal = m_experience;
        int actualLevel = m_level;

        while( expTotal >= neededExp )
        {
            actualLevel++;
            expTotal -= neededExp;
            neededExp = GetNeededExpForCurrLevel();
        }

        Level = actualLevel;
        m_experience = expTotal;
    }

    private static void achievementUpdated(bool updated)
    {
        #if UNITY_ANDROID
        (Social.Active as PlayGamesPlatform).LoadAchievements(InitAchievements);
        #endif
    }

    public static void InitAchievements(IAchievement[] achievements)
    {
        m_achievementsState = new List<IAchievement>(achievements);

        m_achievementsLoaded = true;

        #if UNITY_ANDROID
        (Social.Active as PlayGamesPlatform).LoadAchievementDescriptions(InitAchievementsDesc);
        #endif
    }

    private static void InitAchievementsDesc(IAchievementDescription[] achievementsDesc)
    {
        m_achievementsDesc = new List<IAchievementDescription>(achievementsDesc);
    }

    public static void ReduceLoginQueueLength()
    {
        m_queueLength--;
        mQueueValueRef.SetValueAsync(m_queueLength);
    }

    public static void IncreaseLoginQueueLength()
    {
        m_queueLength++;
        mQueueValueRef.SetValueAsync(m_queueLength);
    }

    public static bool IsSkinUnlocked(int skinID)
    {
        CheckSkinsInfo();

        return m_skinsInfo.unlockedSkins.Contains(skinID);
    }

    public static void UnlockSkin(int skinID)
    {
        if (IsSkinUnlocked(skinID)) return;

        m_skinsInfo.unlockedSkins.Add(skinID);

        SaveProfile();
    }

    public static int GetUpgradeLevel(PlayerController ship, UpgradeData upgrade)
    {
        CheckUpgradesInfo();

        int shipIdx = Launcher.instance.GetShipNumber(ship);
        int upgradeIdx = ship.GetUpgradeNumber(upgrade);

        return m_upgradesInfo.shipUpgradeLevels[shipIdx].upgradeLevels[upgradeIdx];
    }

    public static bool IsUpgradeAvailable(PlayerController ship, UpgradeData upgrade, out bool isMaxLevel)
    {
        CheckUpgradesInfo();

        int shipIdx = Launcher.instance.GetShipNumber(ship);
        int upgradeIdx = ship.GetUpgradeNumber(upgrade);

        int currLvl = m_upgradesInfo.shipUpgradeLevels[shipIdx].upgradeLevels[upgradeIdx];

        int currentCost = upgrade.cost + Mathf.CeilToInt(currLvl * upgrade.cost * 0.5f);

        isMaxLevel = currLvl >= upgrade.maxUpgradeLevels;

        return currLvl < upgrade.maxUpgradeLevels && Currency >= currentCost;
    }

    public static int GetUpgradeLevel(int shipNumber, int upgradeNumber)
    {
        CheckUpgradesInfo();

        return m_upgradesInfo.shipUpgradeLevels[shipNumber].upgradeLevels[upgradeNumber];
    }

    public static void IncreaseUpgradeLevel(PlayerController ship, UpgradeData upgrade)
    {
        CheckUpgradesInfo();

        int shipIdx = Launcher.instance.GetShipNumber(ship);
        int upgradeIdx = ship.GetUpgradeNumber(upgrade);

        m_upgradesInfo.shipUpgradeLevels[shipIdx].upgradeLevels[upgradeIdx] += 1;

        SaveProfile();
    }

    public static void SetUpgradeLevel(PlayerController ship, UpgradeData upgrade, int level)
    {
        CheckUpgradesInfo();

        int shipIdx = Launcher.instance.GetShipNumber(ship);
        int upgradeIdx = ship.GetUpgradeNumber(upgrade);

        m_upgradesInfo.shipUpgradeLevels[shipIdx].upgradeLevels[upgradeIdx] = level;

        SaveProfile();
    }

    public static void SetUpgradeLevel(int shipNumber, int upgradeNumber, int level)
    {
        CheckUpgradesInfo();

        m_upgradesInfo.shipUpgradeLevels[shipNumber].upgradeLevels[upgradeNumber] = level;

        SaveProfile();
    }

    public static void CheckUpgradesInfo()
    {
        if (m_upgradesInfo.shipUpgradeLevels == null || m_upgradesInfo.shipUpgradeLevels.Count == 0)
        {
            m_upgradesInfo.shipUpgradeLevels = new List<ShipUpgradesInfo>();

            for (int i = 0; i < Launcher.instance.AvailableShips.Count; i++)
            {
                ShipUpgradesInfo sui = new ShipUpgradesInfo();
                sui.upgradeLevels = new List<int>();

                for (int u = 0; u < Launcher.instance.AvailableShips[i].Upgrades.Count; u++)
                {
                    sui.upgradeLevels.Add(0);
                }

                m_upgradesInfo.shipUpgradeLevels.Add(sui);
            }
        }
    }

    public static void CheckSkinsInfo()
    {
        if (m_skinsInfo.unlockedSkins == null || m_skinsInfo.unlockedSkins.Count == 0)
        {
            m_skinsInfo.unlockedSkins = new List<int>();
        }
    }

    public static void SaveTutorialStep()
    {
        mDatabaseRef.Child(AuthController.UserID).Child("tutorialStep").SetValueAsync(TutorialController.instance.TutorialStep);
    }

    public static void OnArenaTutorialDone()
    {
        m_arenaTutorialDone = true;
        SaveProfile();
    }

    public static void OnMissionTutorialDone()
    {
        m_missionTutorialDone = true;
        SaveProfile();
    }

    public static void OnControlTutorialDone()
    {
        m_controlTutorialDone = true;
        SaveProfile();
    }

    public static void OnFirstTutorialDone()
    {
        m_tutorialDone = true;
        SaveProfile();
    }

    public static void OnDailyRewardsChanged(int lastDailyRewardDebugTime, string lastDailyRewardTime, int lastDailyReward)
    {
        m_lastDailyRewardDebugTime = lastDailyRewardDebugTime;
        m_lastDailyRewardTime = lastDailyRewardTime;
        m_lastDailyReward = lastDailyReward;

        SaveProfile();
    }

    public static void SaveProfile()
    {
        if (!AuthController.IsAuthorized || !IsLoaded) return;

        DatabaseReference userTable = mDatabaseRef.Child(AuthController.UserID);

        userTable.Child("username").SetValueAsync(m_userName);
        userTable.Child("selectedShip").SetValueAsync(m_selectedShip);
        userTable.Child("nameChanged").SetValueAsync(m_nameChanged);
        userTable.Child("email").SetValueAsync(AuthController.Email);
        userTable.Child("arenaRating").SetValueAsync(m_arenaRating);
        userTable.Child("currency").SetValueAsync(m_currency);
        userTable.Child("experience").SetValueAsync(m_experience);
        userTable.Child("level").SetValueAsync(m_level);

        userTable.Child("lastDailyRewardDebugTime").SetValueAsync(m_lastDailyRewardDebugTime);
        userTable.Child("lastDailyRewardTime").SetValueAsync(m_lastDailyRewardTime);
        userTable.Child("lastDailyReward").SetValueAsync(m_lastDailyReward);

        userTable.Child("tutorialDone").SetValueAsync(m_tutorialDone);
        userTable.Child("arenaTutorialDone").SetValueAsync(m_arenaTutorialDone);
        userTable.Child("missionTutorialDone").SetValueAsync(m_missionTutorialDone);
        userTable.Child("controlTutorialDone").SetValueAsync(m_controlTutorialDone);

        CheckUpgradesInfo();
        CheckSkinsInfo();

        string saveData = JsonUtility.ToJson(m_upgradesInfo);

        userTable.Child("UpgradesInfo").SetRawJsonValueAsync(saveData);

        saveData = JsonUtility.ToJson(m_skinsInfo);

        userTable.Child("SkinsInfo").SetRawJsonValueAsync(saveData);
    }

    public static void SetUserName(string name)
    {
        if (!name.Equals(m_userName)) m_nameChanged = true;
        m_userName = name;
        SaveProfile();
        mNicknamesDB.Child(AuthController.UserID).SetValueAsync(m_userName);
    }

    public static bool IsNicknameUsed(string name)
    {
        return m_usedNicknamesList.Contains(name);
    }

    public static void Init()
    {
        auth = FirebaseAuth.DefaultInstance;

        Launcher.instance.OnApplicationPaused += OnApplicationPaused;
        Launcher.instance.OnApplicationExit += OnApplicationQuit;

        mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference.Child("users");
        mNicknamesDB = FirebaseDatabase.DefaultInstance.RootReference.Child("nicknames");
        mQueueValueRef = FirebaseDatabase.DefaultInstance.RootReference.Child("queueLength");

        mNicknamesDB.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {

            }
            else if (task.IsCompleted)
            {
                LoadNicknamesFromSnapshot(task.Result);

                mQueueValueRef.GetValueAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {

                    }
                    else if (task.IsCompleted)
                    {
                        GetLoginQueueLengthFromSnapshot(task.Result);
                    }
                });
            }
        });
    }

    public static void GenerateUserName()
    {
        m_userName = "Player" + m_usedNicknamesList.Count.ToString();
    }

    public static void SaveCredentials(string email, bool isNewAccount, Action callback = null)
    {
        mDatabaseRef.Child(AuthController.UserID).Child("email").SetValueAsync(email).ContinueWith(task =>
        {
            if (isNewAccount)
            {
                mNicknamesDB.Child(AuthController.UserID).SetValueAsync(m_userName).ContinueWith(task =>
                {
                    callback?.Invoke();
                });
            }
            else
                callback?.Invoke();
        });
    }

    public static void LoadProfile(bool defaultProfile = false)
    {
        if (!defaultProfile && mDatabaseRef.Child(AuthController.UserID) != null)
        {
            mDatabaseRef.Child(AuthController.UserID).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {

                }
                else if (task.IsCompleted)
                {
                    LoadProfileFromSnapshot(task.Result);
                }
            });
        }
        else
            LoadProfileFromSnapshot(null);
    }

    public static void SetOnline()
    {
        mDatabaseRef.Child(AuthController.UserID).Child("online").SetValueAsync(true);
        mDatabaseRef.Child(AuthController.UserID).Child("deviceId").SetValueAsync(AuthController.DeviceID);

        mNicknamesDB.Child(AuthController.UserID).SetValueAsync(m_userName);

        mNicknamesDB.ValueChanged += CheckUsedNicknames;
    }
}
