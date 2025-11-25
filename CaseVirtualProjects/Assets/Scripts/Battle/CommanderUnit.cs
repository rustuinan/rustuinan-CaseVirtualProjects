using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CommanderUnit : MonoBehaviour
{
    public static List<CommanderUnit> All = new List<CommanderUnit>();

    [Header("Config")]
    public UnitConfig config;
    public Team team;

    [Header("Visual")]
    public Transform model;

    [Header("Detection")]
    public LayerMask unitLayer;
    public float detectionRadius = 40f;

    [Header("Separation")]
    public float separationRadius = 2f;
    public float separationStrength = 1f;

    [Header("Sphere Line Bash Extra")]
    public float sphereStopOffset = 1.5f;

    [Header("Melee Attack Visual")]
    public float cubeAttackPunchDistance = 0.7f;
    public float sphereAttackPunchDistance = 0.5f;
    public float attackPunchDuration = 0.2f;
    public int attackPunchVibrato = 8;
    [Range(0f, 1f)] public float attackPunchElasticity = 0.5f;

    [Header("Cube Slam Visual")]
    public float cubeSlamJumpHeight = 2.5f;
    public float cubeSlamUpTime = 0.25f;
    public float cubeSlamDownTime = 0.2f;

    [Header("Cube Slam Knockback")]
    public float cubeSlamKnockbackDistance = 6f;

    [Header("Audio")]
    public AudioSource audioSource;
    [Range(0f, 1f)] public float hitVolume = 0.15f;

    private float currentHealth;
    private float attackTimer;
    private float skillCooldownTimer;

    private Transform currentTarget;

    private enum CommanderState
    {
        Idle,
        Moving,
        Attacking,
        UsingSkill
    }

    private CommanderState state;

    private Vector3 originalModelLocalPos;
    private Tween skillTween;
    private Tween attackTween;

    public bool IsAlive => currentHealth > 0f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Model atanmadıysa otomatik bir child seçmeye çalış
        if (model == null && transform.childCount > 0)
            model = transform.GetChild(0);
    }

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Start()
    {
        currentHealth = config.maxHealth;
        attackTimer = Random.Range(0f, 1f / Mathf.Max(0.01f, config.attackSpeed));
        state = CommanderState.Idle;

        if (model == null && transform.childCount > 0)
            model = transform.GetChild(0);

        if (model != null)
            originalModelLocalPos = model.localPosition;

        // Komutan biraz büyük gözüksün
        transform.localScale = Vector3.one * 1.5f;
    }

    private void Update()
    {
        if (!IsAlive)
            return;

        float dt = Time.deltaTime;

        attackTimer -= dt;
        skillCooldownTimer -= dt;

        if (state == CommanderState.UsingSkill)
            return;

        UpdateTarget();

        if (currentTarget == null)
        {
            state = CommanderState.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.position);

        // CUBE SKILL (SLAM)
        if (team == Team.Cube)
        {
            if (skillCooldownTimer <= 0f && HasEnemyInRadius(config.slamRadius * 0.9f))
            {
                DoCubeSlam();
                return;
            }
        }
        // SPHERE SKILL (LINE BASH)
        else if (team == Team.Sphere)
        {
            if (skillCooldownTimer <= 0f)
            {
                float distToTarget = Vector3.Distance(transform.position, currentTarget.position);

                if (distToTarget > 2f && distToTarget < config.chargeDistance * 1.5f)
                {
                    DoSphereLineBash();
                    return;
                }
            }
        }

        if (dist > config.attackRange)
        {
            MoveTowards(currentTarget.position, dt);
        }
        else
        {
            DoMeleeAttack();
        }
    }

    private void PlayHitSound()
    {
        if (audioSource == null) return;
        if (config == null || config.hitSfx == null) return;

        audioSource.PlayOneShot(config.hitSfx, hitVolume);
    }

    private void UpdateTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, unitLayer);

        Transform closest = null;
        float closestSqr = float.MaxValue;
        Vector3 myPos = transform.position;

        foreach (var col in hits)
        {
            Transform t = col.transform;

            if (!IsValidEnemy(t))
                continue;

            float sqr = (t.position - myPos).sqrMagnitude;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = t;
            }
        }

        currentTarget = closest;
    }

    private bool IsValidEnemy(Transform t)
    {
        var m = t.GetComponentInParent<MeleeUnit>();
        if (m != null && m.IsAlive && m.team != team) return true;

        var a = t.GetComponentInParent<ArcherUnit>();
        if (a != null && a.IsAlive && a.team != team) return true;

        var c = t.GetComponentInParent<CommanderUnit>();
        if (c != null && c.IsAlive && c != this && c.team != team) return true;

        return false;
    }

    private bool HasEnemyInRadius(float radius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, unitLayer);
        foreach (var col in hits)
        {
            if (IsValidEnemy(col.transform))
                return true;
        }
        return false;
    }

    private void MoveTowards(Vector3 targetPos, float dt)
    {
        state = CommanderState.Moving;

        Vector3 myPos = transform.position;
        Vector3 dir = (targetPos - myPos).normalized;

        Vector3 sep = ComputeSeparation();
        if (sep.sqrMagnitude > 0.0001f)
        {
            dir += sep;
            dir.Normalize();
        }

        transform.position = myPos + dir * config.moveSpeed * dt;
    }

    private Vector3 ComputeSeparation()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, separationRadius, unitLayer);

        Vector3 sep = Vector3.zero;
        int count = 0;
        Vector3 myPos = transform.position;

        foreach (var col in hits)
        {
            if (col.transform.IsChildOf(transform)) continue;

            Transform t = col.transform;

            var m = t.GetComponentInParent<MeleeUnit>();
            var a = t.GetComponentInParent<ArcherUnit>();
            var c = t.GetComponentInParent<CommanderUnit>();

            bool isUnit = (m != null) || (a != null) || (c != null);

            if (!isUnit)
                continue;

            Vector3 diff = myPos - t.position;
            float sqr = diff.sqrMagnitude;

            if (sqr > 0.01f && sqr < separationRadius * separationRadius)
            {
                sep += diff / sqr;
                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        sep /= count;
        sep.y = 0f;
        return sep.normalized * separationStrength;
    }

    private void DoMeleeAttack()
    {
        if (currentTarget == null)
            return;

        float dist = Vector3.Distance(transform.position, currentTarget.position);
        if (dist > config.attackRange)
            return;

        if (attackTimer > 0f)
            return;

        state = CommanderState.Attacking;

        attackTimer = 1f / Mathf.Max(0.01f, config.attackSpeed);

        float dmg = config.attackDamage;
        if (config.damageRandomPercent > 0f)
        {
            float min = 1f - config.damageRandomPercent;
            float max = 1f + config.damageRandomPercent;
            dmg *= Random.Range(min, max);
        }

        PlayMeleeAttackAnimation();
        TryDamage(currentTarget, dmg);
    }

    private void PlayMeleeAttackAnimation()
    {
        if (model == null)
            return;

        if (attackTween != null && attackTween.IsActive())
            attackTween.Kill();

        // Model kaydıysa resetle
        model.localPosition = originalModelLocalPos;

        float distance = (team == Team.Cube) ? cubeAttackPunchDistance : sphereAttackPunchDistance;
        float duration = attackPunchDuration;

        // Mümkünse hedefe doğru vur
        Vector3 dirWorld;

        if (currentTarget != null)
        {
            dirWorld = (currentTarget.position - model.position);
            dirWorld.y = 0f;

            if (dirWorld.sqrMagnitude < 0.0001f)
                dirWorld = transform.forward;
        }
        else
        {
            dirWorld = transform.forward;
        }

        dirWorld.Normalize();

        // World yönünü model local uzayına çevir
        Vector3 dirLocal = model.InverseTransformDirection(dirWorld).normalized;
        Vector3 punchLocal = dirLocal * distance;

        attackTween = model
            .DOPunchPosition(punchLocal, duration, attackPunchVibrato, attackPunchElasticity);
    }

    private bool TryDamage(Transform t, float dmg)
    {
        var m = t.GetComponentInParent<MeleeUnit>();
        if (m != null && m.IsAlive && m.team != team)
        {
            m.TakeDamage(dmg);
            return true;
        }

        var a = t.GetComponentInParent<ArcherUnit>();
        if (a != null && a.IsAlive && a.team != team)
        {
            a.TakeDamage(dmg);
            return true;
        }

        var c = t.GetComponentInParent<CommanderUnit>();
        if (c != null && c != this && c.IsAlive && c.team != team)
        {
            c.TakeDamage(dmg);
            return true;
        }

        return false;
    }

    private void DoCubeSlam()
    {
        state = CommanderState.UsingSkill;
        skillCooldownTimer = config.slamCooldown;

        if (attackTween != null && attackTween.IsActive())
            attackTween.Kill();

        if (skillTween != null && skillTween.IsActive())
            skillTween.Kill();

        if (model == null)
        {
            ApplyCubeSlamDamage();
            attackTimer = 0f;
            state = CommanderState.Idle;
            return;
        }

        // Model pozisyonunu garantiye al
        model.localPosition = originalModelLocalPos;

        float y0 = originalModelLocalPos.y;
        float jumpHeight = cubeSlamJumpHeight;
        float upTime = cubeSlamUpTime;
        float downTime = cubeSlamDownTime;

        skillTween = DOTween.Sequence()
            .Append(model.DOLocalMoveY(y0 + jumpHeight, upTime).SetEase(Ease.OutQuad))
            .Append(model.DOLocalMoveY(y0, downTime).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                ApplyCubeSlamDamage();
                attackTimer = 0f;
                state = CommanderState.Idle;
            });
    }

    private void ApplyCubeSlamDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, config.slamRadius, unitLayer);

        foreach (var col in hits)
        {
            Transform t = col.transform;

            if (!IsValidEnemy(t))
                continue;

            float dmg = config.slamDamage;
            if (config.damageRandomPercent > 0f)
            {
                float min = 1f - config.damageRandomPercent;
                float max = 1f + config.damageRandomPercent;
                dmg *= Random.Range(min, max);
            }

            if (TryDamage(t, dmg))
            {
                Transform root = GetUnitRoot(t);
                ApplyRadialKnockback(root, cubeSlamKnockbackDistance);
            }
        }
    }

    private void ApplyRadialKnockback(Transform target, float distance)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            dir = Random.insideUnitSphere;

        dir.y = 0f;
        dir.Normalize();

        Vector3 endPos = target.position + dir * distance;

        float jumpPower = 1.8f;
        float duration = 0.45f;

        target.DOJump(endPos, jumpPower, 1, duration)
              .SetEase(Ease.OutQuad);
    }

    private void DoSphereLineBash()
    {
        if (currentTarget == null)
        {
            state = CommanderState.Idle;
            return;
        }

        if (attackTween != null && attackTween.IsActive())
            attackTween.Kill();

        if (skillTween != null && skillTween.IsActive())
            skillTween.Kill();

        Vector3 dashStart = transform.position;
        Vector3 dir = currentTarget.position - dashStart;
        dir.y = 0f;

        float distToTarget = dir.magnitude;
        if (distToTarget < 1f)
        {
            state = CommanderState.Idle;
            return;
        }

        dir.Normalize();

        float travel = Mathf.Clamp(distToTarget - sphereStopOffset, 1f, config.chargeDistance);
        float dashDuration = travel / Mathf.Max(0.01f, config.chargeSpeed);
        Vector3 dashEnd = dashStart + dir * travel;

        state = CommanderState.UsingSkill;
        skillCooldownTimer = config.chargeCooldown;

        float backDistance = 0.8f;
        Vector3 backPos = dashStart - dir * backDistance;
        float backTime = 0.12f;

        skillTween = DOTween.Sequence()
            .Append(transform.DOMove(backPos, backTime).SetEase(Ease.InQuad))
            .Append(transform.DOMove(dashEnd, dashDuration).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                ApplySphereLineBashDamage(dashEnd, dir);
                attackTimer = 0f;
                state = CommanderState.Idle;
            });
    }

    private void ApplySphereLineBashDamage(Vector3 impactPos, Vector3 dir)
    {
        float radius = config.chargeRadius * 1.5f;

        Collider[] hits = Physics.OverlapSphere(impactPos, radius, unitLayer);

        foreach (var col in hits)
        {
            Transform t = col.transform;

            if (!IsValidEnemy(t))
                continue;

            float dmg = config.chargeDamage;
            if (config.damageRandomPercent > 0f)
            {
                float min = 1f - config.damageRandomPercent;
                float max = 1f + config.damageRandomPercent;
                dmg *= Random.Range(min, max);
            }

            if (TryDamage(t, dmg))
            {
                Transform root = GetUnitRoot(t);
                ApplyLinearKnockback(root, dir, config.chargeKnockbackDistance);
            }
        }
    }

    private void ApplyLinearKnockback(Transform target, Vector3 dir, float distance)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = (target.position - transform.position);

        dir.y = 0f;
        dir.Normalize();

        Vector3 endPos = target.position + dir * distance;

        target.DOMove(endPos, 0.3f).SetEase(Ease.OutQuad);
    }

    private Transform GetUnitRoot(Transform t)
    {
        var m = t.GetComponentInParent<MeleeUnit>();
        if (m != null) return m.transform;

        var a = t.GetComponentInParent<ArcherUnit>();
        if (a != null) return a.transform;

        var c = t.GetComponentInParent<CommanderUnit>();
        if (c != null) return c.transform;

        return t;
    }

    public void TakeDamage(float amount)
    {
        if (config.dodgeChance > 0f)
        {
            float roll = Random.value;
            if (roll < config.dodgeChance)
                return;
        }

        currentHealth -= amount;
        PlayHitSound();
        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (skillTween != null && skillTween.IsActive())
            skillTween.Kill();

        if (attackTween != null && attackTween.IsActive())
            attackTween.Kill();

        state = CommanderState.Idle;

        if (config.deathVfxPrefab != null && VfxPool.Instance != null)
        {
            Vector3 vfxPos = transform.position + Vector3.up * 0.1f;
            VfxPool.Instance.PlayOneShot(config.deathVfxPrefab, vfxPos, config.deathVfxLifetime);
        }

        gameObject.SetActive(false);
    }
}
