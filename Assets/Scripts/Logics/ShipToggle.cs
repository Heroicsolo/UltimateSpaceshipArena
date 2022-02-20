using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShipToggle : MonoBehaviour
{
    [SerializeField] PlayerController m_shipData;
    [SerializeField] Image m_barDurability;
    [SerializeField] Image m_barShield;
    [SerializeField] Image m_barSpeed;
    [SerializeField] Image m_skillIcon1;
    [SerializeField] Image m_skillIcon2;
    [SerializeField] Image m_skillIcon3;
    [SerializeField] Image m_shipIcon;
    [SerializeField] Text m_shipTitle;

    public PlayerController ShipData => m_shipData;

    public void OnShipSelected(bool selected)
    {
        if( selected )
        {
            Launcher.instance.SelectedShipPrefab = m_shipData.gameObject;
        }
    }

    private void Awake()
    {
        m_barDurability.fillAmount = m_shipData.BaseDurability / 200f;
        m_barShield.fillAmount = m_shipData.BaseShield / 200f;
        m_barSpeed.fillAmount = m_shipData.MaxSpeed / 100f;
        m_shipIcon.sprite = m_shipData.ShipIcon;
        m_shipTitle.text = LangResolver.instance.GetLocalizedString(m_shipData.ShipTitle);

        m_skillIcon1.sprite = m_shipData.Skills[0].icon;
        m_skillIcon2.sprite = m_shipData.Skills[1].icon;
        m_skillIcon3.sprite = m_shipData.Skills[2].icon;
    }

    private void Refresh()
    {
        m_shipTitle.text = LangResolver.instance.GetLocalizedString(m_shipData.ShipTitle);

        List<UpgradeData> upgradesList = m_shipData.Upgrades;

        Animator buttonAnimator = GetComponentInChildren<Animator>(true);
        buttonAnimator.enabled = false;

        foreach (var upgrade in upgradesList)
        {
            bool isMaxLvl = false;
            if (AccountManager.IsUpgradeAvailable(m_shipData, upgrade, out isMaxLvl))
            {
                buttonAnimator.enabled = true;
                break;
            }
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }
}
