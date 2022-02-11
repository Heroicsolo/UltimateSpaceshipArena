using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This component moves material texture which can be useful for conveyor effect or some fake "monitor" animations
//All you need is to add this component to your GameObject with MeshRenderer component and set movementVector value in inspector
public class TextureMover : MonoBehaviour
{
    private Material m_mat;

    [SerializeField] Vector2 m_movementVector;
    [SerializeField] bool m_sinMovement = false;
    [SerializeField] float m_movementPeriod = 1f;
    [SerializeField] float m_movementSpeed = 1f;

    private void Awake()
    {
        m_mat = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        if( !m_sinMovement )
            m_mat.mainTextureOffset += m_movementVector * Time.deltaTime;
        else
            m_mat.mainTextureOffset = m_movementVector * Mathf.Sin(m_movementSpeed * Time.time);
    }
}
