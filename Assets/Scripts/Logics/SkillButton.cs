using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [SerializeField] Image m_cdImage;
    [SerializeField] Image m_icon;

    private SkillData m_skillData;
    private float m_currCD = 0f;

    // Start is called before the first frame update
    void Start()
    {
        m_cdImage.gameObject.SetActive(false);
    }

    public void SetData(SkillData skillData)
    {
        m_skillData = skillData;
        m_icon.sprite = m_skillData.icon;
    }

    public void TryUse()
    {
        if( m_currCD > 0f ) return;

        m_currCD = m_skillData.cooldown;

        m_cdImage.gameObject.SetActive(true);
        m_cdImage.fillAmount = 1f;

        PlayerController.LocalPlayer.UseSkill(m_skillData);
    }

    // Update is called once per frame
    void Update()
    {
        if( m_currCD > 0f )
        {
            m_currCD -= Time.deltaTime;

            m_cdImage.fillAmount = m_currCD / m_skillData.cooldown;

            if( m_currCD <= 0f )
            {
                m_cdImage.gameObject.SetActive(false);

                m_icon.GetComponent<LabelScaler>().RunAnim();
            }
        }
    }
}
