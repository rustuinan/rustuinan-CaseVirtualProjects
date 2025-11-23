using UnityEngine;
using DG.Tweening;

public class ArcherUnit : MonoBehaviour
{
    [Header("Config")]
    public UnitConfig config;
    public Team team;

    [Header("Visual")]
    public Transform modelTransform;
    public float shootHeightOffset = 0.5f;
    public float shootPunchDistance = 0.3f;

    [Header("Target Search")]
    public LayerMask unitLayer;
    public float searchRadius = 60f;
    public float targetSearchInterval = 0.3f;

    [Header("Separation (İç İçe Girmeyi Azaltma)")]
    public float separationRadius = 1.2f;
    public float separationStrength = 0.7f;

    private float currentHealth;
    private float attackTimer;
    private float targetSearchTimer;
    private Transform currentTarget;

    private bool inMeleeMode = false;

    private Tween shootTween;
    private Tween hitTween;

    public bool IsAlive => currentHealth > 0f;

    private void Start()
    {
        currentHealth = config.maxHealth;

        float baseInterval = 1f / Mathf.Max(0.01f, config.attackSpeed);
        attackTimer = Random.Range(0f, baseInterval);

        targetSearchTimer = Random.Range(0f, targetSearchInterval);

        if (BattleManager.Instance != null)
            BattleManager.Instance.RegisterArcher(this);
    }

    private void OnDisable()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.UnregisterArcher(this);
    }

    private void Update()
    {
        if (!IsAlive)
            return;

        float dt = Time.deltaTime;
        attackTimer -= dt;

        targetSearchTimer -= dt;
        if (targetSearchTimer <= 0f || currentTarget == null)
        {
            targetSearchTimer = targetSearchInterval;
            currentTarget = FindClosestEnemy();
        }

        if (currentTarget == null)
            return;

        Vector3 myPos = transform.position;
        Vector3 targetPos = currentTarget.position;
        float dist = Vector3.Distance(myPos, targetPos);

        float rangedRadius = config.attackRange;
        float meleeRadius = config.fallbackMeleeRange;

        if (!config.canFallbackMelee)
            meleeRadius = 0f;

        if (meleeRadius > 0f && dist <= meleeRadius + 0.1f)
        {
            inMeleeMode = true;
        }

        if (inMeleeMode)
        {
            MoveTowards(targetPos, dt);

            DoFallbackMeleeAttack();
            return;
        }


        if (dist > rangedRadius)
        {
            MoveTowards(targetPos, dt);
            return;
        }

        ApplySeparationOnly(dt);
        DoRangedAttack(targetPos);
    }

    private Transform FindClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, unitLayer);

        Transform closest = null;
        float closestSqr = float.MaxValue;
        Vector3 myPos = transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];

            MeleeUnit melee = col.GetComponentInParent<MeleeUnit>();
            ArcherUnit archer = col.GetComponentInParent<ArcherUnit>();
            CommanderUnit commander = col.GetComponentInParent<CommanderUnit>();

            if (melee != null && melee.IsAlive && melee.team != team)
            {
                float sqr = (melee.transform.position - myPos).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = melee.transform;
                }
                continue;
            }

            if (archer != null && archer.IsAlive && archer.team != team)
            {
                float sqr = (archer.transform.position - myPos).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = archer.transform;
                }
                continue;
            }

            if (commander != null && commander.IsAlive && commander.team != team)
            {
                float sqr = (commander.transform.position - myPos).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = commander.transform;
                }
            }
        }

        return closest;
    }

    private void MoveTowards(Vector3 targetPos, float dt)
    {
        Vector3 myPos = transform.position;
        Vector3 dir = (targetPos - myPos).normalized;

        Vector3 separation = ComputeSeparation();
        if (separation.sqrMagnitude > 0.0001f)
        {
            dir += separation;
            dir.Normalize();
        }

        transform.position = myPos + dir * config.moveSpeed * dt;
    }

    private void ApplySeparationOnly(float dt)
    {
        Vector3 myPos = transform.position;
        Vector3 sep = ComputeSeparation();
        if (sep.sqrMagnitude > 0.0001f)
        {
            Vector3 pos = myPos + sep * config.moveSpeed * dt * 0.5f;
            transform.position = pos;
        }
    }

    private Vector3 ComputeSeparation()
    {
        Vector3 myPos = transform.position;
        Collider[] hits = Physics.OverlapSphere(myPos, separationRadius, unitLayer);

        Vector3 separation = Vector3.zero;
        int count = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];

            if (col.transform == this.transform || col.transform.IsChildOf(this.transform))
                continue;

            MeleeUnit melee = col.GetComponentInParent<MeleeUnit>();
            if (melee != null && melee.IsAlive && melee.team == team)
            {
                Vector3 diff = myPos - melee.transform.position;
                float sqr = diff.sqrMagnitude;
                if (sqr > 0.0001f)
                {
                    separation += diff / sqr;
                    count++;
                }
                continue;
            }

            ArcherUnit archer = col.GetComponentInParent<ArcherUnit>();
            if (archer != null && archer.IsAlive && archer.team == team)
            {
                Vector3 diff = myPos - archer.transform.position;
                float sqr = diff.sqrMagnitude;
                if (sqr > 0.0001f)
                {
                    separation += diff / sqr;
                    count++;
                }
                continue;
            }

            CommanderUnit commander = col.GetComponentInParent<CommanderUnit>();
            if (commander != null && commander.IsAlive && commander.team == team)
            {
                Vector3 diff = myPos - commander.transform.position;
                float sqr = diff.sqrMagnitude;
                if (sqr > 0.0001f)
                {
                    separation += diff / sqr;
                    count++;
                }
                continue;
            }
        }

        if (count == 0)
            return Vector3.zero;

        separation /= count;
        separation.y = 0f;

        if (separation == Vector3.zero)
            return Vector3.zero;

        return separation.normalized * separationStrength;
    }

    private void DoRangedAttack(Vector3 targetPos)
    {
        if (attackTimer > 0f)
            return;

        float baseInterval = 1f / Mathf.Max(0.01f, config.attackSpeed);
        attackTimer = baseInterval;

        ArrowProjectile proj = ProjectilePool.Instance.Get();
        if (proj == null)
            return;

        Vector3 shootPos = transform.position + Vector3.up * shootHeightOffset;

        float randomFactor = 1f;
        if (config.damageRandomPercent > 0f)
        {
            float min = 1f - config.damageRandomPercent;
            float max = 1f + config.damageRandomPercent;
            randomFactor = Random.Range(min, max);
        }

        float finalDamage = config.attackDamage * randomFactor;

        proj.Launch(shootPos, targetPos, finalDamage, team);

        PlayShootAnimation();
    }

    private void DoFallbackMeleeAttack()
    {
        if (!config.canFallbackMelee)
            return;

        if (attackTimer > 0f)
            return;

        float baseInterval = 1f / Mathf.Max(0.01f, config.attackSpeed);
        attackTimer = baseInterval;

        PlayHitAnimation();

        float dmg = config.fallbackMeleeDamage > 0f
            ? config.fallbackMeleeDamage
            : config.attackDamage * 0.3f;

        Collider[] hits = Physics.OverlapSphere(transform.position, config.fallbackMeleeRange, unitLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];

            MeleeUnit melee = col.GetComponentInParent<MeleeUnit>();
            if (melee != null && melee.IsAlive && melee.team != team)
            {
                melee.TakeDamage(dmg);
                return;
            }

            ArcherUnit archer = col.GetComponentInParent<ArcherUnit>();
            if (archer != null && archer.IsAlive && archer.team != team)
            {
                archer.TakeDamage(dmg);
                return;
            }

            CommanderUnit commander = col.GetComponentInParent<CommanderUnit>();
            if (commander != null && commander.IsAlive && commander.team != team)
            {
                commander.TakeDamage(dmg);
                return;
            }
        }
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
        PlayHitAnimation();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (shootTween != null && shootTween.IsActive()) shootTween.Kill();
        if (hitTween != null && hitTween.IsActive()) hitTween.Kill();

        gameObject.SetActive(false);
    }

    private void PlayShootAnimation()
    {
        if (modelTransform == null)
            return;

        if (shootTween != null && shootTween.IsActive())
            shootTween.Kill();

        Vector3 punch = transform.forward * shootPunchDistance;

        shootTween = modelTransform
            .DOPunchPosition(punch, 0.15f, 5, 0.5f);
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
}
