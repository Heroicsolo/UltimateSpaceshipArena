using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Projectile : SyncTransform
{
    public bool poolable = false;
    public bool turretProjectile = false;
    public int damageMin = 2;
    public int damageMax = 3;
    public float critChance = 0.05f;
    public float critDamageModifier = 2f;
    public float speed = 10f;
    public float lifeTime = 3f;
    public bool ignoreField = false;
    public bool isGuiding = false;
    public bool isDynamicGuiding = false;
    public bool isSingleGuiding = false;
    public float dynamicGuidingInterval = 0.5f;
    [Range(0f, 1f)]
    public float guidingForce = 0.4f;
    [Min(0f)]
    public float guidingSpeed = 4f;
    [Range(0f, 180f)]
    public float guidingAngle = 30f;
    public float guidingMaxDist = 200f;
    [HideInInspector]
    public string ownerID = "";
    [HideInInspector]
    public string ownerName = "";
    [HideInInspector]
    public Transform target;

    private bool targetSelected = false;
    private float timeToDeath = 3f;
    private float timeToChangeTarget = 0f;

    private MissionController missionController;
    private ArenaController arenaController;
    private bool isMissionMode = false;
    private List<PlayerController> m_roomPlayers;
    private List<TurretController> m_roomTurrets;
    private Quaternion m_targetRot;

    [SerializeField] private ParticleSystem explodeEffect;

    // Start is called before the first frame update
    void Start()
    {
        timeToDeath = lifeTime;

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
        m_roomTurrets = isMissionMode ? missionController.RoomTurrets : arenaController.RoomTurrets;
    }

    public override void OnEnable()
    {
        timeToDeath = lifeTime;

        target = null;
        targetSelected = false;
        timeToChangeTarget = 0f;

        if (explodeEffect)
        {
            explodeEffect.transform.parent = transform;
        }
    }

    void FindNewGuidingTarget()
    {
        List<PlayerController> selectedPlayers = new List<PlayerController>();

        List<TurretController> selectedTurrets = new List<TurretController>();

        foreach (var player in m_roomPlayers)
        {
            if (player.Name != ownerName && player.transform.Distance(transform) < guidingMaxDist && player.DurabilityPercent > 0f && !player.InStealth)
                selectedPlayers.Add(player);
        }

        if (selectedPlayers.Count > 0)
        {
            if (selectedPlayers.Count > 1)
                selectedPlayers.Sort((a, b) => a.transform.Distance(transform).CompareTo(b.transform.Distance(transform)));

            target = selectedPlayers[0].transform;

            timeToChangeTarget = dynamicGuidingInterval;

            targetSelected = true;
        }

        foreach (var turret in m_roomTurrets)
        {
            if (turret != null && turret.transform.Distance(transform) < guidingMaxDist && turret.DurabilityPercent > 0f)
                selectedTurrets.Add(turret);
        }

        if (selectedTurrets.Count > 0)
        {
            if (selectedTurrets.Count > 1)
                selectedTurrets.Sort((a, b) => a.transform.Distance(transform).CompareTo(b.transform.Distance(transform)));

            if (selectedPlayers.Count < 1 || selectedPlayers[0] == null || (selectedTurrets[0].transform.Distance(transform) < selectedPlayers[0].transform.Distance(transform)))
            {
                target = selectedTurrets[0].transform;

                timeToChangeTarget = dynamicGuidingInterval;

                targetSelected = true;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            timeToDeath -= Time.deltaTime;

            if (timeToChangeTarget > 0f)
                timeToChangeTarget -= Time.deltaTime;

            if (timeToDeath <= 0f)
            {
                Explode();
            }

            if (isGuiding && guidingForce > 0f && (!isSingleGuiding || (isSingleGuiding && !targetSelected)))
            {
                if (timeToChangeTarget <= 0f || target == null)
                {
                    if (isDynamicGuiding)
                        FindNewGuidingTarget();
                    else if (!isDynamicGuiding && target == null && !targetSelected)
                        FindNewGuidingTarget();
                }
            }

            if (isGuiding && guidingForce > 0f && target != null && target.Distance(transform) < guidingMaxDist)
            {
                Vector3 dir = (target.position - transform.position).normalized;

                Vector3 localDir = transform.InverseTransformDirection(dir);

                if (localDir.z > 0f && Mathf.Atan2(localDir.z, localDir.x) < guidingAngle)
                {
                    Vector3 guidedDir = dir * guidingForce + transform.forward * (1f - guidingForce);
                    m_targetRot = Quaternion.LookRotation(guidedDir, Vector3.up);
                    transform.rotation = Quaternion.Lerp(transform.rotation, m_targetRot, guidingSpeed * Time.deltaTime);
                }
                else
                {
                    target = null;
                }
            }
            else
            {
                target = null;
            }

            transform.Translate(transform.forward * speed * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (other.CompareTag("Obstacle"))
        {
            Explode();
        }
        else if (other.CompareTag("ForceField"))
        {
            PlayerController pc = other.transform.parent.GetComponent<PlayerController>();

            if (pc && pc.Name == ownerName) return;

            Explode();
        }
        else if (other.CompareTag("Turret") && !turretProjectile)
        {
            Explode();
        }
    }

    public void SetOwner(string _ownerName, string _ownerID)
    {
        this.ownerID = _ownerID;
        this.ownerName = _ownerName;
        photonView.RPC("SetOwner_RPC", RpcTarget.All, _ownerName, _ownerID);
    }

    [PunRPC]
    void SetOwner_RPC(string _ownerName, string _ownerID)
    {
        this.ownerID = _ownerID;
        this.ownerName = _ownerName;
    }

    public void Explode()
    {
        photonView.RPC("DestroyOnNetwork", RpcTarget.All);
    }

    [PunRPC]
    public void DestroyOnNetwork()
    {
        if (explodeEffect)
        {
            explodeEffect.transform.parent = null;
            explodeEffect.Play();
        }

        timeToDeath = lifeTime;
        target = null;
        targetSelected = false;
        timeToChangeTarget = 0f;

        if (!poolable)
            PhotonNetwork.Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
