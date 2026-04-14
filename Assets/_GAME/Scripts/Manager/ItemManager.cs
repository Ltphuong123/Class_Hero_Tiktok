using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized manager for all collectible items and dropped swords.
/// Provides spatial queries, object pooling, and registration APIs
/// for the future AI State Machine system.
/// </summary>
public class ItemManager : Singleton<ItemManager>
{
    [SerializeField] private int poolInitialSize = 50;
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

        float cellSize = MapManager.Instance != null ? MapManager.Instance.CellSize : 5f;
        grid = new SpatialGrid<ICollectibleItem>(cellSize);

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    // ── Registration ──

    /// <summary>
    /// Register a collectible item. Idempotent — duplicate calls are ignored.
    /// </summary>
    public void RegisterItem(ICollectibleItem item)
    {
        if (!allItems.Add(item))
            return;

        grid.Add(item, item.Position);
    }

    /// <summary>
    /// Deregister a collectible item. Silently ignores unregistered items.
    /// </summary>
    public void DeregisterItem(ICollectibleItem item)
    {
        if (!allItems.Remove(item))
            return;

        grid.Remove(item);
    }

    /// <summary>
    /// Register a dropped sword. Also registers it as a collectible item.
    /// </summary>
    public void RegisterDroppedSword(Sword sword)
    {
        droppedSwords.Add(sword);
        RegisterItem(sword);
    }

    /// <summary>
    /// Deregister a dropped sword. Also deregisters it as a collectible item.
    /// </summary>
    public void DeregisterDroppedSword(Sword sword)
    {
        droppedSwords.Remove(sword);
        DeregisterItem(sword);
    }

    // ── Spawning ──

    /// <summary>
    /// Spawn an item from a prefab, reusing a pooled instance if available.
    /// </summary>
    public GameObject SpawnItem(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[ItemManager] SpawnItem called with null prefab.");
            return null;
        }

        // Determine item type from prefab for pool lookup
        CollectibleItemType? itemType = null;
        var prefabItem = prefab.GetComponent<ICollectibleItem>();
        if (prefabItem != null)
            itemType = prefabItem.ItemType;

        GameObject instance = null;

        // Try to reuse from pool
        if (itemType.HasValue && pools.TryGetValue(itemType.Value, out var pool))
        {
            while (pool.Count > 0)
            {
                var candidate = pool.Pop();
                if (candidate != null)
                {
                    instance = candidate;
                    break;
                }
            }
        }

        // Instantiate if no pooled instance available
        if (instance == null)
            instance = Instantiate(prefab);

        // Initialize and register
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

    /// <summary>
    /// Despawn an item, returning it to the pool or destroying it if the pool is full.
    /// </summary>
    public void DespawnItem(ICollectibleItem item)
    {
        if (item == null || !allItems.Contains(item))
            return;

        DeregisterItem(item);

        if (item is Sword sword)
            droppedSwords.Remove(sword);

        item.OnDespawn();

        // Return to pool or destroy
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

    // ── Queries ──

    /// <summary>
    /// Get all collectible items within a radius. Delegates to the spatial grid.
    /// </summary>
    public void GetNearbyItems(Vector3 position, float radius, List<ICollectibleItem> results)
    {
        grid.GetInRadius(position, radius, results);
    }

    /// <summary>
    /// Get all dropped swords within a radius.
    /// </summary>
    public void GetNearbySwords(Vector3 position, float radius, List<Sword> results)
    {
        results.Clear();
        grid.GetInRadius(position, radius, queryBuffer);

        for (int i = 0; i < queryBuffer.Count; i++)
        {
            if (queryBuffer[i] is Sword sword && sword.State == SwordState.Dropped)
                results.Add(sword);
        }
    }

    /// <summary>
    /// Get the nearest collectible item within a radius.
    /// </summary>
    public ICollectibleItem GetNearestItem(Vector3 position, float radius)
    {
        return grid.GetNearest(position, radius);
    }

    /// <summary>
    /// Get the nearest dropped sword within a radius.
    /// </summary>
    public Sword GetNearestSword(Vector3 position, float radius)
    {
        grid.GetInRadius(position, radius, queryBuffer);

        Sword best = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < queryBuffer.Count; i++)
        {
            if (queryBuffer[i] is Sword sword && sword.State == SwordState.Dropped)
            {
                float distSq = (sword.transform.position - position).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = sword;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Get the nearest collectible item of a specific type within a radius.
    /// </summary>
    public ICollectibleItem GetNearestItemOfType(Vector3 position, float radius, CollectibleItemType itemType)
    {
        grid.GetInRadius(position, radius, queryBuffer);

        ICollectibleItem best = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < queryBuffer.Count; i++)
        {
            var candidate = queryBuffer[i];
            if (candidate.ItemType == itemType)
            {
                float distSq = (candidate.Position - position).sqrMagnitude;
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
