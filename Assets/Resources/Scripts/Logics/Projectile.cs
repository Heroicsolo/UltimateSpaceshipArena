using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Projectile : MonoBehaviourPunCallbacks
{
    public int damage = 2;
    public float critChance = 0.05f;
    public float critDamageModifier = 2f;
    public float speed = 10f;
    public float lifeTime = 3f;
    public bool ignoreField = false;
    public bool isGuiding = false;
    public bool isDynamicGuiding = false;
    public float dynamicGuidingInterval = 0.5f;
    [Range(0f, 1f)]
    public float guidingForce = 0.4f;
    public float guidingMaxDist = 200f;
    [HideInInspector]
    public string ownerID = "";
    [HideInInspector]
    public string ownerName = "";
    [HideInInspector]
    public Transform target;

    private float timeToDeath = 3f;
    private float timeToChangeTarget = 0f;

    [SerializeField] private ParticleSystem explodeEffect;

    // Start is called before the first frame update
    void Start()
    {
        timeToDeath = lifeTime;
    }

    void FindNewGuidingTarget()
    {
        List<PlayerController> players = ArenaController.instance.RoomPlayers;

        List<PlayerController> selectedPlayers = new List<PlayerController>();

        foreach (var player in players)
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
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            timeToDeath -= Time.deltaTime;

            if (timeToChangeTarget > 0f)
                timeToChangeTarget -= Time.deltaTime;

            if (timeToDeath <= 0f)
            {
                Explode();
            }

            if (isGuiding && isDynamicGuiding && guidingForce > 0f && (timeToChangeTarget <= 0f || target == null))
            {
                FindNewGuidingTarget();
            }

            if (isGuiding && guidingForce > 0f && target != null && target.Distance(transform) < guidingMaxDist)
            {
                Vector3 dir = (target.position - transform.position).normalized;
                Vector3 guidedDir = dir * guidingForce + transform.forward * (1f - guidingForce);
                transform.Translate(guidedDir * speed * Time.deltaTime, Space.World);
            }
            else
            {
                transform.Translate(transform.forward * speed * Time.deltaTime, Space.World);
                target = null;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ForceField") || other.CompareTag("Obstacle"))
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
        if (explodeEffect)
        {
            explodeEffect.transform.parent = null;
            explodeEffect.Play();
        }
        Destroy(gameObject);
    }
}
