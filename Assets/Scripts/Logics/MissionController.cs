using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionController : MonoBehaviourPunCallbacks, IRoomController
{
    public static MissionController instance;

    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private List<Transform> botsSpawnPoints;
    [SerializeField] private List<GameObject> botsPrefabs;
    [SerializeField] private List<Transform> pickupPoints;
    [SerializeField] private List<TurretController> m_roomTurrets;
    [SerializeField] private Transform leftBound;
    [SerializeField] private Transform rightBound;
    [SerializeField] private Transform topBound;
    [SerializeField] private Transform bottomBound;
    [SerializeField] Transform nexusPosition;
    [SerializeField] Transform mapCenter;
    [SerializeField] float missionTime = 180f;

    public Vector3 RandomSpawnPoint => spawnPoints.GetRandomElement().position;

    private List<PlayerController> m_roomPlayers = new List<PlayerController>();
    private List<PlayerController> m_missionBots = new List<PlayerController>();
    private List<Pickup> m_roomPickups = new List<Pickup>();
    private List<Transform> m_availableSpawnPoints = new List<Transform>();
    private List<string> m_connectedPlayersNames = new List<string>();

    private float m_timeToJoin = 0f;
    private bool m_nexusCaptured = false;
    public int InitBotsCount => botsSpawnPoints.Count;
    public int KilledBotsCount => botsSpawnPoints.Count - m_missionBots.Count;

    public float MissionTime => missionTime;
    public bool IsObjectiveDone => KilledBotsCount >= InitBotsCount;
    public bool IsNexusCaptured { get { return m_nexusCaptured; } set { m_nexusCaptured = value; } }

    public float MapHeight => topBound.Distance(bottomBound);
    public float MapWidth => leftBound.Distance(rightBound);

    public Transform NexusTransform => nexusPosition;
    public Vector3 NexusPosition => nexusPosition.position;
    public List<PlayerController> RoomPlayers => m_roomPlayers;
    public List<TurretController> RoomTurrets => m_roomTurrets;
    public List<PlayerController> MissionBots => m_missionBots;
    public List<Pickup> RoomPickups => m_roomPickups;

    public Vector3 GetMapPosition(Vector3 worldPos)
    {
        Vector3 mapPos = new Vector3(worldPos.x - mapCenter.position.x, worldPos.y, worldPos.z - mapCenter.position.z);

        return mapPos;
    }

    [PunRPC]
    public void FreeSpawnPointResponse_RPC(int spawnPointIdx)
    {
        if (!PhotonNetwork.IsMasterClient && PlayerController.LocalPlayer == null)
        {
            m_availableSpawnPoints.Add(spawnPoints[spawnPointIdx]);

            SpawnPlayer();
        }
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

    public bool TryPassMasterClient()
    {
        foreach (var p in m_roomPlayers)
        {
            if (p.photonView.Controller != PlayerController.LocalPlayer.photonView.Controller)
            {
                return PhotonNetwork.SetMasterClient(p.photonView.Controller);
            }
        }

        return false;
    }

    public PlayerController GetPlayerByName(string name)
    {
        PlayerController pc = m_roomPlayers.Find(x => x.Name == name);

        if (pc) return pc;

        pc = m_missionBots.Find(x => x.Name == name);

        return pc;
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

                if (PlayerUI.Instance != null)
                {
                    PlayerUI.Instance.AddEnemyToMiniMap(player, player.Name, true);
                }
            }
        }
        else
        {
            if (!m_roomPlayers.Contains(player))
            {
                m_roomPlayers.Add(player);

                if (PlayerUI.Instance != null)
                {
                    PlayerUI.Instance.OnLobbyPlayerAdded(player.Name, player.ShipIcon, false);
                }

                ScaleBots();
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
                PlayerController.LocalPlayer.KillsCount++;
            }
        }
        else
        {
            if (m_roomPlayers.Contains(player))
            {
                m_roomPlayers.Remove(player);

                ScaleBots();

                if (PlayerUI.Instance != null)
                {
                    PlayerUI.Instance.OnLobbyPlayerDeleted(player.Name);
                }
            }
        }
    }

    void ScaleBots()
    {
        foreach (var bot in m_missionBots)
        {
            bot.ScaleStats((float)RoomPlayers.Count / 4f);
        }
    }

    public void RegisterPickup(Pickup pickup)
    {
        if (!m_roomPickups.Contains(pickup))
            m_roomPickups.Add(pickup);
    }

    public void SpawnPickups()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var point in pickupPoints)
            {
                PredefinedPickup pp = point.GetComponent<PredefinedPickup>();

                if (!pp)
                {
                    PhotonNetwork.InstantiateRoomObject(Random.value < 0.5f ? "PickupShield" : "PickupDurability", point.position, Quaternion.identity);
                }
                else
                {
                    PhotonNetwork.InstantiateRoomObject(pp.PickupPrefab.name, point.position, Quaternion.identity);
                }
            }

            PhotonNetwork.InstantiateRoomObject("PickupNexus", nexusPosition.position, Quaternion.identity);
        }
    }

    public void SpawnBots()
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

        //photonView.ViewID = 1;

        if (PlayerController.LocalPlayerInstance == null)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                m_timeToJoin = BalanceProvider.Balance.lobbyLength;

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

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient == this.photonView.Controller)
        {
            PlayerController.LocalPlayer.RunMatchTimer();
        }

        PlayerUI.Instance.OnMasterClientSwitched(newMasterClient == this.photonView.Controller);
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

        SendPlayersNamesData();

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
        //PhotonNetwork.IsMessageQueueRunning = false;
        SceneManager.LoadScene(0);
    }

    public void LeaveRoom()
    {
        Launcher.instance.OnRoomLeavingStarted();
        PlayerUI.Instance.OnRoomLeft();
        PhotonNetwork.SendAllOutgoingCommands();
        PhotonNetwork.LeaveRoom();
    }

    public void SendPlayersNamesData()
    {
        foreach (var p in m_roomPlayers)
        {
            p.SendNameData();
        }
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

    public List<PlayerController> GetRoomPlayers()
    {
        return m_roomPlayers;
    }

    public List<TurretController> GetRoomTurrets()
    {
        return m_roomTurrets;
    }
}
