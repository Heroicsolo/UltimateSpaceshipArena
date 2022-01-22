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

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;

    #region Private Serializable Fields


    #endregion


    #region Private Fields


    /// <summary>
    /// This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking changes).
    /// </summary>
    string gameVersion = "2";
    [SerializeField] private byte maxPlayersPerRoom = 8;
    [SerializeField] private int initArenaRating = 1000;
    [SerializeField] private GameObject m_loginScreen;
    [SerializeField] private GameObject m_signupScreen;
    [SerializeField] private GameObject m_loadingScreen;
    [SerializeField] private InputField m_inputEmail;
    [SerializeField] private InputField m_inputPass;
    [SerializeField] private InputField m_inputSignUpEmail;
    [SerializeField] private InputField m_inputSignUpPass;
    [SerializeField] private InputField m_inputName;
    [SerializeField] private TextMeshProUGUI m_loadingText;
    [SerializeField] private TextMeshProUGUI m_playersCountText;
    [SerializeField] private GameObject m_loadingGears;
    [SerializeField] private Toggle defaultShipToggle;
    [SerializeField] private GameObject m_canvas;
    [SerializeField] private GameObject m_homeScreen;
    [SerializeField] AudioClip buttonSound;

    private Firebase.FirebaseApp app = null;
    private FirebaseAuth auth;
    private string m_userName = "Player";
    private string m_email = "";
    private string m_password = "";
    private string m_userId = "";
    private string m_loginError = "";
    private string authCode = "";
    private string m_deviceId = "";

    private int m_arenaRating = 0;

    private bool m_signingIn = false;
    private bool m_signedIn = false;
    private bool m_signInFailed = false;
    private bool m_googlePlayConnected = false;
    private bool m_loadedProfile = false;
    private bool m_closeGameOnError = false;
    private bool m_DB_loaded = false;
    private bool m_credentialsSaved = false;
    private bool m_newProfile = false;

    private bool isConnecting;
    private bool isConnectedToMaster;
    private bool isRoomCreating = false;
    private bool isRoomLoading = false;

    private GameObject m_selectedShip;

    private DatabaseReference mDatabaseRef;
    private DatabaseReference mNicknamesDB;

    private AudioSource audioSource;

    private List<string> m_usedNicknamesList = new List<string>();

    #endregion


    #region MonoBehaviour CallBacks

    public string UserID => m_userId;
    public string UserName => m_userName;

    public int CurrentRating => m_arenaRating;
    public GameObject SelectedShipPrefab { get { return m_selectedShip; } set { m_selectedShip = value; } }

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

        mNicknamesDB.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {

            }
            else if (task.IsCompleted)
            {
                LoadNicknamesFromSnapshot(task.Result);
                m_DB_loaded = true;
            }
        });

        mNicknamesDB.ValueChanged += CheckUsedNicknames;
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


    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during initialization phase.
    /// </summary>
    void Start()
    {
        StartCoroutine(AwaitForProfile());
    }

    private IEnumerator AwaitForProfile(bool skipSignIn = false)
    {
        m_loadingScreen.SetActive(true);

        m_loadingText.text = "CONNECTING TO SERVER...";

        yield return new WaitUntil(() => m_DB_loaded);

        if (!skipSignIn)
        {
            m_loadingText.text = "SIGNING IN...";

            bool emailSigningIn = TrySignIn();

            if (!emailSigningIn)
            {
                OpenLoginScreen();
            }

            yield return new WaitUntil(() => m_signedIn);

            SaveCredentials(false, m_signupScreen.activeSelf);

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
    }

    #endregion


    #region Public Methods

    public void OnShipSelectorOpened()
    {
        defaultShipToggle.SetIsOnWithoutNotify(true);
    }

    public void GetProfileData()
    {
        m_loadingScreen.SetActive(false);

        SaveProfile();
    }

    public void OnFightLoss()
    {
        m_arenaRating = Mathf.Max(0, m_arenaRating - Mathf.FloorToInt(0.1f * m_arenaRating));
        SaveProfile();
    }

    public void OnFightWon()
    {
        m_arenaRating = Mathf.Max(0, m_arenaRating + Mathf.CeilToInt(200 * 2000f / Mathf.Max(1000f, m_arenaRating)));
        SaveProfile();
    }

    public void SetOnline()
    {
        mDatabaseRef.Child(m_userId).Child("online").SetValueAsync(true);
        mDatabaseRef.Child(m_userId).Child("deviceId").SetValueAsync(m_deviceId);
    }

    public void SaveCredentials(bool onlyLocal = false, bool isNewAccount = false)
    {
        PlayerPrefs.SetString("email", m_email);
        PlayerPrefs.SetString("pass", m_password);

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

    public void SaveProfile()
    {
        if (!m_signedIn || !m_loadedProfile) return;

        DatabaseReference userTable = mDatabaseRef.Child(m_userId);

        userTable.Child("username").SetValueAsync(m_userName);
        userTable.Child("email").SetValueAsync(m_email);
        userTable.Child("arenaRating").SetValueAsync(m_arenaRating);
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

        SignOut();
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

        m_arenaRating = notEmptyProfile && snapshot.HasChild("arenaRating") ? int.Parse(snapshot.Child("arenaRating").Value.ToString()) : initArenaRating;

        m_loadedProfile = true;
    }

    public bool TrySignIn()
    {
        m_email = PlayerPrefs.GetString("email", "");
        m_password = PlayerPrefs.GetString("pass", "");

        if (m_email.Length > 2)
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
    }

    public void FindRoom()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom && isConnectedToMaster)
        {
            m_loadingScreen.SetActive(true);
            m_loadingText.text = "FINDING A GAME...";
            // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
            PhotonNetwork.JoinRandomRoom();
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

        isConnectedToMaster = true;
    }

    public override void OnLeftRoom()
    {
        if (!PhotonNetwork.IsConnected)
            Connect();
        isConnectedToMaster = false;
        isRoomLoading = false;
        m_homeScreen.SetActive(true);
    }

    private void Update()
    {
        if (isConnectedToMaster && m_playersCountText)
        {
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
        roomOptions.MaxPlayers = maxPlayersPerRoom;
        roomOptions.EmptyRoomTtl = 1000;
        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.NetworkClientState == ClientState.Joining || isRoomLoading || !PhotonNetwork.IsMessageQueueRunning) return;
        isRoomCreating = false;
        isRoomLoading = true;
        Debug.Log("OnJoinedRoom() called by PUN. Now this client is in a room.");
        m_homeScreen.SetActive(false);
        PhotonNetwork.LoadLevel("Arena00");
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

    private IEnumerator CloseGameAfterDelay()
    {
        yield return new WaitForSecondsRealtime(3f);

        Application.Quit();
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
}
