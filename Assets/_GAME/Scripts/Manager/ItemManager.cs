using UnityEngine;
using System.Collections.Generic;

public class ItemManager : Singleton<ItemManager>
{
    [SerializeField] private int poolMaxSize = 200;
    [SerializeField] private bool persistAcrossScenes = false;

    private SpatialGrid<ICollectibleItem> grid;
    private readonly HashSet<ICollectibleItem> allItems = new();
    private readonly HashSet<Sword> droppedSwords = new();
    private readonly Dictionary<CollectibleItemType, Stack<GameObject>> pools = new();
    private readonly List<ICollectibleItem> queryBuffer = new();

    public int ItemCount => allItems.Count;
    public int DroppedSwordCount => droppedSwords.Count;

    protected override void Awake()
    {
        base.Awake();

        MapManager map = MapManager.Instance;
        grid = new SpatialGrid<ICollectibleItem>(map != null ? map.CellSize : 5f);

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    public void RegisterItem(ICollectibleItem item)
    {
        if (allItems.Add(item))
            grid.Add(item, item.Position);
    }

    public void DeregisterItem(ICollectibleItem item)
    {
        if (allItems.Remove(item))
            grid.Remove(item);
    }

    public void RegisterDroppedSword(Sword sword)
    {
        droppedSwords.Add(sword);
        RegisterItem(sword);
    }

    public void DeregisterDroppedSword(Sword sword)
    {
        droppedSwords.Remove(sword);
        DeregisterItem(sword);
    }

    public GameObject SpawnItem(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;

        CollectibleItemType? itemType = null;
        var prefabItem = prefab.GetComponent<ICollectibleItem>();
        if (prefabItem != null)
            itemType = prefabItem.ItemType;

        GameObject instance = null;

        if (itemType.HasValue && pools.TryGetValue(itemType.Value, out var pool))
        {
            while (pool.Count > 0)
            {
                var candidate = pool.Pop();
                if (candidate != null) { instance = candidate; break; }
            }
        }

        if (instance == null)
            instance = Instantiate(prefab);

        var collectible = instance.GetComponent<ICollectibleItem>();
        if (collectible != null)
        {
            collectible.OnSpawn(position);
            RegisterItem(collectible);
        }
        else
        {
            instance.transform.position = position;
            instance.SetActive(true);
        }

        return instance;
    }

    public void DespawnItem(ICollectibleItem item)
    {
        if (item == null || !allItems.Contains(item)) return;

        DeregisterItem(item);

        if (item is Sword sword)
            droppedSwords.Remove(sword);

        item.OnDespawn();

        CollectibleItemType type = item.ItemType;
        if (!pools.TryGetValue(type, out var pool))
        {
            pool = new Stack<GameObject>();
            pools[type] = pool;
        }

        if (pool.Count < poolMaxSize)
            pool.Push(item.GameObject);
        else
            Destroy(item.GameObject);
    }

    public void GetNearbyItems(Vector3 position, float radius, List<ICollectibleItem> results)
    {
        grid.GetInRadius(position, radius, results);
    }

    public void GetNearbySwords(Vector3 position, float radius, List<Sword> results)
    {
        results.Clear();
        grid.GetInRadius(position, radius, queryBuffer);

        int count = queryBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            if (queryBuffer[i] is Sword sword && sword.State == SwordState.Dropped)
                results.Add(sword);
        }
    }

    public ICollectibleItem GetNearestItem(Vector3 position, float radius)
    {
        return grid.GetNearest(position, radius);
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
            if (queryBuffer[i] is Sword sword && sword.State == SwordState.Dropped)
            {
                Vector3 sp = sword.Position;
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

    public ICollectibleItem GetNearestItemOfType(Vector3 position, float radius, CollectibleItemType itemType)
    {
        grid.GetInRadius(position, radius, queryBuffer);

        ICollectibleItem best = null;
        float bestDistSq = float.MaxValue;
        float px = position.x, py = position.y;

        int count = queryBuffer.Count;
        for (int i = 0; i < count; i++)
        {
            var candidate = queryBuffer[i];
            if (candidate.ItemType == itemType)
            {
                Vector3 cp = candidate.Position;
                float dx = cp.x - px;
                float dy = cp.y - py;
                float distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = candidate;
                }
            }
        }

        return best;
    }
}
