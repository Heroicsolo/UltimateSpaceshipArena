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
using Google;
using System.Threading.Tasks;
using GooglePlayGames.BasicApi;
using GooglePlayGames;
using UnityEngine.SocialPlatforms;
using NiobiumStudios;
using GameAnalyticsSDK;
using Facebook.Unity;

public class Launcher : MonoBehaviourPunCallbacks, IMatchmakingCallbacks
{
    public static Launcher instance;

    #region Private Serializable Fields
    [Header("SignIn and SignUp UI")]
    [SerializeField] private GameObject m_loginScreen;
    [SerializeField] private GameObject m_signupScreen;
    [SerializeField] private GameObject m_loadingScreen;
    [SerializeField] private InputField m_inputEmail;
    [SerializeField] private InputField m_inputPass;
    [SerializeField] private InputField m_inputSignUpEmail;
    [SerializeField] private InputField m_inputSignUpPass;
    [SerializeField] private InputField m_inputName;
    [SerializeField] private InputField m_inputProfileName;

    [Header("Loading Screen")]
    [SerializeField] private TextMeshProUGUI m_loadingText;
    [SerializeField] private TextMeshProUGUI m_loginQueueText;
    [SerializeField] private GameObject m_loadingGears;

    [Header("Hangar UI")]
    [SerializeField] private TextMeshProUGUI m_playersCountText;
    [SerializeField] private Toggle defaultShipToggle;
    [SerializeField] private GameObject m_canvas;
    [SerializeField] private GameObject m_homeScreen;
    [SerializeField] AudioClip buttonSound;
    [SerializeField] private TextMeshProUGUI userIdLabel;
    [SerializeField] private TextMeshProUGUI currencyLabel;
    [SerializeField] private TextMeshProUGUI ratingLabel;
    [SerializeField] private List<PlayerController> availableShips;
    [SerializeField] private List<GameObject> hangarShips;
    [SerializeField] private GameObject dailyRewardsScreen;
    [SerializeField] private GameObject m_shipsSelector;
    [SerializeField] private GameObject m_shipsButton;
    [SerializeField] private GameObject m_arenaButton;
    [SerializeField] private GameObject m_shopButton;
    [SerializeField] private GameObject m_missionButton;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private GameObject chatUI;

    [Header("Profile Screen")]
    [SerializeField] private Button changeNameBtn;
    [SerializeField] private Button changeNameBtnPaid;
    [SerializeField] private Text changeNameCostLabel;

    [Header("Skins")]
    [SerializeField] private List<SkinData> availableSkins;

    #endregion

    #region Private Fields

    /// <summary>
    /// This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking changes).
    /// </summary>
    const string gameVersion = "6";

    private bool m_closeGameOnError = false;
    private bool m_newProfile = false;

    private bool isConnecting;
    private bool isConnectedToMaster;
    private bool isRoomCreating = false;
    private bool isRoomLoading = false;
    private bool isFightCompleted = false;

    private bool isSoundOn = true;

    private GameObject m_selectedShip;

    private AudioSource audioSource;

    private TypedLobby sqlLobby = new TypedLobby("customSqlLobby", LobbyType.SqlLobby);
    private LoadBalancingClient loadBalancingClient;
    private string selectedMap = "Arena00";

    #endregion

    #region Public Fields
    
    public Action<bool> OnApplicationPaused;
    public Action OnApplicationExit;
    
    public List<SkinData> AvailableSkins => availableSkins;

    public GameObject SelectedShipPrefab { get { return m_selectedShip; } set { m_selectedShip = value; SelectHangarShip(m_selectedShip.name); } }

    public bool IsSoundOn { get { return isSoundOn; } set { isSoundOn = value; PlayerPrefs.SetInt("soundOn", isSoundOn ? 1 : 0); } }

    public bool CloseGameOnError { get { return m_closeGameOnError; } set { m_closeGameOnError = value; } }

    public List<PlayerController> AvailableShips => availableShips;
    
    public const string ELO_PROP_KEY = "C0";
    public const string MAP_PROP_KEY = "C1";
    #endregion

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

    #region MonoBehavior Callbacks
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

        audioSource = GetComponentInChildren<AudioSource>();

        Input.multiTouchEnabled = true;

        GameAnalytics.Initialize();
        FB.Init();

        // #Critical
        // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
        PhotonNetwork.AutomaticallySyncScene = true;

        isSoundOn = PlayerPrefs.GetInt("soundOn", 1) == 1;

        AudioListener.volume = IsSoundOn ? 1f : 0f;

        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder().RequestServerAuthCode(false).Build();

        AuthController.Init();
        AuthController.OnLoginFailed += OnLoginFailed;

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.Activate();

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
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

    private void OnApplicationPause(bool pause)
    {
        OnApplicationPaused?.Invoke(pause);
    }

    private void OnApplicationQuit()
    {
        OnApplicationExit?.Invoke();
    }

    private void Update()
    {
        if (isConnectedToMaster && m_playersCountText)
        {
            m_playersCountText.text = "Players online: " + PhotonNetwork.CountOfPlayers + "\nPlayers on arena: " + PhotonNetwork.CountOfPlayersInRooms;
        }
    }

    void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
            StartCoroutine(AwaitForProfile());
    }

    #endregion

    private void InitFirebase()
    {
        AccountManager.Init();
        BalanceProvider.Init();

        AccountManager.OnQueueChanged += OnQueueLengthChanged;
        
        BalanceProvider.OnValueChanged += OnBalanceChanged;
    }

    void OnBalanceChanged()
    {
        changeNameCostLabel.text = BalanceProvider.Balance.nameChangeCost.ToString();
    }

    private void OnQueueLengthChanged(int queueLength)
    {
        m_loginQueueText.text = "Your position in queue: " + queueLength.ToString();
    }

    public void CloseGameDelayed(float delay = 3f, bool openAppStore = false)
    {
        StartCoroutine(CloseGameAfterDelay(delay, openAppStore));
    }

    public void OnCurrencyChanged()
    {
        currencyLabel.text = AccountManager.Currency.ToString();
    }

    public void ShowShipsSelector()
    {
        m_shipsSelector.SetActive(true);
    }

    public void HideShipsSelector()
    {
        m_shipsSelector.SetActive(false);
    }

    public string GetCurrentSkin(int shipID, SkinType type)
    {
        foreach (var skin in availableSkins)
        {
            if (skin.Type == type && 
                (skin.SupportedShips == null || 
                skin.SupportedShips.Count == 0 || 
                skin.SupportedShips.FindIndex(x => x.ID == shipID) >= 0) && AccountManager.IsSkinUnlocked(skin.ID))
            {
                return skin.SkinObject.name;
            }
        }

        return "";
    }

    public void RefreshTopButtons()
    {
        bool anyUpgradeAvailable = false;

        foreach (var ship in availableShips)
        {
            foreach (var upgrade in ship.Upgrades)
            {
                bool isMaxLvl = false;
                if (AccountManager.IsUpgradeAvailable(ship, upgrade, out isMaxLvl))
                {
                    anyUpgradeAvailable = true;
                    break;
                }
            }
        }

        bool anyShopItemAvailable = false;

        foreach (var item in AvailableSkins)
        {
            if (item.Cost <= AccountManager.Currency && !AccountManager.IsSkinUnlocked(item.ID))
            {
                anyShopItemAvailable = true;
                break;
            }
        }

        m_arenaButton.GetComponent<Animator>().enabled = !AccountManager.IsArenaTutorialDone;
        m_missionButton.GetComponent<Animator>().enabled = AccountManager.IsArenaTutorialDone && !AccountManager.IsMissionTutorialDone;
        m_shopButton.GetComponent<Animator>().enabled = anyShopItemAvailable;
        m_shipsButton.GetComponent<Animator>().enabled = anyUpgradeAvailable;
    }

    private IEnumerator CloseGameAfterDelay(float delay = 3f, bool openAppStore = false)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (openAppStore)
            OpenAppStorePage();

        Application.Quit();
    }

    private IEnumerator AwaitForProfile(bool skipSignIn = false)
    {
        m_loadingScreen.SetActive(true);

        m_loadingText.text = "CONNECTING TO SERVER...";

        yield return new WaitUntil(() => AccountManager.IsNicknamesListLoaded && BalanceProvider.IsLoaded);

        m_loadingText.text = "CONNECTING TO GOOGLE PLAY...";

        AuthController.InitGP();

        yield return new WaitUntil(() => AuthController.GooglePlayConnected);

        if (!skipSignIn)
        {
            m_loadingText.text = "SIGNING IN...";

            AuthController.SignIn();

            yield return new WaitUntil(() => AuthController.IsAuthorized);

            m_signupScreen.SetActive(false);

            CloseLoginScreen();

            yield return new WaitUntil(() => AuthController.CredentialsSaved);
        }

        m_loadingText.text = "LOADING PROFILE...";

        AccountManager.LoadProfile(m_newProfile);

        yield return new WaitUntil(() => AccountManager.IsLoaded);

        if (AuthController.IsAuthorized)
        {
            AccountManager.SetOnline();
        }

        m_loadingText.text = "CONNECTING TO MASTER SERVER...";

        Connect();

        yield return new WaitUntil(() => isConnectedToMaster);

        GetProfileData();

        userIdLabel.text = "User ID: " + AuthController.UserID;

        ratingLabel.text = AccountManager.CurrentRating.ToString();
        currencyLabel.text = AccountManager.Currency.ToString();

        m_inputProfileName.text = AccountManager.UserName;
        changeNameBtn.gameObject.SetActive(!AccountManager.IsNameChanged);
        changeNameBtnPaid.gameObject.SetActive(AccountManager.IsNameChanged);
        changeNameCostLabel.text = BalanceProvider.Balance.nameChangeCost.ToString();

        chatManager.gameObject.SetActive(true);
        chatUI.gameObject.SetActive(true);

        RefreshTopButtons();

        if (AccountManager.IsFirstTutorialDone)
        {
            OpenDailyRewards();
        }
        else
        {
            TutorialController.instance.ShowFirstTutorial(AccountManager.FirstTutorialStep, OpenDailyRewards);
        }
    }

    private void OpenDailyRewards()
    {
        dailyRewardsScreen.SetActive(true);
        DailyRewards.instance.onClaimPrize += OnClaimPrizeDailyRewards;
    }

    private void OnClaimPrizeDailyRewards(int day)
    {
        //This returns a Reward object
        Reward myReward = DailyRewards.instance.GetReward(day);

        if (myReward.unit == "Credits")
        {
            AccountManager.Currency += myReward.reward;
            GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, "credits", myReward.reward, "DailyRewards", "DailyReward_" + day.ToString());
        }

        currencyLabel.text = AccountManager.Currency.ToString();

        AccountManager.SaveProfile();
    }

    #region Public Methods

    public void OnArenaLoaded()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        m_loadingScreen.SetActive(false);
    }

    public void OpenLeaderboard()
    {
        if (Social.localUser.authenticated)
            Social.Active.ShowLeaderboardUI();
    }

    public void OpenLoginScreen()
    {
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

    public void PlayButtonSound()
    {
        audioSource.PlayOneShot(buttonSound);
    }

    public void OnArenaTutorialDone()
    {
        AccountManager.OnArenaTutorialDone();
    }

    public void OnMissionTutorialDone()
    {
        AccountManager.OnMissionTutorialDone();
    }

    public void OpenAppStorePage()
    {
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.marktsemma.ultimatespaceshiparena");
    }

    public void ShowLeaderBoard()
    {
        ((PlayGamesPlatform)Social.Active).ShowLeaderboardUI(GPGSIds.leaderboard_arena);
    }

    public void AddScoreToLeaderBoard()
    {
        if (Social.localUser.authenticated)
        {
            Social.ReportScore(AccountManager.CurrentRating, GPGSIds.leaderboard_arena, (bool success) => { });
        }
    }

    public void OnMainScreenLoaded()
    {
        m_loadingScreen?.SetActive(false);
        chatManager.ReconnectIfNeeded();
        ratingLabel.text = AccountManager.CurrentRating.ToString();
        RefreshTopButtons();
        if (isFightCompleted)
            ShowShipsSelector();
    }

    public void OnShipSelectorOpened()
    {
        defaultShipToggle.isOn = true;
        defaultShipToggle.Select();
    }

    public void GetProfileData()
    {
        m_loadingScreen.SetActive(false);

        AccountManager.SaveProfile();
    }

    public int OnMissionCompleted(float completionTime)
    {
        isFightCompleted = true;
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, "Mission", PlayerController.LocalPlayer.Score);
        int moneyGained = BalanceProvider.Balance.currencyPerMissionMin + Mathf.CeilToInt((180f / (10f + completionTime)) * BalanceProvider.Balance.missionTimeRewardModifier * 100);
        AccountManager.Currency += moneyGained;
        GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, "credits", moneyGained, "MissionRewards", "MissionReward");
        AccountManager.SaveProfile();
        AccountManager.UnlockAchievement(GPGSIds.achievement_mission_is_possible);
        currencyLabel.text = AccountManager.Currency.ToString();
        return moneyGained;
    }

    public void OnMissionFailed()
    {
        isFightCompleted = true;
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, "Mission", PlayerController.LocalPlayer.Score);
    }

    public int OnFightLoss()
    {
        isFightCompleted = true;
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, "Arena", PlayerController.LocalPlayer.Score);
        AccountManager.Currency += BalanceProvider.Balance.currencyPerFightMin;
        GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, "credits", BalanceProvider.Balance.currencyPerFightMin, "ArenaRewards", "FightLoss");
        AccountManager.CurrentRating = Mathf.Max(0, AccountManager.CurrentRating - Mathf.FloorToInt(0.1f * BalanceProvider.Balance.lossRatingMod * AccountManager.CurrentRating));
        AccountManager.SaveProfile();
        AddScoreToLeaderBoard();
        AccountManager.UnlockAchievement(GPGSIds.achievement_first_steps_in_space);
        currencyLabel.text = AccountManager.Currency.ToString();
        ratingLabel.text = AccountManager.CurrentRating.ToString();
        return BalanceProvider.Balance.currencyPerFightMin;
    }

    public int OnFightWon(int place = 1)
    {
        isFightCompleted = true;
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, "Arena", PlayerController.LocalPlayer.Score);
        int bonusForPlace = (BalanceProvider.Balance.winnersCount - place) * 100;
        int moneyGained = BalanceProvider.Balance.currencyPerFightMin + BalanceProvider.Balance.currencyPerWin + BalanceProvider.Balance.currencyPlaceBonus * (BalanceProvider.Balance.winnersCount - place);
        moneyGained = Mathf.CeilToInt(moneyGained * (1f + BalanceProvider.Balance.currencyPerRatingBonus * (float)AccountManager.CurrentRating / 1000f));
        AccountManager.Currency += moneyGained;
        GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, "credits", moneyGained, "ArenaRewards", "FightWon");
        AccountManager.CurrentRating = Mathf.Max(0, AccountManager.CurrentRating + Mathf.CeilToInt((bonusForPlace + 200) * BalanceProvider.Balance.victoryRatingMod * 2000f / Mathf.Max(1000f, AccountManager.CurrentRating)));
        AccountManager.SaveProfile();
        AddScoreToLeaderBoard();
        AccountManager.UnlockAchievement(GPGSIds.achievement_first_steps_in_space);
        currencyLabel.text = AccountManager.Currency.ToString();
        ratingLabel.text = AccountManager.CurrentRating.ToString();
        return moneyGained;
    }

    public int GetShipNumber(PlayerController ship)
    {
        return availableShips.FindIndex(x => x.ID == ship.ID);
    }

    public void OnDailyRewardsChanged(int lastDailyRewardDebugTime, string lastDailyRewardTime, int lastDailyReward)
    {
        AccountManager.OnDailyRewardsChanged(lastDailyRewardDebugTime, lastDailyRewardTime, lastDailyReward);
    }

    public void OnTutorialDone()
    {
        GameAnalytics.NewDesignEvent("tutorial_complete");
        AccountManager.OnFirstTutorialDone();
    }

    

    #region Auth Handlers
    public void SignUp()
    {
        if (AuthController.IsSigningIn) return;

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

        if (AccountManager.IsNicknameUsed(m_inputName.text))
        {
            MessageBox.instance.Show("This nickname is already taken!");
            return;
        }

        AuthController.WaitForLoginFail();
        AuthController.CreateAccount(m_inputSignUpEmail.text, m_inputSignUpPass.text);
    }

    public void SignIn()
    {
        if (AuthController.IsSigningIn) return;

        AuthController.WaitForLoginFail();
        AuthController.SignIn(m_inputEmail.text, m_inputPass.text);
    }

    public void SignUpWithGoogle()
    {
        if (AuthController.IsSigningIn) return;

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

        if (AccountManager.IsNicknameUsed(m_inputName.text))
        {
            MessageBox.instance.Show("This nickname is already taken!");
            return;
        }

        AuthController.WaitForLoginFail();
        AuthController.SignInWithGoogle();
    }
    #endregion

    public void TryChangeName()
    {
        string newName = m_inputProfileName.text;

        if (newName.Equals(AccountManager.UserName))
        {
            return;
        }

        if (newName.Length < 2)
        {
            MessageBox.instance.Show("Your nickname is too short!");
            m_inputProfileName.text = AccountManager.UserName;
            return;
        }

        if (AccountManager.IsNicknameUsed(newName))
        {
            MessageBox.instance.Show("This nickname is already taken!");
            m_inputProfileName.text = AccountManager.UserName;
            return;
        }

        GameAnalytics.NewDesignEvent("name_changed_free");
        AccountManager.SetUserName(newName);
        PhotonNetwork.NickName = newName;
        changeNameBtn.gameObject.SetActive(false);
        changeNameBtnPaid.gameObject.SetActive(true);
    }

    public void ChangeNamePaid()
    {
        string newName = m_inputProfileName.text;

        if (newName.Equals(AccountManager.UserName))
        {
            return;
        }

        if (newName.Length < 2)
        {
            MessageBox.instance.Show("Your nickname is too short!");
            m_inputProfileName.text = AccountManager.UserName;
            return;
        }

        if (AccountManager.IsNicknameUsed(newName))
        {
            MessageBox.instance.Show("This nickname is already taken!");
            m_inputProfileName.text = AccountManager.UserName;
            return;
        }

        if (AccountManager.Currency < BalanceProvider.Balance.nameChangeCost)
        {
            MessageBox.instance.Show("Not enough money!");
            m_inputProfileName.text = AccountManager.UserName;
            return;
        }

        AccountManager.Currency -= BalanceProvider.Balance.nameChangeCost;
        GameAnalytics.NewDesignEvent("name_changed_paid");
        GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, "credits", BalanceProvider.Balance.nameChangeCost, "AccountServices", "NameChange");
        AccountManager.SetUserName(newName);
        PhotonNetwork.NickName = newName;

        changeNameBtn.gameObject.SetActive(false);
    }

    public void Connect()
    {
        if (isConnecting) return;
        // #Critical, we must first and foremost connect to Photon Online Server.
        PhotonNetwork.NickName = AccountManager.UserName;
        isConnecting = PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = gameVersion;
        AccountManager.IncreaseLoginQueueLength();
        if (AccountManager.LoginQueueLength > 1)
            m_loginQueueText.gameObject.SetActive(true);
        m_loginQueueText.text = "Your position in queue: " + AccountManager.LoginQueueLength.ToString();
    }

    public void FindRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom && isConnectedToMaster)
        {
            selectedMap = "Arena00";
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, "Arena", selectedMap);
            m_loadingScreen.SetActive(true);
            m_loadingText.text = "FINDING A GAME...";
            // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
            string sqlLobbyFilter = string.Format("C0 BETWEEN {0} AND {1} AND C1 = '{2}'", Mathf.Max(0, AccountManager.CurrentRating - BalanceProvider.Balance.matchmakingGap), AccountManager.CurrentRating + BalanceProvider.Balance.matchmakingGap, selectedMap);
            PhotonNetwork.JoinRandomRoom(null, 0, MatchmakingMode.FillRoom, sqlLobby, sqlLobbyFilter);
        }
        else
        {
            Connect();
        }
    }

    public void FindMission()
    {
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom && isConnectedToMaster)
        {
            selectedMap = "Mission00";
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, "Mission", selectedMap);
            m_loadingScreen.SetActive(true);
            m_loadingText.text = "STARTING MISSION...";
            // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
            //string sqlLobbyFilter = string.Format("C1 = '{0}'", selectedMap);
            //PhotonNetwork.JoinRandomRoom(null, 0, MatchmakingMode.FillRoom, sqlLobby, sqlLobbyFilter);
            CreateRoom();
        }
        else
        {
            Connect();
        }
    }

    public void OnRoomLeavingStarted()
    {
        m_loadingText.text = "LOADING HANGAR...";
        m_loadingScreen?.SetActive(true);
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
        
        AccountManager.ReduceLoginQueueLength();

        //Some optimization
        PhotonNetwork.UseRpcMonoBehaviourCache = true;
        PhotonNetwork.NetworkingClient.LoadBalancingPeer.ReuseEventInstance = true;
        //Some optimization

        isConnectedToMaster = true;
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby() called by PUN. Now this client is in lobby");
        isConnectedToMaster = true;
    }

    public override void OnLeftRoom()
    {
        //if (!PhotonNetwork.IsConnectedAndReady)
            //Connect();
        Debug.Log("OnLeftRoom() called by PUN. Now this client is not in a room.");
        isRoomLoading = false;
        m_homeScreen.SetActive(true);
        SelectHangarShip(SelectedShipPrefab != null ? SelectedShipPrefab.name : "Spaceship00");
        OnMainScreenLoaded();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        isConnectedToMaster = false;
        isRoomCreating = false;
        isRoomLoading = false;
        Debug.LogWarningFormat("OnDisconnected() was called by PUN with reason {0}", cause);
        m_loadingScreen?.SetActive(false);
        isConnecting = false;
        //Connect();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (isRoomCreating || PhotonNetwork.NetworkClientState == ClientState.Joining || isRoomLoading) return;

        Debug.Log("OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

        CreateRoom();
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.NetworkClientState == ClientState.Joining || isRoomLoading) return;
        isRoomCreating = false;
        isRoomLoading = true;
        isFightCompleted = false;
        Debug.Log("OnJoinedRoom() called by PUN. Now this client is in a room.");
        HideHangarShips();
        m_homeScreen.SetActive(false);
        PhotonNetwork.LoadLevel(selectedMap);
    }

    #endregion

    private void CreateRoom()
    {
        isRoomCreating = true;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)BalanceProvider.Balance.maxPlayersPerRoom;
        roomOptions.EmptyRoomTtl = 1000;
        roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { ELO_PROP_KEY, AccountManager.CurrentRating }, { MAP_PROP_KEY, selectedMap } };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { ELO_PROP_KEY, MAP_PROP_KEY };
        PhotonNetwork.CreateRoom(null, roomOptions, sqlLobby);
    }

    private void OnLoginFailed()
    {
        if (m_closeGameOnError)
            StartCoroutine(CloseGameAfterDelay());
    }
}
