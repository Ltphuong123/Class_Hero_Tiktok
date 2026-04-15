using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A* pathfinder on the WallGrid. Returns a list of world positions from start to goal,
/// avoiding blocked cells. Supports 8-directional movement.
/// Designed for AI usage — call FindPath() during Evaluate(), cache the result,
/// then follow waypoints during ExecuteMovement().
/// </summary>
public class GridPathfinder
{
    private readonly WallGrid wallGrid;

    // Pre-allocated structures to avoid GC
    private readonly Dictionary<int, float> gScore = new();
    private readonly Dictionary<int, float> fScore = new();
    private readonly Dictionary<int, int> cameFrom = new();
    private readonly HashSet<int> closedSet = new();
    private readonly HashSet<int> openSet = new();
    private readonly List<int> openList = new();
    private readonly List<int> reconstructKeys = new();

    // 8 directions: N, NE, E, SE, S, SW, W, NW
    private static readonly int[] DCol = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private static readonly int[] DRow = { 1, 1, 0, -1, -1, -1, 0, 1 };
    private static readonly float[] DCost = { 1f, 1.414f, 1f, 1.414f, 1f, 1.414f, 1f, 1.414f };

    private readonly int maxSearchNodes;

    public GridPathfinder(WallGrid wallGrid, int maxSearchNodes = 200)
    {
        this.wallGrid = wallGrid;
        this.maxSearchNodes = maxSearchNodes;
    }

    /// <summary>
    /// Compute the total walking distance of a path (sum of segment lengths).
    /// Returns float.MaxValue if path is empty (no route).
    /// </summary>
    public static float PathDistance(Vector3 start, List<Vector3> path)
    {
        if (path == null || path.Count == 0) return float.MaxValue;

        float dist = Vector3.Distance(start, path[0]);
        for (int i = 1; i < path.Count; i++)
            dist += Vector3.Distance(path[i - 1], path[i]);
        return dist;
    }

    /// <summary>
    /// Find path distance from start to goal using A* gScore.
    /// Much cheaper than FindPath() when you only need the distance, not the waypoints.
    /// Returns float.MaxValue if no path found.
    /// </summary>
    public float FindPathDistance(Vector3 startWorld, Vector3 goalWorld)
    {
        Vector2Int startCell = wallGrid.WorldToCell(startWorld);
        Vector2Int goalCell = wallGrid.WorldToCell(goalWorld);

        if (wallGrid.IsBlocked(goalCell.x, goalCell.y))
        {
            goalCell = FindNearestOpen(goalCell);
            if (goalCell.x < 0) return float.MaxValue;
        }
        if (wallGrid.IsBlocked(startCell.x, startCell.y))
        {
            startCell = FindNearestOpen(startCell);
            if (startCell.x < 0) return float.MaxValue;
        }
        if (startCell == goalCell)
            return Vector3.Distance(startWorld, goalWorld);

        return RunAStar(startCell, goalCell, distanceOnly: true, goalWorld, null);
    }

    /// <summary>
    /// Find a path and write waypoints into the provided buffer (no allocation).
    /// Returns the path distance, or float.MaxValue if no path found.
    /// </summary>
    public float FindPath(Vector3 startWorld, Vector3 goalWorld, List<Vector3> result)
    {
        result.Clear();

        Vector2Int startCell = wallGrid.WorldToCell(startWorld);
        Vector2Int goalCell = wallGrid.WorldToCell(goalWorld);

        if (wallGrid.IsBlocked(goalCell.x, goalCell.y))
        {
            goalCell = FindNearestOpen(goalCell);
            if (goalCell.x < 0) return float.MaxValue;
        }
        if (wallGrid.IsBlocked(startCell.x, startCell.y))
        {
            startCell = FindNearestOpen(startCell);
            if (startCell.x < 0) return float.MaxValue;
        }
        if (startCell == goalCell)
        {
            result.Add(goalWorld);
            return Vector3.Distance(startWorld, goalWorld);
        }

        return RunAStar(startCell, goalCell, distanceOnly: false, goalWorld, result);
    }

    /// <summary>
    /// Legacy overload — allocates a new list. Prefer the buffer overload.
    /// </summary>
    public List<Vector3> FindPath(Vector3 startWorld, Vector3 goalWorld)
    {
        var result = new List<Vector3>();
        FindPath(startWorld, goalWorld, result);
        return result;
    }

    /// <summary>
    /// Core A* implementation. If distanceOnly=true, returns gScore at goal (cell distance × cellSize)
    /// without reconstructing the path. Otherwise reconstructs into result list.
    /// </summary>
    private float RunAStar(Vector2Int startCell, Vector2Int goalCell, bool distanceOnly, Vector3 goalWorld, List<Vector3> result)
    {
        gScore.Clear();
        fScore.Clear();
        cameFrom.Clear();
        closedSet.Clear();
        openSet.Clear();
        openList.Clear();

        int cols = wallGrid.Columns;
        float cellSize = wallGrid.CellSize;
        int startKey = startCell.x + startCell.y * cols;
        int goalKey = goalCell.x + goalCell.y * cols;

        gScore[startKey] = 0f;
        fScore[startKey] = Heuristic(startCell.x, startCell.y, goalCell.x, goalCell.y);
        openList.Add(startKey);
        openSet.Add(startKey);

        int nodesSearched = 0;

        while (openList.Count > 0)
        {
            // Find node with lowest fScore
            int bestIdx = 0;
            float bestF = fScore[openList[0]];
            for (int i = 1; i < openList.Count; i++)
            {
                float f = fScore[openList[i]];
                if (f < bestF)
                {
                    bestF = f;
                    bestIdx = i;
                }
            }

            int currentKey = openList[bestIdx];
            openList.RemoveAt(bestIdx);
            openSet.Remove(currentKey);

            if (currentKey == goalKey)
            {
                float dist = gScore[goalKey] * cellSize;
                if (!distanceOnly && result != null)
                    ReconstructPath(currentKey, cols, goalWorld, result);
                return dist;
            }

            closedSet.Add(currentKey);
            nodesSearched++;
            if (nodesSearched > maxSearchNodes) break;

            int curCol = currentKey % cols;
            int curRow = currentKey / cols;
            float curG = gScore[currentKey];

            for (int d = 0; d < 8; d++)
            {
                int nc = curCol + DCol[d];
                int nr = curRow + DRow[d];

                if (wallGrid.IsBlocked(nc, nr)) continue;

                // Diagonal: block if either adjacent cardinal is blocked (no corner cutting)
                if (DCost[d] > 1f)
                {
                    if (wallGrid.IsBlocked(curCol + DCol[d], curRow) ||
                        wallGrid.IsBlocked(curCol, curRow + DRow[d]))
                        continue;
                }

                int neighborKey = nc + nr * cols;
                if (closedSet.Contains(neighborKey)) continue;

                float tentativeG = curG + DCost[d];

                if (!gScore.TryGetValue(neighborKey, out float existingG) || tentativeG < existingG)
                {
                    cameFrom[neighborKey] = currentKey;
                    gScore[neighborKey] = tentativeG;
                    fScore[neighborKey] = tentativeG + Heuristic(nc, nr, goalCell.x, goalCell.y);

                    if (openSet.Add(neighborKey))
                        openList.Add(neighborKey);
                }
            }
        }

        return float.MaxValue; // no path found
    }

    private void ReconstructPath(int currentKey, int cols, Vector3 goalWorld, List<Vector3> result)
    {
        reconstructKeys.Clear();
        int key = currentKey;
        while (cameFrom.ContainsKey(key))
        {
            reconstructKeys.Add(key);
            key = cameFrom[key];
        }

        for (int i = reconstructKeys.Count - 1; i >= 0; i--)
        {
            int k = reconstructKeys[i];
            int col = k % cols;
            int row = k / cols;
            result.Add(wallGrid.CellToWorld(col, row));
        }

        if (result.Count > 0)
            result[result.Count - 1] = goalWorld;
    }

    private static float Heuristic(int col1, int row1, int col2, int row2)
    {
        int dx = Mathf.Abs(col1 - col2);
        int dy = Mathf.Abs(row1 - row2);
        return Mathf.Max(dx, dy) + 0.414f * Mathf.Min(dx, dy);
    }

    private Vector2Int FindNearestOpen(Vector2Int cell)
    {
        for (int r = 1; r <= 5; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    int nc = cell.x + dx;
                    int nr = cell.y + dy;
                    if (!wallGrid.IsBlocked(nc, nr))
                        return new Vector2Int(nc, nr);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }
}
