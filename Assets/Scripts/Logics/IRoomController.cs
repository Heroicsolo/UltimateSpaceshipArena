using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRoomController
{
    public bool TryPassMasterClient();
    public void RemoveRoomFromList();
    public void RegisterPlayer(PlayerController player);
    public void UnregisterPlayer(PlayerController player);
    public void RegisterPickup(Pickup pickup);
    void SpawnPickups();
    void SpawnBots();
    void SendPlayersNamesData();
    public void LeaveRoom();
    public void AddRegisteredPlayersToLobbyUI();
}
