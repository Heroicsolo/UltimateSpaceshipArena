using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LabelScaler : MonoBehaviour
{
    [Min(0.1f)]
    public float m_animTime = 0.3f;
    [Min(1f)]
    public float m_maxScaleFactor = 1.1f;

    private Vector3 m_initScale = Vector3.one;
    private float m_timeLeft = 0f;
    private bool m_animRunning = false;

    public void SetLabelText(string _text)
    {
        GetComponent<Text>().text = _text;

        RunAnim();
    }

    public void RunAnim()
    {
        m_animRunning = true;
        m_timeLeft = m_animTime;
    }

    void Start()
    {
        m_initScale = transform.localScale;
    }

    void Update()
    {
        if( m_animRunning && m_timeLeft > 0f )
        {
            m_timeLeft -= Time.deltaTime;

            float percent = 1f - m_timeLeft / m_animTime;

            transform.localScale = m_initScale * (1f + (m_maxScaleFactor - 1f) * Mathf.Sin(Mathf.PI * percent * 0.5f));

            if( m_timeLeft <= 0f )
            {
                transform.localScale = m_initScale;

                m_animRunning = false;
            }
        }
    }
}
