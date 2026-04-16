using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Vẽ đường đi (PathBuffer) của tất cả nhân vật AI.
/// Hiển thị trong cả Scene view (Gizmos) và Game view (GL).
/// Attach vào bất kỳ GameObject nào trong scene.
/// </summary>
public class PathDebug : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool showInGameView = true;
    [SerializeField] private bool showInSceneView = true;
    [SerializeField] private bool showCurrentPosition = true;
    [SerializeField] private bool showStateName = true;

    [Header("Colors")]
    [SerializeField] private Color pathColor = new Color(0f, 1f, 0.5f, 0.8f);
    [SerializeField] private Color waypointColor = new Color(1f, 1f, 0f, 0.9f);
    [SerializeField] private Color positionColor = new Color(1f, 0.3f, 0.3f, 0.9f);

    [Header("Size")]
    [SerializeField] private float waypointSize = 0.15f;
    [SerializeField] private float positionSize = 0.2f;

    private static Material lineMaterial;
    private CharacterBase[] characters;
    private float refreshTimer;
    private const float RefreshInterval = 0.2f;

    private static void CreateLineMaterial()
    {
        if (lineMaterial != null) return;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = RefreshInterval;
            characters = FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
        }
    }

    // --- Game View rendering (GL) ---

    private void OnRenderObject()
    {
        if (!showInGameView || !Application.isPlaying) return;
        if (characters == null) return;

        CreateLineMaterial();
        lineMaterial.SetPass(0);

        foreach (var c in characters)
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (c.IsPlayerControlled) continue;

            CharacterStateMachine sm = c.GetStateMachine();
            if (sm == null) continue;

            List<Vector3> path = sm.PathBuffer;
            if (path == null || path.Count == 0) continue;

            // Vẽ đường từ vị trí hiện tại đến waypoint đầu tiên
            GL.Begin(GL.LINES);
            GL.Color(pathColor);
            Vector3 pos = c.Position;
            GL.Vertex3(pos.x, pos.y, pos.z);
            GL.Vertex3(path[0].x, path[0].y, path[0].z);

            // Vẽ đường giữa các waypoint
            for (int i = 0; i < path.Count - 1; i++)
            {
                GL.Vertex3(path[i].x, path[i].y, path[i].z);
                GL.Vertex3(path[i + 1].x, path[i + 1].y, path[i + 1].z);
            }
            GL.End();

            // Vẽ waypoint markers (hình thoi nhỏ)
            GL.Begin(GL.TRIANGLES);
            GL.Color(waypointColor);
            for (int i = 0; i < path.Count; i++)
            {
                DrawDiamondGL(path[i], waypointSize);
            }
            GL.End();

            // Vẽ vị trí hiện tại
            if (showCurrentPosition)
            {
                GL.Begin(GL.TRIANGLES);
                GL.Color(positionColor);
                DrawDiamondGL(pos, positionSize);
                GL.End();
            }
        }
    }

    private void DrawDiamondGL(Vector3 center, float size)
    {
        float x = center.x, y = center.y, z = center.z;
        // Top triangle
        GL.Vertex3(x, y + size, z);
        GL.Vertex3(x - size, y, z);
        GL.Vertex3(x + size, y, z);
        // Bottom triangle
        GL.Vertex3(x, y - size, z);
        GL.Vertex3(x + size, y, z);
        GL.Vertex3(x - size, y, z);
    }

    // --- Scene View rendering (Gizmos) ---

    private void OnDrawGizmos()
    {
        if (!showInSceneView || !Application.isPlaying) return;

        var chars = FindObjectsByType<CharacterBase>(FindObjectsSortMode.None);
        foreach (var c in chars)
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (c.IsPlayerControlled) continue;

            CharacterStateMachine sm = c.GetStateMachine();
            if (sm == null) continue;

            List<Vector3> path = sm.PathBuffer;
            if (path == null || path.Count == 0) continue;

            Vector3 pos = c.Position;

            // Đường path
            Gizmos.color = pathColor;
            Gizmos.DrawLine(pos, path[0]);
            for (int i = 0; i < path.Count - 1; i++)
                Gizmos.DrawLine(path[i], path[i + 1]);

            // Waypoint markers
            Gizmos.color = waypointColor;
            for (int i = 0; i < path.Count; i++)
                Gizmos.DrawWireSphere(path[i], waypointSize);

            // Vị trí hiện tại
            if (showCurrentPosition)
            {
                Gizmos.color = positionColor;
                Gizmos.DrawWireSphere(pos, positionSize);
            }

            // Tên state
            if (showStateName)
            {
#if UNITY_EDITOR
                string stateName = sm.CurrentState?.GetType().Name ?? "None";
                UnityEditor.Handles.Label(
                    pos + Vector3.up * 0.5f,
                    stateName,
                    new GUIStyle
                    {
                        fontSize = 10,
                        normal = { textColor = Color.white },
                        alignment = TextAnchor.MiddleCenter
                    }
                );
#endif
            }
        }
    }
}
