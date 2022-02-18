using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance;

    [SerializeField]
    private Transform mainCanvas;
    [Header("Battle UI")]
    [SerializeField]
    private GameObject statsButton;
    [SerializeField]
    private GameObject missionObjectiveHolder;
    [SerializeField]
    private TextMeshProUGUI missionObjectiveLabel;
    [SerializeField]
    private TextMeshProUGUI missionObjectiveLabel2;
    [SerializeField]
    private Image playerHealthBar;
    [SerializeField]
    private Image playerForceFieldBar;
    [SerializeField]
    private Joystick joystick;
    [SerializeField]
    private Joystick weaponJoystick;
    [SerializeField]
    private Transform minimapPlayer;
    [SerializeField]
    private GameObject minimapEnemyPrefab;
    [SerializeField]
    private GameObject minimapNexus;
    [SerializeField]
    private Transform radar;
    [SerializeField]
    private List<SkillButton> m_skillsButtons;
    [SerializeField]
    private TextMeshProUGUI announcementsLabel;
    [SerializeField]
    private TextMeshProUGUI matchTimerLabel;
    [SerializeField]
    private GameObject enemyArrowPrefab;
    [SerializeField]
    private GameObject nexusArrowPrefab;
    [Header("Battle stats screen")]
    [SerializeField]
    private GameObject m_battleStatsScreen;
    [SerializeField]
    private Transform m_battleStatsItemsHolder;
    [SerializeField]
    private GameObject m_battleStatsItemPrefab;
    [Header("Lobby")]
    [SerializeField]
    private GameObject lobbyScreen;
    [SerializeField]
    private GameObject lobbyPlayerSlotPrefab;
    [SerializeField]
    private TextMeshProUGUI lobbyTimer;
    [SerializeField]
    private Transform lobbyPlayersHolder;
    [SerializeField]
    private GameObject lobbyMissionButtons;
    [SerializeField]
    private GameObject lobbyArenaButtons;
    [Header("Win/Loss Screens")]
    [SerializeField]
    private GameObject winScreen;
    [SerializeField]
    private TextMeshProUGUI winScreenRatingLabel;
    [SerializeField]
    private TextMeshProUGUI winScreenCurrencyLabel;
    [SerializeField]
    private TextMeshProUGUI winScreenPlaceLabel;
    [SerializeField]
    private TextMeshProUGUI loseScreenRatingLabel;
    [SerializeField]
    private TextMeshProUGUI loseScreenCurrencyLabel;
    [SerializeField]
    private TextMeshProUGUI loseScreenPlaceLabel;
    [SerializeField]
    private GameObject lossScreen;
    [SerializeField]
    private GameObject missionCompletedScreen;
    [SerializeField]
    private TextMeshProUGUI missionCompletedCurrencyLabel;
    [SerializeField]
    private GameObject missionFailedScreen;
    [SerializeField]
    private TextMeshProUGUI lobbyScreenTitle;
    [Header("UI Announcements")]
    [SerializeField]
    private GameObject killAnnounceObject;
    [SerializeField]
    private TextMeshProUGUI killAnnounceKillerLabel;
    [SerializeField]
    private TextMeshProUGUI killAnnounceVictimLabel;
    [SerializeField]
    private GameObject captureAnnounceObject;
    [SerializeField]
    private GameObject respawnAnnounceObject;
    [SerializeField]
    private TextMeshProUGUI respawnAnnounceLabel;
    [SerializeField]
    private TextMeshProUGUI capturerLabel;
    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private AudioClip getReadySound;
    [SerializeField]
    private AudioClip lobbyTimerStartSound;
    [SerializeField]
    private AudioClip lobbyTimerThreeSound;
    [SerializeField]
    private AudioClip lobbyTimerTwoSound;
    [SerializeField]
    private AudioClip lobbyTimerOneSound;
    [SerializeField]
    private List<AudioClip> lobbyTimerZeroSounds;
    [SerializeField]
    private List<AudioClip> killSounds;
    [SerializeField]
    private AudioClip capturingSound;
    [SerializeField]
    private AudioClip capturingFriendlySound;
    [SerializeField]
    private AudioClip finishHimSound;
    [SerializeField]
    private AudioClip victorySound;
    [SerializeField]
    private AudioClip lossSound;
    [SerializeField]
    private List<AudioClip> lowDurabilitySounds;

    private PlayerController target;
    private List<PlayerController> enemies = new List<PlayerController>();
    private List<Transform> enemiesIcons = new List<Transform>();
    private List<LobbyPlayerSlot> m_lobbyPlayers = new List<LobbyPlayerSlot>();
    private List<PlayerStatsSlot> m_battleStatsSlots = new List<PlayerStatsSlot>();

    private MissionController missionController;
    private ArenaController arenaController;
    private bool isMissionMode = false;

    private float mapSizeAmplifier = 5f;

    public bool IsLobbyState = true;
    private bool IsInitialized = false;
    private Dictionary<SoundType, float> soundsCDs = new Dictionary<SoundType, float>();

    private Dictionary<PlayerController, GameObject> enemyArrows = new Dictionary<PlayerController, GameObject>();

    public enum SoundType
    {
        GetReady,
        LobbyTimerStart,
        LobbyTimerOne,
        LobbyTimerTwo,
        LobbyTimerThree,
        LobbyTimerEnd,
        Kill,
        Capturing,
        CapturingFriendly,
        FinishHim,
        Victory,
        Loss,
        LowDurability
    }

    public void AddPlayerStatsSlot(PlayerController pc, int rating, int upgradesScore)
    {
        if (isMissionMode || m_battleStatsSlots.FindIndex(x => x.PlayerName == pc.Name) >= 0) return;
        GameObject slot = Instantiate(m_battleStatsItemPrefab, m_battleStatsItemsHolder);
        PlayerStatsSlot ps = slot.GetComponent<PlayerStatsSlot>();
        ps.SetData(pc, rating, upgradesScore);
        m_battleStatsSlots.Add(ps);
    }

    public void RemovePlayerStatsSlot(string playerName)
    {
        PlayerStatsSlot ps = m_battleStatsSlots.Find(x => x.PlayerName == playerName);

        if (ps)
        {
            m_battleStatsSlots.Remove(ps);
            Destroy(ps.gameObject);
        }
    }

    public void SortPlayerStatsSlots()
    {
        List<PlayerStatsSlot> sortedSlots = m_battleStatsSlots.OrderByDescending(x => x.Score).ToList();

        m_battleStatsItemsHolder.GetComponent<VerticalLayoutGroup>().enabled = false;

        for (int i = 0; i < sortedSlots.Count; i++)
        {
            sortedSlots[i].transform.SetSiblingIndex(i);
        }

        m_battleStatsItemsHolder.GetComponent<VerticalLayoutGroup>().enabled = true;
    }

    public void ShowStatsScreen()
    {
        m_battleStatsScreen.SetActive(true);
    }

    public void PlaySound(SoundType soundType, float cooldown = 0f)
    {
        switch (soundType)
        {
            case SoundType.GetReady:
                if (getReadySound && (!soundsCDs.ContainsKey(SoundType.GetReady) || soundsCDs[SoundType.GetReady] <= 0f))
                {
                    audioSource.PlayOneShot(getReadySound);
                    soundsCDs[SoundType.GetReady] = cooldown;
                }
                break;
            case SoundType.Kill:
                if (killSounds.Count > 0 && (!soundsCDs.ContainsKey(SoundType.Kill) || soundsCDs[SoundType.Kill] <= 0f))
                {
                    audioSource.PlayOneShot(killSounds.GetRandomElement());
                    soundsCDs[SoundType.Kill] = cooldown;
                }
                break;
            case SoundType.FinishHim:
                if (finishHimSound && (!soundsCDs.ContainsKey(SoundType.FinishHim) || soundsCDs[SoundType.FinishHim] <= 0f))
                {
                    audioSource.PlayOneShot(finishHimSound);
                    soundsCDs[SoundType.FinishHim] = cooldown;
                }
                break;
            case SoundType.Victory:
                if (victorySound && (!soundsCDs.ContainsKey(SoundType.Victory) || soundsCDs[SoundType.Victory] <= 0f))
                {
                    audioSource.PlayOneShot(victorySound);
                    soundsCDs[SoundType.Victory] = cooldown;
                }
                break;
            case SoundType.Loss:
                if (lossSound && (!soundsCDs.ContainsKey(SoundType.Loss) || soundsCDs[SoundType.Loss] <= 0f))
                {
                    audioSource.PlayOneShot(lossSound);
                    soundsCDs[SoundType.Loss] = cooldown;
                }
                break;
            case SoundType.LowDurability:
                if (lowDurabilitySounds.Count > 0 && (!soundsCDs.ContainsKey(SoundType.LowDurability) || soundsCDs[SoundType.LowDurability] <= 0f))
                {
                    audioSource.PlayOneShot(lowDurabilitySounds.GetRandomElement());
                    soundsCDs[SoundType.LowDurability] = cooldown;
                }
                break;
            case SoundType.Capturing:
                if (capturingSound && (!soundsCDs.ContainsKey(SoundType.Capturing) || soundsCDs[SoundType.Capturing] <= 0f))
                {
                    audioSource.PlayOneShot(capturingSound);
                    soundsCDs[SoundType.Capturing] = cooldown;
                }
                break;
            case SoundType.CapturingFriendly:
                if (capturingFriendlySound && (!soundsCDs.ContainsKey(SoundType.CapturingFriendly) || soundsCDs[SoundType.CapturingFriendly] <= 0f))
                {
                    audioSource.PlayOneShot(capturingFriendlySound);
                    soundsCDs[SoundType.CapturingFriendly] = cooldown;
                }
                break;
            case SoundType.LobbyTimerStart:
                if (lobbyTimerStartSound && (!soundsCDs.ContainsKey(SoundType.LobbyTimerStart) || soundsCDs[SoundType.LobbyTimerStart] <= 0f))
                {
                    audioSource.PlayOneShot(lobbyTimerStartSound);
                    soundsCDs[SoundType.LobbyTimerStart] = cooldown;
                }
                break;
            case SoundType.LobbyTimerThree:
                if (lobbyTimerThreeSound && (!soundsCDs.ContainsKey(SoundType.LobbyTimerThree) || soundsCDs[SoundType.LobbyTimerThree] <= 0f))
                {
                    audioSource.PlayOneShot(lobbyTimerThreeSound);
                    soundsCDs[SoundType.LobbyTimerThree] = cooldown;
                }
                break;
            case SoundType.LobbyTimerTwo:
                if (lobbyTimerTwoSound && (!soundsCDs.ContainsKey(SoundType.LobbyTimerTwo) || soundsCDs[SoundType.LobbyTimerTwo] <= 0f))
                {
                    audioSource.PlayOneShot(lobbyTimerTwoSound);
                    soundsCDs[SoundType.LobbyTimerTwo] = cooldown;
                }
                break;
            case SoundType.LobbyTimerOne:
                if (lobbyTimerOneSound && (!soundsCDs.ContainsKey(SoundType.LobbyTimerOne) || soundsCDs[SoundType.LobbyTimerOne] <= 0f))
                {
                    audioSource.PlayOneShot(lobbyTimerOneSound);
                    soundsCDs[SoundType.LobbyTimerOne] = cooldown;
                }
                break;
            case SoundType.LobbyTimerEnd:
                if (lobbyTimerZeroSounds.Count > 0 && (!soundsCDs.ContainsKey(SoundType.LobbyTimerEnd) || soundsCDs[SoundType.LobbyTimerEnd] <= 0f))
                {
                    audioSource.PlayOneShot(lobbyTimerZeroSounds.GetRandomElement());
                    soundsCDs[SoundType.LobbyTimerEnd] = cooldown;
                }
                break;
        }
    }

    public void DoAnnounce(string msg)
    {
        announcementsLabel.text = msg;
    }

    public void DoKillAnnounce(string killerName, string victimName)
    {
        HideCaptureAnnounce();
        killAnnounceObject.SetActive(false);
        killAnnounceObject.SetActive(true);

        if (killerName.Length < 2)
            killerName = "Turret";

        killAnnounceKillerLabel.text = killerName;
        killAnnounceVictimLabel.text = victimName;

        if (killerName == target.Name)
        {
            PlaySound(SoundType.Kill, 4f);
        }
    }

    public void DoCaptureAnnounce(string capturerName)
    {
        HideKillAnnounce();
        captureAnnounceObject.SetActive(false);
        captureAnnounceObject.SetActive(true);
        capturerLabel.text = capturerName + "<color=\"white\"> is capturing Nexus!</color>";
    }

    public void DoCapturedAnnounce(string capturerName)
    {
        HideKillAnnounce();
        captureAnnounceObject.SetActive(false);
        captureAnnounceObject.SetActive(true);
        capturerLabel.text = capturerName + "<color=\"white\"> has captured Nexus!</color>";
    }

    public void DoRespawnAnnounce(int seconds)
    {
        HideCaptureAnnounce();
        HideKillAnnounce();
        respawnAnnounceObject.SetActive(false);
        respawnAnnounceObject.SetActive(true);
        respawnAnnounceLabel.text = "Respawn in: " + seconds.ToString();
    }

    public void HideKillAnnounce()
    {
        killAnnounceObject.SetActive(false);
    }

    public void HideCaptureAnnounce()
    {
        captureAnnounceObject.SetActive(false);
    }

    public void StartMission()
    {
        OnLobbyTimerEnded();
    }

    public void AddEnemyToMiniMap(PlayerController _enemy, string nickname, bool isAI)
    {
        if (!isMissionMode || (isMissionMode && !isAI))
            OnLobbyPlayerAdded(nickname, _enemy.ShipIcon, isAI);

        if (enemies.Contains(_enemy)) return;

        enemies.Add(_enemy);

        try
        {
            GameObject miniMapEnemy = Instantiate(minimapEnemyPrefab, minimapPlayer.parent);
            miniMapEnemy.transform.localPosition = new Vector3(_enemy.transform.position.x / mapSizeAmplifier, _enemy.transform.position.z / mapSizeAmplifier, 0f);
            miniMapEnemy.transform.localEulerAngles = new Vector3(0f, 0f, -_enemy.transform.localEulerAngles.y);
            enemiesIcons.Add(miniMapEnemy.transform);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void AddMissionBotToMiniMap(PlayerController _enemy)
    {
        if (enemies.Contains(_enemy)) return;

        enemies.Add(_enemy);
        GameObject miniMapEnemy = Instantiate(minimapEnemyPrefab, minimapPlayer.parent);
        miniMapEnemy.transform.localPosition = new Vector3(_enemy.transform.position.x / mapSizeAmplifier, _enemy.transform.position.z / mapSizeAmplifier, 0f);
        miniMapEnemy.transform.localEulerAngles = new Vector3(0f, 0f, -_enemy.transform.localEulerAngles.y);
        enemiesIcons.Add(miniMapEnemy.transform);
    }

    public void SetTarget(PlayerController _target)
    {
        if (_target == null)
        {
            Debug.LogError("<Color=Red><a>Missing</a></Color> PlayMakerManager target for PlayerUI.SetTarget.", this);
            return;
        }
        // Cache references for efficiency
        target = _target;

        target.SetJoystick(Joystick);
        target.SetWeaponJoystick(WeaponJoystick);

        WeaponJoystick.OnPointerDownCallback += Shoot;
        WeaponJoystick.OnPointerUpCallback += ShootEnd;

        if (ArenaController.instance != null)
        {
            arenaController = ArenaController.instance;
            isMissionMode = false;
            lobbyMissionButtons.SetActive(false);
            //lobbyArenaButtons.SetActive(true);

            mapSizeAmplifier = 5f;
        }
        else if (MissionController.instance != null)
        {
            missionController = MissionController.instance;
            isMissionMode = true;

            if (PhotonNetwork.IsMasterClient)
            {
                lobbyMissionButtons.SetActive(true);
                lobbyArenaButtons.SetActive(false);
            }
            else
            {
                lobbyMissionButtons.SetActive(false);
                //lobbyArenaButtons.SetActive(true);
            }

            mapSizeAmplifier = Mathf.Max(missionController.MapWidth, missionController.MapHeight) * 5f / 1000f;

            GameObject arrow = Instantiate(nexusArrowPrefab, mainCanvas);
            arrow.GetComponent<HUDArrow>().target = missionController.NexusTransform;
        }

#if UNITY_STANDALONE || UNITY_EDITOR
        //joystick.gameObject.SetActive(false);
#endif

        radar.localScale = Vector3.one * target.RadarRadius / 40f;

        for (int i = 0; i < m_skillsButtons.Count; i++)
        {
            m_skillsButtons[i].SetData(target.Skills[i]);
        }

        //TODO: Remove this when cooperative missions will be fixed
        IsLobbyState = !isMissionMode;

        if (!isMissionMode)
        {
            lobbyScreen.SetActive(true);
            matchTimerLabel.transform.parent.gameObject.SetActive(false);

            target.timer.OnUpdated += OnLobbyTimerUpdated;
            target.timer.OnFinished += OnLobbyTimerEnded;
            target.matchTimer.OnUpdated += OnMatchTimerUpdated;

            if (PhotonNetwork.IsMasterClient)
                PlaySound(SoundType.GetReady, 20f);
        }
        else
            OnLobbyTimerEnded();
        ////TODO
        
        //TODO: Uncomment when cooperative missions will be fixed
        /*IsLobbyState = true;
        lobbyScreen.SetActive(true);
        matchTimerLabel.transform.parent.gameObject.SetActive(false);

        target.timer.OnUpdated += OnLobbyTimerUpdated;
        target.timer.OnFinished += OnLobbyTimerEnded;
        target.matchTimer.OnUpdated += OnMatchTimerUpdated;

        if (PhotonNetwork.IsMasterClient)
            PlaySound(SoundType.GetReady, 20f);*/

        OnLobbyPlayerAdded(PhotonNetwork.NickName, target.ShipIcon, false);
        AddPlayerStatsSlot(target, AccountManager.CurrentRating, target.UpgradesScore);

        if (isMissionMode)
        {
            statsButton.SetActive(false);
            Vector3 mapPos = missionController.GetMapPosition(missionController.NexusPosition);
            minimapNexus.transform.localPosition = new Vector3(mapPos.x / mapSizeAmplifier, mapPos.z / mapSizeAmplifier, 0f);
            missionObjectiveHolder.SetActive(true);
            missionObjectiveLabel.text = string.Format("Drones killed: {0}/{1}", missionController.KilledBotsCount, missionController.InitBotsCount);
            missionObjectiveLabel2.text = "Nexus captured: 0/1";
        }

        IsInitialized = true;
    }

    void OnMatchTimerUpdated()
    {
        if (target.MatchTimer < BalanceProvider.Balance.fightLength)
        {
            OnLobbyTimerEnded();
        }

        target.matchTimer.OnUpdated -= OnMatchTimerUpdated;
    }

    void OnLobbyTimerEnded()
    {
        target.timer.Stop();

        IsLobbyState = false;

        lobbyScreen.SetActive(false);
        matchTimerLabel.transform.parent.gameObject.SetActive(true);

        target.SendRatingAndUpgrades();

        PlaySound(SoundType.LobbyTimerEnd);

        target.timer.OnUpdated -= OnLobbyTimerUpdated;
        target.timer.OnFinished -= OnLobbyTimerEnded;
        target.matchTimer.OnUpdated -= OnMatchTimerUpdated;

        if (isMissionMode)
        {
            target.OnMissionStarted();

            if (!AccountManager.IsMissionTutorialDone)
                TutorialController.instance.ShowCustomTutorialUnit("Capture the Nexus on the right side of this area and kill all drones on the way!", minimapNexus.GetComponent<RectTransform>(), OnMissionTutorialDone);
        }
        else
        {
            if (!AccountManager.IsArenaTutorialDone)
                TutorialController.instance.ShowCustomTutorialUnit("Move to the Nexus on Arena center and try to capture it!", minimapNexus.GetComponent<RectTransform>(), OnArenaTutorialDone);
        }

        if (PhotonNetwork.IsMasterClient && !isMissionMode)
        {
            ArenaController.instance.OnBattleStarted();
        }
    }

    void OnLobbyTimerUpdated()
    {
        IsLobbyState = target.LobbyTimer > 0f;

        if (!IsLobbyState)
        {
            OnLobbyTimerEnded();
        }
    }

    void OnArenaTutorialDone()
    {
        Launcher.instance.OnArenaTutorialDone();
        if (!AccountManager.IsControlTutorialDone)
            TutorialController.instance.ShowCustomTutorialUnit("Use left joystick to move your ship!", joystick.GetComponent<RectTransform>(), OnMovementTutorialDone);
    }

    void OnMissionTutorialDone()
    {
        Launcher.instance.OnMissionTutorialDone();
        if (!AccountManager.IsControlTutorialDone)
            TutorialController.instance.ShowCustomTutorialUnit("Use left joystick to move your ship!", joystick.GetComponent<RectTransform>(), OnMovementTutorialDone);
    }

    void OnMovementTutorialDone()
    {
        TutorialController.instance.ShowCustomTutorialUnit("Use right joystick to rotate your weapon!", weaponJoystick.GetComponent<RectTransform>(), OnWeaponTutorialDone);
    }

    void OnWeaponTutorialDone()
    {
        AccountManager.OnControlTutorialDone();
    }

    public void OnLobbyPlayerAdded(string nickname, Sprite shipIcon, bool isAI)
    {
        if (!IsLobbyState || m_lobbyPlayers.FindIndex(x => x.Nickname == nickname) >= 0 || PhotonNetwork.CurrentRoom == null) return;
        GameObject playerSlot = Instantiate(lobbyPlayerSlotPrefab, lobbyPlayersHolder);
        LobbyPlayerSlot lps = playerSlot.GetComponent<LobbyPlayerSlot>();
        lps.SetData(nickname, shipIcon, isAI);
        m_lobbyPlayers.Add(lps);
        lobbyScreenTitle.text = string.Format("Players ready to fight: {0}/{1}", m_lobbyPlayers.Count, PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public void OnLobbyPlayerDeleted(string nickname)
    {
        if (!IsLobbyState || m_lobbyPlayers.FindIndex(x => x.Nickname == nickname) < 0 || PhotonNetwork.CurrentRoom == null) return;
        LobbyPlayerSlot lps = m_lobbyPlayers.Find(x => x.Nickname == nickname);
        m_lobbyPlayers.Remove(lps);
        Destroy(lps.gameObject);
        lobbyScreenTitle.text = string.Format("Players ready to fight: {0}/{1}", m_lobbyPlayers.Count, PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public void OnLoss(int currRating, int ratingChange, int place, int moneyGained)
    {
        lossScreen.SetActive(true);

        loseScreenRatingLabel.text = currRating.ToString();
        string suffix = "th";
        if (place == 1) suffix = "st";
        if (place == 2) suffix = "nd";
        if (place == 3) suffix = "rd";
        loseScreenPlaceLabel.text = place.ToString() + suffix + " Place!";

        PlaySound(SoundType.Loss, 5f);

        StartCoroutine(LoseScreenAnim(currRating, ratingChange, moneyGained));
    }

    public void OnWin(int currRating, int ratingChange, int place, int moneyGained)
    {
        winScreen.SetActive(true);
        winScreenRatingLabel.text = "+0";
        winScreenCurrencyLabel.text = "+0";
        string suffix = "th";
        if (place == 1) suffix = "st";
        if (place == 2) suffix = "nd";
        if (place == 3) suffix = "rd";
        winScreenPlaceLabel.text = place.ToString() + suffix + " Place!";

        PlaySound(SoundType.Victory, 5f);

        StartCoroutine(WinScreenAnim(currRating, ratingChange, moneyGained));
    }

    public void OnMissionCompleted(int moneyGained)
    {
        missionCompletedScreen.SetActive(true);
        missionCompletedCurrencyLabel.text = "+0";

        PlaySound(SoundType.Victory, 5f);

        StartCoroutine(MissionCompletedScreenAnim(moneyGained));
    }

    public void OnMissionFailed()
    {
        missionFailedScreen.SetActive(true);

        PlaySound(SoundType.Loss, 5f);
    }

    public void OnMasterClientSwitched(bool imMaster = false)
    {
        if (imMaster && isMissionMode)
        {
            lobbyMissionButtons.SetActive(true);
            lobbyArenaButtons.SetActive(false);
        }
        else
        {
            lobbyMissionButtons.SetActive(false);
            lobbyArenaButtons.SetActive(true);
        }
    }

    public void OnRoomLeft()
    {
        target.timer.OnUpdated -= OnLobbyTimerUpdated;
        target.timer.OnFinished -= OnLobbyTimerEnded;
        target.matchTimer.OnUpdated -= OnMatchTimerUpdated;
    }

    void AddEnemyArrow(PlayerController enemy)
    {
        if (!enemyArrows.ContainsKey(enemy))
        {
            GameObject arrow = Instantiate(enemyArrowPrefab, mainCanvas);
            arrow.GetComponent<HUDArrow>().target = enemy.transform;
            enemyArrows.Add(enemy, arrow);
        }
    }

    void RemoveEnemyArrow(PlayerController enemy)
    {
        if (enemyArrows.ContainsKey(enemy))
        {
            GameObject arrow = enemyArrows[enemy];
            enemyArrows.Remove(enemy);
            Destroy(arrow);
        }
    }

    public bool ResultsScreenShown => winScreen.activeSelf || lossScreen.activeSelf || missionCompletedScreen.activeSelf;

    public Joystick Joystick { get => joystick; }

    public Joystick WeaponJoystick { get => weaponJoystick; }

    private IEnumerator MissionCompletedScreenAnim(int moneyGained)
    {
        float t = 0f;

        int startValue = 0;
        int endValue = moneyGained;

        do
        {
            t += Time.deltaTime * 2f;

            int moneyToShow = Mathf.FloorToInt(Mathf.Lerp(startValue, endValue, t));
            missionCompletedCurrencyLabel.text = "+" + moneyToShow.ToString();

            yield return null;
        }
        while (t < 1f);

        missionCompletedCurrencyLabel.text = "+" + endValue.ToString();
    }

    private IEnumerator WinScreenAnim(int currRating, int ratingChange, int moneyGained)
    {
        float t = 0f;

        int startValue = 0;
        int endValue = ratingChange;
        int endValue2 = moneyGained;

        do
        {
            t += Time.deltaTime * 2f;

            int ratingToShow = Mathf.FloorToInt(Mathf.Lerp(startValue, endValue, t));
            int moneyToShow = Mathf.FloorToInt(Mathf.Lerp(startValue, endValue2, t));
            winScreenRatingLabel.text = "+" + ratingToShow.ToString();
            winScreenCurrencyLabel.text = "+" + moneyToShow.ToString();

            yield return null;
        }
        while (t < 1f);

        winScreenRatingLabel.text = "+" + endValue.ToString();
        winScreenCurrencyLabel.text = "+" + endValue2.ToString();
    }

    private IEnumerator LoseScreenAnim(int currRating, int ratingChange, int moneyGained)
    {
        float t = 0f;

        int startValue = 0;
        int endValue = ratingChange;
        int endValue2 = moneyGained;

        do
        {
            t += Time.deltaTime * 2f;

            int ratingToShow = Mathf.FloorToInt(Mathf.Lerp(startValue, endValue, t));
            int moneyToShow = Mathf.FloorToInt(Mathf.Lerp(startValue, endValue2, t));
            loseScreenRatingLabel.text = ratingToShow.ToString();
            loseScreenCurrencyLabel.text = "+" + moneyToShow.ToString();

            yield return null;
        }
        while (t < 1f);

        loseScreenRatingLabel.text = endValue.ToString();
        loseScreenCurrencyLabel.text = "+" + endValue2.ToString();
    }

    public void LeaveArena(bool changeRating = false)
    {
        if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Leaving) return;

        //if (PhotonNetwork.IsMasterClient)
        //{
        //target.timer.Stop();
        //target.matchTimer.Stop();
        //}

        if (!isMissionMode)
            ArenaController.instance.LeaveRoom();
        else
            MissionController.instance.LeaveRoom();

        if (changeRating && !isMissionMode)
            Launcher.instance.OnFightLoss();
        else if (isMissionMode)
            Launcher.instance.OnMissionFailed();
    }

    public void Shoot()
    {
        target.StartShooting();
    }

    public void ShootEnd()
    {
        target.EndShooting();
    }

    public void OnDeath()
    {
        foreach (var s in m_skillsButtons)
        {
            s.GetComponent<Button>().interactable = false;
        }

        Joystick.enabled = false;
        WeaponJoystick.enabled = false;
    }

    public void OnSpawn()
    {
        foreach (var s in m_skillsButtons)
        {
            s.GetComponent<Button>().interactable = true;
        }

        Joystick.enabled = true;
        WeaponJoystick.enabled = true;
    }

    private void Awake()
    {
        if (!Instance) Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    private void OnDestroy()
    {
        target.timer.OnUpdated -= OnLobbyTimerUpdated;
        target.timer.OnFinished -= OnLobbyTimerEnded;
        target.matchTimer.OnUpdated -= OnMatchTimerUpdated;
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
        {
            if (IsInitialized)
                Destroy(this.gameObject);
            return;
        }

        #if UNITY_EDITOR
        if (!target.IsDied)
        {
            if (Input.GetKeyUp(KeyCode.Q))
            {
                target.UseSkill(0);
            }

            if (Input.GetKeyUp(KeyCode.E))
            {
                target.UseSkill(1);
            }

            if (Input.GetKeyUp(KeyCode.C))
            {
                target.UseSkill(2);
            }
        }
        #endif

        List<SoundType> soundTypes = new List<SoundType>(soundsCDs.Keys);
        foreach (var soundType in soundTypes)
        {
            if (soundsCDs[soundType] > 0f)
                soundsCDs[soundType] -= Time.deltaTime;
        }

        if (IsLobbyState)
        {
            if (target.LobbyTimer > 0f)
            {
                int deltaSeconds = Mathf.CeilToInt(target.LobbyTimer);
                int minutes = Mathf.FloorToInt(deltaSeconds / 60);
                int seconds = deltaSeconds % 60;
                lobbyTimer.text = minutes.ToString("00") + ":" + seconds.ToString("00");
                if (deltaSeconds == 3)
                {
                    PlaySound(SoundType.LobbyTimerThree, 2f);
                }
                else if (deltaSeconds == 2)
                {
                    PlaySound(SoundType.LobbyTimerTwo, 2f);
                }
                else if (deltaSeconds == 1)
                {
                    PlaySound(SoundType.LobbyTimerOne, 2f);
                }
                else if (deltaSeconds == 6)
                {
                    PlaySound(SoundType.LobbyTimerStart, 2f);
                }
            }
        }
        else
        {
            int deltaSeconds = Mathf.CeilToInt(target.MatchTimer);
            int minutes = Mathf.FloorToInt(deltaSeconds / 60);
            int seconds = deltaSeconds % 60;
            matchTimerLabel.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        if (!isMissionMode)
        {
            minimapPlayer.localPosition = new Vector3(target.transform.position.x / mapSizeAmplifier, target.transform.position.z / mapSizeAmplifier, 0f);
        }
        else
        {
            Vector3 mapPos = missionController.GetMapPosition(target.transform.position);
            minimapPlayer.localPosition = new Vector3(mapPos.x / mapSizeAmplifier, mapPos.z / mapSizeAmplifier, 0f);
        }

        minimapPlayer.localEulerAngles = new Vector3(0f, 0f, -target.transform.localEulerAngles.y);

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] && !enemies[i].IsDied)
            {
                Vector3 mapPos = isMissionMode ? missionController.GetMapPosition(enemies[i].transform.position) : enemies[i].transform.position;

                enemiesIcons[i].localPosition = new Vector3(mapPos.x / mapSizeAmplifier, mapPos.z / mapSizeAmplifier, 0f);
                enemiesIcons[i].localEulerAngles = new Vector3(0f, 0f, -enemies[i].transform.localEulerAngles.y);

                enemiesIcons[i].gameObject.SetActive(enemies[i].DurabilityPercent > 0f && enemiesIcons[i].localPosition.Distance(minimapPlayer.localPosition) < target.RadarRadius);

                if (enemiesIcons[i].gameObject.activeSelf)
                    AddEnemyArrow(enemies[i]);
                else
                    RemoveEnemyArrow(enemies[i]);
            }
            else
            {
                enemiesIcons[i].gameObject.SetActive(false);
                RemoveEnemyArrow(enemies[i]);
                continue;
            }
        }

        if (isMissionMode)
        {
            missionObjectiveLabel.text = string.Format("Drones killed: {0}/{1}", missionController.KilledBotsCount, missionController.InitBotsCount);
            missionObjectiveLabel2.text = string.Format("Nexus captured: {0}/{1}", missionController.IsNexusCaptured ? 1 : 0, 1);

            if (missionController.IsObjectiveDone && missionController.IsNexusCaptured && PhotonNetwork.IsMasterClient)
            {
                target.EndMatch(true);
            }
        }

        if (playerHealthBar != null)
        {
            playerHealthBar.fillAmount = target.DurabilityPercent;
        }

        if (playerForceFieldBar != null)
        {
            playerForceFieldBar.fillAmount = target.FieldPercent;
        }
    }
}
