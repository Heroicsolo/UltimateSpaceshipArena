using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopScreen : MonoBehaviour
{
    [SerializeField] private ShopItem itemPrefab;
    [SerializeField] private Transform contentTransform;

    void Start()
    {
        LoadItems();
    }

    private void LoadItems()
    {
        foreach (var item in Launcher.instance.AvailableSkins)
        {
            ShopItem newItem = Instantiate(itemPrefab, contentTransform);
            newItem.SetData(this, item);
        }
    }

    public void Refresh()
    {

    }
}
