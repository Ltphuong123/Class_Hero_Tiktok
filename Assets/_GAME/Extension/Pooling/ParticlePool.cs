using System.Collections.Generic;
using UnityEngine;

public static class ParticlePool
{
    private static Dictionary<ParticleType, ParticlePoolInstance> pools = new Dictionary<ParticleType, ParticlePoolInstance>();
    private static bool isQuitting = false;

    public static void Preload(ParticleUnit prefab, int amount, Transform parent)
    {
        if (prefab == null || isQuitting)
        {
            if (!isQuitting) Debug.LogWarning("[ParticlePool] Prefab is null");
            return;
        }

        if (!pools.ContainsKey(prefab.ParticleType) || pools[prefab.ParticleType] == null)
        {
            ParticlePoolInstance pool = new ParticlePoolInstance();
            pool.Preload(prefab, amount, parent);
            pools[prefab.ParticleType] = pool;
        }
    }

    public static ParticleUnit Spawn(ParticleType type, Vector3 position)
    {
        return Spawn(type, position, Quaternion.identity);
    }

    public static ParticleUnit Spawn(ParticleType type, Vector3 position, Quaternion rotation)
    {
        if (isQuitting) return null;

        if (!pools.ContainsKey(type))
        {
            Debug.LogWarning($"[ParticlePool] {type} is not preloaded");
            return null;
        }
        return pools[type].Spawn(position, rotation);
    }

    public static T Spawn<T>(ParticleType type, Vector3 position, Quaternion rotation) where T : ParticleUnit
    {
        return Spawn(type, position, rotation) as T;
    }

    public static void Despawn(ParticleUnit unit)
    {
        if (unit == null || isQuitting) return;

        if (!pools.ContainsKey(unit.ParticleType))
        {
            if (!isQuitting) Debug.LogWarning($"[ParticlePool] {unit.ParticleType} is not preloaded");
            return;
        }
        pools[unit.ParticleType].Despawn(unit);
    }

    public static void Collect(ParticleType type)
    {
        if (isQuitting) return;

        if (!pools.ContainsKey(type))
        {
            Debug.LogWarning($"[ParticlePool] {type} is not preloaded");
            return;
        }
        pools[type].Collect();
    }

    public static void CollectAll()
    {
        if (isQuitting) return;

        foreach (var pool in pools.Values)
            pool.Collect();
    }

    public static void Release(ParticleType type)
    {
        if (isQuitting) return;

        if (!pools.ContainsKey(type))
        {
            Debug.LogWarning($"[ParticlePool] {type} is not preloaded");
            return;
        }
        pools[type].Release();
        pools.Remove(type);
    }

    public static void ReleaseAll()
    {
        if (isQuitting) return;

        foreach (var pool in pools.Values)
            pool.Release();
        pools.Clear();
    }

    public static void ClearOnQuit()
    {
        isQuitting = true;
        pools.Clear();
    }

    public static bool HasPool(ParticleType type)
    {
        return pools.ContainsKey(type);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        pools = new Dictionary<ParticleType, ParticlePoolInstance>();
        isQuitting = false;
    }
}

public class ParticlePoolInstance
{
    private Transform parent;
    private ParticleUnit prefab;
    private Queue<ParticleUnit> inactives = new Queue<ParticleUnit>();
    private List<ParticleUnit> actives = new List<ParticleUnit>();

    public void Preload(ParticleUnit prefab, int amount, Transform parent)
    {
        this.parent = parent;
        this.prefab = prefab;

        for (int i = 0; i < amount; i++)
        {
            ParticleUnit unit = Object.Instantiate(prefab, parent);
            Despawn(unit);
        }
    }

    public ParticleUnit Spawn(Vector3 position, Quaternion rotation)
    {
        ParticleUnit unit;

        if (inactives.Count <= 0)
        {
            unit = Object.Instantiate(prefab, parent);
        }
        else
        {
            unit = inactives.Dequeue();
        }

        unit.TF.SetPositionAndRotation(position, rotation);
        actives.Add(unit);
        unit.OnSpawn();

        return unit;
    }

    public void Despawn(ParticleUnit unit)
    {
        if (unit == null) return;

        if (actives.Remove(unit))
        {
            unit.OnDespawn();
            unit.TF.SetParent(parent);
            inactives.Enqueue(unit);
        }
        else if (!inactives.Contains(unit))
        {
            unit.OnDespawn();
            unit.TF.SetParent(parent);
            inactives.Enqueue(unit);
        }
    }

    public void Collect()
    {
        while (actives.Count > 0)
        {
            Despawn(actives[0]);
        }
    }

    public void Release()
    {
        Collect();
        while (inactives.Count > 0)
        {
            ParticleUnit unit = inactives.Dequeue();
            if (unit != null && unit.gameObject != null)
                Object.Destroy(unit.gameObject);
        }
        inactives.Clear();
    }
}
