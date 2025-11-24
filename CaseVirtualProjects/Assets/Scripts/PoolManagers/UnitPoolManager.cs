using System.Collections.Generic;
using UnityEngine;

public enum UnitKind
{
    CubeMelee,
    CubeArcher,
    CubeCommander,
    SphereMelee,
    SphereArcher,
    SphereCommander
}

[System.Serializable]
public class UnitPool
{
    public UnitKind kind;
    public GameObject prefab;
    public int initialSize = 200;

    [HideInInspector] public List<GameObject> instances = new List<GameObject>();
}

public class UnitPoolManager : MonoBehaviour
{
    public static UnitPoolManager Instance { get; private set; }

    [Header("Pool Ayarları")]
    public UnitPool[] pools;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (var pool in pools)
        {
            Prewarm(pool);
        }
    }

    private void Prewarm(UnitPool pool)
    {
        if (pool == null || pool.prefab == null)
            return;

        for (int i = 0; i < pool.initialSize; i++)
        {
            GameObject obj = Instantiate(pool.prefab, transform);
            obj.SetActive(false);
            pool.instances.Add(obj);
        }
    }

    private UnitPool GetPool(UnitKind kind)
    {
        for (int i = 0; i < pools.Length; i++)
        {
            if (pools[i].kind == kind)
                return pools[i];
        }

        return null;
    }

    public GameObject Get(UnitKind kind)
    {
        UnitPool pool = GetPool(kind);
        if (pool == null || pool.prefab == null)
            return null;

        for (int i = 0; i < pool.instances.Count; i++)
        {
            if (!pool.instances[i].activeSelf)
                return pool.instances[i];
        }

        GameObject obj = Instantiate(pool.prefab, transform);
        obj.SetActive(false);
        pool.instances.Add(obj);
        return obj;
    }
}
