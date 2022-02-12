using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New UpgradeData", menuName = "Upgrade Data", order = 51)]
public class UpgradeData : ScriptableObject
{
    [SerializeField] public string id;
    [SerializeField] public string title;
    [SerializeField] public string desc;
    [SerializeField] public Sprite icon;
    [SerializeField] public int cost = 0;
    [SerializeField] public int tier = 0;
    [SerializeField] public int maxUpgradeLevels = 1;
    [SerializeField] public int damageBonus = 0;
    [SerializeField] public float speedBonus = 0f;
    [SerializeField] public float durabilityBonus = 0f;
    [SerializeField] public float shieldBonus = 0f;
    [SerializeField] public float durabilityRegenBonus = 0f;
    [SerializeField] public float shieldRegenBonus = 0f;
    [SerializeField] public float critChanceBonus = 0f;
    [SerializeField] public float critDamageBonus = 0f;
    [SerializeField] public float stealthLengthBonus = 0f;
    [SerializeField] public float stealthSpeedBonus = 0f;
    [SerializeField] public float stealthCritChanceBonus = 0f;
    [SerializeField] public float repairBonus = 0f;
    [SerializeField] public float respawnTimeBonus = 0f;
    [SerializeField] public float respawnImmortalityTimeBonus = 0f;
}
