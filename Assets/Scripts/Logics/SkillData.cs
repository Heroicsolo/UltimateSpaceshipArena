using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New SkillData", menuName = "Skill Data", order = 51)]
public class SkillData : ScriptableObject
{
    [SerializeField] public float cooldown = 1f;
    [SerializeField] public Sprite icon;
    [SerializeField] public float stealthLength = 0f;
    [SerializeField] public float speedBonus = 0f;
    [SerializeField] public float speedBonusLength = 0f;
    [SerializeField] public float durabilityBonus = 0f;
    [SerializeField] public float shieldBonus = 0f;
    [SerializeField] public GameObject projectilePrefab;
    [SerializeField] public AudioClip sound;
}
