using UnityEngine;
using System.Collections.Generic;
using System;

public struct CharacterRankData
{
    public CharacterBase Character;
    public int Rank;
    public string Id;
    public string Name;
    public Sprite Avatar;
    public float CurrentHp;
    public float MaxHp;
    public int SwordCount;
    public int Level;
    public float LevelTimeRemaining;
    
    public int MagnetStackCount;
    public int ShieldStackCount;
    public int MeteorStackCount;
    
    public float MagnetTimeRemaining;
    public float ShieldTimeRemaining;
    public float MeteorCastTimeRemaining;
}

public class CharacterManager : Singleton<CharacterManager>
{
    [SerializeField] private float rankUpdateInterval = 0.5f;

    private SpatialGrid<CharacterBase> grid;
    private readonly List<CharacterBase> characters = new();
    private readonly List<CharacterBase> pendingAdd = new();
    private readonly List<CharacterBase> pendingRemove = new();
    private readonly HashSet<CharacterBase> characterSet = new();
    private readonly Dictionary<string, CharacterBase> characterIdMap = new();
    private readonly HashSet<string> spawnedCharacterIds = new();
    private MapManager cachedMap;
    private bool isUpdating;

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
    }

    public CharacterBase Spawn(Vector3 position, Quaternion rotation, string id, string name, Sprite avatarSprite, int level = 1)
    {
        PoolType poolType = GetRandomCharacterPoolType();
        CharacterBase character = SimplePool.Spawn<CharacterBase>(poolType, position, rotation);
        if (character != null)
        {
            character.TF.position = position;
            character.TF.rotation = rotation;
            character.gameObject.SetActive(true);
            character.OnInit(id, name, avatarSprite, level);
            Register(character);
        }
        return character;
    }

    private PoolType GetRandomCharacterPoolType()
    {
        int randomIndex = UnityEngine.Random.Range(1, 10);
        
        return randomIndex switch
        {
            1 => PoolType.Character1,
            2 => PoolType.Character2,
            3 => PoolType.Character3,
            4 => PoolType.Character4,
            5 => PoolType.Character5,
            6 => PoolType.Character6,
            7 => PoolType.Character7,
            8 => PoolType.Character8,
            9 => PoolType.Character9,
            _ => PoolType.Character1
        };
    }

    public CharacterBase Spawn(Vector3 position, string id, string name, Sprite avatarSprite, int level = 1)
    {
        return Spawn(position, Quaternion.identity, id, name, avatarSprite, level);
    }

    public void Despawn(CharacterBase character)
    {
        if (character == null) return;
        Deregister(character);
        SimplePool.Despawn(character);
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

            rankedList.Add(new CharacterRankData
            {
                Character = c,
                Id = c.CharacterId,
                Name = c.CharacterName,
                Avatar = c.Avatar,
                CurrentHp = c.CurrentHp,
                MaxHp = c.MaxHp,
                SwordCount = c.SwordCount,
                Level = c.CurrentLevel,
                LevelTimeRemaining = c.LevelTimeRemaining,
                MagnetStackCount = c.MagnetStackCount,
                ShieldStackCount = c.ShieldStackCount,
                MeteorStackCount = c.MeteorStackCount,
                MagnetTimeRemaining = c.MagnetTimeRemaining,
                ShieldTimeRemaining = c.ShieldTimeRemaining,
                MeteorCastTimeRemaining = c.MeteorCastTimeRemaining
            });
        }

        rankedList.Sort((a, b) =>
        {
            int levelCompare = b.Level.CompareTo(a.Level);
            if (levelCompare != 0) return levelCompare;
            
            int swordCompare = b.SwordCount.CompareTo(a.SwordCount);
            if (swordCompare != 0) return swordCompare;
            
            return b.CurrentHp.CompareTo(a.CurrentHp);
        });

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
                
                if (!string.IsNullOrEmpty(c.CharacterId))
                {
                    characterIdMap[c.CharacterId] = c;
                    spawnedCharacterIds.Add(c.CharacterId);
                }
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
                
                if (!string.IsNullOrEmpty(c.CharacterId))
                {
                    characterIdMap.Remove(c.CharacterId);
                }
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
        
        if (!string.IsNullOrEmpty(character.CharacterId))
        {
            characterIdMap[character.CharacterId] = character;
            spawnedCharacterIds.Add(character.CharacterId);
        }
        
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
        
        if (!string.IsNullOrEmpty(character.CharacterId))
        {
            characterIdMap.Remove(character.CharacterId);
        }
        
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

    public CharacterBase SpawnFromTikTok(string userId, string nickname, Sprite avatar = null, int level = 1)
    {
        if (HasCharacterBeenSpawned(userId))
            return null;

        MapManager map = MapManager.Instance;
        if (map == null)
            return null;

        Vector3 spawnPos = FindOpenSpawnPosition(map);
        CharacterBase character = Spawn(spawnPos, userId, nickname, avatar, level);
        
        if (character != null && EventNotificationManager.Instance != null)
        {
            EventNotificationManager.Instance.ShowCharacterJoinedNotification(nickname);
        }
        
        return character;
    }

    public CharacterBase RespawnCharacter(string userId, string nickname, Sprite avatar = null, int level = 1)
    {
        // Nếu character còn sống, không respawn và return null
        if (IsCharacterAlive(userId))
        {
            return null;
        }

        MapManager map = MapManager.Instance;
        if (map == null)
            return null;

        Vector3 spawnPos = FindOpenSpawnPosition(map);
        CharacterBase character = Spawn(spawnPos, userId, nickname, avatar, level);
        
        // Không hiển thị notification ở đây nữa
        // Notification sẽ được hiển thị từ TikTokGameHandler
        
        return character;
    }

    private Vector3 FindOpenSpawnPosition(MapManager map)
    {
        Vector2 min = map.MapMin;
        Vector2 max = map.MapMax;
        float padding = map.CellSize * 2f;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 pos = new Vector3(
                UnityEngine.Random.Range(min.x + padding, max.x - padding),
                UnityEngine.Random.Range(min.y + padding, max.y - padding),
                0f
            );

            if (!map.IsWall(pos))
                return pos;
        }

        return new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
    }

    public CharacterBase GetCharacterById(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return null;

        characterIdMap.TryGetValue(characterId, out CharacterBase character);
        return character;
    }

    public bool HasCharacterBeenSpawned(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return false;

        return spawnedCharacterIds.Contains(characterId);
    }

    public bool HasCharacterBeenCreated(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return false;

        return characterIdMap.ContainsKey(characterId);
    }

    public bool IsCharacterAlive(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return false;

        CharacterBase character = GetCharacterById(characterId);
        
        if (character == null)
            return false;

        return character.gameObject.activeInHierarchy 
            && character.CurrentHp > 0f 
            && !character.IsDead;
    }

    public bool IsCharacterDead(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return false;

        CharacterBase character = GetCharacterById(characterId);
        
        if (character == null)
            return false;

        return character.IsDead || character.CurrentHp <= 0f;
    }

    public void ClearSpawnHistory()
    {
        spawnedCharacterIds.Clear();
    }

    public int GetTotalSpawnedCount()
    {
        return spawnedCharacterIds.Count;
    }

    public bool UpgradeToLevel2(string characterId, int count = 1)
    {
        CharacterBase character = GetCharacterById(characterId);
        if (character == null || !character.gameObject.activeInHierarchy || character.IsDead)
            return false;

        character.AddLevelReserveTime(2, count);
        return true;
    }

    public bool UpgradeToLevel3(string characterId, int count = 1)
    {
        CharacterBase character = GetCharacterById(characterId);
        if (character == null || !character.gameObject.activeInHierarchy || character.IsDead)
            return false;

        character.AddLevelReserveTime(3, count);
        return true;
    }

    public bool UpgradeToLevel4(string characterId, int count = 1)
    {
        CharacterBase character = GetCharacterById(characterId);
        if (character == null || !character.gameObject.activeInHierarchy || character.IsDead)
            return false;

        character.AddLevelReserveTime(4, count);
        return true;
    }

    public bool UpgradeToLevel5(string characterId, int count = 1)
    {
        CharacterBase character = GetCharacterById(characterId);
        if (character == null || !character.gameObject.activeInHierarchy || character.IsDead)
            return false;

        character.AddLevelReserveTime(5, count);
        return true;
    }

    public bool ActivateMagnetBooster(string characterId, int count = 1)
    {
        CharacterBase character = GetCharacterById(characterId);
        if (character == null || !character.gameObject.activeInHierarchy || character.IsDead)
            return false;

        character.ActivateMagnetBooster(count);
        return true;
    }

    public bool ActivateShieldBooster(string characterId, int count = 1)
    {
        CharacterBase character = GetCharacterById(characterId);
        if (character == null || !character.gameObject.activeInHierarchy || character.IsDead)
            return false;

        character.ActivateShieldBooster(count);
        return true;
    }

    public bool ActivateMeteorBooster(string characterId, int count = 1)
    {
        CharacterBase character = GetCharacterById(characterId);
        if (character == null || !character.gameObject.activeInHierarchy || character.IsDead)
            return false;

        character.ActivateMeteorBooster(count);
        return true;
    }

    public bool AddSwordsToCharacter(string characterId, int swordsToAdd)
    {
        CharacterBase currentCharacter = GetCharacterById(characterId);
        if (currentCharacter == null) return false;

        SwordOrbit orbit = currentCharacter.GetSwordOrbit();
        if (orbit == null) return false;

        if (currentCharacter.IsSwordFull) return false;

        int currentSwordCount = currentCharacter.SwordCount;
        int maxSwordCount = currentCharacter.MaxSwordCount;
        int actualSwordsToAdd = Mathf.Min(swordsToAdd, maxSwordCount - currentSwordCount);

        if (actualSwordsToAdd <= 0) return false;

        for (int i = 0; i < actualSwordsToAdd; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle.normalized * 2f;
            Vector3 spawnPos = currentCharacter.TF.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
            Sword sword = ItemManager.Instance.Spawn(spawnPos);
            if (sword != null)
                sword.Collect(currentCharacter);
        }

        return true;
    }
}
