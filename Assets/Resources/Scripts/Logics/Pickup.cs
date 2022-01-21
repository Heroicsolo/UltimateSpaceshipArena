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
    public float captureTime = 10f;
    [SerializeField] private GameObject effectsHolder;
    [SerializeField] private Image capturingBar;

    private float timeToRespawn = 0f;
    private float currCapturingTime = 0f;
    private List<string> currCapturerNames = new List<string>();
    private int botsTargetedCount = 0;
    private int capturersCount = 0;

    public bool IsActive => m_collider.enabled;

    public int BotsTargetedCount => botsTargetedCount;

    private Collider m_collider;

    private void Awake()
    {
        m_collider = GetComponent<Collider>();
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
            PlayerUI.Instance.DoCaptureAnnounce(capturerName);
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
        foreach (var p in ArenaController.instance.RoomPlayers)
        {
            if (p.DurabilityPercent <= 0f)
            {
                photonView.RPC("RemoveCapturer_RPC", RpcTarget.All, p.Name);
            }
        }

        foreach (var c in currCapturerNames)
        {
            PlayerController pc = ArenaController.instance.RoomPlayers.Find(x => x.Name == c);

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
            capturingBar.fillAmount = currCapturingTime / captureTime;

            if (PhotonNetwork.IsMasterClient)
            {
                CheckDeadCapturers();

                if (capturersCount == 1)
                {
                    currCapturingTime += Time.deltaTime;
                }
                else if (currCapturingTime > 0f)
                {
                    currCapturingTime -= 3f * Time.deltaTime;
                    currCapturingTime = Mathf.Max(0f, currCapturingTime);
                }

                if (currCapturingTime >= captureTime && capturersCount == 1)
                {
                    PlayerController.LocalPlayer.OnNexusUsed(currCapturerNames[0], transform.position);
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
