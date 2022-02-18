using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI costLabel;
    [SerializeField] private Image itemImage;
    [SerializeField] private GameObject unlockedIndicator;
    [SerializeField] private GameObject purchaseBlock;
    [SerializeField] private Animator buyButtonAnimator;

    private SkinData skinData;
    private ShopScreen shop;

    public void SetData(ShopScreen shopScreen, SkinData data)
    {
        shop = shopScreen;

        skinData = data;

        bool isUnlocked = AccountManager.IsSkinUnlocked(data.ID);

        unlockedIndicator.SetActive(isUnlocked);
        purchaseBlock.SetActive(!isUnlocked);

        titleLabel.text = data.Title;
        costLabel.text = data.Cost.ToString();
        itemImage.sprite = data.Icon;

        buyButtonAnimator.enabled = AccountManager.Currency >= data.Cost;
    }

    public void Purchase()
    {
        if (AccountManager.Currency < skinData.Cost) return;

        AccountManager.Currency -= skinData.Cost;

        AccountManager.UnlockSkin(skinData.ID);

        Refresh();
    }

    public void Refresh()
    {
        bool isUnlocked = AccountManager.IsSkinUnlocked(skinData.ID);

        unlockedIndicator.SetActive(isUnlocked);
        purchaseBlock.SetActive(!isUnlocked);

        buyButtonAnimator.enabled = AccountManager.Currency >= skinData.Cost;

        shop.Refresh();
    }
}
