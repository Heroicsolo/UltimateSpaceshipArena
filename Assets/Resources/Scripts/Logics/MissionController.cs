using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionController : MonoBehaviourPunCallbacks
{
    public static MissionController instance;

    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private List<Transform> botsSpawnPoints;
    [SerializeField] private List<GameObject> botsPrefabs;
    [SerializeField] private List<Transform> pickupPoints;
    [SerializeField] Transform nexusPosition;

    public Vector3 RandomSpawnPoint => spawnPoints.GetRandomElement().position;

    private List<PlayerController> m_roomPlayers = new List<PlayerController>();
    private List<PlayerController> m_missionBots = new List<PlayerController>();
    private List<Pickup> m_roomPickups = new List<Pickup>();
    private List<Transform> m_availableSpawnPoints = new List<Transform>();
    private List<string> m_connectedPlayersNames = new List<string>();

    private float m_timeToJoin = 0f;

    public List<PlayerController> RoomPlayers => m_roomPlayers;
    public List<PlayerController> MissionBots => m_missionBots;
    public List<Pickup> RoomPickups => m_roomPickups;

    [PunRPC]
    public void FreeSpawnPointResponse_RPC(int spawnPointIdx)
    {
        m_availableSpawnPoints.Add(spawnPoints[spawnPointIdx]);

        SpawnPlayer();
    }

    [PunRPC]
    public void FreeSpawnPointRequest_RPC(string playerName)
    {
        if (PhotonNetwork.IsMasterClient && !m_connectedPlayersNames.Contains(playerName))
        {
            int idx = Random.Range(0, m_availableSpawnPoints.Count);
            int pointID = spawnPoints.FindIndex(x => x == m_availableSpawnPoints[idx]);
            m_availableSpawnPoints.RemoveAt(idx);
            m_connectedPlayersNames.Add(playerName);
            photonView.RPC("FreeSpawnPointResponse_RPC", RpcTarget.All, pointID);
        }
    }

    public PlayerController GetPlayerByName(string name)
    {
        return m_roomPlayers.Find(x => x.Name == name);
    }

    public void RemoveRoomFromList()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
    }

    public void RegisterPlayer(PlayerController player)
    {
        if (player.IsAI)
        {
            if (!m_missionBots.Contains(player))
            {
                m_missionBots.Add(player);
            }
        }
        else
        {
            if (!m_roomPlayers.Contains(player))
            {
                m_roomPlayers.Add(player);
            }
        }
    }

    public void UnregisterPlayer(PlayerController player)
    {
        if (player.IsAI)
        {
            if (m_missionBots.Contains(player))
            {
                m_missionBots.Remove(player);
            }
        }
        else
        {
            if (m_roomPlayers.Contains(player))
            {
                m_roomPlayers.Remove(player);

                if (PlayerUI.Instance != null)
                {
                    PlayerUI.Instance.OnLobbyPlayerDeleted(player.Name);
                }
            }
        }
    }

    void SpawnPickups()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var point in pickupPoints)
            {
                GameObject go = PhotonNetwork.InstantiateRoomObject(Random.value < 0.5f ? "PickupShield" : "PickupDurability", point.position, Quaternion.identity);
                m_roomPickups.Add(go.GetComponent<Pickup>());
            }
        }

        GameObject nexusGO = PhotonNetwork.InstantiateRoomObject("PickupNexus", nexusPosition.position, Quaternion.identity);
        m_roomPickups.Add(nexusGO.GetComponent<Pickup>());
    }

    void SpawnBots()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            for (int i = 0; i < botsSpawnPoints.Count; i++)
            {
                PhotonNetwork.InstantiateRoomObject(botsPrefabs.GetRandomElement().name, botsSpawnPoints[i].position, Quaternion.identity);
            }
        }
    }

    void SpawnPlayer()
    {
        int pointIdx = Random.Range(0, m_availableSpawnPoints.Count);
        Transform point = m_availableSpawnPoints[pointIdx];
        m_availableSpawnPoints.RemoveAt(pointIdx);

        if (Launcher.instance.SelectedShipPrefab == null)
            PhotonNetwork.Instantiate("Spaceship00", point.position, Quaternion.identity, 0);
        else
            PhotonNetwork.Instantiate(Launcher.instance.SelectedShipPrefab.name, point.position, Quaternion.identity, 0);

        AddRegisteredPlayersToLobbyUI();
    }

    void Start()
    {
        instance = this;

        photonView.ViewID = 1;

        if (PlayerController.LocalPlayerInstance == null)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                m_timeToJoin = Launcher.instance.Balance.joinStageLength;

                m_availableSpawnPoints.AddRange(spawnPoints);

                SpawnPlayer();
            }
            else
            {
                photonView.RPC("FreeSpawnPointRequest_RPC", RpcTarget.MasterClient, PhotonNetwork.NickName);
            }

            SpawnPickups();
            SpawnBots();
        }

        Launcher.instance.OnArenaLoaded();
    }

    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (m_timeToJoin > 0f)
            {
                m_timeToJoin -= Time.deltaTime;

                if (m_timeToJoin <= 0f)
                {
                    RemoveRoomFromList();
                }
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player other)
    {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); // not seen if you're the player connecting

        PlayerUI.Instance.DoAnnounce(other.NickName + " entered to arena");

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


            //LoadArena();
        }
    }


    public override void OnPlayerLeftRoom(Player other)
    {
        Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); // seen when other disconnects

        PlayerUI.Instance.DoAnnounce(other.NickName + " left from arena");

        PlayerUI.Instance.OnLobbyPlayerDeleted(other.NickName);

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


            //LoadArena();
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void AddRegisteredPlayersToLobbyUI()
    {
        foreach (var p in m_roomPlayers)
        {
            PlayerUI.Instance.OnLobbyPlayerAdded(p.Name, p.ShipIcon, false);
        }

        foreach (var b in m_missionBots)
        {
            PlayerUI.Instance.AddMissionBotToMiniMap(b);
        }
    }
}
