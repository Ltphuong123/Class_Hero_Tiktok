using UnityEngine;

/// <summary>
/// Debug visualizer for the SpatialGrid system.
/// Draws grid lines, map bounds, and highlights occupied cells in the Scene view.
/// Attach to any GameObject (e.g., the CharacterManager).
/// </summary>
public class SpatialGridDebug : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool showGrid = true;
    [SerializeField] private bool showBounds = true;
    [SerializeField] private bool showOccupiedCells = true;
    [SerializeField] private bool showCharacterCount = true;

    [Header("Colors")]
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color boundsColor = new Color(1f, 0f, 0f, 0.8f);
    [SerializeField] private Color occupiedCellColor = new Color(0f, 1f, 0f, 0.2f);
    [SerializeField] private Color itemCellColor = new Color(1f, 1f, 0f, 0.2f);

    private void OnDrawGizmos()
    {
        MapManager map = MapManager.Instance;
        if (map == null) return;

        Vector2 mapMin = map.MapMin;
        Vector2 mapMax = map.MapMax;
        float cellSize = map.CellSize;

        if (showBounds) DrawBounds(mapMin, mapMax);
        if (showGrid) DrawGrid(mapMin, mapMax, cellSize);
        if (showOccupiedCells) DrawOccupiedCells(cellSize);
    }

    private float GetCellSize()
    {
        MapManager map = MapManager.Instance;
        return map != null ? map.CellSize : 5f;
    }

    private void DrawBounds(Vector2 min, Vector2 max)
    {
        Gizmos.color = boundsColor;
        Vector3 bl = new Vector3(min.x, min.y, 0f);
        Vector3 br = new Vector3(max.x, min.y, 0f);
        Vector3 tl = new Vector3(min.x, max.y, 0f);
        Vector3 tr = new Vector3(max.x, max.y, 0f);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }

    private void DrawGrid(Vector2 min, Vector2 max, float cellSize)
    {
        Gizmos.color = gridColor;

        // Snap to cell boundaries
        float startX = Mathf.Floor(min.x / cellSize) * cellSize;
        float startY = Mathf.Floor(min.y / cellSize) * cellSize;

        // Vertical lines
        for (float x = startX; x <= max.x; x += cellSize)
        {
            Gizmos.DrawLine(
                new Vector3(x, min.y, 0f),
                new Vector3(x, max.y, 0f)
            );
        }

        // Horizontal lines
        for (float y = startY; y <= max.y; y += cellSize)
        {
            Gizmos.DrawLine(
                new Vector3(min.x, y, 0f),
                new Vector3(max.x, y, 0f)
            );
        }
    }

    private void DrawOccupiedCells(float cellSize)
    {
        if (!Application.isPlaying) return;

        CharacterManager charMgr = CharacterManager.Instance;
        if (charMgr == null) return;

        // Draw a highlight for each character's cell
        var characters = FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
        foreach (var c in characters)
        {
            int cx = Mathf.FloorToInt(c.Position.x / cellSize);
            int cy = Mathf.FloorToInt(c.Position.y / cellSize);

            Vector3 cellCenter = new Vector3(
                (cx + 0.5f) * cellSize,
                (cy + 0.5f) * cellSize,
                0f
            );

            Gizmos.color = occupiedCellColor;
            Gizmos.DrawCube(cellCenter, new Vector3(cellSize, cellSize, 0.01f));
        }

        // Draw item cells
        ItemManager itemMgr = ItemManager.Instance;
        if (itemMgr != null && itemMgr.DroppedSwordCount > 0)
        {
            var swords = FindObjectsByType<Sword>(FindObjectsSortMode.None);
            foreach (var s in swords)
            {
                if (s.State != SwordState.Dropped) continue;

                int cx = Mathf.FloorToInt(s.Position.x / cellSize);
                int cy = Mathf.FloorToInt(s.Position.y / cellSize);

                Vector3 cellCenter = new Vector3(
                    (cx + 0.5f) * cellSize,
                    (cy + 0.5f) * cellSize,
                    0f
                );

                Gizmos.color = itemCellColor;
                Gizmos.DrawCube(cellCenter, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.01f));
            }
        }
    }
}
