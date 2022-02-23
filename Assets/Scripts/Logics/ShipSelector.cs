using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSelector : MonoBehaviour
{
    [SerializeField] UpgradesScreen upgradesScreen;

    [SerializeField] List<ShipToggle> shipToggles;

    private void Start()
    {
        shipToggles[AccountManager.SelectedShip].Select();
    }

    private void OnEnable()
    {
        Launcher.instance.OnShipSelectorOpened();
    }

    public void OpenUpgradesScreen(ShipToggle toggle)
    {
        upgradesScreen.SetData(toggle.ShipData);
        upgradesScreen.gameObject.SetActive(true);
    }
}
