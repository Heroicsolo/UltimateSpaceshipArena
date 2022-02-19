using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopScreen : MonoBehaviour
{
    [SerializeField] private ShopItem itemPrefab;
    [SerializeField] private Transform contentTransform;

    private List<ShopItem> shopItems = new List<ShopItem>();

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
            shopItems.Add(newItem);
        }
    }

    public void Refresh()
    {
        foreach (var item in shopItems)
        {
            item.Refresh();
        }
    }
}
