using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeSlot : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] TextMeshProUGUI costLabel;
    [SerializeField] TextMeshProUGUI levelLabel;

    private int currentCost = 0;
    private int currentLevel = 0;
    private UpgradeData data;
    private PlayerController pc;
    private UpgradesScreen upgradesScreen;

    public int Level => currentLevel;
    public UpgradeData Upgrade => data;

    public void SetData(UpgradeData _data, int level, UpgradesScreen _us, PlayerController _pc)
    {
        upgradesScreen = _us;
        pc = _pc;
        data = _data;
        currentLevel = level;
        icon.sprite = data.icon;
        title.text = data.title;
        currentCost = data.cost + Mathf.CeilToInt(level * data.cost * 0.5f);
        if (currentCost > Launcher.instance.Currency) costLabel.color = Color.red;
        Refresh();
    }

    void Refresh()
    {
        costLabel.text = currentCost.ToString();
        levelLabel.text = "lvl " + currentLevel.ToString();
        if (currentCost > Launcher.instance.Currency) costLabel.color = Color.red;
    }

    public void ShowDesc()
    {
        MessageBox.instance.Show(data.desc);
    }

    public void TryUpgrade()
    {
        if (currentCost > Launcher.instance.Currency) return;

        Launcher.instance.Currency -= currentCost;

        currentLevel++;
        currentCost = data.cost + Mathf.CeilToInt(currentLevel * data.cost * 0.5f);
        Refresh();
        upgradesScreen.Refresh();

        Launcher.instance.IncreaseUpgradeLevel(pc, data);
    }
}
