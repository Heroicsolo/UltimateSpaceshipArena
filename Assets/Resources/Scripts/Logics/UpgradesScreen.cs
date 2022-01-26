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

    private List<UpgradeSlot> upgradeSlots = new List<UpgradeSlot>();
    private PlayerController playerController;

    public void SetData(PlayerController pc, bool refreshOnly = false)
    {
        shipImage.sprite = pc.ShipIcon;

        if (playerController == null)
            playerController = pc;

        int modifiedDurability = pc.MaxDurability;
        int modifiedShield = pc.MaxShield;
        float modifiedSpeed = pc.MaxSpeed;
        float modifiedCrit = pc.MainProjectile.critChance;
        float modifiedCritDamage = pc.MainProjectile.critDamageModifier;

        List<UpgradeData> upgradesList = pc.Upgrades;

        if (!refreshOnly)
        {
            if (upgradeSlots.Count > 0)
            {
                foreach (var slot in upgradeSlots)
                {
                    Destroy(slot.gameObject);
                }

                upgradeSlots.Clear();
            }

            foreach (var upgrade in upgradesList)
            {
                GameObject slot = Instantiate(upgradeSlotPrefab, upgradesHolder);
                UpgradeSlot us = slot.GetComponent<UpgradeSlot>();
                int upgradeLevel = Launcher.instance.GetUpgradeLevel(pc, upgrade);
                us.SetData(upgrade, upgradeLevel, this, pc);
                upgradeSlots.Add(us);

                if (upgradeLevel > 0)
                {
                    modifiedDurability = Mathf.CeilToInt(modifiedDurability * (1f + upgrade.durabilityBonus * upgradeLevel));
                    modifiedShield = Mathf.CeilToInt(modifiedShield * (1f + upgrade.shieldBonus * upgradeLevel));
                    modifiedSpeed = modifiedSpeed * (1f + upgrade.speedBonus * upgradeLevel);
                    modifiedCrit += upgrade.critChanceBonus * upgradeLevel;
                    modifiedCritDamage += upgrade.critDamageBonus * upgradeLevel;
                }
            }
        }
        else
        {
            foreach (var upgrade in upgradeSlots)
            {
                int upgradeLevel = upgrade.Level;
                UpgradeData data = upgrade.Upgrade;

                if (upgradeLevel > 0)
                {
                    modifiedDurability = Mathf.CeilToInt(modifiedDurability * (1f + data.durabilityBonus * upgradeLevel));
                    modifiedShield = Mathf.CeilToInt(modifiedShield * (1f + data.shieldBonus * upgradeLevel));
                    modifiedSpeed = modifiedSpeed * (1f + data.speedBonus * upgradeLevel);
                    modifiedCrit += data.critChanceBonus * upgradeLevel;
                    modifiedCritDamage += data.critDamageBonus * upgradeLevel;
                }
            }
        }

        durabilityLabel.text = modifiedDurability.ToString();
        shieldLabel.text = modifiedShield.ToString();
        speedLabel.text = modifiedSpeed.ToString() + " m/s";
        critLabel.text = string.Format("Crit chance: {0}%", Mathf.CeilToInt(100f * modifiedCrit));
        critDamageLabel.text = string.Format("Crit damage: {0}%", Mathf.CeilToInt(100f * modifiedCritDamage));
    }

    public void Refresh()
    {
        SetData(playerController, true);
    }
}
