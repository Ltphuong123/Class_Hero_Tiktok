using UnityEngine;

/// <summary>
/// Simple debug script - chỉ hiển thị vòng tròn vision radius.
/// Attach vào bất kỳ character nào cần debug.
/// </summary>
public class SimpleVisionDebug : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float visionRadius = 15f;
    [SerializeField] private Color visionColor = Color.green;
    [SerializeField] private Color chaseGiveUpColor = Color.yellow;
    [SerializeField] private int segments = 64;

    private void OnDrawGizmos()
    {
        Vector3 pos = transform.position;

        // Vision radius (15 units) - Green
        Gizmos.color = visionColor;
        DrawWireCircle(pos, visionRadius, segments);

        // Chase give up radius (18 units) - Yellow
        Gizmos.color = chaseGiveUpColor;
        DrawWireCircle(pos, visionRadius * 1.2f, segments);
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw text when selected
        Vector3 pos = transform.position;
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * 2f, $"Vision: {visionRadius}u");
        UnityEditor.Handles.Label(pos + Vector3.up * 2.5f, $"Give Up: {visionRadius * 1.2f}u");
#endif
    }
}
