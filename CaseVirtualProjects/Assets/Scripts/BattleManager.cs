using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    Cube,
    Sphere
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Separation (Takım Arkadaşlarından Uzaklaşma)")]
    public float separationRadius = 0.8f;
    public float separationStrength = 0.5f;

    [Header("Melee Kalabalık Kontrolü (Sıra Bekleme)")]
    public int maxAttackersPerTarget = 6;
    public float crowdRangeMultiplier = 1.2f;

    private readonly List<MeleeUnit> cubes = new List<MeleeUnit>(128);
    private readonly List<MeleeUnit> spheres = new List<MeleeUnit>(128);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterUnit(MeleeUnit unit)
    {
        if (unit == null) return;

        if (unit.team == Team.Cube)
        {
            if (!cubes.Contains(unit))
                cubes.Add(unit);
        }
        else
        {
            if (!spheres.Contains(unit))
                spheres.Add(unit);
        }
    }

    public void UnregisterUnit(MeleeUnit unit)
    {
        if (unit == null) return;

        if (unit.team == Team.Cube)
            cubes.Remove(unit);
        else
            spheres.Remove(unit);
    }

    public void UpdateUnit(MeleeUnit unit)
    {
        if (unit == null || !unit.IsAlive)
            return;

        List<MeleeUnit> enemies = unit.team == Team.Cube ? spheres : cubes;
        if (enemies.Count == 0)
        {
            unit.StopMoving();
            return;
        }

        MeleeUnit closest = null;
        float closestDist = float.MaxValue;
        Vector3 myPos = unit.transform.position;

        for (int i = 0; i < enemies.Count; i++)
        {
            MeleeUnit enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            float dist = Vector3.SqrMagnitude(enemy.transform.position - myPos);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        if (closest == null)
        {
            unit.StopMoving();
            return;
        }

        Vector3 targetPos = closest.transform.position;

        if (unit.IsInAttackRange(targetPos))
        {
            unit.Attack(closest);
        }
        else
        {
            bool crowded = IsTargetCrowded(unit, closest);
            if (!crowded)
            {
                unit.MoveTowards(targetPos);
            }
            else
            {
                unit.StopMoving();
            }
        }
    }

    private bool IsTargetCrowded(MeleeUnit requester, MeleeUnit target)
    {
        List<MeleeUnit> sameTeam = requester.team == Team.Cube ? cubes : spheres;

        int count = 0;
        float allowedRange = requester.config.attackRange * crowdRangeMultiplier;
        float allowedRangeSqr = allowedRange * allowedRange;
        Vector3 targetPos = target.transform.position;

        for (int i = 0; i < sameTeam.Count; i++)
        {
            MeleeUnit ally = sameTeam[i];
            if (ally == null || !ally.IsAlive)
                continue;

            float sqrDist = Vector3.SqrMagnitude(ally.transform.position - targetPos);
            if (sqrDist <= allowedRangeSqr)
            {
                count++;
                if (count >= maxAttackersPerTarget)
                    return true;
            }
        }

        return false;
    }

    public Vector3 GetSeparationDirection(MeleeUnit unit)
    {
        Vector3 myPos = unit.transform.position;
        Vector3 separation = Vector3.zero;
        int neighborCount = 0;

        float radius = separationRadius;
        float radiusSqr = radius * radius;

        List<MeleeUnit> sameTeam = unit.team == Team.Cube ? cubes : spheres;

        for (int i = 0; i < sameTeam.Count; i++)
        {
            MeleeUnit other = sameTeam[i];
            if (other == null || other == unit || !other.IsAlive)
                continue;

            Vector3 diff = myPos - other.transform.position;
            float sqrDist = diff.sqrMagnitude;

            if (sqrDist > 0.0001f && sqrDist < radiusSqr)
            {
                separation += diff / sqrDist;
                neighborCount++;
            }
        }

        List<MeleeUnit> enemies = unit.team == Team.Cube ? spheres : cubes;

        for (int i = 0; i < enemies.Count; i++)
        {
            MeleeUnit other = enemies[i];
            if (other == null || !other.IsAlive)
                continue;

            Vector3 diff = myPos - other.transform.position;
            float sqrDist = diff.sqrMagnitude;

            if (sqrDist > 0.0001f && sqrDist < radiusSqr)
            {
                separation += diff / sqrDist;
                neighborCount++;
            }
        }

        if (neighborCount == 0)
            return Vector3.zero;

        separation /= neighborCount;
        separation.y = 0f;

        if (separation == Vector3.zero)
            return Vector3.zero;

        separation = separation.normalized * separationStrength;
        return separation;
    }

}
