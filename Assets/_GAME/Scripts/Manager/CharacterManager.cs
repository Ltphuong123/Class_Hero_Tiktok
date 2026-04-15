using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized manager for all CharacterBase instances.
/// Provides batch updates, spatial queries, and deferred registration
/// to handle 500+ characters efficiently.
/// </summary>
public class CharacterManager : Singleton<CharacterManager>
{
    [SerializeField] private bool persistAcrossScenes = false;

    private SpatialGrid<CharacterBase> grid;
    private readonly List<CharacterBase> characters = new();
    private readonly List<CharacterBase> pendingAdd = new();
    private readonly List<CharacterBase> pendingRemove = new();
    private readonly HashSet<CharacterBase> characterSet = new();
    private bool isUpdating = false;

    public int CharacterCount => characterSet.Count;

    protected override void Awake()
    {
        base.Awake();

        float cellSize = MapManager.Instance != null ? MapManager.Instance.CellSize : 5f;
        grid = new SpatialGrid<CharacterBase>(cellSize);

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Flush pending additions
        for (int i = 0; i < pendingAdd.Count; i++)
        {
            CharacterBase c = pendingAdd[i];
            if (characterSet.Add(c))
            {
                characters.Add(c);
                grid.Add(c, c.transform.position);
            }
        }
        pendingAdd.Clear();

        // Flush pending removals
        for (int i = 0; i < pendingRemove.Count; i++)
        {
            CharacterBase c = pendingRemove[i];
            if (characterSet.Remove(c))
            {
                characters.Remove(c);
                grid.Remove(c);
            }
        }
        pendingRemove.Clear();

        // Batch update loop
        isUpdating = true;
        float dt = Time.deltaTime;

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterBase c = characters[i];
            Vector3 prevPos = c.transform.position; // lưu vị trí trước khi di chuyển

            if (c is IManagedUpdate managed)
                managed.ManagedUpdate(dt);

            // Clamp position to map bounds and prevent entering wall cells
            Vector3 pos = c.transform.position;
            MapManager map = MapManager.Instance;
            if (map != null)
            {
                pos = map.ClampToMap(pos);

                // If new position is inside a wall, revert to previous position
                WallGrid wg = map.WallGrid;
                if (wg != null && wg.IsBlockedWorld(pos))
                {
                    // Try keeping only X movement
                    Vector3 tryX = new Vector3(pos.x, prevPos.y, pos.z);
                    // Try keeping only Y movement
                    Vector3 tryY = new Vector3(prevPos.x, pos.y, pos.z);

                    if (!wg.IsBlockedWorld(tryX))
                        pos = tryX; // slide along X
                    else if (!wg.IsBlockedWorld(tryY))
                        pos = tryY; // slide along Y
                    else
                        pos = prevPos; // fully blocked, stay put
                }
            }
            pos.z = pos.y + 25f;
            c.transform.position = pos;

            grid.UpdatePosition(c, pos);
        }

        isUpdating = false;
    }

    /// <summary>
    /// Register a character with the manager. Idempotent — duplicate calls are ignored.
    /// If called during the update loop, registration is deferred to the next frame.
    /// </summary>
    public void Register(CharacterBase character)
    {
        if (characterSet.Contains(character))
            return;

        if (isUpdating)
        {
            pendingAdd.Add(character);
            return;
        }

        characterSet.Add(character);
        characters.Add(character);
        grid.Add(character, character.transform.position);
    }

    /// <summary>
    /// Deregister a character from the manager. Silently ignores unregistered characters.
    /// If called during the update loop, deregistration is deferred to the next frame.
    /// </summary>
    public void Deregister(CharacterBase character)
    {
        if (!characterSet.Contains(character))
            return;

        if (isUpdating)
        {
            pendingRemove.Add(character);
            return;
        }

        characterSet.Remove(character);
        characters.Remove(character);
        grid.Remove(character);
    }

    /// <summary>
    /// Get all characters within a radius of a position. Delegates to the spatial grid.
    /// </summary>
    public void GetNearbyCharacters(Vector3 position, float radius, List<CharacterBase> results)
    {
        grid.GetInRadius(position, radius, results);
    }

    /// <summary>
    /// Get the nearest character within a radius, optionally excluding a specific character.
    /// </summary>
    public CharacterBase GetNearestCharacter(Vector3 position, float radius, CharacterBase excludeSelf = null)
    {
        return grid.GetNearest(position, radius, excludeSelf);
    }

    /// <summary>
    /// Get all characters within a radius, sorted by distance ascending.
    /// </summary>
    public void GetCharactersInRadius(Vector3 position, float radius, List<CharacterBase> results)
    {
        grid.GetInRadius(position, radius, results);

        Vector3 center = position;
        results.Sort((a, b) =>
        {
            float distA = (a.transform.position - center).sqrMagnitude;
            float distB = (b.transform.position - center).sqrMagnitude;
            return distA.CompareTo(distB);
        });
    }
}
