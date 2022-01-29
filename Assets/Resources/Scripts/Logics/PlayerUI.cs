using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance;

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
    private VirtualJoystick joystick;
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

    public bool IsLobbyState = true;
    private bool IsInitialized = false;
    private Dictionary<SoundType, float> soundsCDs = new Dictionary<SoundType, float>();

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
        IsLobbyState = false;
        lobbyScreen.SetActive(false);
        matchTimerLabel.transform.parent.gameObject.SetActive(true);
        PlaySound(SoundType.LobbyTimerEnd);

        target.SendRatingAndUpgrades();
        target.OnMissionStarted();
    }

    public void AddEnemyToMiniMap(PlayerController _enemy, string nickname, bool isAI)
    {
        if (enemies.Contains(_enemy)) return;

        OnLobbyPlayerAdded(nickname, _enemy.ShipIcon, isAI);
        enemies.Add(_enemy);
        GameObject miniMapEnemy = Instantiate(minimapEnemyPrefab, minimapPlayer.parent);
        miniMapEnemy.transform.localPosition = new Vector3(_enemy.transform.position.x / 5f, _enemy.transform.position.z / 5f, 0f);
        miniMapEnemy.transform.localEulerAngles = new Vector3(0f, 0f, -_enemy.transform.localEulerAngles.y);
        enemiesIcons.Add(miniMapEnemy.transform);
    }

    public void AddMissionBotToMiniMap(PlayerController _enemy)
    {
        if (enemies.Contains(_enemy)) return;

        enemies.Add(_enemy);
        GameObject miniMapEnemy = Instantiate(minimapEnemyPrefab, minimapPlayer.parent);
        miniMapEnemy.transform.localPosition = new Vector3(_enemy.transform.position.x / 5f, _enemy.transform.position.z / 5f, 0f);
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

        target.SetJoystick(joystick);

        if (ArenaController.instance != null)
        {
            arenaController = ArenaController.instance;
            isMissionMode = false;
            lobbyMissionButtons.SetActive(false);
            lobbyArenaButtons.SetActive(true);
        }
        else if (MissionController.instance != null)
        {
            missionController = MissionController.instance;
            isMissionMode = true;
            lobbyMissionButtons.SetActive(true);
            lobbyArenaButtons.SetActive(false);
        }

#if UNITY_STANDALONE || UNITY_EDITOR
        joystick.gameObject.SetActive(false);
#endif

        radar.localScale = Vector3.one * target.RadarRadius / 40f;

        for (int i = 0; i < m_skillsButtons.Count; i++)
        {
            m_skillsButtons[i].SetData(target.Skills[i]);
        }

        IsLobbyState = target.LobbyTimer > 0f;
        lobbyScreen.SetActive(IsLobbyState);
        matchTimerLabel.transform.parent.gameObject.SetActive(!IsLobbyState);

        if (IsLobbyState) PlaySound(SoundType.GetReady, 20f);

        OnLobbyPlayerAdded(PhotonNetwork.NickName, target.ShipIcon, false);

        if (isMissionMode)
        {
            statsButton.SetActive(false);
            minimapNexus.SetActive(false);
            missionObjectiveHolder.SetActive(true);
            missionObjectiveLabel.text = string.Format("Drones killed: {0}/{1}", missionController.KilledBotsCount, missionController.InitBotsCount);
            missionObjectiveLabel2.text = "Nexus captured: 0/1";
        }

        IsInitialized = true;
    }

    public void OnLobbyPlayerAdded(string nickname, Sprite shipIcon, bool isAI)
    {
        if (!IsLobbyState) return;
        GameObject playerSlot = Instantiate(lobbyPlayerSlotPrefab, lobbyPlayersHolder);
        LobbyPlayerSlot lps = playerSlot.GetComponent<LobbyPlayerSlot>();
        lps.SetData(nickname, shipIcon, isAI);
        m_lobbyPlayers.Add(lps);
        lobbyScreenTitle.text = string.Format("Players ready to fight: {0}/{1}", m_lobbyPlayers.Count, PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public void OnLobbyPlayerDeleted(string nickname)
    {
        if (!IsLobbyState) return;
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

    public bool ResultsScreenShown => winScreen.activeSelf || lossScreen.activeSelf || missionCompletedScreen.activeSelf;

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
        if (!isMissionMode)
            ArenaController.instance.LeaveRoom();
        else
            MissionController.instance.LeaveRoom();

        if (changeRating)
            Launcher.instance.OnFightLoss();
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

        joystick.Disable();
    }

    public void OnSpawn()
    {
        foreach (var s in m_skillsButtons)
        {
            s.GetComponent<Button>().interactable = true;
        }

        joystick.Enable();
    }

    private void Awake()
    {
        if (!Instance) Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {

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

        List<SoundType> soundTypes = new List<SoundType>(soundsCDs.Keys);
        foreach (var soundType in soundTypes)
        {
            if (soundsCDs[soundType] > 0f)
                soundsCDs[soundType] -= Time.deltaTime;
        }

        if (IsLobbyState)
        {
            if (target.LobbyTimer <= 0f)
            {
                IsLobbyState = false;
                lobbyScreen.SetActive(false);
                matchTimerLabel.transform.parent.gameObject.SetActive(true);
                PlaySound(SoundType.LobbyTimerEnd);

                target.SendRatingAndUpgrades();

                if (isMissionMode)
                {
                    target.OnMissionStarted();
                }

                if (PhotonNetwork.IsMasterClient && !isMissionMode)
                {
                    ArenaController.instance.OnBattleStarted();
                }
            }
            else
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

        minimapPlayer.localPosition = new Vector3(target.transform.position.x / 5f, target.transform.position.z / 5f, 0f);
        minimapPlayer.localEulerAngles = new Vector3(0f, 0f, -target.transform.localEulerAngles.y);

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i])
            {
                enemiesIcons[i].localPosition = new Vector3(enemies[i].transform.position.x / 5f, enemies[i].transform.position.z / 5f, 0f);
                enemiesIcons[i].localEulerAngles = new Vector3(0f, 0f, -enemies[i].transform.localEulerAngles.y);

                enemiesIcons[i].gameObject.SetActive(enemies[i].DurabilityPercent > 0f && enemiesIcons[i].localPosition.Distance(minimapPlayer.localPosition) < target.RadarRadius);
            }
            else
            {
                enemiesIcons[i].gameObject.SetActive(false);
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
