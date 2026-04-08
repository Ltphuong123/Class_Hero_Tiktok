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
            Debug.Log(poolType + "IS NOT PRELOAD");
            return null;
        }
        return poolInstance[poolType].Spawn(pos, rot) as T;
    }

    //tra phan tu vao
    public static void Despawn(GameUnit unit)
    {
        if (!poolInstance.ContainsKey(unit.PoolType))
        {
            Debug.Log(unit.PoolType + "IS NOT PRELOAD");
        }
        poolInstance[unit.PoolType].Despawn(unit);
    }
    public static void Collect(PoolType poolType)
    {
        if (!poolInstance.ContainsKey(poolType))
        {
            Debug.Log(poolType + "IS NOT PRELOAD");
        }
        poolInstance[poolType].Collect();
    }
    public static void CollectAll()
    {
        foreach (var item in poolInstance.Values)
        {
            item.Collect();
        }
    }

    //destrol 1 pool
    public static void Release(PoolType poolType)
    {
        if (!poolInstance.ContainsKey(poolType))
        {
            Debug.Log(poolType + "IS NOT PRELOAD");
        }
        poolInstance[poolType].Release();
    }
    public static void ReleaseAll()
    {
        foreach (var item in poolInstance.Values)
        {
            item.Release();
        }
    }
    public static Boolean GetPool(PoolType poolType)
    {
        return poolInstance.ContainsKey(poolType);
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

        if (inactives.Count <= 0)
        {
            unit = GameObject.Instantiate(prefab, parent);
        }
        else
        {
            unit = inactives.Dequeue();
        }

        unit.TF.SetLocalPositionAndRotation(pos, rot);
        actives.Add(unit);
        unit.gameObject.SetActive(true);

        return unit;
    }

    public void Despawn(GameUnit unit)
    {
        if (unit != null && unit.gameObject.activeSelf)
        {
            actives.Remove(unit);
            inactives.Enqueue(unit);
            unit.gameObject.transform.SetParent(parent);
            unit.gameObject.SetActive(false);
        }
    }

    //thu thap tat ca phan tu ve pool
    public void Collect()
    {
        while (actives.Count > 0)
        {
            Despawn(actives[0]);
        }
    }

    // destroy tat ca phan tu
    public void Release()
    {
        Collect();
        while (inactives.Count > 0)
        {
            GameObject.Destroy(inactives.Dequeue().gameObject);
        }
        inactives.Clear();
    }
}
