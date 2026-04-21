using UnityEngine;
using System.Collections.Generic;

public class ItemManager : Singleton<ItemManager>
{
    private SpatialGrid<Sword> grid;
    private readonly HashSet<Sword> allSwords = new();
    private readonly List<Sword> queryBuffer = new();

    public int SwordCount => allSwords.Count;

    protected override void Awake()
    {
        base.Awake();

        MapManager map = MapManager.Instance;
        grid = new SpatialGrid<Sword>(map != null ? map.CellSize : 5f);
    }

    public Sword Spawn(Vector3 position, Quaternion rotation)
    {
        Sword sword = SimplePool.Spawn<Sword>(PoolType.Sword, position, rotation);
        if (sword != null)
        {
            sword.OnInit();
            sword.TF.position = position;
            sword.TF.rotation = rotation;
            sword.gameObject.SetActive(true);
            Register(sword);
        }
        return sword;
    }

    public Sword Spawn(Vector3 position)
    {
        return Spawn(position, Quaternion.identity);
    }

    public void Register(Sword sword)
    {
        if (sword != null && allSwords.Add(sword))
            grid.Add(sword, sword.TF.position);
    }

    public void Unregister(Sword sword)
    {
        if (sword != null && allSwords.Remove(sword))
            grid.Remove(sword);
    }

    public void Despawn(Sword sword)
    {
        if (sword == null) return;
        Unregister(sword);
        SimplePool.Despawn(sword);
    }

    public void GetNearbySwords(Vector3 position, float radius, List<Sword> results)
    {
        results.Clear();
        grid.GetInRadius(position, radius, queryBuffer);

        int count = queryBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            if (queryBuffer[i].State == SwordState.Dropped)
                results.Add(queryBuffer[i]);
        }
    }

    public Sword GetNearestSword(Vector3 position, float radius)
    {
        grid.GetInRadius(position, radius, queryBuffer);

        Sword best = null;
        float bestDistSq = float.MaxValue;
        float px = position.x, py = position.y;

        int count = queryBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            Sword sword = queryBuffer[i];
            if (sword.State == SwordState.Dropped)
            {
                Vector3 sp = sword.TF.position;
                float dx = sp.x - px;
                float dy = sp.y - py;
                float distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = sword;
                }
            }
        }

        return best;
    }
}
