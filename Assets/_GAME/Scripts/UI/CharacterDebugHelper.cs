using UnityEngine;

/// <summary>
/// Helper class để spawn items cho debug panel.
/// Đặt script này vào một GameObject trong scene và gán Sword Prefab.
/// </summary>
public class CharacterDebugHelper : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Sword swordPrefab;

    private static CharacterDebugHelper instance;
    public static CharacterDebugHelper Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Spawn sword và tự động collect cho character.
    /// </summary>
    public bool SpawnSwordForCharacter(CharacterBase character, int count = 1)
    {
        if (character == null || swordPrefab == null)
        {
            Debug.LogError("[CharacterDebugHelper] Character hoặc Sword Prefab null!");
            return false;
        }

        SwordOrbit orbit = character.GetSwordOrbit();
        if (orbit == null)
        {
            Debug.LogError($"[CharacterDebugHelper] {character.CharacterName} không có SwordOrbit!");
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            Sword sword = Instantiate(swordPrefab, character.Position, Quaternion.identity);
            sword.Collect(character);
        }

        Debug.Log($"[CharacterDebugHelper] Đã spawn {count} kiếm cho {character.CharacterName}");
        return true;
    }

    /// <summary>
    /// Spawn sword tại vị trí cụ thể.
    /// </summary>
    public Sword SpawnSwordAtPosition(Vector3 position)
    {
        if (swordPrefab == null)
        {
            Debug.LogError("[CharacterDebugHelper] Sword Prefab null!");
            return null;
        }

        Sword sword = Instantiate(swordPrefab, position, Quaternion.identity);
        return sword;
    }
}
