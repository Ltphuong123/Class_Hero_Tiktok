using System;
using System.Collections.Generic;
using UnityEngine;

public static class SimplePool
{
    private static Dictionary<PoolType, Pool> poolInstance = new Dictionary<PoolType, Pool>();

    //khoi tao pool moi
    public static void Preload(GameUnit prefab, int amount, Transform parent)
    {
        if (prefab == null)
        {
            Debug.Log("PREFAB IS EMPTY");
            return;
        }
        if (!poolInstance.ContainsKey(prefab.PoolType) || poolInstance[prefab.PoolType] == null)
        {
            Pool p = new Pool();
            p.Preload(prefab, amount, parent);
            poolInstance[prefab.PoolType] = p;
        }
    }


    //lay phan tu ra
    public static T Spawn<T>(PoolType poolType, Vector3 pos, Quaternion rot) where T : GameUnit
    {
        if (!poolInstance.ContainsKey(poolType))
        {
            Debug.Log(poolType + " IS NOT PRELOAD");
            return null;
        }
        
        // Validate pool trước khi spawn
        if (!poolInstance[poolType].IsValid())
        {
            Debug.LogWarning($"Pool {poolType} is invalid, removing...");
            poolInstance.Remove(poolType);
            return null;
        }
        
        return poolInstance[poolType].Spawn(pos, rot) as T;
    }

    //tra phan tu vao
    public static void Despawn(GameUnit unit)
    {
        if (unit == null) return;
        
        if (!poolInstance.ContainsKey(unit.PoolType))
        {
            Debug.Log(unit.PoolType + " IS NOT PRELOAD");
            return;
        }
        poolInstance[unit.PoolType].Despawn(unit);
    }
    
    public static void Collect(PoolType poolType)
    {
        if (!poolInstance.ContainsKey(poolType))
        {
            Debug.Log(poolType + " IS NOT PRELOAD");
            return;
        }
        poolInstance[poolType].Collect();
    }
    
    public static void CollectAll()
    {
        foreach (var item in poolInstance.Values)
        {
            if (item != null)
                item.Collect();
        }
    }

    //destrol 1 pool
    public static void Release(PoolType poolType)
    {
        if (!poolInstance.ContainsKey(poolType))
        {
            Debug.Log(poolType + " IS NOT PRELOAD");
            return;
        }
        poolInstance[poolType].Release();
        poolInstance.Remove(poolType);
    }
    
    public static void ReleaseAll()
    {
        foreach (var item in poolInstance.Values)
        {
            if (item != null)
                item.Release();
        }
        poolInstance.Clear();
    }
    
    public static Boolean GetPool(PoolType poolType)
    {
        return poolInstance.ContainsKey(poolType);
    }
    
    // Cleanup invalid pools (gọi khi chuyển scene)
    public static void CleanupInvalidPools()
    {
        List<PoolType> toRemove = new List<PoolType>();
        
        foreach (var kvp in poolInstance)
        {
            if (kvp.Value == null || !kvp.Value.IsValid())
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var poolType in toRemove)
        {
            Debug.Log($"Removing invalid pool: {poolType}");
            poolInstance.Remove(poolType);
        }
    }
} 

public class Pool
{
    private Transform parent;
    private GameUnit prefab;
    private Queue<GameUnit> inactives = new Queue<GameUnit>();
    private List<GameUnit> actives = new List<GameUnit>();

    public void Preload(GameUnit prefab, int amount, Transform parent)
    {
        this.parent = parent;
        this.prefab = prefab;
        
        for (int i = 0; i < amount; i++)
        {
            Despawn(GameObject.Instantiate(prefab, parent));
        }
    }

    public GameUnit Spawn(Vector3 pos, Quaternion rot)
    {
        GameUnit unit;

        // Cleanup null objects trong queue
        while (inactives.Count > 0 && inactives.Peek() == null)
        {
            inactives.Dequeue();
        }

        if (inactives.Count <= 0)
        {
            if (prefab == null)
            {
                Debug.LogError("Prefab is null, cannot spawn!");
                return null;
            }
            unit = GameObject.Instantiate(prefab, parent);
        }
        else
        {
            unit = inactives.Dequeue();
        }

        if (unit == null)
        {
            Debug.LogError("Spawned unit is null!");
            return null;
        }

        unit.TF.SetLocalPositionAndRotation(pos, rot);
        actives.Add(unit);
        unit.gameObject.SetActive(true);

        return unit;
    }

    public void Despawn(GameUnit unit)
    {
        if (unit == null) return;
        
        // Luôn remove khỏi actives trước
        bool wasActive = actives.Remove(unit);
        
        // Chỉ enqueue và deactivate nếu object còn tồn tại
        if (unit.gameObject != null)
        {
            if (unit.gameObject.activeSelf)
            {
                unit.gameObject.SetActive(false);
            }
            
            unit.gameObject.transform.SetParent(parent);
            
            // Chỉ enqueue nếu đã remove thành công từ actives
            if (wasActive)
            {
                inactives.Enqueue(unit);
            }
        }
    }

    //thu thap tat ca phan tu ve pool
    public void Collect()
    {
        // Cleanup null references
        actives.RemoveAll(unit => unit == null);
        
        // Sử dụng for loop ngược để tránh vô hạn
        for (int i = actives.Count - 1; i >= 0; i--)
        {
            if (i < actives.Count) // Double check index còn valid
            {
                Despawn(actives[i]);
            }
        }
        
        // Fallback: Clear nếu vẫn còn
        if (actives.Count > 0)
        {
            Debug.LogWarning($"Force clearing {actives.Count} active units");
            actives.Clear();
        }
    }

    // destroy tat ca phan tu
    public void Release()
    {
        Collect();
        
        while (inactives.Count > 0)
        {
            GameUnit unit = inactives.Dequeue();
            if (unit != null && unit.gameObject != null)
                GameObject.Destroy(unit.gameObject);
        }
        inactives.Clear();
    }
    
    // Kiểm tra pool còn valid không
    public bool IsValid()
    {
        // Nếu prefab hoặc parent bị destroy thì pool không còn valid
        return prefab != null && parent != null;
    }
}
