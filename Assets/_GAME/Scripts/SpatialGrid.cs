using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic spatial partitioning grid for efficient radius and nearest-neighbor queries.
/// Uses a uniform 2D grid with packed long keys for O(1) cell lookups.
/// Designed for 500+ entities with minimal allocation.
/// </summary>
public class SpatialGrid<T> where T : class
{
    private readonly float cellSize;
    private readonly float invCellSize;
    private readonly Dictionary<long, List<T>> cells = new Dictionary<long, List<T>>();
    private readonly Dictionary<T, long> entityCells = new Dictionary<T, long>();
    private readonly Dictionary<T, Vector3> entityPositions = new Dictionary<T, Vector3>();
    private readonly Stack<List<T>> listPool = new Stack<List<T>>();
    private int count;

    private const float MaxCoord = 100000f;

    public int Count => count;

    public SpatialGrid(float cellSize)
    {
        this.cellSize = cellSize;
        this.invCellSize = 1f / cellSize;
    }

    public void Add(T entity, Vector3 worldPosition)
    {
        worldPosition = SanitizePosition(worldPosition);
        long key = PositionToKey(worldPosition);

        if (!cells.TryGetValue(key, out var list))
        {
            list = listPool.Count > 0 ? listPool.Pop() : new List<T>();
            cells[key] = list;
        }

        list.Add(entity);
        entityCells[entity] = key;
        entityPositions[entity] = worldPosition;
        count++;
    }

    public void Remove(T entity)
    {
        if (!entityCells.TryGetValue(entity, out long key))
            return;

        entityCells.Remove(entity);
        entityPositions.Remove(entity);

        if (cells.TryGetValue(key, out var list))
        {
            list.Remove(entity);
            if (list.Count == 0)
            {
                list.Clear();
                listPool.Push(list);
                cells.Remove(key);
            }
        }

        count--;
    }

    public void UpdatePosition(T entity, Vector3 newWorldPosition)
    {
        if (!entityCells.TryGetValue(entity, out long oldKey))
            return;

        newWorldPosition = SanitizePosition(newWorldPosition);
        long newKey = PositionToKey(newWorldPosition);

        entityPositions[entity] = newWorldPosition;

        if (oldKey == newKey)
            return;

        // Remove from old cell
        if (cells.TryGetValue(oldKey, out var oldList))
        {
            oldList.Remove(entity);
            if (oldList.Count == 0)
            {
                oldList.Clear();
                listPool.Push(oldList);
                cells.Remove(oldKey);
            }
        }

        // Add to new cell
        if (!cells.TryGetValue(newKey, out var newList))
        {
            newList = listPool.Count > 0 ? listPool.Pop() : new List<T>();
            cells[newKey] = newList;
        }

        newList.Add(entity);
        entityCells[entity] = newKey;
    }

    public void Clear()
    {
        cells.Clear();
        entityCells.Clear();
        entityPositions.Clear();
        listPool.Clear();
        count = 0;
    }

    public void GetInRadius(Vector3 center, float radius, List<T> results)
    {
        results.Clear();

        center = SanitizePosition(center);
        float radiusSq = radius * radius;

        int minCellX = Mathf.FloorToInt((center.x - radius) * invCellSize);
        int maxCellX = Mathf.FloorToInt((center.x + radius) * invCellSize);
        int minCellY = Mathf.FloorToInt((center.y - radius) * invCellSize);
        int maxCellY = Mathf.FloorToInt((center.y + radius) * invCellSize);

        for (int cx = minCellX; cx <= maxCellX; cx++)
        {
            for (int cy = minCellY; cy <= maxCellY; cy++)
            {
                long key = PackKey(cx, cy);
                if (!cells.TryGetValue(key, out var list))
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    T entity = list[i];
                    if (!entityPositions.TryGetValue(entity, out Vector3 pos))
                        continue;

                    float dx = pos.x - center.x;
                    float dy = pos.y - center.y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= radiusSq)
                        results.Add(entity);
                }
            }
        }
    }

    public T GetNearest(Vector3 center, float radius, T exclude = null)
    {
        center = SanitizePosition(center);
        float radiusSq = radius * radius;
        float bestDistSq = float.MaxValue;
        T best = null;

        int minCellX = Mathf.FloorToInt((center.x - radius) * invCellSize);
        int maxCellX = Mathf.FloorToInt((center.x + radius) * invCellSize);
        int minCellY = Mathf.FloorToInt((center.y - radius) * invCellSize);
        int maxCellY = Mathf.FloorToInt((center.y + radius) * invCellSize);

        for (int cx = minCellX; cx <= maxCellX; cx++)
        {
            for (int cy = minCellY; cy <= maxCellY; cy++)
            {
                long key = PackKey(cx, cy);
                if (!cells.TryGetValue(key, out var list))
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    T entity = list[i];
                    if (entity == exclude)
                        continue;

                    if (!entityPositions.TryGetValue(entity, out Vector3 pos))
                        continue;

                    float dx = pos.x - center.x;
                    float dy = pos.y - center.y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= radiusSq && distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        best = entity;
                    }
                }
            }
        }

        return best;
    }

    private long PositionToKey(Vector3 pos)
    {
        int cellX = Mathf.FloorToInt(pos.x * invCellSize);
        int cellY = Mathf.FloorToInt(pos.y * invCellSize);
        return PackKey(cellX, cellY);
    }

    private static long PackKey(int cellX, int cellY)
    {
        return (long)cellX << 32 | (uint)cellY;
    }

    private static Vector3 SanitizePosition(Vector3 pos)
    {
        bool sanitized = false;

        if (float.IsNaN(pos.x) || float.IsInfinity(pos.x))
        {
            pos.x = pos.x > 0 ? MaxCoord : -MaxCoord;
            if (float.IsNaN(pos.x)) pos.x = 0f;
            sanitized = true;
        }
        else if (pos.x > MaxCoord)
        {
            pos.x = MaxCoord;
            sanitized = true;
        }
        else if (pos.x < -MaxCoord)
        {
            pos.x = -MaxCoord;
            sanitized = true;
        }

        if (float.IsNaN(pos.y) || float.IsInfinity(pos.y))
        {
            pos.y = pos.y > 0 ? MaxCoord : -MaxCoord;
            if (float.IsNaN(pos.y)) pos.y = 0f;
            sanitized = true;
        }
        else if (pos.y > MaxCoord)
        {
            pos.y = MaxCoord;
            sanitized = true;
        }
        else if (pos.y < -MaxCoord)
        {
            pos.y = -MaxCoord;
            sanitized = true;
        }

        if (sanitized)
            Debug.LogWarning($"[SpatialGrid] Position contained NaN/Infinity or exceeded bounds, clamped to {pos}");

        return pos;
    }
}
