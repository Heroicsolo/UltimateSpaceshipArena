using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    public static GameObject LocalPlayerInstance;
    public static PlayerController LocalPlayer;

    public int ID = 0;
    [SerializeField]
    public GameObject PlayerUiPrefab;
    [SerializeField]
    private GameObject ProjectilePrefab;
    [SerializeField]
    private List<Transform> ShootPositions;
    [SerializeField]
    private Transform BombLaunchPosition;
    [SerializeField]
    private Transform FloatingTextSpawnPosition;
    [SerializeField]
    private GameObject ImmortalityOrb;
    [SerializeField]
    private TextMeshPro NameLabel;
    [SerializeField]
    private List<ParticleSystem> EngineFlames;
    [SerializeField]
    private MeshRenderer meshRenderer;
    [SerializeField]
    private Material transparentMaterial;
    [SerializeField]
    private ParticleSystem NitroEffect;
    [SerializeField]
    private GameObject DamageTextPrefab;
    [SerializeField]
    private GameObject RepairTextPrefab;
    [SerializeField]
    private GameObject InfoTextPrefab;
    [SerializeField]
    private GameObject DeathEffect;
    [SerializeField]
    private bool isAI = false;
    [SerializeField]
    private bool isMissionBot = false;
    [SerializeField]
    private Image HPBar;
    [SerializeField]
    private Image ShieldBar;
    [SerializeField]
    private float BarsOffset = 17f;
    [SerializeField]
    private float AIEnemyFindingRadius = 500f;
    [SerializeField]
    private float AIPickupsFindingRadius = 600f;
    [SerializeField]
    private float AINexusFindingRadius = 600f;
    [SerializeField]
    private float AIDurabilityPercentToRetreat = 0.4f;
    [SerializeField] private List<UpgradeData> m_upgrades;

    [SerializeField] private List<SkillData> m_skills;

    public Sprite ShipIcon;
    public string ShipTitle = "Spaceship";

    private float m_shootInternalCD = 0.25f;

    private float m_durability = 100f;

    private float m_critBonus = 0f;
    private float m_critDamageBonus = 0f;

    [SerializeField]
    private float m_forceField = 0f;

    [SerializeField]
    private float m_radarRadius = 1f;

    private float m_immuneTime = 0f;
    private float m_stealthTime = 0f;

    [SerializeField]
    private float m_durabilityRegen = 0f;
    [SerializeField]
    private float m_fieldRegen = 0f;

    [SerializeField]
    private int m_maxDurability = 100;
    [SerializeField]
    private int m_maxField = 100;

    private int m_scaledMaxDurability;
    private int m_scaledMaxField;
    private float m_damageModifier = 1f;

    private float m_speed = 0f;
    private float m_currShieldRegenDelay = 0f;

    [SerializeField]
    private float m_maxSpeed = 50f;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip deathSound;

    private float m_acceleration = 6f;
    private float m_rollForce = 20f;
    private float m_speedBonus = 0f;
    private float m_speedBonusLength = 0f;

    private float m_currRoll = 0f;

    private bool isFiring = false;
    private bool isNitroActive = false;

    private bool missionStarted = false;

    private VirtualJoystick joystick;

    private Transform cameraTransform;
    private CharacterController charController;

    private Vector3 movementDir;
    private Vector3 initPos;

    private Vector3 networkPos;
    private Quaternion networkRot;
    private Vector3 targetCameraPos;

    private Material initMaterial;

    private Transform barsHolder;

    private Timer timer;
    private Timer matchTimer;

    private string m_name = "";

    private bool m_targetIsPlayer = false;
    private bool m_targetIsNexus = false;
    private Transform m_currentAITarget;
    private PlayerController m_currentAIEnemy;
    private float m_AITargetChangeDelay = 10f;
    private float m_spectacleTime = 3f;
    private bool m_isDied = false;
    private bool m_isWon = false;
    private bool m_isLoss = false;
    private bool m_nexusUsed = false;
    private bool m_initialized = false;
    private Transform m_lastEnemy;
    private string m_lastEnemyName = "";
    private List<float> m_AI_skillsCD = new List<float>();
    private float m_currAITargetChangeDelay = 0f;
    private float m_globalCD = 0f;
    private Vector3 m_spawnPoint;
    private float m_currRespawnTime = 0f;
    private float m_respawnTimeBonus = 0f;
    private float m_stealthSpeedBonus = 0f;
    private float m_stealthLengthBonus = 0f;
    private float m_stealthCritBonus = 0f;
    private float m_repairBonus = 0f;
    private float m_immortalityTimeBonus = 0f;
    private int m_killsCount = 0;
    private int m_deathsCount = 0;
    private int m_upgradesScore = 0;

    private MissionController missionController;
    private ArenaController arenaController;
    private bool isMissionMode = false;
    private List<PlayerController> m_roomPlayers;

    private AudioSource audioSource;

    private BalanceInfo m_balance;

    public float DurabilityPercent => (float)m_durability / (float)m_scaledMaxDurability;
    public float FieldPercent => (float)m_forceField / (float)m_scaledMaxField;

    public float RadarRadius => m_radarRadius;

    public int MaxDurability => m_scaledMaxDurability;
    public int MaxShield => m_scaledMaxField;

    public float MaxSpeed => m_maxSpeed;

    public Projectile MainProjectile => ProjectilePrefab.GetComponent<Projectile>();

    public bool IsAI => isAI;

    public bool IsDied => m_durability <= 0 || m_isDied;

    public int KillsCount { get { return m_killsCount; } set { m_killsCount = value; } }
    public int DeathsCount { get { return m_deathsCount; } set { m_deathsCount = value; } }

    public int Score => KillsCount > 0 ? KillsCount - DeathsCount + 1 : KillsCount - DeathsCount;

    public bool InStealth => m_stealthTime > 0f;
    public List<SkillData> Skills => m_skills;

    public List<UpgradeData> Upgrades => m_upgrades;

    public string Name => m_name;

    public float LobbyTimer => timer.GetTime();
    public float MatchTimer => matchTimer.GetTime();

#if UNITY_5_4_OR_NEWER
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadingMode)
    {
        if (photonView.IsMine)
            this.CalledOnLevelWasLoaded(scene.buildIndex);
    }
#endif

    // Start is called before the first frame update
    void Start()
    {
        if (photonView.IsMine && !IsAI)
        {
            LocalPlayerInstance = this.gameObject;
            LocalPlayer = this;
        }

        if (ArenaController.instance != null)
        {
            arenaController = ArenaController.instance;
            isMissionMode = false;
        }
        else if (MissionController.instance != null)
        {
            missionController = MissionController.instance;
            isMissionMode = true;
        }

        m_roomPlayers = isMissionMode ? missionController.RoomPlayers : arenaController.RoomPlayers;

        m_balance = Launcher.instance.Balance;

        charController = GetComponent<CharacterController>();

        initMaterial = meshRenderer.material;

        barsHolder = HPBar.transform.parent.parent;

        m_spawnPoint = transform.position;

        DontDestroyOnLoad(this.gameObject);

        m_spectacleTime = m_balance.spectacleTime;
        m_isDied = false;
        m_isWon = false;
        m_isLoss = false;

        m_durability = m_maxDurability;
        m_forceField = 0;
        m_speed = 0f;

        m_scaledMaxDurability = m_maxDurability;
        m_scaledMaxField = m_maxField;

        ImmortalityOrb.SetActive(true);

        cameraTransform = Camera.main.transform;

        if (!isMissionMode)
            m_name = IsAI ? ArenaController.instance.RandomBotName : photonView.Owner.NickName;
        else
            m_name = IsAI ? "" : photonView.Owner.NickName;

        NameLabel.text = m_name;

        cameraTransform.localEulerAngles = new Vector3(90f, 0f, 0f);

        if (!IsAI && photonView.IsMine)
        {
            ApplyUpgrades();
        }

        timer = new Timer();
        timer.Initialize("lobbyTimer");

        matchTimer = new Timer();
        matchTimer.Initialize("matchTimer");

        if (photonView.IsMine && !IsAI)
        {
            if (PhotonNetwork.IsMasterClient)
                timer.Start(m_balance.lobbyLength);

            if (PhotonNetwork.IsMasterClient && !isMissionMode)
                matchTimer.Start(m_balance.lobbyLength + m_balance.fightLength);

            if (PlayerUiPrefab != null)
            {
                GameObject _uiGo = Instantiate(PlayerUiPrefab);
                _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
            }
            else
            {
                Debug.LogWarning("<Color=Red><a>Missing</a></Color> PlayerUiPrefab reference on player Prefab.", this);
            }
        }
        else if (PlayerUI.Instance != null && !isMissionBot)
        {
            PlayerUI.Instance.AddEnemyToMiniMap(this, m_name, IsAI);
        }
        else if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.AddMissionBotToMiniMap(this);
        }

        m_immuneTime = timer.GetTime() + 3f + m_immortalityTimeBonus;

        if (IsAI || LocalPlayer != this)
        {
            HPBar.transform.parent.gameObject.SetActive(true);
            ShieldBar.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            HPBar.transform.parent.gameObject.SetActive(false);
            ShieldBar.transform.parent.gameObject.SetActive(false);
        }

        if (!isMissionMode)
            ArenaController.instance.RegisterPlayer(this);
        else
            MissionController.instance.RegisterPlayer(this);

        if (IsAI)
        {
            foreach (var skill in m_skills)
            {
                m_AI_skillsCD.Add(0f);
            }
        }

        m_initialized = true;

        StartCoroutine(ShootingCoroutine());

        //#if UNITY_5_4_OR_NEWER
        // Unity 5.4 has a new scene management. register a method to call CalledOnLevelWasLoaded.
        //UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        //#endif
    }

#if !UNITY_5_4_OR_NEWER
            /// <summary>See CalledOnLevelWasLoaded. Outdated in Unity 5.4.</summary>
            void OnLevelWasLoaded(int level)
            {
                this.CalledOnLevelWasLoaded(level);
            }
#endif

    void CalledOnLevelWasLoaded(int level)
    {
        // check if we are outside the Arena and if it's the case, spawn around the center of the arena in a safe zone
        //if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
        //{
        //transform.position = ArenaController.instance.RandomSpawnPoint;
        //initPos = transform.position;
        //}

        ReconstructUI();
    }

    void OnDestroy()
    {
        if (!isMissionMode)
            ArenaController.instance.UnregisterPlayer(this);
        else
            MissionController.instance.UnregisterPlayer(this);
    }

    void ReconstructUI()
    {
        if (photonView.IsMine && PlayerUI.Instance == null && PlayerUiPrefab != null && !IsAI)
        {
            GameObject _uiGo = Instantiate(this.PlayerUiPrefab);
            _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
        }
    }

    public void OnMissionStarted()
    {
        if (PhotonNetwork.IsMasterClient)
            matchTimer.Start(missionController.MissionTime);

        m_immuneTime = 3f + m_immortalityTimeBonus;

        missionStarted = true;
    }

#if UNITY_5_4_OR_NEWER
    public override void OnDisable()
    {
        // Always call the base to remove callbacks
        base.OnDisable();
        //UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
#endif

    void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();
    }

    void LateUpdate()
    {
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, m_currRoll);
        if (m_spawnPoint != Vector3.zero)
            transform.position = new Vector3(transform.position.x, m_spawnPoint.y, transform.position.z);
    }

    void ApplyUpgrades()
    {
        foreach (var upgrade in m_upgrades)
        {
            int upgradeLevel = Launcher.instance.GetUpgradeLevel(this, upgrade);

            if (upgradeLevel > 0)
            {
                m_maxDurability = Mathf.CeilToInt(m_maxDurability * (1f + upgrade.durabilityBonus * upgradeLevel));
                m_maxField = Mathf.CeilToInt(m_maxField * (1f + upgrade.shieldBonus * upgradeLevel));
                m_maxSpeed = m_maxSpeed * (1f + upgrade.speedBonus * upgradeLevel);

                m_critBonus += upgrade.critChanceBonus * upgradeLevel;
                m_critDamageBonus += upgrade.critDamageBonus * upgradeLevel;

                m_durabilityRegen = m_durabilityRegen * (1f + upgrade.durabilityRegenBonus * upgradeLevel);
                m_fieldRegen = m_fieldRegen * (1f + upgrade.shieldRegenBonus * upgradeLevel);

                m_respawnTimeBonus += upgrade.respawnTimeBonus * upgradeLevel;
                m_immortalityTimeBonus += upgrade.respawnImmortalityTimeBonus * upgradeLevel;

                m_stealthSpeedBonus += upgrade.stealthSpeedBonus * upgradeLevel;
                m_stealthLengthBonus += upgrade.stealthLengthBonus * upgradeLevel;
                m_stealthCritBonus += upgrade.stealthCritChanceBonus * upgradeLevel;

                m_repairBonus += upgrade.repairBonus * upgradeLevel;

                m_upgradesScore += upgradeLevel;
            }
        }

        m_durability = m_maxDurability;
    }

    public void ScaleStats(float modifier)
    {
        m_scaledMaxDurability = Mathf.CeilToInt(m_maxDurability * modifier);
        m_scaledMaxField = Mathf.CeilToInt(m_maxField * modifier);
        m_damageModifier = modifier;

        m_durability = m_scaledMaxDurability;
    }

    public int GetUpgradeNumber(UpgradeData upgrade)
    {
        return m_upgrades.FindIndex(x => x == upgrade);
    }

    public void SendRatingAndUpgrades()
    {
        if (!isMissionMode)
            photonView.RPC("SendRatingAndUpgrades_RPC", RpcTarget.All, Name, Launcher.instance.CurrentRating, m_upgradesScore);
    }

    [PunRPC]
    void SendRatingAndUpgrades_RPC(string playerName, int rating, int upgradesScore)
    {
        ArenaController.instance.SetPlayerRating(playerName, rating);

        PlayerController pc = ArenaController.instance.GetPlayerByName(playerName);

        if (pc)
            PlayerUI.Instance.AddPlayerStatsSlot(pc, rating, upgradesScore);
    }

    public void Spawn()
    {
        m_immuneTime = 3f;
        m_isDied = false;
        m_durability = m_maxDurability;
        m_forceField = 0;
        m_speed = 0f;

        GetComponent<Collider>().enabled = true;
        charController.enabled = true;

        foreach (var flame in EngineFlames)
        {
            flame.Play();
        }

        if (m_stealthTime > 0f)
            EndStealth();

        m_stealthTime = 0f;

        ImmortalityOrb.SetActive(true);

        if (IsAI)
        {
            m_AI_skillsCD.Clear();

            foreach (var skill in m_skills)
            {
                m_AI_skillsCD.Add(0f);
            }
        }

        StartCoroutine(ShootingCoroutine());

        if (IsAI || LocalPlayer != this)
        {
            HPBar.transform.parent.gameObject.SetActive(true);
            ShieldBar.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            HPBar.transform.parent.gameObject.SetActive(false);
            ShieldBar.transform.parent.gameObject.SetActive(false);
        }

        PlayerUI.Instance.OnSpawn();
    }

    public void StartShooting()
    {
        if (isFiring || m_isDied) return;

        isFiring = true;
    }

    public void EndShooting()
    {
        isFiring = false;
    }

    public void UseSkill(SkillData skill)
    {
        if (m_isDied) return;

        if (skill.projectilePrefab)
            LaunchProjectileCustom(skill.projectilePrefab);

        if (skill.stealthLength > 0f)
            BeginStealth(skill.stealthLength);

        if (skill.durabilityBonus > 0f)
        {
            int amount = Mathf.CeilToInt(skill.durabilityBonus * m_maxDurability * (1f + m_repairBonus));
            m_durability = Mathf.Min(m_durability + amount, m_maxDurability);
            SpawnRepairText(amount);
        }

        if (skill.shieldBonus > 0f)
            m_forceField = Mathf.Min(m_forceField + Mathf.CeilToInt(skill.shieldBonus * m_maxField), m_maxField);

        if (skill.speedBonusLength > 0f)
        {
            m_speedBonus = skill.speedBonus;
            m_speedBonusLength = skill.speedBonusLength;
            isNitroActive = true;
        }

        if (skill.sound && audioSource)
        {
            audioSource.PlayOneShot(skill.sound);
        }
    }

    private IEnumerator StealthStartAnim()
    {
        meshRenderer.material = transparentMaterial;
        meshRenderer.material.ToFadeMode();

        float t = 0f;

        Color startColor = meshRenderer.material.color;
        Color endColor = startColor;
        endColor.a = photonView.IsMine ? 0.2f : 0f;

        do
        {
            t += Time.deltaTime * 2f;

            meshRenderer.material.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }
        while (t < 1f);
    }

    private IEnumerator StealthEndAnim()
    {
        float t = 0f;

        Color startColor = meshRenderer.material.color;
        Color endColor = startColor;
        endColor.a = 1f;

        do
        {
            t += Time.deltaTime * 2f;

            meshRenderer.material.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }
        while (t < 1f);

        foreach (var flame in EngineFlames)
        {
            flame.Play();
        }

        meshRenderer.material = initMaterial;
        meshRenderer.material.ToOpaqueMode();
    }

    public void Die()
    {
        if (m_isDied) return;

        m_isDied = true;

        StopAllCoroutines();

        EndShooting();
        meshRenderer.gameObject.SetActive(false);
        GetComponent<Collider>().enabled = false;
        charController.enabled = false;
        NameLabel.gameObject.SetActive(false);
        foreach (var flame in EngineFlames)
        {
            flame.Stop();
        }

        HPBar.transform.parent.gameObject.SetActive(false);
        ShieldBar.transform.parent.gameObject.SetActive(false);

        if (audioSource && deathSound)
            audioSource.PlayOneShot(deathSound);

        if (!isMissionMode)
        {
            m_currRespawnTime = ArenaController.instance.RespawnTime + Mathf.Min(DeathsCount * DeathsCount, m_balance.respawnTimeMax);
            m_currRespawnTime *= (1f - m_respawnTimeBonus);

            Invoke("StartSpawning", m_spectacleTime);

            ArenaController.instance.OnPlayerKilled(m_lastEnemyName, Name);

            if (!LocalPlayer.m_isDied)
                PlayerUI.Instance.DoKillAnnounce(m_lastEnemyName, Name);
        }
        else if (this == LocalPlayer)
        {
            OnLoss(1);
        }

        if (DeathEffect)
        {
            DeathEffect.transform.parent = null;
            DeathEffect.SetActive(true);
        }

        if (this == LocalPlayer)
        {
            PlayerUI.Instance.OnDeath();
        }
    }

    public void StartSpawning()
    {
        Invoke("Spawn", m_currRespawnTime);

        transform.position = m_spawnPoint;

        meshRenderer.gameObject.SetActive(true);
        NameLabel.gameObject.SetActive(true);

        if (this == LocalPlayer)
            StartCoroutine(SpawnAnnouncements());
    }

    private IEnumerator SpawnAnnouncements()
    {
        int t = Mathf.CeilToInt(m_currRespawnTime);

        do
        {
            PlayerUI.Instance.DoRespawnAnnounce(t);

            t--;

            yield return new WaitForSeconds(1f);
        }
        while (t > 0);
    }

    void ExitFromRoom()
    {
        if (!isMissionMode)
        {
            ArenaController.instance.UnregisterPlayer(this);
            ArenaController.instance.LeaveRoom();
        }
        else
        {
            MissionController.instance.UnregisterPlayer(this);
            MissionController.instance.LeaveRoom();
        }
    }

    public void BeginStealth(float length = 5f)
    {
        length += m_stealthLengthBonus;
        photonView.RPC("BeginStealth_RPC", RpcTarget.All, length);
    }

    public void EndStealth()
    {
        photonView.RPC("EndStealth_RPC", RpcTarget.All);
    }

    [PunRPC]
    void BeginStealth_RPC(float length = 5f)
    {
        m_stealthTime = length;

        if (!photonView.IsMine)
            NameLabel.gameObject.SetActive(false);

        foreach (var flame in EngineFlames)
        {
            flame.Stop();
        }

        StartCoroutine(StealthStartAnim());
    }

    [PunRPC]
    void EndStealth_RPC()
    {
        m_stealthTime = 0f;
        NameLabel.gameObject.SetActive(true);
        StartCoroutine(StealthEndAnim());
    }

    void SpawnRepairText(int amount, bool isCrit = false)
    {
        photonView.RPC("SpawnRepairText_RPC", RpcTarget.All, amount, isCrit);
    }

    [PunRPC]
    void SpawnRepairText_RPC(int amount, bool isCrit = false)
    {
        GameObject txt = Instantiate(RepairTextPrefab, FloatingTextSpawnPosition.position, Quaternion.identity);
        txt.GetComponent<FloatingText>().SetText(amount.ToString() + (isCrit ? "!" : ""));
    }

    void SpawnDamageText(int amount, bool isCrit = false)
    {
        photonView.RPC("SpawnDamageText_RPC", RpcTarget.All, amount, isCrit);
    }

    [PunRPC]
    void SpawnDamageText_RPC(int amount, bool isCrit = false)
    {
        GameObject txt = Instantiate(DamageTextPrefab, FloatingTextSpawnPosition.position, Quaternion.identity);
        txt.GetComponent<FloatingText>().SetText(amount.ToString() + (isCrit ? "!" : ""));
    }

    void SpawnInfoText(string info)
    {
        photonView.RPC("SpawnInfoText_RPC", RpcTarget.All, info);
    }

    [PunRPC]
    void SpawnInfoText_RPC(string info)
    {
        GameObject txt = Instantiate(InfoTextPrefab, FloatingTextSpawnPosition.position, Quaternion.identity);
        txt.GetComponent<FloatingText>().SetText(info);
    }

    private IEnumerator ShootingCoroutine()
    {
        while (true)
        {
            while (isFiring)
            {
                LaunchProjectile();

                yield return new WaitForSeconds(IsAI ? 2f * m_shootInternalCD : m_shootInternalCD);
            }

            yield return null;
        }
    }

    public void LaunchProjectile()
    {
        bool fromStealth = false;

        if (m_stealthTime > 0f)
        {
            fromStealth = true;
            EndStealth();
        }

        photonView.RPC("LaunchProjectile_RPC", RpcTarget.All, fromStealth ? m_stealthCritBonus : 0f, m_critDamageBonus);
    }

    [PunRPC]
    void LaunchProjectile_RPC(float critBonus, float critDmgBonus)
    {
        foreach (var shootPos in ShootPositions)
        {
            GameObject proj = PhotonNetwork.Instantiate(ProjectilePrefab.name, shootPos.position, transform.rotation);
            Projectile p = proj.GetComponent<Projectile>();
            p.SetOwner(m_name, photonView.Owner.UserId);
            p.critChance += critBonus;
            p.critDamageModifier += critDmgBonus;
            p.damageMin = Mathf.CeilToInt(p.damageMin * m_damageModifier);
            p.damageMax = Mathf.CeilToInt(p.damageMax * m_damageModifier);
        }

        if (audioSource && shootSound)
            audioSource.PlayOneShot(shootSound, 0.6f);
    }

    public void LaunchProjectileCustom(GameObject projectilePrefab)
    {
        photonView.RPC("LaunchProjectileCustom_RPC", RpcTarget.All, projectilePrefab.name);
    }

    [PunRPC]
    void LaunchProjectileCustom_RPC(string projectilePrefabName)
    {
        foreach (var shootPos in ShootPositions)
        {
            GameObject projObj = PhotonNetwork.Instantiate(projectilePrefabName, shootPos.position, transform.rotation);
            Bomb bomb = projObj.GetComponent<Bomb>();
            Projectile proj = projObj.GetComponent<Projectile>();
            if (bomb)
            {
                projObj.transform.position = BombLaunchPosition.position;
                bomb.SetOwner(m_name, photonView.Owner.UserId);
                bomb.damageMin = Mathf.CeilToInt(bomb.damageMin * m_damageModifier);
                bomb.damageMax = Mathf.CeilToInt(bomb.damageMax * m_damageModifier);
                return;
            }
            else if (proj)
            {
                proj.SetOwner(m_name, photonView.Owner.UserId);
                proj.damageMin = Mathf.CeilToInt(proj.damageMin * m_damageModifier);
                proj.damageMax = Mathf.CeilToInt(proj.damageMax * m_damageModifier);
            }
        }
    }

    public void GetDamage(int amount, int damageToShield, bool ignoreShield = false, bool isCrit = false, float critModifier = 2f, string owner = "")
    {
        int actualDamage = amount;

        if (isCrit)
        {
            actualDamage = Mathf.CeilToInt(actualDamage * critModifier);
        }

        if (damageToShield > 0)
        {
            int damageToField = Mathf.Min((int)m_forceField, damageToShield);

            m_forceField -= damageToField;
        }

        if (!ignoreShield)
        {
            int damageToField = Mathf.Min((int)m_forceField, actualDamage);

            m_forceField -= damageToField;

            actualDamage -= damageToField;
        }

        if (actualDamage > 0)
        {
            m_durability -= actualDamage;

            PlayerController enemy = m_roomPlayers.Find(x => x.Name == owner);

            if (enemy)
            {
                m_lastEnemy = enemy.transform;

                m_lastEnemyName = enemy.Name;
            }

            m_currShieldRegenDelay = m_balance.shieldRegenDelay;

            SpawnDamageText(actualDamage, isCrit);

            if (DurabilityPercent < 0.25f && photonView.IsMine && !IsAI)
            {
                PlayerUI.Instance.PlaySound(PlayerUI.SoundType.LowDurability, 10f);
            }
        }
        else
        {
            SpawnInfoText("Absorbed");
        }

        if (m_stealthTime > 0f)
            EndStealth();
    }

    void CheckVictory()
    {
        if (!m_isWon && PlayerUI.Instance != null &&
            !PlayerUI.Instance.IsLobbyState &&
            m_roomPlayers.Count == 1 &&
            m_roomPlayers[0] == this &&
            DurabilityPercent > 0f)
        {
            OnWin();
        }
    }

    public void EndMatch(bool isVictory = false)
    {
        photonView.RPC("EndMatch_RPC", RpcTarget.All, isVictory);
    }

    [PunRPC]
    void EndMatch_RPC(bool isVictory = false)
    {
        if (IsAI)
        {
            PhotonNetwork.Destroy(gameObject);
        }

        if (!isMissionMode)
        {
            List<PlayerController> sortedPlayers = m_roomPlayers.OrderByDescending(x => x.Score).ToList();

            int place = Mathf.Min(m_balance.maxPlayersPerRoom, sortedPlayers.FindIndex(x => x == this) + 1);

            if (place < m_balance.winnersCount + 1)
                OnWin(place);
            else
                OnLoss(place);
        }
        else
        {
            if (isVictory)
                OnWin(1);
            else
                OnLoss(1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        ReconstructUI();

        //CheckVictory();

        HPBar.fillAmount = DurabilityPercent;
        ShieldBar.fillAmount = FieldPercent;

        if (photonView.IsMine)
        {
            timer.Update(Time.deltaTime);
            matchTimer.Update(Time.deltaTime);

            if (MatchTimer <= 0f && PhotonNetwork.IsMasterClient && !PlayerUI.Instance.IsLobbyState && ((isMissionMode && missionStarted) || !isMissionMode))
            {
                matchTimer.Stop();
                EndMatch();
            }
        }

        NameLabel.transform.position = transform.position + Vector3.forward * (BarsOffset + 3f);
        barsHolder.position = transform.position + Vector3.forward * BarsOffset;

        if (m_immuneTime <= 0f && ImmortalityOrb.activeSelf) ImmortalityOrb.SetActive(false);

        if (photonView.IsMine)
        {
            if (m_stealthTime > 0f)
            {
                m_stealthTime -= Time.deltaTime;

                if (m_stealthTime <= 0f)
                {
                    EndStealth();
                }
            }

            if (m_currShieldRegenDelay > 0f)
            {
                m_currShieldRegenDelay -= Time.deltaTime;

                if (m_currShieldRegenDelay <= 0f)
                {
                    m_currShieldRegenDelay = 0f;
                }
            }
        }

        if (NitroEffect != null)
        {
            if (isNitroActive && m_stealthTime <= 0f && !NitroEffect.isPlaying)
            {
                NitroEffect.Play();
            }

            if ((!isNitroActive || m_stealthTime > 0f) && NitroEffect.isPlaying)
            {
                NitroEffect.Stop();
            }
        }

        if (!photonView.IsMine)
        {
            transform.position = Vector3.MoveTowards(transform.position, networkPos, Time.deltaTime * 100.0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, networkRot, Time.deltaTime * 180.0f);
            return;
        }

        if (m_durability < m_maxDurability && m_durabilityRegen > 0f && m_durability > 0)
        {
            m_durability = Mathf.Min(m_maxDurability, m_durability + m_maxDurability * m_durabilityRegen * Time.deltaTime);
        }

        if (m_forceField < m_maxField && m_fieldRegen > 0f && m_currShieldRegenDelay <= 0f)
        {
            m_forceField = Mathf.Min(m_maxField, m_forceField + m_maxField * m_fieldRegen * Time.deltaTime);
        }

        if (m_immuneTime > 0f) m_immuneTime -= Time.deltaTime;

        if (m_speedBonusLength > 0f)
        {
            m_speedBonusLength -= Time.deltaTime;

            if (m_speedBonusLength <= 0f)
            {
                m_speedBonus = Mathf.Lerp(m_speedBonus, 0f, 6f * Time.deltaTime);
                isNitroActive = false;
            }
        }

        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        if (!IsAI)
        {
            if (!m_isDied)
            {
                if (targetCameraPos != null && m_nexusUsed)
                    cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetCameraPos + Vector3.up * 90f, Time.deltaTime * 8f);
                else
                    cameraTransform.position = Vector3.Lerp(cameraTransform.position, transform.position + Vector3.up * 90f, Time.deltaTime * 8f);
            }
            else if (m_lastEnemy != null && !meshRenderer.gameObject.activeSelf)
                cameraTransform.position = Vector3.Lerp(cameraTransform.position, m_lastEnemy.position + Vector3.up * 90f, Time.deltaTime * 8f);
            else if (m_isDied && meshRenderer.gameObject.activeSelf)
                cameraTransform.position = Vector3.Lerp(cameraTransform.position, transform.position + Vector3.up * 90f, Time.deltaTime * 8f);
        }

#if UNITY_STANDALONE || UNITY_EDITOR
        //movementDir = Vector3.zero;
        Vector3 newDir = Vector3.zero;
        if (!m_isDied && !m_isWon && !IsAI)
        {
            if (Input.GetKey(KeyCode.W))
            {
                newDir += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S))
            {
                newDir += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A))
            {
                newDir += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D))
            {
                newDir += Vector3.right;
            }
            newDir.Normalize();
            movementDir = Vector3.Lerp(movementDir, newDir, 6f * Time.deltaTime);
        }
#else
            if (!m_isDied && !m_isWon && !IsAI)
                movementDir = new Vector3(joystick.GetAxis("Horizontal"), 0f, joystick.GetAxis("Vertical"));
            else
                movementDir = Vector3.zero;
#endif

        if (IsAI && PhotonNetwork.IsMasterClient && !PlayerUI.Instance.IsLobbyState)
        {
            ProccessMovement_AI();
            ProccessShooting_AI();
            ProccessSkills_AI();
        }

        if (!PlayerUI.Instance.IsLobbyState)
            ProccessMovement();

        m_currRoll = Mathf.Lerp(m_currRoll, 0f, 2f * Time.deltaTime);

        m_currRoll -= transform.InverseTransformDirection(movementDir).x * m_rollForce;

        if (m_durability <= 0 && !m_isWon)
        {
            Die();
        }
    }

    void ProccessSkills_AI()
    {
        List<SkillData> selectedSkills = new List<SkillData>();

        if (m_globalCD > 0f) m_globalCD -= Time.deltaTime;

        for (int i = 0; i < m_AI_skillsCD.Count; i++)
        {
            if (m_AI_skillsCD[i] <= 0f)
                selectedSkills.Add(m_skills[i]);
            else
                m_AI_skillsCD[i] -= Time.deltaTime;
        }

        if (isMissionBot)
        {
            if (m_currentAITarget == null || !m_targetIsPlayer) return;
        }

        if (selectedSkills.Count > 0)
        {
            SkillData skillToUse = selectedSkills.GetRandomElement();
            int idx = m_skills.FindIndex(x => x == skillToUse);
            m_AI_skillsCD[idx] = skillToUse.cooldown;
            UseSkill(skillToUse);
            m_globalCD = 5f;
        }
    }

    void ProccessShooting_AI()
    {
        List<PlayerController> playersList = new List<PlayerController>(m_roomPlayers);

        List<PlayerController> selectedPlayers = new List<PlayerController>();

        if (!isMissionBot)
        {
            foreach (var player in playersList)
            {
                if (player != this && player.transform.Distance(transform) < 90f && player.DurabilityPercent > 0f && !player.InStealth)
                    selectedPlayers.Add(player);
            }
        }
        else
        {
            foreach (var player in playersList)
            {
                if (player != this && player.transform.Distance(transform) < 90f && player.DurabilityPercent > 0f && !player.InStealth && !player.IsAI)
                    selectedPlayers.Add(player);
            }
        }

        if (selectedPlayers.Count > 1)
            selectedPlayers.Sort((a, b) => a.transform.Distance(transform).CompareTo(b.transform.Distance(transform)));

        if (selectedPlayers.Count > 0)
        {
            m_currentAITarget = selectedPlayers[0].transform;
            m_currentAIEnemy = selectedPlayers[0];
            m_targetIsPlayer = true;
            movementDir = (m_currentAITarget.position - transform.position).normalized;
            movementDir.y = 0f;
        }

        if (m_currentAIEnemy != null && m_currentAIEnemy.transform.Distance(transform) < 300f && m_currentAIEnemy.transform.Distance(transform) > 10f && !InStealth)
        {
            StartShooting();
        }
        else
        {
            EndShooting();
        }
    }

    void ProccessMovement_AI()
    {
        if (m_currAITargetChangeDelay > 0f)
            m_currAITargetChangeDelay -= Time.deltaTime;

        if (m_targetIsPlayer && (m_currentAIEnemy == null || m_currentAIEnemy != null && (m_currentAIEnemy.DurabilityPercent <= 0f || m_currentAIEnemy.InStealth)))
        {
            m_currentAITarget = null;
            m_currentAIEnemy = null;
            m_targetIsPlayer = false;
        }

        if (m_currentAITarget == null || m_currentAITarget.Distance(transform) > 500f || m_currAITargetChangeDelay <= 0f || (m_currentAIEnemy != null && DurabilityPercent < 0.4f))
        {
            List<PlayerController> playersList = new List<PlayerController>(m_roomPlayers);

            List<Pickup> pickupsList = isMissionMode ? new List<Pickup>(MissionController.instance.RoomPickups) : new List<Pickup>(ArenaController.instance.RoomPickups);

            List<PlayerController> selectedPlayers = new List<PlayerController>();

            if (!isMissionBot)
            {
                foreach (var player in playersList)
                {
                    if (player != this && player.DurabilityPercent > 0f && !player.InStealth && player.transform.Distance(transform) < AIEnemyFindingRadius)
                        selectedPlayers.Add(player);
                }
            }
            else
            {
                foreach (var player in playersList)
                {
                    if (player != this && !player.IsAI && !player.InStealth && player.DurabilityPercent > 0f && player.transform.Distance(transform) < AIEnemyFindingRadius)
                        selectedPlayers.Add(player);
                }
            }

            List<Pickup> selectedPickups = new List<Pickup>();

            foreach (var pickup in pickupsList)
            {
                if (pickup.transform.Distance(transform) < AIPickupsFindingRadius && pickup.IsActive)
                    selectedPickups.Add(pickup);
            }

            if (selectedPlayers.Count > 1)
                selectedPlayers.Sort((a, b) => a.transform.Distance(transform).CompareTo(b.transform.Distance(transform)));

            if (selectedPickups.Count > 1)
                selectedPickups.Sort((a, b) => a.transform.Distance(transform).CompareTo(b.transform.Distance(transform)));

            Pickup currPickup = m_currentAITarget != null ? m_currentAITarget.GetComponent<Pickup>() : null;

            if (currPickup && currPickup.isNexus) currPickup.OnAbandonedByBot();

            m_targetIsNexus = false;

            if (selectedPickups.Count > 0 && !isMissionBot)
            {
                Pickup nexus = selectedPickups.Find(x => x.isNexus == true);

                if (nexus != null)
                {
                    bool nexusInPrio = (DurabilityPercent > 0.5f && nexus.transform.Distance(transform) < AINexusFindingRadius && nexus.BotsTargetedCount < 2) || (nexus.CapturersCount == 1 && DurabilityPercent > 0.35f);

                    if (nexusInPrio)
                    {
                        m_currentAITarget = nexus.transform;
                        m_currentAIEnemy = null;
                        m_targetIsPlayer = false;
                        nexus.OnTargetedByBot();
                        m_targetIsNexus = true;
                    }
                }
            }

            if (!m_targetIsNexus)
            {
                if (DurabilityPercent < AIDurabilityPercentToRetreat && selectedPickups.Count > 0)
                {
                    m_currentAITarget = selectedPickups[0].transform;
                    m_currentAIEnemy = null;
                    m_targetIsPlayer = false;
                }
                else if (selectedPlayers.Count > 0)
                {
                    m_currentAITarget = selectedPlayers[0].transform;
                    m_currentAIEnemy = selectedPlayers[0];
                    m_targetIsPlayer = true;
                }
                else if (selectedPickups.Count > 0)
                {
                    m_currentAITarget = selectedPickups[0].transform;
                    m_currentAIEnemy = null;
                    m_targetIsPlayer = false;
                }
                else
                {
                    m_currentAITarget = null;
                    m_currentAIEnemy = null;
                    m_targetIsPlayer = false;
                }
            }

            if (m_currentAITarget != null)
                m_currAITargetChangeDelay = m_targetIsPlayer ? m_AITargetChangeDelay * 0.5f : m_AITargetChangeDelay;
        }

        if (m_currentAITarget != null)
        {
            movementDir = (m_currentAITarget.position - transform.position).normalized;
            movementDir.y = 0f;
        }
    }

    void ProccessMovement()
    {
        {
            float minDist = 5f;

            if (m_targetIsNexus) minDist = 30f;
            if (m_targetIsPlayer) minDist = 90f;

            if (IsAI && (m_currentAITarget == null || m_currentAITarget.Distance(transform) < minDist))
            {
                m_speed = Mathf.Lerp(m_speed, 0f, 4f * m_acceleration * Time.deltaTime);
            }
            else if (movementDir.magnitude > 0f)
            {
                m_speed = Mathf.Lerp(m_speed, m_maxSpeed * movementDir.magnitude, m_acceleration * Time.deltaTime);
            }
            else if (movementDir.magnitude <= 0f)
            {
                m_speed = Mathf.Lerp(m_speed, 0f, 2f * m_acceleration * Time.deltaTime);
            }

            if (movementDir.magnitude > 0f)
                transform.LookAt(transform.position + movementDir);

            charController.Move(transform.forward * m_speed * (1f + m_speedBonus + (m_stealthTime > 0f ? m_stealthSpeedBonus : 0f)) * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {

    }

    public void OnNexusUsed(string byPlayer, Vector3 pos)
    {
        photonView.RPC("OnNexusUsed_RPC", RpcTarget.All, byPlayer, pos);
    }

    [PunRPC]
    void OnNexusUsed_RPC(string byPlayer, Vector3 pos)
    {
        if (IsAI)
        {
            PhotonNetwork.Destroy(gameObject);
        }

        targetCameraPos = pos;
        m_nexusUsed = true;

        if (!isMissionMode)
        {
            List<PlayerController> sortedPlayers = m_roomPlayers.OrderByDescending(x => x.Score).ToList();

            int place = Mathf.Min(m_balance.maxPlayersPerRoom, sortedPlayers.FindIndex(x => x == this) + 1);

            if (Name == byPlayer)
                OnWin();
            else
                OnLoss(place + 1);
        }
        else
        {
            missionController.IsNexusCaptured = true;
        }
    }


    void OnLoss(int place)
    {
        if (m_isWon || m_isLoss || !photonView.IsMine || PlayerUI.Instance.ResultsScreenShown) return;
        m_isLoss = true;

        if (!isMissionMode)
        {
            int oldRating = Launcher.instance.CurrentRating;
            int moneyGained = Launcher.instance.OnFightLoss();
            int newRating = Launcher.instance.CurrentRating;

            PlayerUI.Instance.OnLoss(oldRating, newRating - oldRating, place, moneyGained);
        }
        else
        {
            Launcher.instance.OnMissionFailed();
            PlayerUI.Instance.OnMissionFailed();
        }

        Invoke("ExitFromRoom", 5f);
    }

    void OnWin(int place = 1)
    {
        if (m_isWon || m_isLoss || !photonView.IsMine || PlayerUI.Instance.ResultsScreenShown) return;
        m_isWon = true;

        if (!isMissionMode)
        {
            int oldRating = Launcher.instance.CurrentRating;
            int moneyGained = Launcher.instance.OnFightWon(place);
            int newRating = Launcher.instance.CurrentRating;

            PlayerUI.Instance.OnWin(oldRating, newRating - oldRating, place, moneyGained);
        }
        else
        {
            int moneyGained = Launcher.instance.OnMissionCompleted(MatchTimer);
            PlayerUI.Instance.OnMissionCompleted(moneyGained);
        }

        Invoke("ExitFromRoom", 5f);
    }

    public void SetJoystick(VirtualJoystick _joystick)
    {
        joystick = _joystick;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this player: send the others our data
            //stream.SendNext(isFiring);
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(m_durability);
            stream.SendNext(m_forceField);
            stream.SendNext(m_immuneTime);
            stream.SendNext(m_stealthTime);
            stream.SendNext(isNitroActive);
            stream.SendNext(m_killsCount);
            stream.SendNext(m_deathsCount);
        }
        else
        {
            // Network player, receive data
            //this.isFiring = (bool)stream.ReceiveNext();
            networkPos = (Vector3)stream.ReceiveNext();
            networkRot = (Quaternion)stream.ReceiveNext();

            this.m_durability = (float)stream.ReceiveNext();
            this.m_forceField = (float)stream.ReceiveNext();
            this.m_immuneTime = (float)stream.ReceiveNext();
            this.m_stealthTime = (float)stream.ReceiveNext();
            this.isNitroActive = (bool)stream.ReceiveNext();
            this.m_killsCount = (int)stream.ReceiveNext();
            this.m_deathsCount = (int)stream.ReceiveNext();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (other.CompareTag("Obstacle"))
        {
            m_speed = 0f;
        }
        else if (other.CompareTag("Projectile") && m_immuneTime <= 0f && !m_isWon)
        {
            Projectile proj = other.GetComponent<Projectile>();

            PlayerController owner = isMissionMode ? missionController.GetPlayerByName(proj.ownerName) : arenaController.GetPlayerByName(proj.ownerName);

            if (isMissionMode && owner.IsAI && IsAI) return;

            if (proj.ownerName != m_name)
            {
                int actualDamage = Random.Range(proj.damageMin, proj.damageMax + 1);

                bool isCrit = Random.value <= proj.critChance;

                GetDamage(actualDamage, 0, proj.ignoreField, isCrit, proj.critDamageModifier, proj.ownerName);

                proj.Explode();
            }
        }
        else if (other.CompareTag("Bomb") && m_immuneTime <= 0f && !m_isWon)
        {
            Bomb bomb = other.GetComponent<Bomb>();

            PlayerController owner = isMissionMode ? missionController.GetPlayerByName(bomb.ownerName) : arenaController.GetPlayerByName(bomb.ownerName);

            if (isMissionMode && owner.IsAI && IsAI) return;

            if (bomb.IsActive)
            {
                int actualDamage = Random.Range(bomb.damageMin, bomb.damageMax + 1);

                bool isCrit = Random.value <= bomb.critChance;

                GetDamage(actualDamage, bomb.damageToShield, bomb.ignoreField, isCrit, bomb.critDamageModifier, bomb.ownerName);

                bomb.Explode();
            }
        }
        else if (other.CompareTag("Pickup"))
        {
            Pickup pickup = other.GetComponent<Pickup>();

            if (pickup.isNexus) return;

            if (pickup.durabilityBonus > 0f)
            {
                int amount = Mathf.CeilToInt(pickup.durabilityBonus * m_maxDurability);
                float oldValue = m_durability;
                m_durability = Mathf.Min(m_durability + amount, m_maxDurability);
                if (m_durability - oldValue > 0)
                {
                    SpawnRepairText(amount);
                    pickup.OnPickupUsed();
                }
            }

            if (pickup.shieldBonus > 0f)
            {
                float oldValue = m_forceField;
                m_forceField = Mathf.Min(m_forceField + Mathf.CeilToInt(pickup.shieldBonus * m_maxField), m_maxField);
                if (m_forceField - oldValue > 0)
                {
                    pickup.OnPickupUsed();
                }
            }

            if (pickup.speedBonusLength > 0f)
            {
                m_speedBonus = pickup.speedBonus;
                m_speedBonusLength = pickup.speedBonusLength;
                isNitroActive = true;
                pickup.OnPickupUsed();
            }
        }
    }
}