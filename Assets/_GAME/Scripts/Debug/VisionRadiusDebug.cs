using UnityEngine;

/// <summary>
/// Debug script để hiển thị vision radius của character.
/// Attach vào GameObject có CharacterStateMachine.
/// </summary>
[RequireComponent(typeof(CharacterStateMachine))]
public class VisionRadiusDebug : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showVisionRadius = true;
    [SerializeField] private bool showChaseGiveUpRadius = true;
    [SerializeField] private bool showSeparationRadius = false;
    [SerializeField] private bool showTargetLine = true;

    [Header("Colors")]
    [SerializeField] private Color visionColor = new Color(0f, 1f, 0f, 0.3f);      // Green
    [SerializeField] private Color chaseGiveUpColor = new Color(1f, 1f, 0f, 0.3f); // Yellow
    [SerializeField] private Color separationColor = new Color(1f, 0f, 0f, 0.3f);  // Red
    [SerializeField] private Color targetLineColor = new Color(1f, 0f, 0f, 1f);    // Red

    [Header("Display")]
    [SerializeField] private int circleSegments = 64;
    [SerializeField] private float lineWidth = 0.1f;

    private CharacterStateMachine stateMachine;
    private CharacterBase characterBase;

    private void Awake()
    {
        stateMachine = GetComponent<CharacterStateMachine>();
        characterBase = GetComponent<CharacterBase>();
    }

    private void OnDrawGizmos()
    {
        if (stateMachine == null) return;

        Vector3 position = transform.position;

        // Vision Radius (15 units)
        if (showVisionRadius)
        {
            Gizmos.color = visionColor;
            DrawCircle(position, stateMachine.VisionRadius, circleSegments);
        }

        // Chase Give Up Radius (18 units = vision × 1.2)
        if (showChaseGiveUpRadius)
        {
            Gizmos.color = chaseGiveUpColor;
            DrawCircle(position, stateMachine.VisionRadius * 1.2f, circleSegments);
        }

        // Separation Radius (1.2 units)
        if (showSeparationRadius)
        {
            Gizmos.color = separationColor;
            DrawCircle(position, stateMachine.SeparationRadius, circleSegments);
        }

        // Draw line to current target
        if (showTargetLine && Application.isPlaying)
        {
            if (stateMachine.CurrentState == stateMachine.Attack)
            {
                // Get target from AttackState (need to expose it)
                DrawTargetInfo(position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw more detailed info when selected
        if (stateMachine == null) return;

        Vector3 position = transform.position;

        // Draw labels
        DrawLabel(position + Vector3.up * 2f, $"Vision: {stateMachine.VisionRadius}u");
        DrawLabel(position + Vector3.up * 2.5f, $"Swords: {stateMachine.MySwordCount}");
        DrawLabel(position + Vector3.up * 3f, $"State: {stateMachine.CurrentState?.GetType().Name}");
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
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

    private void DrawTargetInfo(Vector3 position)
    {
        // Find nearby characters to visualize
        if (stateMachine.CharMgr == null) return;

        stateMachine.CharMgr.GetNearbyCharacters(position, stateMachine.VisionRadius, stateMachine.NearbyCharacters);

        foreach (var other in stateMachine.NearbyCharacters)
        {
            if (other == stateMachine.Owner || other.CurrentHp <= 0f) continue;

            int otherSwords = other.SwordCount;
            int mySwords = stateMachine.MySwordCount;

            // Color code based on threat level
            if (otherSwords < mySwords)
            {
                // Weaker target (can attack)
                Gizmos.color = Color.green;
            }
            else if (otherSwords == mySwords)
            {
                // Equal (neutral)
                Gizmos.color = Color.yellow;
            }
            else
            {
                // Stronger (threat)
                Gizmos.color = Color.red;
            }

            // Draw line to target
            Gizmos.DrawLine(position, other.Position);

            // Draw sphere at target
            Gizmos.DrawWireSphere(other.Position, 0.5f);
        }
    }

    private void DrawLabel(Vector3 position, string text)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(position, text);
#endif
    }

    // Runtime debug visualization
    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        if (stateMachine == null || characterBase == null) return;

        // Only show for selected character or player
        if (!IsSelected()) return;

        // Draw info panel
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label($"<b>{characterBase.CharacterName}</b>", new GUIStyle(GUI.skin.label) { richText = true });
        GUILayout.Label($"State: {stateMachine.CurrentState?.GetType().Name}");
        GUILayout.Label($"Swords: {stateMachine.MySwordCount}");
        GUILayout.Label($"HP: {characterBase.CurrentHp:F0}/{characterBase.MaxHp:F0}");
        GUILayout.Label($"Vision Radius: {stateMachine.VisionRadius} units");
        
        GUILayout.Space(10);
        
        // Nearby characters info
        if (stateMachine.NearbyCharacters.Count > 0)
        {
            GUILayout.Label($"<b>Nearby Characters: {stateMachine.NearbyCharacters.Count}</b>", new GUIStyle(GUI.skin.label) { richText = true });
            
            foreach (var other in stateMachine.NearbyCharacters)
            {
                if (other == stateMachine.Owner) continue;
                
                float distance = Vector3.Distance(transform.position, other.Position);
                int otherSwords = other.SwordCount;
                string status = otherSwords < stateMachine.MySwordCount ? "✓ Can Attack" : "✗ Too Strong";
                
                GUILayout.Label($"  {other.CharacterName}: {otherSwords} swords, {distance:F1}u - {status}");
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private bool IsSelected()
    {
#if UNITY_EDITOR
        return UnityEditor.Selection.activeGameObject == gameObject;
#else
        return false;
#endif
    }
}
