using UnityEngine;
using DG.Tweening;

public class MeleeUnit : MonoBehaviour
{
    [Header("Config")]
    public UnitConfig config;
    public Team team;

    [Header("Visual")]
    public Transform modelTransform;

    [Header("Movement Feeling")]
    public float cubeHopHeight = 0.2f;
    public float cubeHopDuration = 0.25f;
    public float sphereRollSpeed = 360f;

    [Header("Audio")]
    public AudioSource audioSource;
    [Range(0f, 1f)] public float hitVolume = 0.15f;

    [Header("Targeting")]
    [Tooltip("En yakın hedefi yeniden arama aralığı (saniye)")]
    public float targetSearchInterval = 0.4f;

    [Header("Separation Optimization")]
    [Tooltip("Separation vektörünün yeniden hesaplanacağı aralık (saniye)")]
    public float separationRecalcInterval = 0.12f;

    [Header("Crowd Control")]
    [Tooltip("Aynı hedefe saldıranların sayısını kontrol etme aralığı (saniye)")]
    public float crowdCheckInterval = 0.2f;

    private float currentHealth;
    private float attackTimer;
    private bool isMoving;

    private float surroundBias;

    private Transform currentTarget;
    private bool currentTargetIsMelee;
    private MeleeUnit currentMeleeCrowdTarget;
    private float targetSearchTimer;

    private float separationTimer;
    private Vector3 cachedSeparation;

    private float crowdCheckTimer;
    private bool lastCrowdCheckResult;

    private Tween moveTween;
    private Tween attackTween;
    private Tween hitTween;

    public bool IsAlive => currentHealth > 0f;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Start()
    {
        currentHealth = config.maxHealth;

        float baseInterval = 1f / Mathf.Max(0.01f, config.attackSpeed);
        attackTimer = Random.Range(0f, baseInterval);

        surroundBias = Random.Range(-1f, 1f);

        targetSearchTimer = Random.Range(0f, targetSearchInterval);
        separationTimer = Random.Range(0f, separationRecalcInterval);
        crowdCheckTimer = Random.Range(0f, crowdCheckInterval);

        BattleManager.Instance.RegisterUnit(this);
    }

    void OnDisable()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.UnregisterUnit(this);
        }
    }

    void Update()
    {
        if (!IsAlive)
            return;

        float dt = Time.deltaTime;

        targetSearchTimer -= dt;
        if (targetSearchTimer <= 0f || currentTarget == null || !IsTargetStillValid(currentTarget))
        {
            targetSearchTimer = targetSearchInterval;

            if (BattleManager.Instance != null)
            {
                currentTarget = BattleManager.Instance.FindBestTargetFor(
                    this,
                    out currentTargetIsMelee,
                    out currentMeleeCrowdTarget);
            }
            else
            {
                currentTarget = null;
            }
        }

        if (currentTarget == null)
        {
            StopMoving();
            return;
        }

        separationTimer -= dt;
        if (separationTimer <= 0f && BattleManager.Instance != null)
        {
            cachedSeparation = BattleManager.Instance.GetSeparationDirection(this);
            separationTimer = separationRecalcInterval;
        }

        bool crowded = false;
        crowdCheckTimer -= dt;
        if (currentTargetIsMelee &&
            currentMeleeCrowdTarget != null &&
            BattleManager.Instance != null)
        {
            if (crowdCheckTimer <= 0f)
            {
                lastCrowdCheckResult =
                    BattleManager.Instance.IsTargetCrowded(this, currentMeleeCrowdTarget);
                crowdCheckTimer = crowdCheckInterval;
            }

            crowded = lastCrowdCheckResult;
        }

        Vector3 targetPos = currentTarget.position;

        if (IsInAttackRange(targetPos))
        {
            Attack(currentTarget);
        }
        else
        {
            if (!crowded)
            {
                MoveTowards(targetPos);
            }
            else
            {
                StopMoving();
            }
        }

        if (team == Team.Sphere && modelTransform != null && isMoving)
        {
            modelTransform.Rotate(Vector3.forward * sphereRollSpeed * dt);
        }
    }

    private bool IsTargetStillValid(Transform t)
    {
        if (t == null) return false;

        var m = t.GetComponentInParent<MeleeUnit>();
        if (m != null && m.IsAlive && m.team != team) return true;

        var a = t.GetComponentInParent<ArcherUnit>();
        if (a != null && a.IsAlive && a.team != team) return true;

        var c = t.GetComponentInParent<CommanderUnit>();
        if (c != null && c.IsAlive && c.team != team) return true;

        return false;
    }

    private void PlayHitSound()
    {
        if (audioSource == null) return;
        if (config == null || config.hitSfx == null) return;

        audioSource.PlayOneShot(config.hitSfx, hitVolume);
    }

    public void MoveTowards(Vector3 targetPos)
    {
        isMoving = true;

        Vector3 myPos = transform.position;
        Vector3 toTarget = targetPos - myPos;

        float attackDist = config.attackRange * 0.9f;
        float dist = toTarget.magnitude;

        Vector3 dir;

        if (dist > attackDist * 1.5f)
        {
            dir = toTarget.normalized;
        }
        else
        {
            Vector3 radialDir = toTarget.normalized;
            Vector3 tangentDir = new Vector3(-radialDir.z, 0f, radialDir.x);
            float side = surroundBias >= 0f ? 1f : -1f;
            Vector3 orbitDir = (radialDir + tangentDir * side * 0.7f).normalized;
            dir = orbitDir;
        }

        if (cachedSeparation.sqrMagnitude > 0.0001f)
        {
            dir += cachedSeparation;
            dir.Normalize();
        }

        transform.position = myPos + dir * config.moveSpeed * Time.deltaTime;

        StartMoveAnimation();
    }

    public void StopMoving()
    {
        if (!isMoving)
            return;

        isMoving = false;
        StopMoveAnimation();
    }

    public bool IsInAttackRange(Vector3 targetPos)
    {
        float dist = Vector3.Distance(transform.position, targetPos);
        return dist <= config.attackRange;
    }

    public void Attack(Transform target)
    {
        if (target == null)
            return;

        StopMoving();

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f)
            return;

        PlayAttackAnimation();

        float baseInterval = 1f / Mathf.Max(0.01f, config.attackSpeed);

        float speedFactor = 1f;
        if (config.attackSpeedRandomPercent > 0f)
        {
            float min = 1f - config.attackSpeedRandomPercent;
            float max = 1f + config.attackSpeedRandomPercent;
            speedFactor = Random.Range(min, max);
        }

        attackTimer = baseInterval * speedFactor;

        float randomFactor = 1f;
        if (config.damageRandomPercent > 0f)
        {
            float min = 1f - config.damageRandomPercent;
            float max = 1f + config.damageRandomPercent;
            randomFactor = Random.Range(min, max);
        }

        float finalDamage = config.attackDamage * randomFactor;

        MeleeUnit melee = target.GetComponentInParent<MeleeUnit>();
        if (melee != null && melee.IsAlive && melee.team != team)
        {
            melee.TakeDamage(finalDamage);
            return;
        }

        ArcherUnit archer = target.GetComponentInParent<ArcherUnit>();
        if (archer != null && archer.IsAlive && archer.team != team)
        {
            archer.TakeDamage(finalDamage);
            return;
        }

        CommanderUnit commander = target.GetComponentInParent<CommanderUnit>();
        if (commander != null && commander.IsAlive && commander.team != team)
        {
            commander.TakeDamage(finalDamage);
            return;
        }
    }

    public void TakeDamage(float amount)
    {
        if (config.dodgeChance > 0f)
        {
            float roll = Random.value;
            if (roll < config.dodgeChance)
            {
                return;
            }
        }

        currentHealth -= amount;

        PlayHitAnimation();
        PlayHitSound();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isMoving = false;
        StopAllAnimations();

        if (config.deathVfxPrefab != null && VfxPool.Instance != null)
        {
            Vector3 vfxPos = transform.position + Vector3.up * 0.1f;
            VfxPool.Instance.PlayOneShot(config.deathVfxPrefab, vfxPos, config.deathVfxLifetime);
        }

        gameObject.SetActive(false);
    }

    private void StartMoveAnimation()
    {
        if (modelTransform == null)
            return;

        if (team == Team.Cube)
        {
            if (moveTween != null && moveTween.IsActive() && moveTween.IsPlaying())
                return;

            moveTween = modelTransform
                .DOLocalMoveY(cubeHopHeight, cubeHopDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    private void StopMoveAnimation()
    {
        if (moveTween != null && moveTween.IsActive())
        {
            moveTween.Kill();
            moveTween = null;
        }

        if (modelTransform != null)
        {
            Vector3 lp = modelTransform.localPosition;
            lp.y = 0f;
            modelTransform.localPosition = lp;
        }
    }

    private void PlayAttackAnimation()
    {
        if (modelTransform == null)
            return;

        if (attackTween != null && attackTween.IsActive())
            attackTween.Kill();

        Vector3 punch = transform.forward * 0.25f;

        attackTween = modelTransform
            .DOPunchPosition(punch, 0.2f, 5, 0.5f);
    }

    private void PlayHitAnimation()
    {
        if (modelTransform == null)
            return;

        if (hitTween != null && hitTween.IsActive())
            hitTween.Kill();

        hitTween = modelTransform
            .DOShakePosition(0.15f, 0.1f, 10, 90f);
    }

    private void StopAllAnimations()
    {
        if (moveTween != null && moveTween.IsActive()) moveTween.Kill();
        if (attackTween != null && attackTween.IsActive()) attackTween.Kill();
        if (hitTween != null && hitTween.IsActive()) hitTween.Kill();

        moveTween = attackTween = hitTween = null;
    }
}
