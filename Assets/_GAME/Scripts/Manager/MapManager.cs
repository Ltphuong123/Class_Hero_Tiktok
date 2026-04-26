using UnityEngine;

public class MapManager : Singleton<MapManager>
{
    [Header("Map List")]
    [SerializeField] private GameObject[] maps;
    
    [Header("Grid Settings (Shared)")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private int columns = 100;
    [SerializeField] private int rows = 100;
    
    [Header("Wall Detection")]
    [SerializeField] private LayerMask wallMask;
    
    private const string SelectedMapPref = "SELECTED_MAP_INDEX";
    
    private bool[] blocked;
    private float invCellSize;
    private Vector2 cachedMin;
    private Vector2 cachedMax;
    private GridPathfinder pathfinder;

    public float CellSize => cellSize;
    public int Columns => columns;
    public int Rows => rows;
    public GridPathfinder Pathfinder => pathfinder;
    public float MapWidth => columns * cellSize;
    public float MapHeight => rows * cellSize;
    public Vector2 MapMin => cachedMin;
    public Vector2 MapMax => cachedMax;
    public int MapCount => maps?.Length ?? 0;

    protected override void Awake()
    {
        base.Awake();
        LoadSelectedMap();
        CacheBounds();
        BakeWallGrid();
    }
    
    private void LoadSelectedMap()
    {
        int selectedIndex = PlayerPrefs.GetInt(SelectedMapPref, 0);
        maps[selectedIndex].SetActive(true);
    }

    private void CacheBounds()
    {
        invCellSize = 1f / cellSize;
        float hw = columns * cellSize * 0.5f;
        float hh = rows * cellSize * 0.5f;
        cachedMin = new Vector2(-hw, -hh);
        cachedMax = new Vector2(hw, hh);
    }

    public void BakeWallGrid()
    {
        int total = columns * rows;
        blocked = new bool[total];
        float size = cellSize * 0.9f;

        for (int i = 0; i < total; i++)
        {
            int col = i % columns;
            int row = i / columns;

            float cx = cachedMin.x + (col + 0.5f) * cellSize;
            float cy = cachedMin.y + (row + 0.5f) * cellSize;

            blocked[i] = Physics2D.OverlapBox(new Vector2(cx, cy), new Vector2(size, size), 0f, wallMask) != null;
        }

        pathfinder = new GridPathfinder(this);
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        int cx = (int)((worldPos.x - cachedMin.x) * invCellSize);
        int cy = (int)((worldPos.y - cachedMin.y) * invCellSize);

        if (cx < 0) cx = 0; else if (cx >= columns) cx = columns - 1;
        if (cy < 0) cy = 0; else if (cy >= rows) cy = rows - 1;

        return new Vector2Int(cx, cy);
    }

    public Vector3 CellToWorld(int col, int row)
    {
        return new Vector3(
            cachedMin.x + (col + 0.5f) * cellSize,
            cachedMin.y + (row + 0.5f) * cellSize,
            0f
        );
    }

    public bool IsBlocked(int col, int row)
    {
        if (blocked == null || (uint)col >= (uint)columns || (uint)row >= (uint)rows)
            return true;
        return blocked[col + row * columns];
    }

    public bool IsBlockedWorld(Vector3 worldPos)
    {
        int col = (int)((worldPos.x - cachedMin.x) * invCellSize);
        int row = (int)((worldPos.y - cachedMin.y) * invCellSize);
        return IsBlocked(col, row);
    }

    public bool IsWall(Vector3 worldPos)
    {
        return blocked != null && IsBlockedWorld(worldPos);
    }

    public bool IsInsideMap(Vector3 worldPos)
    {
        return worldPos.x >= cachedMin.x && worldPos.x <= cachedMax.x
            && worldPos.y >= cachedMin.y && worldPos.y <= cachedMax.y;
    }

    public Vector3 ClampToMap(Vector3 worldPos)
    {
        if (worldPos.x < cachedMin.x) worldPos.x = cachedMin.x;
        else if (worldPos.x > cachedMax.x) worldPos.x = cachedMax.x;

        if (worldPos.y < cachedMin.y) worldPos.y = cachedMin.y;
        else if (worldPos.y > cachedMax.y) worldPos.y = cachedMax.y;

        return worldPos;
    }

    private void OnValidate()
    {
        cellSize = Mathf.Max(0.5f, cellSize);
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
    }
}
