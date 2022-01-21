using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSelector : MonoBehaviour
{
    private void OnEnable()
    {
        Launcher.instance.OnShipSelectorOpened();
    }
}
