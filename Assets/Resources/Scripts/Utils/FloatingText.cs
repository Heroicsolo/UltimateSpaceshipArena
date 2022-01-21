using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloatingText : MonoBehaviour
{
    public float m_lifetime = 1f;
    public float m_floatingSpeed = 1f;
    public bool m_isDeleting = true;
    public bool m_curved = false;
    public Vector3 m_flyDirection = Vector3.up;
    public Vector3 m_curveDirection = Vector3.right;

    private float m_timeToDeath = 1f;
    private TextMeshPro m_textMesh;
    private TextMeshProUGUI m_textMeshUI;
    private Text m_text;
    private Vector3 m_initPos;
    private float m_side = 0f;

    private void Awake()
    {
        m_textMesh = GetComponent<TextMeshPro>();
        m_textMeshUI = GetComponent<TextMeshProUGUI>();
        m_text = GetComponent<Text>();
        m_initPos = transform.position;
    }

    public void SetCurved(bool _value)
    {
        m_curved = _value;
        m_side = Mathf.Sign(2f * Random.value - 1f);
    }

    public void SetText(string _text)
    {
        if( m_textMesh )
            m_textMesh.text = _text;

        if( m_text )
            m_text.text = _text;

        if( m_textMeshUI )
            m_textMeshUI.text = _text;
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

            transform.Translate((m_flyDirection + m_curveDirection * m_side * percent) * m_floatingSpeed * Time.deltaTime, Space.World);

            if( m_textMesh )
            {
                Color color = m_textMesh.color;
                color.a = Mathf.Min(1f, 2f * percent);
                m_textMesh.color = color;
            }
            else if( m_text )
            {
                Color color = m_text.color;
                color.a = Mathf.Min(1f, 2f * percent);
                m_text.color = color;
            }
            else if( m_textMeshUI )
            {
                Color color = m_textMeshUI.color;
                color.a = Mathf.Min(1f, 2f * percent);
                m_textMeshUI.color = color;
            }

            if( m_timeToDeath <= 0f )
            {
                if( m_isDeleting )
                    Destroy(gameObject);
                else
                {
                    transform.position = m_initPos;

                    if( m_textMesh )
                    {
                        Color color = m_textMesh.color;
                        color.a = 1f;
                        m_textMesh.color = color;
                    }
                    else if( m_text )
                    {
                        Color color = m_text.color;
                        color.a = 1f;
                        m_text.color = color;
                    }
                    else if( m_textMeshUI )
                    {
                        Color color = m_textMeshUI.color;
                        color.a = 1f;
                        m_textMeshUI.color = color;
                    }

                    gameObject.SetActive(false);
                }
            }
        }
    }
}
