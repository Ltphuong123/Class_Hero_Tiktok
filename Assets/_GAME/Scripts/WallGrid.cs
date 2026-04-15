using UnityEngine;

/// <summary>
/// Bakes a boolean grid marking which cells are blocked by walls.
/// Uses Physics2D.OverlapBox per cell at startup (and on-demand rebake).
/// Shared by CharacterManager (movement blocking) and GridPathfinder (A*).
/// </summary>
public class WallGrid
{
    private bool[] blocked; // flat array [col + row * columns]
    private int columns;
    private int rows;
    private float cellSize;
    private Vector2 mapMin;

    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;
    public Vector2 MapMin => mapMin;

    public WallGrid(int columns, int rows, float cellSize, Vector2 mapMin)
    {
        this.columns = columns;
        this.rows = rows;
        this.cellSize = cellSize;
        this.mapMin = mapMin;
        blocked = new bool[columns * rows];
    }

    /// <summary>
    /// Bake the grid by checking each cell for wall colliders.
    /// Call once at Start or whenever the map layout changes.
    /// </summary>
    public void Bake(LayerMask wallMask)
    {
        Vector2 halfCell = new Vector2(cellSize * 0.45f, cellSize * 0.45f); // slightly smaller to avoid edge false positives

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector2 center = new Vector2(
                    mapMin.x + (col + 0.5f) * cellSize,
                    mapMin.y + (row + 0.5f) * cellSize
                );

                Collider2D hit = Physics2D.OverlapBox(center, halfCell * 2f, 0f, wallMask);
                blocked[col + row * columns] = (hit != null);
            }
        }
    }

    /// <summary>
    /// Check if a cell is blocked (wall).
    /// Returns true for out-of-bounds coordinates.
    /// </summary>
    public bool IsBlocked(int col, int row)
    {
        if (col < 0 || col >= columns || row < 0 || row >= rows)
            return true;
        return blocked[col + row * columns];
    }

    /// <summary>
    /// Check if a world position is in a blocked cell.
    /// </summary>
    public bool IsBlockedWorld(Vector3 worldPos)
    {
        int col = Mathf.FloorToInt((worldPos.x - mapMin.x) / cellSize);
        int row = Mathf.FloorToInt((worldPos.y - mapMin.y) / cellSize);
        return IsBlocked(col, row);
    }

    /// <summary>
    /// Convert world position to cell coordinates (clamped).
    /// </summary>
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        int col = Mathf.Clamp(Mathf.FloorToInt((worldPos.x - mapMin.x) / cellSize), 0, columns - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt((worldPos.y - mapMin.y) / cellSize), 0, rows - 1);
        return new Vector2Int(col, row);
    }

    /// <summary>
    /// Convert cell coordinates to world center position.
    /// </summary>
    public Vector3 CellToWorld(int col, int row)
    {
        return new Vector3(
            mapMin.x + (col + 0.5f) * cellSize,
            mapMin.y + (row + 0.5f) * cellSize,
            0f
        );
    }
}
