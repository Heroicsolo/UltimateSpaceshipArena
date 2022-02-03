using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Firebase.Auth;
using System.Collections;
using Firebase.Database;
using TMPro;
using System.Collections.Generic;
using System;
using Photon.Chat;
using ExitGames.Client.Photon;
using Google;
using System.Threading.Tasks;
using GooglePlayGames.BasicApi;
using GooglePlayGames;
using UnityEngine.SocialPlatforms;

[Serializable]
public class BalanceInfo
{
    public int techWorks;
    public int techWorksDelay;
    public int version;
    public float fightLength;
    public int initArenaRating;
    public float joinStageLength;
    public float lobbyLength;
    public float lossRatingMod;
    public int matchmakingGap;
    public int maxPlayersPerRoom;
    public float nexusCaptureTime;
    public float victoryRatingMod;
    public float respawnTimeBase;
    public float respawnTimeMax;
    public float spectacleTime;
    public int winnersCount;
    public float shieldRegenDelay;
    public int initCurrency;
    public int currencyPerFightMin;
    public int currencyPerWin;
    public int currencyPlaceBonus;
    public int currencyPerMissionMin;
    public float missionTimeRewardModifier;
}

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

public class Launcher : MonoBehaviourPunCallbacks, IMatchmakingCallbacks, IChatClientListener
{
    public static Launcher instance;

    #region Private Serializable Fields


    #endregion


    #region Private Fields


    /// <summary>
    /// This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking changes).
    /// </summary>
    const string gameVersion = "5";
    const int clientVersion = 3;
    const string chatAppId = "e1f0448d-06a1-40c0-8653-93ffc1b0bee7";
    [SerializeField] private int initArenaRating = 1000;
    [SerializeField] private int maxChatMessagesCount = 20;
    [SerializeField] private GameObject m_loginScreen;
    [SerializeField] private GameObject m_signupScreen;
    [SerializeField] private GameObject m_loadingScreen;
    [SerializeField] private InputField m_inputEmail;
    [SerializeField] private InputField m_inputPass;
    [SerializeField] private InputField m_inputSignUpEmail;
    [SerializeField] private InputField m_inputSignUpPass;
    [SerializeField] private InputField m_inputName;
    [SerializeField] private TextMeshProUGUI m_loadingText;
    [SerializeField] private TextMeshProUGUI m_loginQueueText;
    [SerializeField] private TextMeshProUGUI m_playersCountText;
    [SerializeField] private GameObject m_loadingGears;
    [SerializeField] private Toggle defaultShipToggle;
    [SerializeField] private GameObject m_canvas;
    [SerializeField] private GameObject m_homeScreen;
    [SerializeField] AudioClip buttonSound;
    [SerializeField] private TextMeshProUGUI userIdLabel;
    [SerializeField] private Transform chatContent;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private TMP_InputField chatMessageField;
    [SerializeField] private GameObject chatLoadingIndicator;
    [SerializeField] private TextMeshProUGUI currencyLabel;
    [SerializeField] private TextMeshProUGUI ratingLabel;
    [SerializeField] private List<PlayerController> availableShips;
    [SerializeField] private List<GameObject> hangarShips;

    private UpgradesInfo m_upgradesInfo;
    private BalanceInfo m_balanceData;
    private Firebase.FirebaseApp app = null;
    private FirebaseAuth auth;
    private string m_userName = "Player";
    private string m_email = "";
    private string m_password = "";
    private string m_userId = "";
    private string m_loginError = "";
    private string authCode = "";
    private string m_deviceId = "";

    private int m_currency = 1000;
    private int m_arenaRating = 0;
    private int m_loginQueueLength = 0;

    private bool m_signingIn = false;
    private bool m_signedIn = false;
    private bool m_signInFailed = false;
    private bool m_GP_initialized = false;
    private bool m_playGamesSignInEnded = false;
    private bool m_playGamesSignInSuccess = false;
    private bool m_googlePlayConnected = false;
    private bool m_loadedProfile = false;
    private bool m_closeGameOnError = false;
    private bool m_DB_loaded = false;
    private bool m_credentialsSaved = false;
    private bool m_newProfile = false;
    private bool m_loadedBalance = false;

    private bool isConnecting;
    private bool isConnectedToMaster;
    private bool isRoomCreating = false;
    private bool isRoomLoading = false;

    private List<IAchievement> m_achievementsState;
    private List<IAchievementDescription> m_achievementsDesc;
    private bool m_achievementsLoaded = false;

    private bool isSoundOn = true;

    private GameObject m_selectedShip;

    private DatabaseReference mDatabaseRef;
    private DatabaseReference mNicknamesDB;
    private DatabaseReference mQueueValueRef;
    private DatabaseReference mBalanceDatabaseRef;

    private AudioSource audioSource;

    private ChatClient chatClient;
    private List<GameObject> chatMessages = new List<GameObject>();

    private List<string> m_usedNicknamesList = new List<string>();

    #endregion


    #region MonoBehaviour CallBacks

    public string UserID => m_userId;
    public string UserName => m_userName;

    public int CurrentRating => m_arenaRating;
    public GameObject SelectedShipPrefab { get { return m_selectedShip; } set { m_selectedShip = value; SelectHangarShip(m_selectedShip.name); } }

    public BalanceInfo Balance { get { return m_balanceData; } }

    public int Currency { get { return m_currency; } set { m_currency = value; currencyLabel.text = m_currency.ToString(); } }

    public bool IsSoundOn { get { return isSoundOn; } set { isSoundOn = value; PlayerPrefs.SetInt("soundOn", isSoundOn ? 1 : 0); } }

    public const string ELO_PROP_KEY = "C0";
    public const string MAP_PROP_KEY = "C1";
    private TypedLobby sqlLobby = new TypedLobby("customSqlLobby", LobbyType.SqlLobby);
    private LoadBalancingClient loadBalancingClient;
    private string selectedMap = "Arena00";

    void SelectHangarShip(string shipName)
    {
        foreach (var ship in hangarShips)
        {
            ship.SetActive(ship.name == shipName);
        }
    }

    void HideHangarShips()
    {
        foreach (var ship in hangarShips)
        {
            ship.SetActive(false);
        }
    }

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
    /// </summary>
    void Awake()
    {
        if (!instance)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        m_deviceId = SystemInfo.deviceUniqueIdentifier;

        audioSource = GetComponentInChildren<AudioSource>();

        Input.multiTouchEnabled = true;

        // #Critical
        // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
        PhotonNetwork.AutomaticallySyncScene = true;

        isSoundOn = PlayerPrefs.GetInt("soundOn", 1) == 1;

        AudioListener.volume = IsSoundOn ? 1f : 0f;

        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder().RequestServerAuthCode(false).Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.Activate();

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                app = Firebase.FirebaseApp.DefaultInstance;

                InitFirebase();
                // Set a flag here to indicate whether Firebase is ready to use by your app.
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                    "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });

        isConnectedToMaster = false;
    }

    public void PlayButtonSound()
    {
        audioSource.PlayOneShot(buttonSound);
    }

    private void InitFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;

        mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference.Child("users");
        mNicknamesDB = FirebaseDatabase.DefaultInstance.RootReference.Child("nicknames");
        mQueueValueRef = FirebaseDatabase.DefaultInstance.RootReference.Child("queueLength");
        mBalanceDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference.Child("balance");

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

                        mBalanceDatabaseRef.GetValueAsync().ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {

                            }
                            else if (task.IsCompleted)
                            {
                                RefreshBalance(task.Result);
                                m_DB_loaded = true;
                            }
                        });
                    }
                });
            }
        });

        mNicknamesDB.ValueChanged += CheckUsedNicknames;
        mQueueValueRef.ValueChanged += OnQueueLengthChanged;
        mBalanceDatabaseRef.ValueChanged += HandleBalanceValueChanged;
    }

    private void OnQueueLengthChanged(object sender, ValueChangedEventArgs args)
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

        m_loginQueueLength = int.Parse(args.Snapshot.Value.ToString());
        m_loginQueueText.text = "Your position in queue: " + m_loginQueueLength.ToString();
    }

    private void GetLoginQueueLengthFromSnapshot(DataSnapshot snapshot)
    {
        m_loginQueueLength = int.Parse(snapshot.Value.ToString());
    }

    private void CheckUsedNicknames(object sender, ValueChangedEventArgs args)
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

    private void RefreshBalance(DataSnapshot snapshot)
    {
        string restoredData = snapshot.GetRawJsonValue();

        if (restoredData.Length < 2)
        {
            m_balanceData = new BalanceInfo();
        }
        else
            m_balanceData = JsonUtility.FromJson<BalanceInfo>(restoredData);

        m_loadedBalance = true;
    }

    void HandleBalanceValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        RefreshBalance(args.Snapshot);

        if (Balance.version > clientVersion)
        {
            MessageBox.instance.Show("Your game client version is old. Please, update it on Google Play.");
            CloseGameDelayed();
        }
        else if (Balance.techWorks != 0)
        {
            if (Balance.techWorksDelay < 1)
            {
                MessageBox.instance.Show("The game is currently under maintenance. We are sorry for the inconvenience.");
            }
            else
            {
                MessageBox.instance.Show(string.Format("The game will be under maintenance in {0} minutes. We are sorry for the inconvenience.", Balance.techWorksDelay));
                CloseGameDelayed(Balance.techWorksDelay * 60f);
            }
        }
    }

    public void CloseGameDelayed(float delay = 3f)
    {
        StartCoroutine(CloseGameAfterDelay(delay));
    }

    private IEnumerator CloseGameAfterDelay(float delay = 3f)
    {
        yield return new WaitForSecondsRealtime(delay);

        Application.Quit();
    }

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during initialization phase.
    /// </summary>
    void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
            StartCoroutine(AwaitForProfile());
    }

    private void SignInViaPlayGames()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        SignInWithPlayGamesOnFirebase(authCode);
        #else
        m_playGamesSignInEnded = true;
        m_playGamesSignInSuccess = false;
        #endif
    }

    private void InitGP()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        Social.localUser.Authenticate((bool success) => {
            if( success )
            {
                authCode = PlayGamesPlatform.Instance.GetServerAuthCode();
                m_GP_initialized = true;
                (Social.Active as PlayGamesPlatform).LoadAchievements(InitAchievements);
            }
            else
            {
                m_GP_initialized = false;
            }
        });
        #else
        m_GP_initialized = true;
        #endif
    }

    private IEnumerator AwaitForProfile(bool skipSignIn = false)
    {
        m_loadingScreen.SetActive(true);

        m_loadingText.text = "CONNECTING TO SERVER...";

        yield return new WaitUntil(() => m_DB_loaded);

        m_loadingText.text = "CONNECTING TO GOOGLE PLAY...";

        InitGP();

        yield return new WaitUntil(() => m_GP_initialized);

        if (!skipSignIn)
        {
            m_loadingText.text = "SIGNING IN...";

            bool emailSigningIn = TrySignIn();

            if (!emailSigningIn)
            {
                SignInViaPlayGames();

                yield return new WaitUntil(() => m_playGamesSignInEnded);

                if (!m_playGamesSignInSuccess)
                {
                    OpenLoginScreen();
                }
            }

            yield return new WaitUntil(() => m_signedIn);

            SaveCredentials(false, m_signupScreen.activeSelf, m_playGamesSignInSuccess);

            m_signupScreen.SetActive(false);

            CloseLoginScreen();

            yield return new WaitUntil(() => m_credentialsSaved);
        }

        m_loadingText.text = "LOADING PROFILE...";

        LoadProfile(m_newProfile);

        yield return new WaitUntil(() => m_loadedProfile);

        if (m_signedIn)
        {
            SetOnline();
        }

        m_loadingText.text = "CONNECTING TO MASTER SERVER...";

        Connect();

        yield return new WaitUntil(() => isConnectedToMaster);

        GetProfileData();

        userIdLabel.text = "User ID: " + UserID;

        ratingLabel.text = m_arenaRating.ToString();
        currencyLabel.text = m_currency.ToString();

        chatClient = new ChatClient(this);

        chatClient.ChatRegion = "EU";
        chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion, new Photon.Chat.AuthenticationValues(UserName));
    }

    #endregion


    #region Public Methods

    public void ShowLeaderBoard()
    {
        ((PlayGamesPlatform)Social.Active).ShowLeaderboardUI(GPGSIds.leaderboard_arena);
    }

    public void AddScoreToLeaderBoard()
    {
        if (Social.localUser.authenticated) {
            Social.ReportScore(m_arenaRating, GPGSIds.leaderboard_arena, (bool success) => {});
        }
    }

    public void OnMainScreenLoaded()
    {
        if (chatClient.State == ChatState.Disconnected)
            chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion, new Photon.Chat.AuthenticationValues(UserName));
    }

    public void OnShipSelectorOpened()
    {
        defaultShipToggle.isOn = true;
        defaultShipToggle.Select();
    }

    public void GetProfileData()
    {
        m_loadingScreen.SetActive(false);

        SaveProfile();
    }

    public int OnMissionCompleted(float completionTime)
    {
        int moneyGained = Balance.currencyPerMissionMin + Mathf.CeilToInt((180f / (10f + completionTime)) * Balance.missionTimeRewardModifier * 100);
        m_currency += moneyGained;
        SaveProfile();
        UnlockAchievement(GPGSIds.achievement_mission_is_possible);
        currencyLabel.text = m_currency.ToString();
        return moneyGained;
    }

    public void OnMissionFailed()
    {

    }

    public int OnFightLoss()
    {
        m_currency += Balance.currencyPerFightMin;
        m_arenaRating = Mathf.Max(0, m_arenaRating - Mathf.FloorToInt(0.1f * Balance.lossRatingMod * m_arenaRating));
        SaveProfile();
        AddScoreToLeaderBoard();
        UnlockAchievement(GPGSIds.achievement_first_steps_in_space);
        currencyLabel.text = m_currency.ToString();
        ratingLabel.text = m_arenaRating.ToString();
        return Balance.currencyPerFightMin;
    }

    public int OnFightWon(int place = 1)
    {
        int bonusForPlace = (Balance.winnersCount - place) * 100;
        int moneyGained = Balance.currencyPerFightMin + Balance.currencyPerWin + Balance.currencyPlaceBonus * (Balance.winnersCount - place);
        m_currency += moneyGained;
        m_arenaRating = Mathf.Max(0, m_arenaRating + Mathf.CeilToInt((bonusForPlace + 200) * Balance.victoryRatingMod * 2000f / Mathf.Max(1000f, m_arenaRating)));
        SaveProfile();
        AddScoreToLeaderBoard();
        UnlockAchievement(GPGSIds.achievement_first_steps_in_space);
        currencyLabel.text = m_currency.ToString();
        ratingLabel.text = m_arenaRating.ToString();
        return moneyGained;
    }

    public void SetOnline()
    {
        mDatabaseRef.Child(m_userId).Child("online").SetValueAsync(true);
        mDatabaseRef.Child(m_userId).Child("deviceId").SetValueAsync(m_deviceId);
    }

    public bool IsAchievementUnlocked(string id)
    {
        foreach( var a in m_achievementsState )
        {
            if( a.id == id && a.completed ) return true;
        }

        return false;
    }

    public void UnlockAchievement(string id)
    {
        if (Social.localUser.authenticated && !IsAchievementUnlocked(id))
            (Social.Active as PlayGamesPlatform).UnlockAchievement(id, achievementUpdated);
    }

    private void achievementUpdated(bool updated)
    {
        (Social.Active as PlayGamesPlatform).LoadAchievements(InitAchievements);
    }

    private void InitAchievements(IAchievement[] achievements)
    {
        m_achievementsState = new List<IAchievement>(achievements);

        m_achievementsLoaded = true;

        (Social.Active as PlayGamesPlatform).LoadAchievementDescriptions(InitAchievementsDesc);
    }

    private void InitAchievementsDesc(IAchievementDescription[] achievementsDesc)
    {
        m_achievementsDesc = new List<IAchievementDescription>(achievementsDesc);
    }

    public void SaveCredentials(bool onlyLocal = false, bool isNewAccount = false, bool isPlayGames = false)
    {
        if (!isPlayGames)
        {
            PlayerPrefs.SetString("email", m_email);
            PlayerPrefs.SetString("pass", m_password);
        }

        if (isNewAccount)
            m_userName = m_inputName.text;

        if (onlyLocal) { m_credentialsSaved = true; return; }

        mDatabaseRef.Child(m_userId).Child("email").SetValueAsync(m_email).ContinueWith(task =>
        {
            if (isNewAccount)
            {
                mNicknamesDB.Child(m_usedNicknamesList.Count.ToString()).SetValueAsync(m_userName).ContinueWith(task =>
                {
                    m_credentialsSaved = true;
                });
            }
            else
                m_credentialsSaved = true;
        });
    }

    public int GetShipNumber(PlayerController ship)
    {
        return availableShips.FindIndex(x => x.ID == ship.ID);
    }

    public int GetUpgradeLevel(PlayerController ship, UpgradeData upgrade)
    {
        CheckUpgradesInfo();

        int shipIdx = GetShipNumber(ship);
        int upgradeIdx = ship.GetUpgradeNumber(upgrade);

        return m_upgradesInfo.shipUpgradeLevels[shipIdx].upgradeLevels[upgradeIdx];
    }

    public int GetUpgradeLevel(int shipNumber, int upgradeNumber)
    {
        CheckUpgradesInfo();

        return m_upgradesInfo.shipUpgradeLevels[shipNumber].upgradeLevels[upgradeNumber];
    }

    public void IncreaseUpgradeLevel(PlayerController ship, UpgradeData upgrade)
    {
        CheckUpgradesInfo();

        int shipIdx = GetShipNumber(ship);
        int upgradeIdx = ship.GetUpgradeNumber(upgrade);

        m_upgradesInfo.shipUpgradeLevels[shipIdx].upgradeLevels[upgradeIdx] += 1;

        SaveProfile();
    }

    public void SetUpgradeLevel(PlayerController ship, UpgradeData upgrade, int level)
    {
        CheckUpgradesInfo();

        int shipIdx = GetShipNumber(ship);
        int upgradeIdx = ship.GetUpgradeNumber(upgrade);

        m_upgradesInfo.shipUpgradeLevels[shipIdx].upgradeLevels[upgradeIdx] = level;

        SaveProfile();
    }

    public void SetUpgradeLevel(int shipNumber, int upgradeNumber, int level)
    {
        CheckUpgradesInfo();

        m_upgradesInfo.shipUpgradeLevels[shipNumber].upgradeLevels[upgradeNumber] = level;

        SaveProfile();
    }

    void CheckUpgradesInfo()
    {
        if (m_upgradesInfo.shipUpgradeLevels == null || m_upgradesInfo.shipUpgradeLevels.Count == 0)
        {
            m_upgradesInfo.shipUpgradeLevels = new List<ShipUpgradesInfo>();

            for (int i = 0; i < availableShips.Count; i++)
            {
                ShipUpgradesInfo sui = new ShipUpgradesInfo();
                sui.upgradeLevels = new List<int>();

                for (int u = 0; u < availableShips[i].Upgrades.Count; u++)
                {
                    sui.upgradeLevels.Add(0);
                }

                m_upgradesInfo.shipUpgradeLevels.Add(sui);
            }
        }
    }

    public void SaveProfile()
    {
        if (!m_signedIn || !m_loadedProfile) return;

        DatabaseReference userTable = mDatabaseRef.Child(m_userId);

        userTable.Child("username").SetValueAsync(m_userName);
        userTable.Child("email").SetValueAsync(m_email);
        userTable.Child("arenaRating").SetValueAsync(m_arenaRating);
        userTable.Child("currency").SetValueAsync(m_currency);

        CheckUpgradesInfo();

        string saveData = JsonUtility.ToJson(m_upgradesInfo);

        userTable.Child("UpgradesInfo").SetRawJsonValueAsync(saveData);
    }

    public void LoadProfile(bool defaultProfile = false)
    {
        if (!defaultProfile && mDatabaseRef.Child(m_userId) != null)
        {
            mDatabaseRef.Child(m_userId).GetValueAsync().ContinueWith(task =>
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

    private void OnApplicationPause(bool pause)
    {
        if (m_closeGameOnError || mDatabaseRef == null || !m_signedIn) return;

        if (pause)
            mDatabaseRef.Child(m_userId).Child("online").SetValueAsync(false);
        else
            mDatabaseRef.Child(m_userId).Child("online").SetValueAsync(true);
    }

    private void OnApplicationQuit()
    {
        if (!m_closeGameOnError && m_signedIn)
            mDatabaseRef.Child(m_userId).Child("online").SetValueAsync(false);

        m_loginQueueLength--;
        mQueueValueRef.SetValueAsync(m_loginQueueLength);

        SignOut();

        if (chatClient != null)
        {
            chatClient.Disconnect();
        }
    }

    private void LoadNicknamesFromSnapshot(DataSnapshot snapshot)
    {
        m_usedNicknamesList.Clear();

        foreach (var nicknameData in snapshot.Children)
        {
            m_usedNicknamesList.Add(nicknameData.Value.ToString());
        }
    }

    private void LoadProfileFromSnapshot(DataSnapshot snapshot)
    {
        bool notEmptyProfile = snapshot != null && snapshot.ChildrenCount > 1;

        bool isOnline = notEmptyProfile ? bool.Parse(snapshot.Child("online").Value.ToString()) : false;

        string deviceId = notEmptyProfile && snapshot.HasChild("deviceId") ? snapshot.Child("deviceId").Value.ToString() : m_deviceId;

        if (isOnline && m_deviceId != deviceId)
        {
            m_loginError = "Someone is already logged in your account. Game will be closed.";
            m_closeGameOnError = true;
            m_signInFailed = true;

            return;
        }

        m_userName = notEmptyProfile && snapshot.HasChild("username") ? snapshot.Child("username").Value.ToString() : m_inputName.text;

        if (m_email.Length < 2)
            m_email = notEmptyProfile ? snapshot.Child("email").Value.ToString() : "";

        m_arenaRating = notEmptyProfile && snapshot.HasChild("arenaRating") ? int.Parse(snapshot.Child("arenaRating").Value.ToString()) : Balance.initArenaRating;

        m_currency = notEmptyProfile && snapshot.HasChild("currency") ? int.Parse(snapshot.Child("currency").Value.ToString()) : Balance.initCurrency;

        string restoredData = "";

        if (notEmptyProfile)
            restoredData = snapshot.Child("UpgradesInfo").GetRawJsonValue();

        if (restoredData == null || restoredData.Length < 2)
            m_upgradesInfo = new UpgradesInfo();
        else
            m_upgradesInfo = JsonUtility.FromJson<UpgradesInfo>(restoredData);

        m_loadedProfile = true;
    }

    public bool TrySignIn()
    {
        m_email = PlayerPrefs.GetString("email", "");
        m_password = PlayerPrefs.GetString("pass", "");

        string googleToken = PlayerPrefs.GetString("googleId", "");

        if (googleToken.Length > 2)
        {
            m_signingIn = true;
            SignInWithGoogleOnFirebase(googleToken);
            return true;
        }
        else if (m_email.Length > 2)
        {
            StartCoroutine(WaitForLoginFail());
            m_signingIn = true;
            SignIn(m_email, m_password);
            return true;
        }

        return false;
    }

    public void SignUp()
    {
        if (m_signingIn) return;

        if (m_inputName.text.Length < 2)
        {
            MessageBox.instance.Show("Your nickname is too short!");
            return;
        }

        if (m_inputSignUpEmail.text.Length < 6)
        {
            MessageBox.instance.Show("Incorrect email!");
            return;
        }

        if (m_inputSignUpPass.text.Length < 4)
        {
            MessageBox.instance.Show("Password is too short!");
            return;
        }

        if (m_usedNicknamesList.Contains(m_inputName.text))
        {
            MessageBox.instance.Show("This nickname is already taken!");
            return;
        }

        m_signingIn = true;

        StartCoroutine(WaitForLoginFail());

        CreateAccount(m_inputSignUpEmail.text, m_inputSignUpPass.text);
    }

    public void SignIn()
    {
        if (m_signingIn) return;

        m_signingIn = true;

        StartCoroutine(WaitForLoginFail());

        SignIn(m_inputEmail.text, m_inputPass.text);
    }

    public void SignUpWithGoogle()
    {
        if (m_signingIn) return;

        if (m_inputName.text.Length < 2)
        {
            MessageBox.instance.Show("Your nickname is too short!");
            return;
        }

        if (m_inputSignUpEmail.text.Length < 6)
        {
            MessageBox.instance.Show("Incorrect email!");
            return;
        }

        if (m_inputSignUpPass.text.Length < 4)
        {
            MessageBox.instance.Show("Password is too short!");
            return;
        }

        if (m_usedNicknamesList.Contains(m_inputName.text))
        {
            MessageBox.instance.Show("This nickname is already taken!");
            return;
        }

        m_signingIn = true;

        StartCoroutine(WaitForLoginFail());

        SignInWithGoogle();
    }

    public void SignInWithGoogle()
    {
        GoogleSignIn.DefaultInstance?.SignIn().ContinueWith(OnGoogleAuthFinished);
    }

    void OnGoogleAuthFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsCompleted)
            SignInWithGoogleOnFirebase(task.Result.IdToken);
    }

    private void SignInWithPlayGamesOnFirebase(string idToken)
    {
        Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        Firebase.Auth.Credential credential =
            Firebase.Auth.PlayGamesAuthProvider.GetCredential(idToken);

        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithCredentialAsync was canceled.");
                m_loginError = "Logging in was canceled.";
                m_signingIn = false;
                m_playGamesSignInSuccess = false;
                m_playGamesSignInEnded = true;
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithCredentialAsync encountered an error: " + task.Exception);
                m_loginError = task.Exception.Message;
                m_signingIn = false;
                m_playGamesSignInSuccess = true;
                m_playGamesSignInEnded = true;
                return;
            }

            Firebase.Auth.FirebaseUser newUser = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                newUser.DisplayName, newUser.UserId);

            m_email = newUser.Email;
            m_userId = newUser.UserId;
            m_userName = newUser.DisplayName;
            m_password = "";

            m_signInFailed = false;
            m_signedIn = true;
            m_signingIn = false;
            m_playGamesSignInSuccess = false;
            m_playGamesSignInEnded = true;
        });
    }

    private void SignInWithGoogleOnFirebase(string idToken)
    {
        Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithGoogleOnFirebase was canceled.");
                m_loginError = "Logging in was canceled.";
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithGoogleOnFirebase encountered an error: " + task.Exception);
                m_loginError = task.Exception.Message;
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }

            FirebaseUser newUser = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);

            m_email = newUser.Email;
            m_userId = newUser.UserId;
            m_password = "";

            m_signInFailed = false;
            m_signedIn = true;
            m_signingIn = false;

            PlayerPrefs.SetString("googleId", idToken);
        });
    }

    public void SignIn(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                m_loginError = "Logging in was canceled.";
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                m_loginError = task.Exception.Message;
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }

            FirebaseUser newUser = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);

            m_email = email;
            m_userId = newUser.UserId;
            m_password = password;

            m_signInFailed = false;
            m_signedIn = true;
            m_signingIn = false;
        });
    }

    public void SignOut()
    {
        auth.SignOut();
    }

    /// <summary>
    /// Start the connection process.
    /// - If already connected, we attempt joining a random room
    /// - if not yet connected, Connect this application instance to Photon Cloud Network
    /// </summary>
    public void Connect()
    {
        // #Critical, we must first and foremost connect to Photon Online Server.
        PhotonNetwork.NickName = m_userName;
        isConnecting = PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = gameVersion;
        m_loginQueueLength++;
        if (m_loginQueueLength > 1)
            m_loginQueueText.gameObject.SetActive(true);
        m_loginQueueText.text = "Your position in queue: " + m_loginQueueLength.ToString();
        mQueueValueRef.SetValueAsync(m_loginQueueLength);
    }

    public void FindRoom()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom && isConnectedToMaster)
        {
            selectedMap = "Arena00";
            m_loadingScreen.SetActive(true);
            m_loadingText.text = "FINDING A GAME...";
            // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
            string sqlLobbyFilter = string.Format("C0 BETWEEN {0} AND {1} AND C1 = 'Arena00'", Mathf.Max(0, m_arenaRating - Balance.matchmakingGap), m_arenaRating + Balance.matchmakingGap);
            OpJoinRandomRoomParams opJoinRandomRoomParams = new OpJoinRandomRoomParams();
            opJoinRandomRoomParams.SqlLobbyFilter = sqlLobbyFilter;
            if (loadBalancingClient == null)
                loadBalancingClient = PhotonNetwork.NetworkingClient;
            loadBalancingClient.OpJoinRandomRoom(opJoinRandomRoomParams);
        }
    }

    public void FindMission()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom && isConnectedToMaster)
        {
            selectedMap = "Mission00";
            m_loadingScreen.SetActive(true);
            m_loadingText.text = "FINDING A GAME...";
            // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
            string sqlLobbyFilter = string.Format("C1 = '{0}'", selectedMap);
            OpJoinRandomRoomParams opJoinRandomRoomParams = new OpJoinRandomRoomParams();
            opJoinRandomRoomParams.SqlLobbyFilter = sqlLobbyFilter;
            if (loadBalancingClient == null)
                loadBalancingClient = PhotonNetwork.NetworkingClient;
            loadBalancingClient.OpJoinRandomRoom(opJoinRandomRoomParams);
        }
    }


    #endregion

    #region MonoBehaviourPunCallbacks Callbacks


    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by PUN");

        if (isConnecting)
        {
            isConnecting = false;
        }

        m_loginQueueText.gameObject.SetActive(false);
        m_loginQueueLength--;
        mQueueValueRef.SetValueAsync(m_loginQueueLength);

        isConnectedToMaster = true;
    }

    public override void OnLeftRoom()
    {
        if (!PhotonNetwork.IsConnected)
            Connect();
        isConnectedToMaster = false;
        isRoomLoading = false;
        m_homeScreen.SetActive(true);
        SelectHangarShip(SelectedShipPrefab.name);
        OnMainScreenLoaded();
    }

    private void Update()
    {
        if (isConnectedToMaster && m_playersCountText)
        {
            if (chatClient != null)
                chatClient.Service();

            m_playersCountText.text = "Players online: " + PhotonNetwork.CountOfPlayers + "\nPlayers on arena: " + PhotonNetwork.CountOfPlayersInRooms;
        }
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        isRoomCreating = false;
        isRoomLoading = false;
        Debug.LogWarningFormat("OnDisconnected() was called by PUN with reason {0}", cause);
        m_loadingScreen?.SetActive(false);
        isConnecting = false;
    }


    #endregion

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (isRoomCreating || PhotonNetwork.NetworkClientState == ClientState.Joining || isRoomLoading) return;

        Debug.Log("OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

        isRoomCreating = true;
        // #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)Balance.maxPlayersPerRoom;
        roomOptions.EmptyRoomTtl = 1000;
        roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { ELO_PROP_KEY, m_arenaRating }, { MAP_PROP_KEY, selectedMap } };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { ELO_PROP_KEY, MAP_PROP_KEY };
        EnterRoomParams enterRoomParams = new EnterRoomParams();
        enterRoomParams.RoomOptions = roomOptions;
        enterRoomParams.Lobby = sqlLobby;
        loadBalancingClient.OpCreateRoom(enterRoomParams);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.NetworkClientState == ClientState.Joining || isRoomLoading || !PhotonNetwork.IsMessageQueueRunning) return;
        isRoomCreating = false;
        isRoomLoading = true;
        Debug.Log("OnJoinedRoom() called by PUN. Now this client is in a room.");
        HideHangarShips();
        m_homeScreen.SetActive(false);
        PhotonNetwork.LoadLevel(selectedMap);
    }

    public void OnArenaLoaded()
    {
        m_loadingScreen.SetActive(false);
    }

    public void OpenLeaderboard()
    {
        if (Social.localUser.authenticated)
            Social.Active.ShowLeaderboardUI();
    }

    public void OpenLoginScreen()
    {
        m_signingIn = false;
        m_loadingGears.SetActive(false);
        m_signupScreen.SetActive(false);
        m_loginScreen.SetActive(true);
        m_inputName.text = "Player";
    }

    public void CloseLoginScreen()
    {
        m_loadingGears.SetActive(true);
        m_loginScreen.SetActive(false);
    }

    private IEnumerator WaitForLoginFail()
    {
        yield return new WaitUntil(() => m_signInFailed);

        MessageBox.instance.Show(m_loginError);

        if (m_closeGameOnError)
            StartCoroutine(CloseGameAfterDelay());
    }

    public void CreateAccount(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                m_loginError = "Account creation was canceled.";
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                m_loginError = "Account with this email already exists";
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }

            // Firebase user has been created.
            FirebaseUser newUser = task.Result;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);

            m_email = email;
            m_password = password;
            m_userId = newUser.UserId;

            m_newProfile = true;
            m_signInFailed = false;
            m_signedIn = true;
            m_signingIn = false;
        });
    }

    public void SendChatMessage()
    {
        if (chatClient.CanChat && chatMessageField.text.Length > 0)
        {
            chatClient.PublishMessage("General", chatMessageField.text);
            chatMessageField.text = "";
        }
    }

    public void DebugReturn(DebugLevel level, string message)
    {

    }

    public void OnConnected()
    {
        chatClient.Subscribe("General");
        chatClient.SetOnlineStatus(ChatUserStatus.Online);
        chatLoadingIndicator.SetActive(false);
        chatMessageField.interactable = true;
    }

    public void OnDisconnected()
    {
        chatLoadingIndicator.SetActive(true);
        chatMessageField.interactable = false;
    }

    public void OnChatStateChange(ChatState state)
    {

    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            GameObject chatMsgGO = Instantiate(chatMessagePrefab, chatContent);
            chatMsgGO.GetComponent<TextMeshProUGUI>().text = "<color=\"green\">" + senders[i] + ": </color>" + messages[i].ToString();

            chatMessages.Add(chatMsgGO);

            if (chatMessages.Count > maxChatMessagesCount)
            {
                GameObject firstChatMsg = chatMessages[0];
                chatMessages.RemoveAt(0);
                Destroy(firstChatMsg);
            }
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {

    }

    public void OnSubscribed(string[] channels, bool[] results)
    {

    }

    public void OnUnsubscribed(string[] channels)
    {

    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {

    }

    public void OnUserSubscribed(string channel, string user)
    {

    }

    public void OnUserUnsubscribed(string channel, string user)
    {

    }
}
