using UnityEngine;

public class MapDebugAdvanced : MonoBehaviour
{
    [Header("Visualization")]
    [SerializeField] private bool showGrid = true;
    [SerializeField] private bool showWalls = true;
    [SerializeField] private bool showBounds = true;
    [SerializeField] private bool showMouseCell = true;
    [SerializeField] private bool showPathfinding = false;
    
    [Header("Colors")]
    [SerializeField] private Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [SerializeField] private Color wallColor = new Color(1f, 0f, 0f, 0.6f);
    [SerializeField] private Color boundsColor = Color.cyan;
    [SerializeField] private Color mouseCellColor = Color.yellow;
    [SerializeField] private Color pathColor = Color.green;
    
    [Header("Pathfinding Test")]
    [SerializeField] private Transform pathStart;
    [SerializeField] private Transform pathEnd;
    
    private MapManager map;
    private Vector2Int lastMouseCell = new Vector2Int(-1, -1);

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
        if (showMouseCell) DrawMouseCell();
        if (showPathfinding && pathStart != null && pathEnd != null) DrawPath();
    }

    private void DrawBounds()
    {
        Gizmos.color = boundsColor;
        Vector2 min = map.MapMin;
        Vector2 max = map.MapMax;
        
        Vector3[] corners = new Vector3[]
        {
            new Vector3(min.x, min.y, 0f),
            new Vector3(max.x, min.y, 0f),
            new Vector3(max.x, max.y, 0f),
            new Vector3(min.x, max.y, 0f)
        };
        
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }
        
        Gizmos.DrawWireCube(new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f), 
                           new Vector3(max.x - min.x, max.y - min.y, 0f));
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
            Gizmos.DrawLine(new Vector3(x, min.y, 0f), new Vector3(x, max.y, 0f));
        }

        for (int row = 0; row <= rows; row++)
        {
            float y = min.y + row * cellSize;
            Gizmos.DrawLine(new Vector3(min.x, y, 0f), new Vector3(max.x, y, 0f));
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
                    Gizmos.DrawCube(center, Vector3.one * cellSize * 0.95f);
                }
            }
        }
    }

    private void DrawMouseCell()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        
        if (!map.IsInsideMap(mouseWorld)) return;

        Vector2Int cell = map.WorldToCell(mouseWorld);
        Vector3 cellCenter = map.CellToWorld(cell.x, cell.y);
        
        Gizmos.color = mouseCellColor;
        Gizmos.DrawWireCube(cellCenter, Vector3.one * map.CellSize);
        
        bool isWall = map.IsBlocked(cell.x, cell.y);
        Gizmos.color = isWall ? Color.red : Color.green;
        Gizmos.DrawSphere(cellCenter, map.CellSize * 0.2f);
    }

    private void DrawPath()
    {
        if (map.Pathfinder == null) return;

        var path = map.Pathfinder.FindPath(pathStart.position, pathEnd.position);
        
        if (path != null && path.Count > 0)
        {
            Gizmos.color = pathColor;
            
            Vector3 prev = pathStart.position;
            foreach (var point in path)
            {
                Gizmos.DrawLine(prev, point);
                Gizmos.DrawSphere(point, map.CellSize * 0.15f);
                prev = point;
            }
            
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(pathStart.position, map.CellSize * 0.3f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(pathEnd.position, map.CellSize * 0.3f);
        }
    }

    private void OnGUI()
    {
        if (map == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        GUILayout.BeginVertical("box");
        
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.fontSize = 14;
        
        GUILayout.Label("Map Debug Info", headerStyle);
        GUILayout.Space(5);
        
        GUILayout.Label($"Grid Size: {map.Columns} x {map.Rows}");
        GUILayout.Label($"Cell Size: {map.CellSize:F2}");
        GUILayout.Label($"Map Size: {map.MapWidth:F2} x {map.MapHeight:F2}");
        GUILayout.Label($"Bounds: ({map.MapMin.x:F1}, {map.MapMin.y:F1}) to ({map.MapMax.x:F1}, {map.MapMax.y:F1})");
        
        GUILayout.Space(10);
        
        if (Camera.main != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            
            if (map.IsInsideMap(mouseWorld))
            {
                Vector2Int cell = map.WorldToCell(mouseWorld);
                bool isWall = map.IsBlocked(cell.x, cell.y);
                
                GUILayout.Label($"Mouse Position: ({mouseWorld.x:F2}, {mouseWorld.y:F2})");
                GUILayout.Label($"Cell: ({cell.x}, {cell.y})");
                
                GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
                statusStyle.normal.textColor = isWall ? Color.red : Color.green;
                GUILayout.Label($"Status: {(isWall ? "WALL" : "OPEN")}", statusStyle);
            }
            else
            {
                GUILayout.Label("Mouse: Outside Map");
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
        
        DrawControls();
    }

    private void DrawControls()
    {
        GUILayout.BeginArea(new Rect(10, 270, 300, 150));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Controls", GUI.skin.box);
        
        showGrid = GUILayout.Toggle(showGrid, "Show Grid");
        showWalls = GUILayout.Toggle(showWalls, "Show Walls");
        showBounds = GUILayout.Toggle(showBounds, "Show Bounds");
        showMouseCell = GUILayout.Toggle(showMouseCell, "Show Mouse Cell");
        showPathfinding = GUILayout.Toggle(showPathfinding, "Show Pathfinding");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
