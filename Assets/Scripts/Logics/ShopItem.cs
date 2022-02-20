using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI descLabel;
    [SerializeField] private TextMeshProUGUI costLabel;
    [SerializeField] private Image itemImage;
    [SerializeField] private Image topOverlay;
    [SerializeField] private Image bottomOverlay;
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

        titleLabel.text = LangResolver.instance.GetLocalizedString(data.Title);
        descLabel.text = LangResolver.instance.GetLocalizedString(data.Desc);
        costLabel.text = data.Cost.ToString();
        itemImage.sprite = data.Icon;
        topOverlay.color = data.OverlayColor;
        bottomOverlay.color = data.OverlayColor;

        buyButtonAnimator.enabled = AccountManager.Currency >= data.Cost;
    }

    public void Purchase()
    {
        if (AccountManager.Currency < skinData.Cost)
        {
            MessageBox.instance.Show("Not enough money!");
            return;
        }

        AccountManager.Currency -= skinData.Cost;

        AccountManager.UnlockSkin(skinData.ID);

        shop.Refresh();
    }

    public void Refresh()
    {
        bool isUnlocked = AccountManager.IsSkinUnlocked(skinData.ID);

        unlockedIndicator.SetActive(isUnlocked);
        purchaseBlock.SetActive(!isUnlocked);

        buyButtonAnimator.enabled = AccountManager.Currency >= skinData.Cost;
    }
}
