using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomObjectPool
{
    private List<GameObject> poolObjects = new List<GameObject>();
    public string prefabName;

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        foreach (var p in poolObjects)
        {
            if (p != null && !p.activeSelf)
            {
                p.transform.position = position;
                p.transform.rotation = rotation;
                p.SetActive(true);
                return p;
            }
        }

        GameObject go = PhotonNetwork.InstantiateRoomObject(prefabName, position, rotation);

        poolObjects.Add(go);

        return go;
    }
}
