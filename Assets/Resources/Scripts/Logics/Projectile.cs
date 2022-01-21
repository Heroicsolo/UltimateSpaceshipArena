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
    [HideInInspector]
    public string ownerID = "";
    [HideInInspector]
    public string ownerName = "";

    private float timeToDeath = 3f;

    [SerializeField] private ParticleSystem explodeEffect;

    // Start is called before the first frame update
    void Start()
    {
        timeToDeath = lifeTime;
    }

    // Update is called once per frame
    void Update()
    {
        if( ownerID == Launcher.instance.UserID )
        {
            timeToDeath -= Time.deltaTime;

            if( timeToDeath <= 0f )
            {
                Explode();
            }
        }

        transform.Translate(transform.forward * speed * Time.deltaTime, Space.World);
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
        if( explodeEffect )
        {
            explodeEffect.transform.parent = null;
            explodeEffect.Play();
        }
        Destroy(gameObject);
    }
}
