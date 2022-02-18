using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkinType
{
    Bomb,
    Missile,
    DeathEffect,
    SpawnEffect,
    Projectile
}

[CreateAssetMenu(fileName = "New SkinData", menuName = "Skin Data", order = 51)]
public class SkinData : ScriptableObject
{
    [SerializeField] public int ID;
    [SerializeField] public string Title;
    [SerializeField] public Sprite Icon;
    [SerializeField] public SkinType Type;
    [SerializeField] public List<PlayerController> SupportedShips;
    [SerializeField] public int Cost;
    [SerializeField] public GameObject SkinObject;
}
