using UnityEngine;

/// <summary>
/// Central source of truth for map bounds and grid configuration.
/// All managers and AI systems read from here.
/// </summary>
public class MapManager : Singleton<MapManager>
{
    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 5f;
    [SerializeField] private int columns = 10;
    [SerializeField] private int rows = 10;

    [Header("Map Origin")]
    [SerializeField, Tooltip("Center of the map. Bounds are calculated from this point.")]
    private Vector2 origin = Vector2.zero;

    [Header("Wall Detection")]
    [SerializeField] private LayerMask wallMask;

    private WallGrid wallGrid;
    private GridPathfinder pathfinder;

    // ── Public Properties ──
    public float CellSize => cellSize;
    public int Columns => columns;
    public int Rows => rows;
    public WallGrid WallGrid => wallGrid;
    public GridPathfinder Pathfinder => pathfinder;
    public float MapWidth => columns * cellSize;
    public float MapHeight => rows * cellSize;
    public Vector2 MapMin => origin - new Vector2(MapWidth * 0.5f, MapHeight * 0.5f);
    public Vector2 MapMax => origin + new Vector2(MapWidth * 0.5f, MapHeight * 0.5f);
    public Vector2 Origin => origin;
    public Vector2 MapCenter => origin;

    /// <summary>
    /// Convert world position to grid cell coordinates.
    /// </summary>
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector2 min = MapMin;
        int cx = Mathf.FloorToInt((worldPos.x - min.x) / cellSize);
        int cy = Mathf.FloorToInt((worldPos.y - min.y) / cellSize);
        cx = Mathf.Clamp(cx, 0, columns - 1);
        cy = Mathf.Clamp(cy, 0, rows - 1);
        return new Vector2Int(cx, cy);
    }

    /// <summary>
    /// Convert grid cell coordinates to world center position.
    /// </summary>
    public Vector3 CellToWorld(int col, int row)
    {
        Vector2 min = MapMin;
        return new Vector3(
            min.x + (col + 0.5f) * cellSize,
            min.y + (row + 0.5f) * cellSize,
            0f
        );
    }

    /// <summary>
    /// Check if a world position is inside the map bounds.
    /// </summary>
    public bool IsInsideMap(Vector3 worldPos)
    {
        Vector2 min = MapMin;
        Vector2 max = MapMax;
        return worldPos.x >= min.x && worldPos.x <= max.x
            && worldPos.y >= min.y && worldPos.y <= max.y;
    }

    /// <summary>
    /// Clamp a world position to map bounds.
    /// </summary>
    public Vector3 ClampToMap(Vector3 worldPos)
    {
        Vector2 min = MapMin;
        Vector2 max = MapMax;
        worldPos.x = Mathf.Clamp(worldPos.x, min.x, max.x);
        worldPos.y = Mathf.Clamp(worldPos.y, min.y, max.y);
        return worldPos;
    }

    protected override void Awake()
    {
        base.Awake();
        BakeWallGrid();
    }

    /// <summary>
    /// Bake the wall grid and create the pathfinder.
    /// Call again if walls change at runtime.
    /// </summary>
    public void BakeWallGrid()
    {
        wallGrid = new WallGrid(columns, rows, cellSize, MapMin);
        wallGrid.Bake(wallMask);
        pathfinder = new GridPathfinder(wallGrid);
        Debug.Log($"[MapManager] Wall grid baked: {columns}x{rows}, cellSize={cellSize}");
    }

    /// <summary>
    /// Check if a world position is in a wall cell.
    /// </summary>
    public bool IsWall(Vector3 worldPos)
    {
        return wallGrid != null && wallGrid.IsBlockedWorld(worldPos);
    }

    private void OnValidate()
    {
        cellSize = Mathf.Max(0.5f, cellSize);
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
    }
}
