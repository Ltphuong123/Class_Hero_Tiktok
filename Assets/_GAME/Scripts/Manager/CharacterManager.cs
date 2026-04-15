using UnityEngine;
using System.Collections.Generic;

public class CharacterManager : Singleton<CharacterManager>
{
    [SerializeField] private bool persistAcrossScenes = false;

    private SpatialGrid<CharacterBase> grid;
    private readonly List<CharacterBase> characters = new();
    private readonly List<CharacterBase> pendingAdd = new();
    private readonly List<CharacterBase> pendingRemove = new();
    private readonly HashSet<CharacterBase> characterSet = new();
    private MapManager cachedMap;
    private bool isUpdating;

    public int CharacterCount => characterSet.Count;

    protected override void Awake()
    {
        base.Awake();

        cachedMap = MapManager.Instance;
        float cellSize = cachedMap != null ? cachedMap.CellSize : 5f;
        grid = new SpatialGrid<CharacterBase>(cellSize);

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        FlushPending();

        isUpdating = true;
        float dt = Time.deltaTime;
        int count = characters.Count;
        bool hasMap = cachedMap != null;

        for (int i = 0; i < count; i++)
        {
            CharacterBase c = characters[i];
            Transform t = c.transform;
            Vector3 prevPos = t.position;

            if (c is IManagedUpdate managed)
                managed.ManagedUpdate(dt);

            Vector3 pos = t.position;

            if (hasMap)
            {
                pos = cachedMap.ClampToMap(pos);

                if (cachedMap.IsBlockedWorld(pos))
                {
                    Vector3 tryX = new Vector3(pos.x, prevPos.y, pos.z);
                    if (!cachedMap.IsBlockedWorld(tryX))
                    {
                        pos = tryX;
                    }
                    else
                    {
                        Vector3 tryY = new Vector3(prevPos.x, pos.y, pos.z);
                        pos = !cachedMap.IsBlockedWorld(tryY) ? tryY : prevPos;
                    }
                }
            }

            pos.z = pos.y + 25f;
            t.position = pos;
            grid.UpdatePosition(c, pos);
        }

        isUpdating = false;
    }

    private void FlushPending()
    {
        int addCount = pendingAdd.Count;
        for (int i = 0; i < addCount; i++)
        {
            CharacterBase c = pendingAdd[i];
            if (characterSet.Add(c))
            {
                characters.Add(c);
                grid.Add(c, c.transform.position);
            }
        }
        if (addCount > 0) pendingAdd.Clear();

        int removeCount = pendingRemove.Count;
        for (int i = 0; i < removeCount; i++)
        {
            CharacterBase c = pendingRemove[i];
            if (characterSet.Remove(c))
            {
                int idx = characters.IndexOf(c);
                if (idx >= 0)
                {
                    int last = characters.Count - 1;
                    characters[idx] = characters[last];
                    characters.RemoveAt(last);
                }
                grid.Remove(c);
            }
        }
        if (removeCount > 0) pendingRemove.Clear();
    }

    public void Register(CharacterBase character)
    {
        if (characterSet.Contains(character)) return;

        if (isUpdating)
        {
            pendingAdd.Add(character);
            return;
        }

        characterSet.Add(character);
        characters.Add(character);
        grid.Add(character, character.transform.position);
    }

    public void Deregister(CharacterBase character)
    {
        if (!characterSet.Contains(character)) return;

        if (isUpdating)
        {
            pendingRemove.Add(character);
            return;
        }

        characterSet.Remove(character);
        int idx = characters.IndexOf(character);
        if (idx >= 0)
        {
            int last = characters.Count - 1;
            characters[idx] = characters[last];
            characters.RemoveAt(last);
        }
        grid.Remove(character);
    }

    public void GetNearbyCharacters(Vector3 position, float radius, List<CharacterBase> results)
    {
        grid.GetInRadius(position, radius, results);
    }

    public CharacterBase GetNearestCharacter(Vector3 position, float radius, CharacterBase excludeSelf = null)
    {
        return grid.GetNearest(position, radius, excludeSelf);
    }

    public void GetCharactersInRadius(Vector3 position, float radius, List<CharacterBase> results)
    {
        grid.GetInRadius(position, radius, results);

        Vector3 center = position;
        results.Sort((a, b) =>
        {
            float dA = (a.transform.position - center).sqrMagnitude;
            float dB = (b.transform.position - center).sqrMagnitude;
            return dA.CompareTo(dB);
        });
    }
}
