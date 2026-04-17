using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Test script để verify Spatial Grid hoạt động đúng.
/// </summary>
public class SpatialGridTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private CharacterBase testCharacter;
    [SerializeField] private float testRadius = 15f;

    [ContextMenu("Test Spatial Grid Query")]
    public void TestSpatialGridQuery()
    {
        if (testCharacter == null)
        {
            Debug.LogError("[Test] Test Character chưa được gán!");
            return;
        }

        if (CharacterManager.Instance == null)
        {
            Debug.LogError("[Test] CharacterManager.Instance is NULL!");
            return;
        }

        Vector3 position = testCharacter.Position;
        List<CharacterBase> results = new List<CharacterBase>();

        Debug.Log($"[Test] ========== SPATIAL GRID TEST ==========");
        Debug.Log($"[Test] Test Character: {testCharacter.CharacterName}");
        Debug.Log($"[Test] Position: {position}");
        Debug.Log($"[Test] Test Radius: {testRadius}");
        Debug.Log($"[Test] Total Characters in Manager: {CharacterManager.Instance.CharacterCount}");

        // Query nearby characters
        CharacterManager.Instance.GetNearbyCharacters(position, testRadius, results);

        Debug.Log($"[Test] Nearby Characters Found: {results.Count}");

        if (results.Count == 0)
        {
            Debug.LogWarning("[Test] NO characters found! Possible issues:");
            Debug.LogWarning("  1. No other characters in scene");
            Debug.LogWarning("  2. Characters not registered in CharacterManager");
            Debug.LogWarning("  3. All characters outside test radius");
            Debug.LogWarning("  4. Spatial Grid cell size mismatch");
        }
        else
        {
            for (int i = 0; i < results.Count; i++)
            {
                CharacterBase other = results[i];
                float distance = Vector3.Distance(position, other.Position);
                int swords = other.SwordCount;
                bool isWeaker = swords < testCharacter.SwordCount;

                Debug.Log($"[Test] {i + 1}. {other.CharacterName}:");
                Debug.Log($"     Position: {other.Position}");
                Debug.Log($"     Distance: {distance:F2} units");
                Debug.Log($"     Swords: {swords} (Weaker: {isWeaker})");
                Debug.Log($"     HP: {other.CurrentHp}/{other.MaxHp}");
                Debug.Log($"     Active: {other.gameObject.activeInHierarchy}");
            }
        }

        Debug.Log($"[Test] ========================================");
    }

    [ContextMenu("Test All Characters Registered")]
    public void TestAllCharactersRegistered()
    {
        if (CharacterManager.Instance == null)
        {
            Debug.LogError("[Test] CharacterManager.Instance is NULL!");
            return;
        }

        var allCharacters = FindObjectsOfType<CharacterBase>();
        int registered = CharacterManager.Instance.CharacterCount;

        Debug.Log($"[Test] ========== REGISTRATION TEST ==========");
        Debug.Log($"[Test] Characters in Scene: {allCharacters.Length}");
        Debug.Log($"[Test] Characters Registered: {registered}");

        if (allCharacters.Length != registered)
        {
            Debug.LogWarning($"[Test] MISMATCH! {allCharacters.Length - registered} characters not registered!");

            foreach (var character in allCharacters)
            {
                // Try to query this character
                List<CharacterBase> results = new List<CharacterBase>();
                CharacterManager.Instance.GetNearbyCharacters(character.Position, 0.1f, results);

                bool isRegistered = results.Contains(character);
                Debug.Log($"  {character.CharacterName}: {(isRegistered ? "✓ Registered" : "✗ NOT Registered")}");
            }
        }
        else
        {
            Debug.Log($"[Test] ✓ All characters registered correctly!");
        }

        Debug.Log($"[Test] ========================================");
    }

    [ContextMenu("Test Vision Radius")]
    public void TestVisionRadius()
    {
        if (testCharacter == null)
        {
            Debug.LogError("[Test] Test Character chưa được gán!");
            return;
        }

        var stateMachine = testCharacter.GetComponent<CharacterStateMachine>();
        if (stateMachine == null)
        {
            Debug.LogError("[Test] Character không có CharacterStateMachine!");
            return;
        }

        Debug.Log($"[Test] ========== VISION RADIUS TEST ==========");
        Debug.Log($"[Test] Character: {testCharacter.CharacterName}");
        Debug.Log($"[Test] Vision Radius: {stateMachine.VisionRadius}");
        Debug.Log($"[Test] Vision Radius Squared: {stateMachine.VisionRadiusSq}");
        Debug.Log($"[Test] CharMgr: {(stateMachine.CharMgr != null ? "✓ OK" : "✗ NULL")}");
        Debug.Log($"[Test] ========================================");
    }

    private void OnDrawGizmosSelected()
    {
        if (testCharacter == null) return;

        // Draw test radius
        Gizmos.color = Color.cyan;
        DrawWireCircle(testCharacter.Position, testRadius, 64);

        // Draw to all nearby characters
        if (Application.isPlaying && CharacterManager.Instance != null)
        {
            List<CharacterBase> results = new List<CharacterBase>();
            CharacterManager.Instance.GetNearbyCharacters(testCharacter.Position, testRadius, results);

            foreach (var other in results)
            {
                if (other == testCharacter) continue;

                Gizmos.color = other.SwordCount < testCharacter.SwordCount ? Color.green : Color.red;
                Gizmos.DrawLine(testCharacter.Position, other.Position);
                Gizmos.DrawWireSphere(other.Position, 0.5f);
            }
        }
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
}
