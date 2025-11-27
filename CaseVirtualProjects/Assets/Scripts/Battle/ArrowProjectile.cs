using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 15f;
    public float arcHeight = 2f;
    public float maxLifeTime = 4f;

    [Header("Hit")]
    public float hitRadius = 0.6f;
    public LayerMask unitLayer;

    private float lifeTimer;
    private float damage;
    private Team ownerTeam;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float travelTime;
    private float elapsedTime;

    private ProjectilePool pool;

    private static readonly Collider[] hitResults = new Collider[32];

    private void Awake()
    {
        pool = ProjectilePool.Instance;
    }

    public void Launch(Vector3 from, Vector3 target, float dmg, Team team)
    {
        startPos = from;
        targetPos = target;
        damage = dmg;
        ownerTeam = team;

        elapsedTime = 0f;

        float distance = Vector3.Distance(startPos, targetPos);
        travelTime = distance / Mathf.Max(0.01f, speed);

        lifeTimer = maxLifeTime;

        Vector3 flatDir = (targetPos - startPos);
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flatDir.normalized);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        elapsedTime += dt;
        lifeTimer -= dt;

        if (lifeTimer <= 0f)
        {
            Despawn();
            return;
        }

        if (travelTime <= 0f)
        {
            Despawn();
            return;
        }

        float t = Mathf.Clamp01(elapsedTime / travelTime);

        Vector3 pos = Vector3.Lerp(startPos, targetPos, t);

        float height = 4f * arcHeight * t * (1f - t);
        pos.y += height;

        transform.position = pos;

        if (t >= 1f)
        {
            DoHit();
            Despawn();
        }
    }

    private void DoHit()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            hitRadius,
            hitResults,
            unitLayer);

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitResults[i];
            if (col == null) continue;

            MeleeUnit melee = col.GetComponentInParent<MeleeUnit>();
            if (melee != null && melee.IsAlive && melee.team != ownerTeam)
            {
                melee.TakeDamage(damage);
                return;
            }

            ArcherUnit archer = col.GetComponentInParent<ArcherUnit>();
            if (archer != null && archer.IsAlive && archer.team != ownerTeam)
            {
                archer.TakeDamage(damage);
                return;
            }
        }
    }

    private void Despawn()
    {
        if (pool == null)
            pool = ProjectilePool.Instance;

        if (pool != null)
            pool.Return(this);
        else
            gameObject.SetActive(false);
    }
}
