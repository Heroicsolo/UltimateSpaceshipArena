using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        PlayerUI.Instance.Shoot();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        PlayerUI.Instance.ShootEnd();
    }
}
