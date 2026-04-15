using UnityEngine;
using System.Collections.Generic;

public class GridPathfinder
{
    private readonly MapManager map;
    private readonly int maxSearchNodes;
    private readonly int cols;
    private readonly float cs;

    private readonly float[] gArr;
    private readonly float[] fArr;
    private readonly int[] fromArr;
    private readonly byte[] stateArr; // 0=none, 1=open, 2=closed

    private readonly List<int> openList = new();
    private readonly List<int> reconstructKeys = new();

    private static readonly int[] DCol = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] DRow = { 1, 1, 0, -1, -1, -1, 0, 1 };
    private static readonly float[] DCost = { 1f, 1.414f, 1f, 1.414f, 1f, 1.414f, 1f, 1.414f };
    private static readonly bool[] IsDiag = { false, true, false, true, false, true, false, true };

    private int generation;

    private int[] genArr;

    public GridPathfinder(MapManager map, int maxSearchNodes = 200)
    {
        this.map = map;
        this.maxSearchNodes = maxSearchNodes;
        cols = map.Columns;
        cs = map.CellSize;

        int total = cols * map.Rows;
        gArr = new float[total];
        fArr = new float[total];
        fromArr = new int[total];
        stateArr = new byte[total];
        genArr = new int[total];
    }

    public static float PathDistance(Vector3 start, List<Vector3> path)
    {
        if (path == null || path.Count == 0) return float.MaxValue;

        float dist = Vector3.Distance(start, path[0]);
        for (int i = 1; i < path.Count; i++)
            dist += Vector3.Distance(path[i - 1], path[i]);
        return dist;
    }

    public float FindPathDistance(Vector3 startWorld, Vector3 goalWorld)
    {
        Vector2Int startCell = map.WorldToCell(startWorld);
        Vector2Int goalCell = map.WorldToCell(goalWorld);

        if (map.IsBlocked(goalCell.x, goalCell.y))
        {
            goalCell = FindNearestOpen(goalCell);
            if (goalCell.x < 0) return float.MaxValue;
        }
        if (map.IsBlocked(startCell.x, startCell.y))
        {
            startCell = FindNearestOpen(startCell);
            if (startCell.x < 0) return float.MaxValue;
        }
        if (startCell.x == goalCell.x && startCell.y == goalCell.y)
        {
            float dx = startWorld.x - goalWorld.x;
            float dy = startWorld.y - goalWorld.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        return RunAStar(startCell, goalCell, true, goalWorld, null);
    }

    public float FindPath(Vector3 startWorld, Vector3 goalWorld, List<Vector3> result)
    {
        result.Clear();

        Vector2Int startCell = map.WorldToCell(startWorld);
        Vector2Int goalCell = map.WorldToCell(goalWorld);

        if (map.IsBlocked(goalCell.x, goalCell.y))
        {
            goalCell = FindNearestOpen(goalCell);
            if (goalCell.x < 0) return float.MaxValue;
        }
        if (map.IsBlocked(startCell.x, startCell.y))
        {
            startCell = FindNearestOpen(startCell);
            if (startCell.x < 0) return float.MaxValue;
        }
        if (startCell.x == goalCell.x && startCell.y == goalCell.y)
        {
            result.Add(goalWorld);
            float dx = startWorld.x - goalWorld.x;
            float dy = startWorld.y - goalWorld.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        return RunAStar(startCell, goalCell, false, goalWorld, result);
    }

    public List<Vector3> FindPath(Vector3 startWorld, Vector3 goalWorld)
    {
        var result = new List<Vector3>();
        FindPath(startWorld, goalWorld, result);
        return result;
    }

    private float RunAStar(Vector2Int startCell, Vector2Int goalCell, bool distanceOnly, Vector3 goalWorld, List<Vector3> result)
    {
        generation++;
        openList.Clear();

        int startKey = startCell.x + startCell.y * cols;
        int goalKey = goalCell.x + goalCell.y * cols;
        int goalCol = goalCell.x;
        int goalRow = goalCell.y;

        genArr[startKey] = generation;
        gArr[startKey] = 0f;
        fArr[startKey] = Heuristic(startCell.x, startCell.y, goalCol, goalRow);
        stateArr[startKey] = 1;
        fromArr[startKey] = -1;
        openList.Add(startKey);

        int nodesSearched = 0;

        while (openList.Count > 0)
        {
            int bestIdx = 0;
            float bestF = fArr[openList[0]];
            for (int i = 1; i < openList.Count; i++)
            {
                float f = fArr[openList[i]];
                if (f < bestF) { bestF = f; bestIdx = i; }
            }

            int currentKey = openList[bestIdx];
            int lastIdx = openList.Count - 1;
            openList[bestIdx] = openList[lastIdx];
            openList.RemoveAt(lastIdx);

            if (currentKey == goalKey)
            {
                float dist = gArr[goalKey] * cs;
                if (!distanceOnly && result != null)
                    ReconstructPath(currentKey, goalWorld, result);
                return dist;
            }

            stateArr[currentKey] = 2;
            nodesSearched++;
            if (nodesSearched > maxSearchNodes) break;

            int curCol = currentKey % cols;
            int curRow = currentKey / cols;
            float curG = gArr[currentKey];

            for (int d = 0; d < 8; d++)
            {
                int nc = curCol + DCol[d];
                int nr = curRow + DRow[d];

                if (map.IsBlocked(nc, nr)) continue;

                if (IsDiag[d])
                {
                    if (map.IsBlocked(nc, curRow) || map.IsBlocked(curCol, nr))
                        continue;
                }

                int neighborKey = nc + nr * cols;

                if (genArr[neighborKey] == generation && stateArr[neighborKey] == 2)
                    continue;

                float tentativeG = curG + DCost[d];

                if (genArr[neighborKey] != generation)
                {
                    genArr[neighborKey] = generation;
                    gArr[neighborKey] = tentativeG;
                    fArr[neighborKey] = tentativeG + Heuristic(nc, nr, goalCol, goalRow);
                    fromArr[neighborKey] = currentKey;
                    stateArr[neighborKey] = 1;
                    openList.Add(neighborKey);
                }
                else if (tentativeG < gArr[neighborKey])
                {
                    gArr[neighborKey] = tentativeG;
                    fArr[neighborKey] = tentativeG + Heuristic(nc, nr, goalCol, goalRow);
                    fromArr[neighborKey] = currentKey;

                    if (stateArr[neighborKey] != 1)
                    {
                        stateArr[neighborKey] = 1;
                        openList.Add(neighborKey);
                    }
                }
            }
        }

        return float.MaxValue;
    }

    private void ReconstructPath(int currentKey, Vector3 goalWorld, List<Vector3> result)
    {
        reconstructKeys.Clear();
        int key = currentKey;
        while (fromArr[key] >= 0)
        {
            reconstructKeys.Add(key);
            key = fromArr[key];
        }

        for (int i = reconstructKeys.Count - 1; i >= 0; i--)
        {
            int k = reconstructKeys[i];
            result.Add(map.CellToWorld(k % cols, k / cols));
        }

        if (result.Count > 0)
            result[result.Count - 1] = goalWorld;
    }

    private static float Heuristic(int c1, int r1, int c2, int r2)
    {
        int dx = c1 > c2 ? c1 - c2 : c2 - c1;
        int dy = r1 > r2 ? r1 - r2 : r2 - r1;
        return (dx > dy ? dx : dy) + 0.414f * (dx < dy ? dx : dy);
    }

    private Vector2Int FindNearestOpen(Vector2Int cell)
    {
        for (int r = 1; r <= 5; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx > -r && dx < r && dy > -r && dy < r) continue;
                    int nc = cell.x + dx;
                    int nr = cell.y + dy;
                    if (!map.IsBlocked(nc, nr))
                        return new Vector2Int(nc, nr);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }
}
