using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadingText : MonoBehaviour
{
    public float m_lifetime = 1f;
    public bool m_isDeleting = true;

    private float m_timeToDeath = 1f;
    private TextMesh m_textMesh;
    private Text m_text;
    private System.Action m_callback;

    private void Awake()
    {
        m_textMesh = GetComponent<TextMesh>();
        m_text = GetComponent<Text>();
    }

    public void SetText(string _text)
    {
        if( m_textMesh )
            m_textMesh.text = _text;

        if( m_text )
            m_text.text = _text;
    }

    public void SetText(string _text, System.Action callback)
    {
        if( m_textMesh )
            m_textMesh.text = _text;

        if( m_text )
            m_text.text = _text;

        m_callback = callback;
    }

    void Start()
    {
        m_timeToDeath = m_lifetime;
    }

    private void OnEnable()
    {
        m_timeToDeath = m_lifetime;
    }

    void Update()
    {
        if( m_timeToDeath > 0f )
        {
            m_timeToDeath -= Time.deltaTime;

            float percent = m_timeToDeath / m_lifetime;

            if( m_textMesh )
            {
                Color color = m_textMesh.color;
                color.a = Mathf.Sin(Mathf.PI * percent);
                m_textMesh.color = color;
            }
            else if( m_text )
            {
                Color color = m_text.color;
                color.a = Mathf.Sin(Mathf.PI * percent);
                m_text.color = color;
            }

            if( m_timeToDeath <= 0f )
            {
                if( m_isDeleting )
                    Destroy(gameObject);
                else
                {
                    if( m_textMesh )
                    {
                        Color color = m_textMesh.color;
                        color.a = 0f;
                        m_textMesh.color = color;
                    }
                    else if( m_text )
                    {
                        Color color = m_text.color;
                        color.a = 0f;
                        m_text.color = color;
                    }

                    gameObject.SetActive(false);
                }

                m_callback?.Invoke();
            }
        }
    }
}
