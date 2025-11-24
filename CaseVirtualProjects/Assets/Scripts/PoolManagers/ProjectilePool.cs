using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [Header("Pool Ayarları")]
    public ArrowProjectile projectilePrefab;
    public int initialCount = 50;

    private readonly Queue<ArrowProjectile> pool = new Queue<ArrowProjectile>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        for (int i = 0; i < initialCount; i++)
        {
            ArrowProjectile proj = Instantiate(projectilePrefab, transform);
            proj.gameObject.SetActive(false);
            pool.Enqueue(proj);
        }
    }

    public ArrowProjectile Get()
    {
        ArrowProjectile proj;

        if (pool.Count > 0)
        {
            proj = pool.Dequeue();
        }
        else
        {
            proj = Instantiate(projectilePrefab, transform);
        }

        proj.gameObject.SetActive(true);
        return proj;
    }

    public void Return(ArrowProjectile proj)
    {
        proj.gameObject.SetActive(false);
        pool.Enqueue(proj);
    }
}
