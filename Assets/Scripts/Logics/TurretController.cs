using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretController : MonoBehaviourPunCallbacks
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
    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;

    private PlayerController currentTarget;
    private float currHeating = 0f;
    private float timeAfterPrevShot = 0f;
    private bool isCooling = false;
    private bool isReadyToShoot = false;
    private bool isMissionMode = false;
    private AudioSource audioSource;

    private RoomObjectPool projectilesPool;
    private Quaternion networkRot;
    //Lag compensation
    private float currentTime = 0;
    private double currentPacketTime = 0;
    private double lastPacketTime = 0;
    private Quaternion rotationAtLastPacket = Quaternion.identity;

    private Vector3 targetPos;
    private Quaternion targetRot;

    private List<PlayerController> m_roomPlayers;


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
            stream.SendNext(headTransform.rotation);
        }
        else
        {
            //Lag compensation
            currentTime = 0.0f;
            lastPacketTime = currentPacketTime;
            currentPacketTime = info.SentServerTime;
            rotationAtLastPacket = transform.rotation;

            this.isCooling = (bool)stream.ReceiveNext();
            this.currHeating = (float)stream.ReceiveNext();
            this.networkRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
