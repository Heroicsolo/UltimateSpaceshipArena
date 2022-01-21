using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This is hiding objects manager - script that checks if some AutoHidingObject is situated in front of camera
//Add AutoHidingObject script to all objects which you need to hide (that objects have to contain MeshRenderer component)
//Add this component to your camera and set needed objects check distance
public class HidingObjectsManager : MonoBehaviour
{
    [SerializeField] float m_distance = 15f;

    private List<AutoHidingObject> m_ahoList = new List<AutoHidingObject>();

    private Transform m_playerTransform;

    //Call this method when the next level is loaded, to search all new Auto Hiding Objects
    public void FindAutoHidingObjects()
    {
        m_ahoList.Clear();

        m_ahoList = new List<AutoHidingObject>(FindObjectsOfType<AutoHidingObject>());
    }

    private void Start()
    {
        //m_playerTransform = GameController.instance.PlayerTransform;
    }

    void Update()
    {
        List<RaycastHit> hits = new List<RaycastHit>(Physics.RaycastAll(transform.position, (m_playerTransform.position - transform.position).normalized, Mathf.Min(m_playerTransform.Distance(transform), m_distance)));

        foreach( var aho in m_ahoList )
        {
            if( aho == null ) continue;

            if( !hits.Exists(x => x.transform == aho.transform) )
                aho.Show();
            else if( !aho.IsHidden )
                aho.Hide();
        }
    }
}
