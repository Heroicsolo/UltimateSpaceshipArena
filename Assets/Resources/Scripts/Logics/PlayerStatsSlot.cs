using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsSlot : MonoBehaviour
{
    [SerializeField] private Image shipIcon;
    [SerializeField] private TextMeshProUGUI playerNameLabel;
    [SerializeField] private TextMeshProUGUI killsLabel;
    [SerializeField] private TextMeshProUGUI deathsLabel;
    [SerializeField] private TextMeshProUGUI ratingLabel;

    private PlayerController target;
    private string playerName = "";

    public string PlayerName => playerName;
    public int Score => target != null ? target.Score : 0;

    public void SetData(PlayerController pc, int rating)
    {
        ratingLabel.text = pc.IsAI ? "Bot" : rating.ToString();
        shipIcon.sprite = pc.ShipIcon;
        playerNameLabel.text = pc.Name;
        killsLabel.text = "0";
        deathsLabel.text = "0";

        target = pc;
        playerName = pc.Name;
    }

    private void Update()
    {
        if (target != null)
        {
            killsLabel.text = target.KillsCount.ToString();
            deathsLabel.text = target.DeathsCount.ToString();
        }
    }
}
