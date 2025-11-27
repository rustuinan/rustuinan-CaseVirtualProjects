using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PooledVFX : MonoBehaviour
{
    private ParticleSystem[] systems;

    private void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        if (systems == null) return;

        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Clear(true);
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            systems[i].Play(true);
        }
    }
}
