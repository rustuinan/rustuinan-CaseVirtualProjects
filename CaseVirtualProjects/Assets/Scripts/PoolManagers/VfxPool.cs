using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VfxPool : MonoBehaviour
{
    public static VfxPool Instance { get; private set; }

    [System.Serializable]
    public class VfxEntry
    {
        public GameObject prefab;
        public int initialCount = 10;
    }

    [Header("Önceden Hazırlanacak VFX'ler (Opsiyonel)")]
    public VfxEntry[] prewarmVfx;

    private Dictionary<GameObject, Queue<GameObject>> pools =
        new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Prewarm();
    }

    private void Prewarm()
    {
        if (prewarmVfx == null) return;

        for (int i = 0; i < prewarmVfx.Length; i++)
        {
            var entry = prewarmVfx[i];
            if (entry.prefab == null || entry.initialCount <= 0)
                continue;

            if (!pools.ContainsKey(entry.prefab))
                pools.Add(entry.prefab, new Queue<GameObject>());

            for (int j = 0; j < entry.initialCount; j++)
            {
                GameObject obj = Instantiate(entry.prefab, transform);
                obj.SetActive(false);
                pools[entry.prefab].Enqueue(obj);
            }
        }
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        if (prefab == null) return null;

        if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools.Add(prefab, queue);
        }

        if (queue.Count > 0)
        {
            return queue.Dequeue();
        }
        else
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            return obj;
        }
    }

    private void ReturnToPool(GameObject prefab, GameObject obj)
    {
        if (prefab == null || obj == null) return;

        obj.SetActive(false);

        if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools.Add(prefab, queue);
        }

        queue.Enqueue(obj);
    }

    public void PlayOneShot(GameObject prefab, Vector3 position, float lifeTime)
    {
        if (prefab == null) return;

        GameObject obj = GetFromPool(prefab);
        if (obj == null) return;

        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
        obj.SetActive(true);

        StartCoroutine(ReturnAfterDelay(prefab, obj, lifeTime));
    }

    private IEnumerator ReturnAfterDelay(GameObject prefab, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(prefab, obj);
    }
}
