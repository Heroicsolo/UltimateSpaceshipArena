using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    public float m_floatingAmplitude = 1f;
    public float m_floatingSpeed = 2f;

    private Vector3 initPos;

    void Start()
    {
        initPos = transform.localPosition;
    }

    public void Restart()
    {
        initPos = transform.localPosition;
    }

    void FixedUpdate()
    {
        transform.localPosition = initPos + transform.up * m_floatingAmplitude * Mathf.Sin(m_floatingSpeed * Time.fixedTime);
    }
}
