using UnityEngine;
using System.Collections.Generic;
using System;

public struct CharacterRankData
{
    public CharacterBase Character;
    public int Rank;
    public string Name;
    public Sprite Avatar;
    public float CurrentHp;
    public float MaxHp;
    public int SwordCount;
    public string StateName;
    public float Power;
}

public class CharacterManager : Singleton<CharacterManager>
{
    [SerializeField] private bool persistAcrossScenes = false;
    [SerializeField] private float rankUpdateInterval = 0.5f;

    private SpatialGrid<CharacterBase> grid;
    private readonly List<CharacterBase> characters = new();
    private readonly List<CharacterBase> pendingAdd = new();
    private readonly List<CharacterBase> pendingRemove = new();
    private readonly HashSet<CharacterBase> characterSet = new();
    private MapManager cachedMap;
    private bool isUpdating;

    // Ranking
    private readonly List<CharacterRankData> rankedList = new();
    private float rankTimer;
    private bool rankDirty = true;

    public int CharacterCount => characterSet.Count;
    public IReadOnlyList<CharacterRankData> RankedCharacters => rankedList;
    public event Action OnRankUpdated;

    protected override void Awake()
    {
        base.Awake();

        cachedMap = MapManager.Instance;
        float cellSize = cachedMap != null ? cachedMap.CellSize : 5f;
        grid = new SpatialGrid<CharacterBase>(cellSize);

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
        
        int count = characters.Count;
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                CharacterBase c = characters[i];
                if (c != null)
                    grid.Add(c, c.transform.position);
            }
        }
        
        if (pendingAdd.Count > 0)
            FlushPending();
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

        rankTimer -= dt;
        if (rankTimer <= 0f || rankDirty)
        {
            rankTimer = rankUpdateInterval;
            rankDirty = false;
            UpdateRanking();
        }
    }

    private void UpdateRanking()
    {
        rankedList.Clear();
        int count = characters.Count;
        for (int i = 0; i < count; i++)
        {
            CharacterBase c = characters[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (c.CurrentHp <= 0f) continue;

            int swords = c.SwordCount;
            float power = swords * 100f + c.CurrentHp;

            rankedList.Add(new CharacterRankData
            {
                Character = c,
                Name = c.CharacterName,
                Avatar = c.Avatar,
                CurrentHp = c.CurrentHp,
                MaxHp = c.MaxHp,
                SwordCount = swords,
                StateName = c.CurrentStateName,
                Power = power
            });
        }

        rankedList.Sort((a, b) => b.Power.CompareTo(a.Power));

        for (int i = 0; i < rankedList.Count; i++)
        {
            var d = rankedList[i];
            d.Rank = i + 1;
            rankedList[i] = d;
        }

        OnRankUpdated?.Invoke();
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
        if (character == null) return;
        if (characterSet.Contains(character)) return;

        if (isUpdating)
        {
            pendingAdd.Add(character);
            return;
        }

        characterSet.Add(character);
        characters.Add(character);
        
        if (grid != null)
            grid.Add(character, character.transform.position);
        
        rankDirty = true;
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
        rankDirty = true;
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
