using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pickup : MonoBehaviourPunCallbacks
{
    public float durabilityBonus = 0f;
    public float shieldBonus = 0f;
    public float speedBonus = 0f;
    public float speedBonusLength = 0f;
    public bool isNexus = false;
    public float respawnTime = 30f;
    [SerializeField] private GameObject effectsHolder;
    [SerializeField] private Image capturingBar;
    [SerializeField] private ParticleSystem captureEffect;

    private float timeToRespawn = 0f;
    private float currCapturingTime = 0f;
    private List<string> currCapturerNames = new List<string>();
    private int botsTargetedCount = 0;
    private int capturersCount = 0;
    private bool capturingAnnounceDone = false;

    private MissionController missionController;
    private ArenaController arenaController;
    private bool isMissionMode = false;

    public bool IsActive => m_collider.enabled;

    public int BotsTargetedCount => botsTargetedCount;
    public int CapturersCount => capturersCount;

    private Collider m_collider;
    private BalanceInfo m_balance;
    private List<PlayerController> m_roomPlayers;

    private void Awake()
    {
        m_collider = GetComponent<Collider>();
    }

    private void Start()
    {
        m_balance = Launcher.instance.Balance;

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

    void Activate()
    {
        photonView.RPC("Activate_RPC", RpcTarget.All);
    }

    [PunRPC]
    void Activate_RPC()
    {
        m_collider.enabled = true;
        effectsHolder.SetActive(true);
    }

    void Deactivate()
    {
        photonView.RPC("Deactivate_RPC", RpcTarget.All);
    }

    [PunRPC]
    void Deactivate_RPC()
    {
        m_collider.enabled = false;
        effectsHolder.SetActive(false);
    }

    public void OnPickupUsed()
    {
        Deactivate();
        if (!isNexus)
            timeToRespawn = respawnTime;
        if (captureEffect && !captureEffect.isPlaying && effectsHolder.activeSelf)
            captureEffect.Play();
    }

    public void OnTargetedByBot()
    {
        photonView.RPC("OnTargetedByBot_RPC", RpcTarget.All);
    }

    public void OnAbandonedByBot()
    {
        photonView.RPC("OnAbandonedByBot_RPC", RpcTarget.All);
    }

    [PunRPC]
    void OnTargetedByBot_RPC()
    {
        botsTargetedCount++;
    }

    [PunRPC]
    void OnAbandonedByBot_RPC()
    {
        botsTargetedCount--;
    }

    [PunRPC]
    void AddCapturer_RPC(string capturerName)
    {
        if (!currCapturerNames.Contains(capturerName))
        {
            capturersCount++;
            currCapturerNames.Add(capturerName);
            currCapturingTime = 0f;
            if (capturersCount == 1)
            {
                if (!PlayerController.LocalPlayer.IsDied)
                    PlayerUI.Instance.DoCaptureAnnounce(capturerName);
                capturingAnnounceDone = true;
            }
        }
    }

    [PunRPC]
    void RemoveCapturer_RPC(string capturerName)
    {
        if (currCapturerNames.Contains(capturerName))
        {
            capturersCount--;
            currCapturerNames.Remove(capturerName);

            if (capturersCount < 1)
                currCapturingTime = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isNexus)
        {
            PlayerController pc = other.GetComponent<PlayerController>();

            photonView.RPC("AddCapturer_RPC", RpcTarget.All, pc.Name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isNexus)
        {
            PlayerController pc = other.GetComponent<PlayerController>();

            photonView.RPC("RemoveCapturer_RPC", RpcTarget.All, pc.Name);
        }
    }

    void CheckDeadCapturers()
    {
        foreach (var p in m_roomPlayers)
        {
            if (p.DurabilityPercent <= 0f)
            {
                photonView.RPC("RemoveCapturer_RPC", RpcTarget.All, p.Name);
            }
        }

        foreach (var c in currCapturerNames.ToArray())
        {
            PlayerController pc = m_roomPlayers.Find(x => x.Name == c);

            if (pc == null)
            {
                photonView.RPC("RemoveCapturer_RPC", RpcTarget.All, c);
            }
        }
    }

    private void Update()
    {
        if (capturingBar)
        {
            capturingBar.fillAmount = currCapturingTime / m_balance.nexusCaptureTime;

            if (PhotonNetwork.IsMasterClient)
            {
                CheckDeadCapturers();

                if (capturersCount == 1)
                {
                    currCapturingTime += Time.deltaTime;

                    if (!capturingAnnounceDone)
                    {
                        if (!PlayerController.LocalPlayer.IsDied)
                            PlayerUI.Instance.DoCaptureAnnounce(currCapturerNames[0]);
                        capturingAnnounceDone = true;
                    }
                }
                else
                {
                    capturingAnnounceDone = false;

                    if (currCapturingTime > 0f)
                    {
                        currCapturingTime -= 3f * Time.deltaTime;
                        currCapturingTime = Mathf.Max(0f, currCapturingTime);
                    }
                }

                if (currCapturingTime >= m_balance.nexusCaptureTime && capturersCount == 1)
                {
                    PlayerController.LocalPlayer.OnNexusUsed(currCapturerNames[0], transform.position);
                    if (captureEffect) captureEffect.Play();
                    Deactivate();
                    return;
                }
            }
        }

        if (timeToRespawn > 0f && PhotonNetwork.IsMasterClient)
        {
            timeToRespawn -= Time.deltaTime;

            if (timeToRespawn <= 0f)
            {
                Activate();
            }
        }
    }
}
