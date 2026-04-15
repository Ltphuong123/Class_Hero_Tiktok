using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class SpatialGrid<T> where T : class
{
    private readonly float invCellSize;
    private readonly Dictionary<long, List<T>> cells = new();
    private readonly Dictionary<T, long> entityCells = new();
    private readonly Dictionary<T, Vector3> entityPositions = new();
    private readonly Stack<List<T>> listPool = new();
    private int count;

    public int Count => count;

    public SpatialGrid(float cellSize)
    {
        invCellSize = 1f / cellSize;
    }

    public void Add(T entity, Vector3 pos)
    {
        long key = PosToKey(pos.x, pos.y);

        if (!cells.TryGetValue(key, out var list))
        {
            list = listPool.Count > 0 ? listPool.Pop() : new List<T>(4);
            cells[key] = list;
        }

        list.Add(entity);
        entityCells[entity] = key;
        entityPositions[entity] = pos;
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
            int idx = list.IndexOf(entity);
            if (idx >= 0)
            {
                int last = list.Count - 1;
                list[idx] = list[last];
                list.RemoveAt(last);
            }

            if (list.Count == 0)
            {
                listPool.Push(list);
                cells.Remove(key);
            }
        }

        count--;
    }

    public void UpdatePosition(T entity, Vector3 newPos)
    {
        if (!entityCells.TryGetValue(entity, out long oldKey))
            return;

        long newKey = PosToKey(newPos.x, newPos.y);
        entityPositions[entity] = newPos;

        if (oldKey == newKey)
            return;

        if (cells.TryGetValue(oldKey, out var oldList))
        {
            int idx = oldList.IndexOf(entity);
            if (idx >= 0)
            {
                int last = oldList.Count - 1;
                oldList[idx] = oldList[last];
                oldList.RemoveAt(last);
            }

            if (oldList.Count == 0)
            {
                listPool.Push(oldList);
                cells.Remove(oldKey);
            }
        }

        if (!cells.TryGetValue(newKey, out var newList))
        {
            newList = listPool.Count > 0 ? listPool.Pop() : new List<T>(4);
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

        float cx = center.x, cy = center.y;
        float radiusSq = radius * radius;

        int minX = (int)((cx - radius) * invCellSize) - 1;
        int maxX = (int)((cx + radius) * invCellSize) + 1;
        int minY = (int)((cy - radius) * invCellSize) - 1;
        int maxY = (int)((cy + radius) * invCellSize) + 1;

        for (int gx = minX; gx <= maxX; gx++)
        {
            long keyBase = (long)gx << 32;
            for (int gy = minY; gy <= maxY; gy++)
            {
                long key = keyBase | (uint)gy;
                if (!cells.TryGetValue(key, out var list))
                    continue;

                int listCount = list.Count;
                for (int i = 0; i < listCount; i++)
                {
                    T entity = list[i];
                    Vector3 pos = entityPositions[entity];
                    float dx = pos.x - cx;
                    float dy = pos.y - cy;

                    if (dx * dx + dy * dy <= radiusSq)
                        results.Add(entity);
                }
            }
        }
    }

    public T GetNearest(Vector3 center, float radius, T exclude = null)
    {
        float cx = center.x, cy = center.y;
        float radiusSq = radius * radius;
        float bestDistSq = float.MaxValue;
        T best = null;

        int minX = (int)((cx - radius) * invCellSize) - 1;
        int maxX = (int)((cx + radius) * invCellSize) + 1;
        int minY = (int)((cy - radius) * invCellSize) - 1;
        int maxY = (int)((cy + radius) * invCellSize) + 1;

        for (int gx = minX; gx <= maxX; gx++)
        {
            long keyBase = (long)gx << 32;
            for (int gy = minY; gy <= maxY; gy++)
            {
                long key = keyBase | (uint)gy;
                if (!cells.TryGetValue(key, out var list))
                    continue;

                int listCount = list.Count;
                for (int i = 0; i < listCount; i++)
                {
                    T entity = list[i];
                    if (entity == exclude) continue;

                    Vector3 pos = entityPositions[entity];
                    float dx = pos.x - cx;
                    float dy = pos.y - cy;
                    float distSq = dx * dx + dy * dy;

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        best = entity;
                    }
                }
            }
        }

        return bestDistSq <= radiusSq ? best : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long PosToKey(float x, float y)
    {
        int kx = x >= 0f ? (int)(x * invCellSize) : (int)(x * invCellSize) - 1;
        int ky = y >= 0f ? (int)(y * invCellSize) : (int)(y * invCellSize) - 1;
        return (long)kx << 32 | (uint)ky;
    }
}
