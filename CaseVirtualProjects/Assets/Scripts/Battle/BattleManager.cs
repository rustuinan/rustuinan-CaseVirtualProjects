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

    private readonly List<ArcherUnit> cubeArchers = new List<ArcherUnit>(128);
    private readonly List<ArcherUnit> sphereArchers = new List<ArcherUnit>(128);

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

    public void RegisterArcher(ArcherUnit archer)
    {
        if (archer == null) return;

        if (archer.team == Team.Cube)
        {
            if (!cubeArchers.Contains(archer))
                cubeArchers.Add(archer);
        }
        else
        {
            if (!sphereArchers.Contains(archer))
                sphereArchers.Add(archer);
        }
    }

    public void UnregisterArcher(ArcherUnit archer)
    {
        if (archer == null) return;

        if (archer.team == Team.Cube)
            cubeArchers.Remove(archer);
        else
            sphereArchers.Remove(archer);
    }

    public void UpdateUnit(MeleeUnit unit)
    {
        if (unit == null || !unit.IsAlive)
            return;

        Vector3 myPos = unit.transform.position;

        List<MeleeUnit> enemyMelee = unit.team == Team.Cube ? spheres : cubes;
        MeleeUnit closestMelee = null;
        float closestMeleeDist = float.MaxValue;

        for (int i = 0; i < enemyMelee.Count; i++)
        {
            MeleeUnit enemy = enemyMelee[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            float dist = (enemy.transform.position - myPos).sqrMagnitude;
            if (dist < closestMeleeDist)
            {
                closestMeleeDist = dist;
                closestMelee = enemy;
            }
        }

        List<ArcherUnit> enemyArchers = unit.team == Team.Cube ? sphereArchers : cubeArchers;
        ArcherUnit closestArcher = null;
        float closestArcherDist = float.MaxValue;

        for (int i = 0; i < enemyArchers.Count; i++)
        {
            ArcherUnit enemy = enemyArchers[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            float dist = (enemy.transform.position - myPos).sqrMagnitude;
            if (dist < closestArcherDist)
            {
                closestArcherDist = dist;
                closestArcher = enemy;
            }
        }

        CommanderUnit closestCommander = null;
        float closestCommanderDist = float.MaxValue;

        foreach (var cmd in CommanderUnit.All)
        {
            if (cmd == null || !cmd.IsAlive)
                continue;

            if (cmd.team == unit.team)
                continue;

            float dist = (cmd.transform.position - myPos).sqrMagnitude;
            if (dist < closestCommanderDist)
            {
                closestCommanderDist = dist;
                closestCommander = cmd;
            }
        }

        if (closestMelee == null && closestArcher == null && closestCommander == null)
        {
            unit.StopMoving();
            return;
        }

        Transform finalTarget = null;
        bool finalIsMelee = false;
        float bestDist = float.MaxValue;

        if (closestMelee != null)
        {
            bestDist = closestMeleeDist;
            finalTarget = closestMelee.transform;
            finalIsMelee = true;
        }

        if (closestArcher != null && closestArcherDist < bestDist)
        {
            bestDist = closestArcherDist;
            finalTarget = closestArcher.transform;
            finalIsMelee = false;
        }

        if (closestCommander != null && closestCommanderDist < bestDist)
        {
            bestDist = closestCommanderDist;
            finalTarget = closestCommander.transform;
            finalIsMelee = false;
        }

        if (finalTarget == null)
        {
            unit.StopMoving();
            return;
        }

        Vector3 targetPos = finalTarget.position;

        if (unit.IsInAttackRange(targetPos))
        {
            unit.Attack(finalTarget);
        }
        else
        {
            bool crowded = false;

            if (finalIsMelee && closestMelee != null)
            {
                crowded = IsTargetCrowded(unit, closestMelee);
            }

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

    public Transform FindBestTargetFor(
        MeleeUnit unit,
        out bool finalIsMelee,
        out MeleeUnit meleeTarget)
    {
        finalIsMelee = false;
        meleeTarget = null;

        if (unit == null || !unit.IsAlive)
            return null;

        Vector3 myPos = unit.transform.position;

        List<MeleeUnit> enemyMelee = unit.team == Team.Cube ? spheres : cubes;
        MeleeUnit closestMelee = null;
        float closestMeleeDist = float.MaxValue;

        for (int i = 0; i < enemyMelee.Count; i++)
        {
            MeleeUnit enemy = enemyMelee[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            float dist = (enemy.transform.position - myPos).sqrMagnitude;
            if (dist < closestMeleeDist)
            {
                closestMeleeDist = dist;
                closestMelee = enemy;
            }
        }

        List<ArcherUnit> enemyArchers = unit.team == Team.Cube ? sphereArchers : cubeArchers;
        ArcherUnit closestArcher = null;
        float closestArcherDist = float.MaxValue;

        for (int i = 0; i < enemyArchers.Count; i++)
        {
            ArcherUnit enemy = enemyArchers[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            float dist = (enemy.transform.position - myPos).sqrMagnitude;
            if (dist < closestArcherDist)
            {
                closestArcherDist = dist;
                closestArcher = enemy;
            }
        }

        CommanderUnit closestCommander = null;
        float closestCommanderDist = float.MaxValue;

        foreach (var cmd in CommanderUnit.All)
        {
            if (cmd == null || !cmd.IsAlive)
                continue;

            if (cmd.team == unit.team)
                continue;

            float dist = (cmd.transform.position - myPos).sqrMagnitude;
            if (dist < closestCommanderDist)
            {
                closestCommanderDist = dist;
                closestCommander = cmd;
            }
        }

        if (closestMelee == null && closestArcher == null && closestCommander == null)
            return null;

        Transform finalTarget = null;
        float bestDist = float.MaxValue;
        bool isMelee = false;
        MeleeUnit meleeForCrowd = null;

        if (closestMelee != null)
        {
            bestDist = closestMeleeDist;
            finalTarget = closestMelee.transform;
            isMelee = true;
            meleeForCrowd = closestMelee;
        }

        if (closestArcher != null && closestArcherDist < bestDist)
        {
            bestDist = closestArcherDist;
            finalTarget = closestArcher.transform;
            isMelee = false;
            meleeForCrowd = null;
        }

        if (closestCommander != null && closestCommanderDist < bestDist)
        {
            bestDist = closestCommanderDist;
            finalTarget = closestCommander.transform;
            isMelee = false;
            meleeForCrowd = null;
        }

        finalIsMelee = isMelee;
        meleeTarget = meleeForCrowd;
        return finalTarget;
    }

    public bool IsTargetCrowded(MeleeUnit requester, MeleeUnit target)
    {
        if (target == null) return false;

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

            float sqrDist = (ally.transform.position - targetPos).sqrMagnitude;
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

    public Transform FindClosestEnemyTarget(Team team, Vector3 fromPos)
    {
        List<MeleeUnit> enemies = team == Team.Cube ? spheres : cubes;

        MeleeUnit closest = null;
        float closestSqr = float.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            MeleeUnit e = enemies[i];
            if (e == null || !e.IsAlive)
                continue;

            float sqr = (e.transform.position - fromPos).sqrMagnitude;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = e;
            }
        }

        return closest != null ? closest.transform : null;
    }
}
