using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance;

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
    private Transform radar;
    [SerializeField]
    private List<SkillButton> m_skillsButtons;
    [SerializeField]
    private TextMeshProUGUI announcementsLabel;
    [SerializeField]
    private GameObject lobbyScreen;
    [SerializeField]
    private GameObject lobbyPlayerSlotPrefab;
    [SerializeField]
    private TextMeshProUGUI lobbyTimer;
    [SerializeField]
    private Transform lobbyPlayersHolder;
    [SerializeField]
    private GameObject winScreen;
    [SerializeField]
    private TextMeshProUGUI winScreenRatingLabel;
    [SerializeField]
    private GameObject lossScreen;
    [SerializeField]
    private TextMeshProUGUI lobbyScreenTitle;
    [SerializeField]
    private GameObject killAnnounceObject;
    [SerializeField]
    private TextMeshProUGUI killAnnounceKillerLabel;
    [SerializeField]
    private TextMeshProUGUI killAnnounceVictimLabel;
    [SerializeField]
    private GameObject captureAnnounceObject;
    [SerializeField]
    private TextMeshProUGUI capturerLabel;

    private PlayerController target;
    private List<PlayerController> enemies = new List<PlayerController>();
    private List<Transform> enemiesIcons = new List<Transform>();
    private List<LobbyPlayerSlot> m_lobbyPlayers = new List<LobbyPlayerSlot>();

    public bool IsLobbyState = true;
    private bool IsInitialized = false;

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
    }

    public void DoCaptureAnnounce(string capturerName)
    {
        HideKillAnnounce();
        captureAnnounceObject.SetActive(false);
        captureAnnounceObject.SetActive(true);
        capturerLabel.text = capturerName + "<color=\"white\"> is capturing Nexus!</color>";
    }

    public void HideKillAnnounce()
    {
        killAnnounceObject.SetActive(false);
    }

    public void HideCaptureAnnounce()
    {
        captureAnnounceObject.SetActive(false);
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

        OnLobbyPlayerAdded(PhotonNetwork.NickName, target.ShipIcon, false);

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

    public void OnLoss()
    {
        lossScreen.SetActive(true);
    }

    public void OnWin(int currRating, int ratingChange)
    {
        winScreen.SetActive(true);
        winScreenRatingLabel.text = currRating.ToString();

        StartCoroutine(WinScreenAnim(currRating, ratingChange));
    }

    private IEnumerator WinScreenAnim(int currRating, int ratingChange)
    {
        float t = 0f;

        int startValue = currRating;
        int endValue = currRating + ratingChange;

        do
        {
            t += Time.deltaTime * 2f;

            int ratingToShow = Mathf.FloorToInt(Mathf.Lerp(startValue, endValue, t));
            winScreenRatingLabel.text = ratingToShow.ToString();

            yield return null;
        }
        while( t < 1f );

        winScreenRatingLabel.text = endValue.ToString();
    }

    public void LeaveArena()
    {
        ArenaController.instance.LeaveRoom();
    }

    public void Shoot()
    {
        target.StartShooting();
    }

    public void ShootEnd()
    {
        target.EndShooting();
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

        if (IsLobbyState)
        {
            if (target.LobbyTimer <= 0f)
            {
                IsLobbyState = false;
                lobbyScreen.SetActive(false);
            }
            else
            {
                int deltaSeconds = Mathf.CeilToInt(target.LobbyTimer);
                int minutes = Mathf.FloorToInt(deltaSeconds / 60);
                int seconds = deltaSeconds % 60;
                lobbyTimer.text = minutes.ToString("00") + ":" + seconds.ToString("00");
            }
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
