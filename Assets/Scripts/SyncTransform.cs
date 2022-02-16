using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncTransform : MonoBehaviourPunCallbacks, IPunObservable
{
    private Vector3 networkPos;
    private Quaternion networkRot = Quaternion.identity;
    //Lag compensation
    private float currentTime = 0;
    private double currentPacketTime = 0;
    private double lastPacketTime = 0;
    private Vector3 positionAtLastPacket = Vector3.zero;
    private Quaternion rotationAtLastPacket = Quaternion.identity;

    // Update is called once per frame
    void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            //Lag compensation
            double timeToReachGoal = currentPacketTime - lastPacketTime;
            currentTime += Time.deltaTime;

            transform.position = Vector3.Lerp(positionAtLastPacket, networkPos, (float)(currentTime / timeToReachGoal));
            transform.rotation = Quaternion.Lerp(rotationAtLastPacket, networkRot, (float)(currentTime / timeToReachGoal));
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            this.networkPos = (Vector3)stream.ReceiveNext();
            this.networkRot = (Quaternion)stream.ReceiveNext();

            //Lag compensation
            currentTime = 0.0f;
            lastPacketTime = currentPacketTime;
            currentPacketTime = info.SentServerTime;
            positionAtLastPacket = transform.position;
            rotationAtLastPacket = transform.rotation;
        }
    }
}
