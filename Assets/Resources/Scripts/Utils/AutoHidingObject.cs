using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Add this script to object which must be hidden if will appear near the camera (that object has to contain MeshRenderer component)
//If that object doesn't have MeshRenderer component, this script will search all MeshRenderer components in his children
//You can also switch "proccessChildren" flag to "true", to proccess all children MeshRenderer components by default
//Place needed shader which contains "_Color" property and Fade/Transparency mode into the "fadeShader" field!
//For example, "Legacy Shaders/Transparent/Diffuse" will be fine
public class AutoHidingObject : MonoBehaviour
{
    [SerializeField] Shader m_fadeShader;
    [SerializeField] bool m_proccessChildren = false;

    private List<MeshRenderer> m_renderers;
    private bool m_hidden = false;
    private List<Shader> m_initShaders;

    public bool IsHidden{ get{ return m_hidden; } }

    private void Awake()
    {
        m_renderers = new List<MeshRenderer>();

        MeshRenderer rootRenderer = GetComponent<MeshRenderer>();

        if( !rootRenderer ) m_proccessChildren = true;

        if( m_proccessChildren )
            m_renderers.AddRange(GetComponentsInChildren<MeshRenderer>());
        else
            m_renderers.Add(rootRenderer);

        m_initShaders = new List<Shader>();

        foreach( var rend in m_renderers )
        {
            m_initShaders.Add(rend.material.shader);
        }
        
        m_hidden = false;
    }

    public void Hide()
    {
        m_hidden = true;

        foreach( var rend in m_renderers )
        {
            rend.material.shader = m_fadeShader;

            Color col = rend.material.color;
            col.a = 0.15f;
            rend.material.color = col;
        }
    }

    public void ShowChild(Transform child)
    {
        for( int i = 0; i < m_renderers.Count; i++ )
        {
            if( m_renderers[i].transform == child )
            {
                m_renderers[i].material.shader = m_initShaders[i];

                if( m_renderers[i].material.HasProperty("_Color") )
                {
                    Color col = m_renderers[i].material.color;
                    col.a = 1f;
                    m_renderers[i].material.color = col;
                }
            }
        }
    }

    public void Show()
    {
        m_hidden = false;

        for( int i = 0; i < m_renderers.Count; i++ )
        {
            m_renderers[i].material.shader = m_initShaders[i];

            if( m_renderers[i].material.HasProperty("_Color") )
            {
                Color col = m_renderers[i].material.color;
                col.a = 1f;
                m_renderers[i].material.color = col;
            }
        }
    }
}
