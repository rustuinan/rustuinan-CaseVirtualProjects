using UnityEngine;

[CreateAssetMenu(
    fileName = "NewUnitConfig",
    menuName = "Battle/Unit Config")]
public class UnitConfig : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string id;
    public string displayName;
    public SoldierType soldierType;

    [Header("Temel Statlar")]
    public float maxHealth = 100f;
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackSpeed = 1f;
    public float moveSpeed = 3.5f;

    [Header("Prefab Referansları")]
    public GameObject unitPrefab;
    public GameObject projectilePrefab;

    [Header("Wizard Ayarları")]
    public bool canHeal = false;
    public float healAmount = 10f;
    public float healCooldown = 5f;
    public float areaRadius = 2f;

    [Header("Commander Ayarları")]
    public bool isCommander = false;

    [Header("Balancing")]
    public int cost = 1;

    [Header("RNG Ayarları")]
    [Range(0f, 0.5f)]
    public float damageRandomPercent = 0.1f;

    [Range(0f, 0.5f)]
    public float attackSpeedRandomPercent = 0.05f;

    [Range(0f, 1f)]
    public float dodgeChance = 0.05f;
}
