using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomObjectRotator : SyncTransform
{
    [SerializeField] Vector3 rotationDirection;
    [SerializeField] float rotationSpeed;
    Transform _transform;

    Coroutine rotationCoroutine;

    private void OnEnable()
    {
        StopRotation();
        _transform = GetComponent<Transform>();
        rotationCoroutine = StartCoroutine(Rotate());
    }

    private void Start()
    {
        StopRotation();
        _transform = GetComponent<Transform>();
        rotationCoroutine = StartCoroutine(Rotate());
    }

    private void OnDisable()
    {
        StopRotation();
    }

    IEnumerator Rotate()
    {
        do
        {
            if (PhotonNetwork.IsMasterClient)
                _transform.Rotate(rotationDirection * rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        } while (true);
    }

    void StopRotation()
    {
        if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
    }
}
