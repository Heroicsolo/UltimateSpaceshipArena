using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradesScreen : MonoBehaviour
{
    [SerializeField] Image shipImage;
    [SerializeField] TextMeshProUGUI durabilityLabel;
    [SerializeField] TextMeshProUGUI shieldLabel;
    [SerializeField] TextMeshProUGUI speedLabel;
    [SerializeField] TextMeshProUGUI critLabel;
    [SerializeField] TextMeshProUGUI critDamageLabel;
    [SerializeField] Transform upgradesHolder;
    [SerializeField] GameObject upgradeSlotPrefab;

    private List<GameObject> upgradeSlots = new List<GameObject>();

    public void SetData(PlayerController pc)
    {
        shipImage.sprite = pc.ShipIcon;

        int modifiedDurability = pc.MaxDurability;
        int modifiedShield = pc.MaxShield;
        float modifiedSpeed = pc.MaxSpeed;
        float modifiedCrit = pc.MainProjectile.critChance;
        float modifiedCritDamage = pc.MainProjectile.critDamageModifier;

        List<UpgradeData> upgradesList = pc.Upgrades;

        if (upgradeSlots.Count > 0)
        {
            foreach (var slot in upgradeSlots)
            {
                Destroy(slot);
            }

            upgradeSlots.Clear();
        }

        foreach (var upgrade in upgradesList)
        {
            GameObject slot = Instantiate(upgradeSlotPrefab, upgradesHolder);
            UpgradeSlot us = slot.GetComponent<UpgradeSlot>();
            int upgradeLevel = Launcher.instance.GetUpgradeLevel(pc, upgrade);
            us.SetData(upgrade, upgradeLevel);
            upgradeSlots.Add(slot);

            if (upgradeLevel > 0)
            {
                modifiedDurability = Mathf.CeilToInt(modifiedDurability * (1f + upgrade.durabilityBonus * upgradeLevel));
                modifiedShield = Mathf.CeilToInt(modifiedShield * (1f + upgrade.shieldBonus * upgradeLevel));
                modifiedSpeed = modifiedSpeed * (1f + upgrade.speedBonus * upgradeLevel);
                modifiedCrit += upgrade.critChanceBonus * upgradeLevel;
                modifiedCritDamage += upgrade.critDamageBonus * upgradeLevel;
            }
        }

        durabilityLabel.text = modifiedDurability.ToString();
        shieldLabel.text = modifiedShield.ToString();
        speedLabel.text = modifiedSpeed.ToString() + " m/s";
        critLabel.text = string.Format("Crit chance: {0}%", Mathf.CeilToInt(100f * modifiedCrit));
        critDamageLabel.text = string.Format("Crit damage: {0}%", Mathf.CeilToInt(100f * modifiedCritDamage));
    }
}
