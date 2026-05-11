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

    private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

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
        if (showMouseCell) DrawMouseCell();
        if (showPathfinding && pathStart != null && pathEnd != null) DrawPath();
    }

    private void DrawBounds()
    {
        Gizmos.color = boundsColor;
        Vector2 min = map.MapMin;
        Vector2 max = map.MapMax;

        // min.x / max.x = world X, min.y / max.y = world Z (stored as Vector2.y)
        Vector3[] corners = new Vector3[]
        {
            new Vector3(min.x, 0f, min.y),
            new Vector3(max.x, 0f, min.y),
            new Vector3(max.x, 0f, max.y),
            new Vector3(min.x, 0f, max.y)
        };

        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);

        Vector3 center = new Vector3((min.x + max.x) * 0.5f, 0f, (min.y + max.y) * 0.5f);
        Vector3 size   = new Vector3(max.x - min.x, 0f, max.y - min.y);
        Gizmos.DrawWireCube(center, size);
    }

    private void DrawGrid()
    {
        Gizmos.color = gridColor;
        Vector2 min = map.MapMin;
        Vector2 max = map.MapMax;
        float cellSize = map.CellSize;
        int cols = map.Columns;
        int rows = map.Rows;

        // Vertical lines along Z (fixed X, vary Z)
        for (int col = 0; col <= cols; col++)
        {
            float x = min.x + col * cellSize;
            Gizmos.DrawLine(new Vector3(x, 0f, min.y), new Vector3(x, 0f, max.y));
        }

        // Horizontal lines along X (fixed Z, vary X)
        for (int row = 0; row <= rows; row++)
        {
            float z = min.y + row * cellSize;
            Gizmos.DrawLine(new Vector3(min.x, 0f, z), new Vector3(max.x, 0f, z));
        }
    }

    private void DrawWalls()
    {
        float cellSize = map.CellSize;
        int cols = map.Columns;
        int rows = map.Rows;
        Vector3 wallSize = new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (!map.IsBlocked(col, row)) continue;

                Gizmos.color = wallColor;
                Vector3 center = map.CellToWorld(col, row);
                Gizmos.DrawCube(center, wallSize);
            }
        }
    }

    private void DrawMouseCell()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorld;
        if (!GetMouseWorldPosition(out mouseWorld)) return;
        if (!map.IsInsideMap(mouseWorld)) return;

        Vector2Int cell = map.WorldToCell(mouseWorld);
        Vector3 cellCenter = map.CellToWorld(cell.x, cell.y);

        Gizmos.color = mouseCellColor;
        Gizmos.DrawWireCube(cellCenter, new Vector3(map.CellSize, 0.05f, map.CellSize));

        bool isWall = map.IsBlocked(cell.x, cell.y);
        Gizmos.color = isWall ? Color.red : Color.green;
        Gizmos.DrawSphere(cellCenter, map.CellSize * 0.2f);
    }

    private void DrawPath()
    {
        if (map.Pathfinder == null) return;

        var path = map.Pathfinder.FindPath(pathStart.position, pathEnd.position);
        if (path == null || path.Count == 0) return;

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

    private void OnGUI()
    {
        if (map == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 260));
        GUILayout.BeginVertical("box");

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.fontSize = 14;

        GUILayout.Label("Map Debug Info", headerStyle);
        GUILayout.Space(5);

        GUILayout.Label($"Grid Size: {map.Columns} x {map.Rows}");
        GUILayout.Label($"Cell Size: {map.CellSize:F2}");
        GUILayout.Label($"Map Size: {map.MapWidth:F2} x {map.MapHeight:F2}");
        GUILayout.Label($"Bounds X: ({map.MapMin.x:F1}) to ({map.MapMax.x:F1})");
        GUILayout.Label($"Bounds Z: ({map.MapMin.y:F1}) to ({map.MapMax.y:F1})");

        GUILayout.Space(10);

        Vector3 mouseWorld;
        if (Camera.main != null && GetMouseWorldPosition(out mouseWorld))
        {
            if (map.IsInsideMap(mouseWorld))
            {
                Vector2Int cell = map.WorldToCell(mouseWorld);
                bool isWall = map.IsBlocked(cell.x, cell.y);

                GUILayout.Label($"Mouse World: ({mouseWorld.x:F2}, {mouseWorld.z:F2})");
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
        GUILayout.BeginArea(new Rect(10, 280, 300, 150));
        GUILayout.BeginVertical("box");

        GUILayout.Label("Controls", GUI.skin.box);

        showGrid        = GUILayout.Toggle(showGrid,        "Show Grid");
        showWalls       = GUILayout.Toggle(showWalls,       "Show Walls");
        showBounds      = GUILayout.Toggle(showBounds,      "Show Bounds");
        showMouseCell   = GUILayout.Toggle(showMouseCell,   "Show Mouse Cell");
        showPathfinding = GUILayout.Toggle(showPathfinding, "Show Pathfinding");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // Raycast từ camera xuống ground plane Y=0 để lấy world position trên X,Z
    private static bool GetMouseWorldPosition(out Vector3 worldPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (GroundPlane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            return true;
        }
        worldPos = Vector3.zero;
        return false;
    }
}
