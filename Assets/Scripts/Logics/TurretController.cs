using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurretController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Shooting Params")]
    [SerializeField] float angularSpeed = 90f;
    [SerializeField] float fireRate = 6f;
    [SerializeField] float heatingPerShot = 0.05f;
    [SerializeField] float coolingDelay = 1f;
    [SerializeField] float coolingPerSecond = 0.2f;
    [SerializeField] float attackDistance = 120f;
    [SerializeField] float attackStartAngle = 30f;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] List<Transform> shootPositions;
    [SerializeField] Transform headTransform;
    [SerializeField] MeshRenderer heatingIndicator;
    [Header("Base Params")]
    [SerializeField] bool immortal = false;
    [SerializeField] int durability = 300;
    [SerializeField] int shield = 100;
    [SerializeField] private float m_shieldRegen = 0f;
    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [Header("UI/FX")]
    [SerializeField] private Image HPBar;
    [SerializeField] private Image ShieldBar;
    [SerializeField] private GameObject DamageTextPrefab;
    [SerializeField] private Transform FloatingTextSpawnPosition;
    [SerializeField] private GameObject InfoTextPrefab;
    [SerializeField] private GameObject DeathEffect;

    private PlayerController currentTarget;
    private float currHeating = 0f;
    private float timeAfterPrevShot = 0f;
    private float currentDurability = 300f;
    private float currentShield = 0f;
    private float m_currShieldRegenDelay = 0f;
    private bool isCooling = false;
    private bool isReadyToShoot = false;
    private bool isMissionMode = false;
    private string lastEnemyName;
    private AudioSource audioSource;

    private RoomObjectPool projectilesPool;
    private Quaternion networkRot = Quaternion.identity;
    //Lag compensation
    private float currentTime = 0;
    private double currentPacketTime = 0;
    private double lastPacketTime = 0;
    private Quaternion rotationAtLastPacket = Quaternion.identity;

    private Vector3 targetPos;
    private Quaternion targetRot = Quaternion.identity;

    private List<PlayerController> m_roomPlayers;

    public float DurabilityPercent => currentDurability / durability;


    void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();
    }

    // Start is called before the first frame update
    void Start()
    {
        currHeating = 0f;

        projectilesPool = new RoomObjectPool();
        projectilesPool.prefabName = projectilePrefab.name;

        if (immortal)
        {
            HPBar.transform.parent.parent.gameObject.SetActive(false);
        }

        if (ArenaController.instance != null)
        {
            isMissionMode = false;
        }
        else if (MissionController.instance != null)
        {
            isMissionMode = true;
        }

        m_roomPlayers = isMissionMode ? MissionController.instance.RoomPlayers : ArenaController.instance.RoomPlayers;

        StartCoroutine(ShootingCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        HPBar.fillAmount = currentDurability / durability;
        ShieldBar.fillAmount = currentShield / shield;

        RefreshHeatingIndicator();

        if (!PhotonNetwork.IsMasterClient)
        {
            //Lag compensation
            double timeToReachGoal = currentPacketTime - lastPacketTime;
            currentTime += Time.deltaTime;

            transform.rotation = Quaternion.Lerp(rotationAtLastPacket, networkRot, (float)(currentTime / timeToReachGoal));

            return;
        }

        FindTarget();

        if (m_currShieldRegenDelay > 0f)
        {
            m_currShieldRegenDelay -= Time.deltaTime;

            if (m_currShieldRegenDelay <= 0f)
            {
                m_currShieldRegenDelay = 0f;
            }
        }

        if (currentShield < shield && m_shieldRegen > 0f && m_currShieldRegenDelay <= 0f)
        {
            currentShield = Mathf.Min(shield, currentShield + shield * m_shieldRegen * Time.deltaTime);
        }

        if (currentTarget != null)
        {
            targetPos = currentTarget.transform.position;
            targetPos.y = headTransform.position.y;
            targetRot = Quaternion.LookRotation(targetPos - headTransform.position, Vector3.up);
            headTransform.rotation = Quaternion.RotateTowards(headTransform.rotation, targetRot, angularSpeed * Time.deltaTime);

            Vector3 dir = (targetPos - headTransform.position).normalized;
            Vector3 localDir = headTransform.InverseTransformDirection(dir);

            if (localDir.z > 0f && Mathf.Atan2(localDir.z, localDir.x) < attackStartAngle)
            {
                isReadyToShoot = true;
            }
            else
            {
                isReadyToShoot = false;
            }
        }

        timeAfterPrevShot += Time.deltaTime;

        if (isCooling && currHeating > 0f)
        {
            currHeating -= coolingPerSecond * Time.deltaTime;

            if (currHeating <= 0f)
            {
                currHeating = 0f;
                isCooling = false;
            }
        }
        else if (timeAfterPrevShot >= coolingDelay)
        {
            currHeating -= coolingPerSecond * Time.deltaTime;

            if (currHeating <= 0f)
            {
                currHeating = 0f;
            }
        }
    }

    private void RefreshHeatingIndicator()
    {
        heatingIndicator.material.color = Color.Lerp(Color.green, Color.red, currHeating);
    }

    private bool CheckLineOfSight(Transform target)
    {
        RaycastHit[] hits = new RaycastHit[2];

        Physics.RaycastNonAlloc(transform.position, target.position - transform.position, hits, 500f, 1 << 6);

        return hits[0].transform == target || hits[0].transform == null;
    }

    private void FindTarget()
    {
        List<PlayerController> playersList = new List<PlayerController>(m_roomPlayers);

        List<PlayerController> selectedPlayers = new List<PlayerController>();

        foreach (var player in playersList)
        {
            if (player.transform.Distance(transform) < attackDistance && player.DurabilityPercent > 0f && !player.InStealth && CheckLineOfSight(player.transform))
                selectedPlayers.Add(player);
        }

        if (selectedPlayers.Count > 1)
            selectedPlayers.Sort((a, b) => a.transform.Distance(transform).CompareTo(b.transform.Distance(transform)));

        if (selectedPlayers.Count > 0)
        {
            currentTarget = selectedPlayers[0];
        }
        else
        {
            currentTarget = null;
            isReadyToShoot = false;
        }
    }

    private IEnumerator ShootingCoroutine()
    {
        while (true)
        {
            while (PhotonNetwork.IsMasterClient && !isCooling && currentTarget != null && isReadyToShoot)
            {
                LaunchProjectile();

                yield return new WaitForSeconds(1f / fireRate);
            }

            yield return null;
        }
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

    public void GetDamage(int amount, int damageToShield, bool ignoreShield = false, bool isCrit = false, float critModifier = 2f, string owner = "")
    {
        if (immortal) return;

        int actualDamage = amount;

        if (isCrit)
        {
            actualDamage = Mathf.CeilToInt(actualDamage * critModifier);
        }

        if (damageToShield > 0)
        {
            int damageToField = Mathf.Min((int)currentShield, damageToShield);

            currentShield -= damageToField;
        }

        if (!ignoreShield)
        {
            int damageToField = Mathf.Min((int)currentShield, actualDamage);

            currentShield -= damageToField;

            actualDamage -= damageToField;
        }

        if (actualDamage > 0)
        {
            currentDurability -= actualDamage;

            SpawnDamageText(actualDamage, isCrit);

            lastEnemyName = owner;
        }
        else
        {
            SpawnInfoText("Absorbed");
        }

        m_currShieldRegenDelay = BalanceProvider.Balance.shieldRegenDelay;

        if (currentDurability <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        photonView.RPC("DestroyOnNetwork", RpcTarget.All);
    }

    [PunRPC]
    public void DestroyOnNetwork()
    {
        if (DeathEffect)
        {
            DeathEffect.transform.parent = null;
            DeathEffect.SetActive(true);
        }

        PlayerUI.Instance.DoKillAnnounce(lastEnemyName, "Turret");

        PhotonNetwork.Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Projectile"))
        {
            Projectile proj = other.GetComponent<Projectile>();

            if (!proj.turretProjectile)
            {
                int actualDamage = Random.Range(proj.damageMin, proj.damageMax + 1);

                bool isCrit = Random.value <= proj.critChance;

                GetDamage(actualDamage, 0, proj.ignoreField, isCrit, proj.critDamageModifier, proj.ownerName);

                proj.Explode();
            }
        }
    }

    public void LaunchProjectile()
    {
        int launchPosIdx = Random.Range(0, shootPositions.Count);
        photonView.RPC("LaunchTurretProjectile_RPC", RpcTarget.All, launchPosIdx);
        currHeating += heatingPerShot;
        if (currHeating >= 1f)
        {
            isCooling = true;
        }
        timeAfterPrevShot = 0f;
    }

    [PunRPC]
    void LaunchTurretProjectile_RPC(int launchPosIdx)
    {
        GameObject proj = projectilesPool.Spawn(shootPositions[launchPosIdx].position, headTransform.rotation);
        Projectile p = proj.GetComponent<Projectile>();
        p.SetOwner("", "Turret");

        if (audioSource && shootSound)
            audioSource.PlayOneShot(shootSound, 0.6f);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isCooling);
            stream.SendNext(currHeating);
            stream.SendNext(currentDurability);
            stream.SendNext(headTransform.rotation);
        }
        else
        {
            this.isCooling = (bool)stream.ReceiveNext();
            this.currHeating = (float)stream.ReceiveNext();
            this.currentDurability = (int)stream.ReceiveNext();
            this.networkRot = (Quaternion)stream.ReceiveNext();

            //Lag compensation
            currentTime = 0.0f;
            lastPacketTime = currentPacketTime;
            currentPacketTime = info.SentServerTime;
            rotationAtLastPacket = transform.rotation;
        }
    }
}
