using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 * - Just add this component to your game object and it will automatically rotate to main camera.
 * - You can also assign an anchor transform and set needed anchor distance via appropriate parameters. It can help to place some labels near your game characters, for example. Otherwise, it will use parent transform as anchor.
 */

public class RotateToCamera : MonoBehaviour
{
	// ============== PUBLIC FIELDS ==============
    public Transform m_anchorTransform;
    public float m_anchorDist = 0f;

	// ============== PRIVATE FIELDS ==============
    private Transform m_Transform = null;
    private Camera m_MainCamera = null;
    private Transform m_MainCameraTransform = null;

    // ============== LIFETIME ==============
    void Start()
    {
        m_Transform = transform;
        m_MainCamera = Camera.main;
        m_MainCameraTransform = m_MainCamera?.transform;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if( !m_MainCamera ) m_MainCamera = Camera.main; m_MainCameraTransform = m_MainCamera?.transform;

        if( m_anchorTransform && m_MainCameraTransform )
            transform.position = m_anchorTransform.position - m_MainCameraTransform.rotation * (m_anchorDist * Vector3.forward);

        if( m_MainCameraTransform )
        {
            transform.LookAt(
                m_Transform.position + m_MainCameraTransform.rotation * Vector3.forward,
                m_MainCameraTransform.rotation * Vector3.up );
        }
    }
}
