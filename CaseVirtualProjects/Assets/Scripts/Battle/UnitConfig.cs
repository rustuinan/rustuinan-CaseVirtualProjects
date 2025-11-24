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
    public float maxHealth = 150f;
    public float attackDamage = 15f;
    public float attackRange = 1.6f;
    public float attackSpeed = 1.2f;
    public float moveSpeed = 4f;

    [Header("VFX")]
    public GameObject deathVfxPrefab;
    public float deathVfxLifetime = 1.5f;

    [Header("SFX")]
    public AudioClip hitSfx;

    [Header("Prefab Referansları")]
    public GameObject unitPrefab;
    public GameObject projectilePrefab;

    [Header("Archer Yakın Dövüş")]
    public bool canFallbackMelee = true;
    public float fallbackMeleeDamage = 7f;
    public float fallbackMeleeRange = 1.3f;

    [Header("Wizard Ayarları")]
    public bool canHeal = false;
    public float healAmount = 20f;
    public float healCooldown = 5f;
    public float areaRadius = 3f;

    [Header("Commander Ayarları")]
    public bool isCommander = false;

    [Header("Commander – Cube Slam")]
    public float slamDamage = 180f;
    public float slamRadius = 7f;
    public float slamCooldown = 5f;

    [Header("Commander – Sphere Rolling Charge")]
    public float chargeDamage = 130f;
    public float chargeDistance = 9f;
    public float chargeSpeed = 16f;
    public float chargeRadius = 2.5f;
    public float chargeCooldown = 5.5f;
    public float chargeKnockbackDistance = 7f;

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
