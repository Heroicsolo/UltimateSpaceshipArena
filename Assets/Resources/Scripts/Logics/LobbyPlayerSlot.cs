using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerSlot : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_nicknameLabel;
    [SerializeField] GameObject m_aiLabel;
    [SerializeField] Image m_shipIcon;

    private string m_nickname = "";

    public string Nickname => m_nickname;

    public void SetData(string nickname, Sprite shipSprite, bool isAI = false)
    {
        m_nickname = nickname;
        m_aiLabel.SetActive(isAI);
        m_shipIcon.sprite = shipSprite;
        m_nicknameLabel.text = nickname;
    }
}
