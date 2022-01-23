using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class ArenaController : MonoBehaviourPunCallbacks
{
    public static ArenaController instance;

    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private List<Transform> pickupPoints;
    [SerializeField] private List<GameObject> botsPrefabs;
    [SerializeField] private List<string> botsPossibleNames;
    [SerializeField] Transform nexusPosition;
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private float joiningTime = 60f;

    public Vector3 RandomSpawnPoint => spawnPoints.GetRandomElement().position;
    public string RandomBotName => botsPossibleNames.GetRandomElement();
    public float RespawnTime => respawnTime;

    private List<PlayerController> m_roomPlayers = new List<PlayerController>();
    private List<Pickup> m_roomPickups = new List<Pickup>();
    private List<Transform> m_botsSpawnPoints = new List<Transform>();
    private Dictionary<string, int> m_playersRatings = new Dictionary<string, int>();

    private float m_timeToJoin = 0f;

    public List<PlayerController> RoomPlayers => m_roomPlayers;
    public List<Pickup> RoomPickups => m_roomPickups;

    public PlayerController GetPlayerByName(string name)
    {
        return m_roomPlayers.Find(x => x.Name == name);
    }

    public void RemoveRoomFromList()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
    }

    public void SetPlayerRating(string name, int rating)
    {
        if (m_playersRatings.ContainsKey(name))
            m_playersRatings[name] = rating;
        else
            m_playersRatings.Add(name, rating);
    }

    public void OnPlayerKilled(string killerName, string victimName)
    {
        this.photonView.RPC("OnPlayerKilled_RPC", RpcTarget.All, killerName, victimName);
    }

    [PunRPC]
    void OnPlayerKilled_RPC(string killerName, string victimName)
    {
        PlayerController killer = GetPlayerByName(killerName);
        PlayerController victim = GetPlayerByName(victimName);

        if (killer.photonView.IsMine && killerName != victimName)
            killer.KillsCount++;

        if (victim.photonView.IsMine)
            victim.DeathsCount++;

        PlayerUI.Instance.SortPlayerStatsSlots();
    }

    public int GetPlayerRating(string name)
    {
        if (m_playersRatings.ContainsKey(name))
            return m_playersRatings[name];
        else
            return -1;
    }

    public void RegisterPlayer(PlayerController player)
    {
        if (!m_roomPlayers.Contains(player))
        {
            m_roomPlayers.Add(player);
            botsPossibleNames.Remove(player.Name);
        }
    }

    public void UnregisterPlayer(PlayerController player)
    {
        if (m_roomPlayers.Contains(player))
        {
            m_roomPlayers.Remove(player);
            if (player.IsAI)
                botsPossibleNames.Add(player.Name);
            if (PlayerUI.Instance != null)
            {
                PlayerUI.Instance.OnLobbyPlayerDeleted(player.Name);
                PlayerUI.Instance.RemovePlayerStatsSlot(player.Name);
            }
        }
    }

    public void AddRegisteredPlayersToLobbyUI()
    {
        foreach (var p in m_roomPlayers)
        {
            PlayerUI.Instance.AddEnemyToMiniMap(p, p.Name, p.IsAI);
        }
    }

    public void OnBattleStarted()
    {
        foreach (var p in m_roomPlayers)
        {
            if (p.IsAI)
            {
                p.SendRating();
            }
        }
    }

    void SpawnPickups()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            foreach( var point in pickupPoints )
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
            for (int i = 0; i < 3; i++)
            {
                Transform point = m_botsSpawnPoints.GetRandomElement();
                m_botsSpawnPoints.Remove(point);
                PhotonNetwork.InstantiateRoomObject(botsPrefabs.GetRandomElement().name, point.position, Quaternion.identity);
            }
        }
    }

    void RemoveOneBot()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            List<PlayerController> bots = m_roomPlayers.FindAll(x => x.IsAI);

            PlayerController bot = bots.GetRandomElement();

            PlayerUI.Instance.OnLobbyPlayerDeleted(bot.Name);

            PhotonNetwork.Destroy(bot.gameObject);
        }
    }

    void AddOneBot()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.InstantiateRoomObject(botsPrefabs.GetRandomElement().name, RandomSpawnPoint, Quaternion.identity);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        photonView.ViewID = 1;

        if (PlayerController.LocalPlayerInstance == null)
        {
            Transform point = spawnPoints.GetRandomElement();

            if (PhotonNetwork.IsMasterClient)
            {
                m_botsSpawnPoints.AddRange(spawnPoints);
                m_botsSpawnPoints.Remove(point);

                m_timeToJoin = joiningTime;
            }

            if (Launcher.instance.SelectedShipPrefab == null)
                PhotonNetwork.Instantiate("Spaceship00", point.position, Quaternion.identity, 0);
            else
                PhotonNetwork.Instantiate(Launcher.instance.SelectedShipPrefab.name, point.position, Quaternion.identity, 0);

            SpawnPickups();
            SpawnBots();

            AddRegisteredPlayersToLobbyUI();
        }

        Launcher.instance.OnArenaLoaded();
    }

    // Update is called once per frame
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

        if (m_roomPlayers.Count > 3) RemoveOneBot();

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

        if (m_roomPlayers.Count < 4) AddOneBot();

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


            //LoadArena();
        }
    }

    void LoadArena()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
        }
        PhotonNetwork.LoadLevel("Arena00");
    }

    /// <summary>
    /// Called when the local player left the room. We need to load the launcher scene.
    /// </summary>
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }
}