using UnityEngine;

public class MapDebug : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showGrid = true;
    [SerializeField] private bool showWalls = true;
    [SerializeField] private bool showBounds = true;
    [SerializeField] private bool showCellCoordinates = false;
    
    [Header("Colors")]
    [SerializeField] private Color gridColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color wallColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private Color boundsColor = new Color(0f, 0f, 1f, 1f);
    [SerializeField] private Color openCellColor = new Color(0f, 1f, 0f, 0.2f);
    
    private MapManager map;

    private void Start()
    {
        map = MapManager.Instance;
    }

    private void OnDrawGizmos()
    {
        if (map == null) map = MapManager.Instance;
        if (map == null) return;

        if (showBounds) DrawBounds();
        if (showGrid) DrawGrid();
        if (showWalls) DrawWalls();
    }

    private void DrawBounds()
    {
        Gizmos.color = boundsColor;
        Vector2 min = map.MapMin;
        Vector2 max = map.MapMax;
        
        Vector3 bottomLeft = new Vector3(min.x, min.y, 0f);
        Vector3 bottomRight = new Vector3(max.x, min.y, 0f);
        Vector3 topLeft = new Vector3(min.x, max.y, 0f);
        Vector3 topRight = new Vector3(max.x, max.y, 0f);
        
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }

    private void DrawGrid()
    {
        Gizmos.color = gridColor;
        Vector2 min = map.MapMin;
        Vector2 max = map.MapMax;
        float cellSize = map.CellSize;
        int cols = map.Columns;
        int rows = map.Rows;

        for (int col = 0; col <= cols; col++)
        {
            float x = min.x + col * cellSize;
            Vector3 start = new Vector3(x, min.y, 0f);
            Vector3 end = new Vector3(x, max.y, 0f);
            Gizmos.DrawLine(start, end);
        }

        for (int row = 0; row <= rows; row++)
        {
            float y = min.y + row * cellSize;
            Vector3 start = new Vector3(min.x, y, 0f);
            Vector3 end = new Vector3(max.x, y, 0f);
            Gizmos.DrawLine(start, end);
        }
    }

    private void DrawWalls()
    {
        float cellSize = map.CellSize;
        int cols = map.Columns;
        int rows = map.Rows;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (map.IsBlocked(col, row))
                {
                    Gizmos.color = wallColor;
                    Vector3 center = map.CellToWorld(col, row);
                    Gizmos.DrawCube(center, Vector3.one * cellSize * 0.9f);
                }
                else if (showCellCoordinates)
                {
                    Gizmos.color = openCellColor;
                    Vector3 center = map.CellToWorld(col, row);
                    Gizmos.DrawWireCube(center, Vector3.one * cellSize * 0.8f);
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (map == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 250, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Map Debug Info", GUI.skin.box);
        GUILayout.Label($"Columns: {map.Columns}");
        GUILayout.Label($"Rows: {map.Rows}");
        GUILayout.Label($"Cell Size: {map.CellSize}");
        GUILayout.Label($"Map Width: {map.MapWidth:F2}");
        GUILayout.Label($"Map Height: {map.MapHeight:F2}");
        
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int cell = map.WorldToCell(mouseWorld);
        GUILayout.Label($"Mouse Cell: ({cell.x}, {cell.y})");
        
        bool isWall = map.IsBlocked(cell.x, cell.y);
        GUILayout.Label($"Is Wall: {isWall}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
#endif
}
