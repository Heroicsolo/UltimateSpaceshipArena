using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
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

    private void OnDisable()
    {
        StopRotation();
    }

    IEnumerator Rotate()
    {
        do
        {
            _transform.Rotate(rotationDirection * rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        } while (true);
    }

    void StopRotation()
    {
        if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
    }
}
