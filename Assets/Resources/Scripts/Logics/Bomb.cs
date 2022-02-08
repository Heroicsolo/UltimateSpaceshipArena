using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Bomb : MonoBehaviourPunCallbacks
{
    public int damageMin = 20;
    public int damageMax = 22;
    public int damageToShield = 30;
    public float critChance = 0.05f;
    public float critDamageModifier = 2f;
    public float speed = 10f;
    public float lifeTime = 0f;
    public float activationTime = 2f;
    public float explosionRadius = 20f;
    public bool ignoreField = true;
    [HideInInspector]
    public string ownerID = "";
    [HideInInspector]
    public string ownerName = "";

    private float timeToDeath = 3f;
    private float timeToEnable = 2f;
    private bool isActivated = false;

    private MissionController missionController;
    private ArenaController arenaController;
    private bool isMissionMode = false;
    private List<PlayerController> m_roomPlayers;

    [SerializeField] private GameObject explosionObject;
    [SerializeField] private GameObject activationObject;

    public bool IsActive => isActivated;

    // Start is called before the first frame update
    void Start()
    {
        timeToDeath = lifeTime;
        timeToEnable = activationTime;
        isActivated = false;

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
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            if (timeToDeath > 0f && timeToEnable <= 0f)
            {
                timeToDeath -= Time.deltaTime;

                if (timeToDeath <= 0f)
                {
                    Explode();
                }
            }

            if (timeToEnable > 0f)
            {
                timeToEnable -= Time.deltaTime;

                if (timeToEnable <= 0f)
                {
                    Activate();
                }
            }
            else if (!isActivated)
            {
                Activate();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ForceField") || other.CompareTag("Obstacle") || other.CompareTag("Projectile") || other.CompareTag("Bomb"))
        {
            Explode();
        }
    }

    public void SetOwner(string ownerName, string ownerID)
    {
        photonView.RPC("SetOwner_RPC", RpcTarget.All, ownerName, ownerID);
    }

    [PunRPC]
    void SetOwner_RPC(string ownerName, string ownerID)
    {
        this.ownerID = ownerID;
        this.ownerName = ownerName;
    }

    public void Explode()
    {
        photonView.RPC("DestroyOnNetwork", RpcTarget.All);
    }

    [PunRPC]
    public void DestroyOnNetwork()
    {
        if (explosionObject)
        {
            explosionObject.transform.parent = null;
            explosionObject.SetActive(true);
            Destroy(explosionObject, 2f);
        }

        if (m_roomPlayers != null && m_roomPlayers.Count > 0)
        {
            foreach (var p in m_roomPlayers)
            {
                if (p.transform.Distance(transform) < explosionRadius)
                {
                    bool isCrit = Random.value <= critChance;

                    p.GetDamage(Random.Range(damageMin, damageMax + 1), damageToShield, ignoreField, isCrit, critDamageModifier, ownerName);
                }
            }
        }

        PhotonNetwork.Destroy(gameObject);
    }

    public void Activate()
    {
        photonView.RPC("ActivateOnNetwork", RpcTarget.All);
    }

    [PunRPC]
    public void ActivateOnNetwork()
    {
        if (activationObject)
        {
            activationObject.SetActive(true);
        }
        isActivated = true;
    }
}
