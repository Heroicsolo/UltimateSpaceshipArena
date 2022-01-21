using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    public float m_floatingAmplitude = 1f;
    public float m_floatingSpeed = 2f;
    public bool m_randomizeStart = false;

    private Vector3 initPos;

    void Start()
    {
        initPos = m_randomizeStart ? transform.position + Random.Range(-m_floatingAmplitude, m_floatingAmplitude) * Vector3.up : transform.position;
    }

    void FixedUpdate()
    {
        transform.position = new Vector3(transform.position.x, initPos.y + m_floatingAmplitude * Mathf.Sin(m_floatingSpeed * Time.fixedTime), transform.position.z);
    }
}
