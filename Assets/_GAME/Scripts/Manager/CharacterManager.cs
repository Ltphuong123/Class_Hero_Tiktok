using UnityEngine;
using System.Collections.Generic;
using System;

public struct CharacterRankData
{
    public CharacterBase Character;
    public int Rank;
    public int NumericId;
    public string Id;
    public string Name;
    public Sprite Avatar;
    public float CurrentHp;
    public float MaxHp;
    public int SwordCount;
    public int Level;
    public float LevelTimeRemaining;
    public int KillPoints;
    public int Score;
    public int MagnetStackCount;
    public int ShieldStackCount;
    public float MagnetTimeRemaining;
    public float ShieldTimeRemaining;
    public int SwordQueue;
    public bool IsDead;
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
    private readonly Dictionary<int, CharacterBase> characterNumericIdMap = new();
    private readonly HashSet<string> spawnedCharacterIds = new();
    private MapManager cachedMap;
    private bool isUpdating;
    private int nextCharacterNumericId = 1;

    private readonly List<CharacterRankData> rankedList = new();
    private readonly Dictionary<string, int> persistedKillPoints = new();
    private readonly Dictionary<string, int> persistedScores = new();
    private readonly Dictionary<string, int> persistedNumericIds = new();
    private readonly Dictionary<string, CharacterRankData> deadRecords = new();
    private float rankTimer;

    public int TotalMatchScore { get; private set; }
    private bool rankDirty = true;

    public int CharacterCount => characterSet.Count;
    public IReadOnlyList<CharacterRankData> RankedCharacters => rankedList;
    public event Action OnRankUpdated;

    protected override void Awake()
    {
        base.Awake();
        cachedMap = MapManager.Instance;
        grid = new SpatialGrid<CharacterBase>(cachedMap != null ? cachedMap.CellSize : 5f);
    }

    public CharacterBase Spawn(Vector3 position, Quaternion rotation, string id, string name, Sprite avatarSprite, int level = 1)
    {
        bool isFirstJoin = !string.IsNullOrEmpty(id) && !spawnedCharacterIds.Contains(id);

        CharacterBase character = SimplePool.Spawn<CharacterBase>(GetRandomCharacterPoolType(), position, rotation);
        if (character != null)
        {
            character.TF.position = position;
            character.TF.rotation = rotation;
            character.gameObject.SetActive(true);
            character.OnInit(id, name, avatarSprite, level);
            deadRecords.Remove(id);
            if (persistedKillPoints.TryGetValue(id, out int savedKills))
            {
                character.RestoreKillPoints(savedKills);
                persistedKillPoints.Remove(id);
            }
            if (persistedScores.TryGetValue(id, out int savedScore))
            {
                character.RestoreScore(savedScore);
                persistedScores.Remove(id);
            }
            int numId;
            if (persistedNumericIds.TryGetValue(id, out int savedNumId))
            {
                numId = savedNumId;
                persistedNumericIds.Remove(id);
            }
            else
            {
                numId = nextCharacterNumericId++;
            }
            character.SetCharacterNumericId(numId);
            characterNumericIdMap[numId] = character;
            Register(character);

            if (isFirstJoin)
                RankServerService.CheckPlayerRanks(this, id, name);
        }
        return character;
    }

    public CharacterBase Spawn(Vector3 position, string id, string name, Sprite avatarSprite, int level = 1)
        => Spawn(position, Quaternion.identity, id, name, avatarSprite, level);

    public void Despawn(CharacterBase character)
    {
        if (character == null) return;
        Deregister(character);
        SimplePool.Despawn(character);
    }

    private PoolType GetRandomCharacterPoolType() => UnityEngine.Random.Range(1, 10) switch
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
                if (!c.IsKnockedBack && cachedMap.IsBlockedWorld(pos))
                {
                    Vector3 tryX = new Vector3(pos.x, 0f, prevPos.z);
                    if (!cachedMap.IsBlockedWorld(tryX))
                        pos = tryX;
                    else
                    {
                        Vector3 tryZ = new Vector3(prevPos.x, 0f, pos.z);
                        pos = !cachedMap.IsBlockedWorld(tryZ) ? tryZ : prevPos;
                    }
                }
            }

            pos.y = 0f;
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
            if (c == null || !c.gameObject.activeInHierarchy || c.CurrentHp <= 0f) continue;
            rankedList.Add(new CharacterRankData
            {
                Character = c,
                NumericId = c.CharacterNumericId,
                Id = c.CharacterId,
                Name = c.CharacterName,
                Avatar = c.Avatar,
                CurrentHp = c.CurrentHp,
                MaxHp = c.MaxHp,
                SwordCount = c.SwordCount,
                Level = c.CurrentLevel,
                LevelTimeRemaining = c.LevelTimeRemaining,
                KillPoints = c.KillPoints,
                Score = c.Score,
                MagnetStackCount = c.MagnetStackCount,
                ShieldStackCount = c.ShieldStackCount,
                MagnetTimeRemaining = c.MagnetTimeRemaining,
                ShieldTimeRemaining = c.ShieldTimeRemaining,
                SwordQueue = c.SwordQueue
            });
        }

        foreach (var rec in deadRecords.Values)
            rankedList.Add(rec);

        rankedList.Sort((a, b) =>
        {
            int cmp = b.Score.CompareTo(a.Score); if (cmp != 0) return cmp;
            cmp = b.KillPoints.CompareTo(a.KillPoints); if (cmp != 0) return cmp;
            cmp = b.Level.CompareTo(a.Level); if (cmp != 0) return cmp;
            cmp = b.SwordCount.CompareTo(a.SwordCount); if (cmp != 0) return cmp;
            return b.CurrentHp.CompareTo(a.CurrentHp);
        });

        for (int i = 0; i < rankedList.Count; i++)
        {
            CharacterRankData d = rankedList[i];
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
            RemoveFromCollections(pendingRemove[i]);
        if (removeCount > 0) pendingRemove.Clear();
    }

    public void Register(CharacterBase character)
    {
        if (character == null || characterSet.Contains(character)) return;
        if (isUpdating) { pendingAdd.Add(character); return; }

        characterSet.Add(character);
        characters.Add(character);
        grid?.Add(character, character.transform.position);
        if (!string.IsNullOrEmpty(character.CharacterId))
        {
            characterIdMap[character.CharacterId] = character;
            spawnedCharacterIds.Add(character.CharacterId);
        }
        if (character.CharacterNumericId > 0)
            characterNumericIdMap[character.CharacterNumericId] = character;
        rankDirty = true;
    }

    public void Deregister(CharacterBase character)
    {
        if (!characterSet.Contains(character)) return;
        if (isUpdating) { pendingRemove.Add(character); return; }
        RemoveFromCollections(character);
        rankDirty = true;
    }

    private void RemoveFromCollections(CharacterBase c)
    {
        if (!characterSet.Remove(c)) return;
        int idx = characters.IndexOf(c);
        if (idx >= 0)
        {
            int last = characters.Count - 1;
            characters[idx] = characters[last];
            characters.RemoveAt(last);
        }
        grid.Remove(c);
        if (!string.IsNullOrEmpty(c.CharacterId)
            && characterIdMap.TryGetValue(c.CharacterId, out CharacterBase byId) && byId == c)
            characterIdMap.Remove(c.CharacterId);
        if (c.CharacterNumericId > 0
            && characterNumericIdMap.TryGetValue(c.CharacterNumericId, out CharacterBase byNum) && byNum == c)
            characterNumericIdMap.Remove(c.CharacterNumericId);
    }

    public void ReleaseCharacterIdentity(CharacterBase character)
    {
        if (!string.IsNullOrEmpty(character.CharacterId))
        {
            if (character.KillPoints > 0)
                persistedKillPoints[character.CharacterId] = character.KillPoints;
            if (character.Score > 0)
                persistedScores[character.CharacterId] = character.Score;
            if (character.CharacterNumericId > 0)
                persistedNumericIds[character.CharacterId] = character.CharacterNumericId;

            deadRecords[character.CharacterId] = new CharacterRankData
            {
                IsDead              = true,
                Id                  = character.CharacterId,
                NumericId           = character.CharacterNumericId,
                Name                = character.CharacterName,
                Avatar              = character.Avatar,
                KillPoints          = character.KillPoints,
                Score               = character.Score,
                Level               = 1,
                CurrentHp           = 0f,
                MaxHp               = character.MaxHp,
                SwordCount          = 0,
                SwordQueue          = 0,
                MagnetStackCount    = character.MagnetStackCount,
                ShieldStackCount    = character.ShieldStackCount,
                MagnetTimeRemaining = character.MagnetTimeRemaining,
                ShieldTimeRemaining = character.ShieldTimeRemaining,
            };

            if (characterIdMap.TryGetValue(character.CharacterId, out CharacterBase byId) && byId == character)
                characterIdMap.Remove(character.CharacterId);
        }
        if (character.CharacterNumericId > 0
            && characterNumericIdMap.TryGetValue(character.CharacterNumericId, out CharacterBase byNum) && byNum == character)
            characterNumericIdMap.Remove(character.CharacterNumericId);
        grid.Remove(character);
        rankDirty = true;
    }

    public void GetNearbyCharacters(Vector3 position, float radius, List<CharacterBase> results)
        => grid.GetInRadius(position, radius, results);

    public List<CharacterBase> GetEnemiesInRadius(Vector3 center, float radius, CharacterBase self)
    {
        List<CharacterBase> results = new List<CharacterBase>();
        grid.GetInRadius(center, radius, results);
        for (int i = results.Count - 1; i >= 0; i--)
            if (results[i] == self || results[i].IsDead)
                results.RemoveAt(i);
        return results;
    }

    public List<CharacterBase> GetAllLivingEnemies(CharacterBase self)
    {
        List<CharacterBase> results = new List<CharacterBase>();
        for (int i = 0; i < characters.Count; i++)
        {
            CharacterBase c = characters[i];
            if (c != null && c != self && !c.IsDead) results.Add(c);
        }
        return results;
    }

    public List<CharacterBase> GetNearestEnemies(Vector3 center, CharacterBase self, int count)
    {
        List<CharacterBase> all = GetAllLivingEnemies(self);
        all.Sort((a, b) =>
            (a.TF.position - center).sqrMagnitude.CompareTo(
            (b.TF.position - center).sqrMagnitude));
        if (all.Count > count) all.RemoveRange(count, all.Count - count);
        return all;
    }

    public void GetAllActiveCharacters(List<CharacterBase> results)
    {
        results.Clear();
        for (int i = 0; i < characters.Count; i++)
            if (characters[i] != null && !characters[i].IsDead) results.Add(characters[i]);
    }

    public CharacterBase GetNearestCharacter(Vector3 position, float radius, CharacterBase excludeSelf = null)
        => grid.GetNearest(position, radius, excludeSelf);

    public void GetCharactersInRadius(Vector3 position, float radius, List<CharacterBase> results)
    {
        grid.GetInRadius(position, radius, results);
        results.Sort((a, b) =>
            (a.transform.position - position).sqrMagnitude.CompareTo(
            (b.transform.position - position).sqrMagnitude));
    }

    public CharacterBase SpawnFromTikTok(string userId, string nickname, Sprite avatar = null, int level = 1)
    {
        if (HasCharacterBeenSpawned(userId)) return null;
        MapManager map = MapManager.Instance;
        if (map == null) return null;
        CharacterBase character = Spawn(FindOpenSpawnPosition(map), userId, nickname, avatar, level);
        if (character != null)
            EventNotificationManager.Instance?.ShowCharacterJoinedNotification(nickname);
        return character;
    }

    public CharacterBase RespawnCharacter(string userId, string nickname, Sprite avatar = null, int level = 1)
    {
        CharacterBase existing = GetCharacterByIdIncludingDead(userId);
        if (existing != null)
        {
            if (!existing.IsDead && existing.CurrentHp > 0f && existing.gameObject.activeInHierarchy)
                return existing;
            if (existing.gameObject != null) Despawn(existing);
        }
        MapManager map = MapManager.Instance;
        if (map == null) return null;
        return Spawn(FindOpenSpawnPosition(map), userId, nickname, avatar, level);
    }

    private Vector3 FindOpenSpawnPosition(MapManager map)
    {
        Vector2 min = map.MapMin;
        Vector2 max = map.MapMax;
        float padding = map.CellSize * 2f;
        for (int i = 0; i < 30; i++)
        {
            Vector3 pos = new Vector3(
                UnityEngine.Random.Range(min.x + padding, max.x - padding),
                0f,
                UnityEngine.Random.Range(min.y + padding, max.y - padding));
            if (!map.IsWall(pos)) return pos;
        }
        return new Vector3((min.x + max.x) * 0.5f, 0f, (min.y + max.y) * 0.5f);
    }

    public CharacterBase GetCharacterById(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return null;
        characterIdMap.TryGetValue(characterId, out CharacterBase c);
        return c != null && !c.IsDead && c.gameObject.activeInHierarchy ? c : null;
    }

    private CharacterBase GetCharacterByIdIncludingDead(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return null;
        characterIdMap.TryGetValue(characterId, out CharacterBase c);
        return c;
    }

    public CharacterBase GetCharacterByNumericId(int numericId)
    {
        if (numericId <= 0) return null;
        characterNumericIdMap.TryGetValue(numericId, out CharacterBase c);
        return c;
    }

    public bool HasCharacterBeenSpawned(string characterId)
        => !string.IsNullOrEmpty(characterId) && spawnedCharacterIds.Contains(characterId);

    public bool HasCharacterBeenCreated(string characterId)
        => !string.IsNullOrEmpty(characterId) && characterIdMap.ContainsKey(characterId);

    public bool IsCharacterAlive(string characterId)
    {
        CharacterBase c = GetCharacterById(characterId);
        return c != null && c.gameObject.activeInHierarchy && c.CurrentHp > 0f && !c.IsDead;
    }

    public bool IsCharacterDead(string characterId)
    {
        CharacterBase c = GetCharacterById(characterId);
        return c != null && (c.IsDead || c.CurrentHp <= 0f);
    }

    public void ClearSpawnHistory() => spawnedCharacterIds.Clear();
    public int GetTotalSpawnedCount() => spawnedCharacterIds.Count;

    private bool UpgradeLevelInternal(string characterId, string nickname, int level, int count)
    {
        CharacterBase c = EnsureCharacterAlive(characterId, nickname);
        if (c == null) return false;
        LevelIntroController.Instance?.EnqueueIntro(level, nickname);
        if (c.IsDead) { StartCoroutine(DelayedUpgradeLevel(c, level, count)); return true; }
        c.AddLevelReserveTime(level, count);
        return true;
    }

    private System.Collections.IEnumerator DelayedUpgradeLevel(CharacterBase c, int level, int count)
    {
        yield return new WaitForSeconds(0.3f);
        if (c != null && !c.IsDead) c.AddLevelReserveTime(level, count);
    }

    public bool UpgradeToLevel2(string id, string name, int count = 1) => UpgradeLevelInternal(id, name, 2, count);
    public bool UpgradeToLevel3(string id, string name, int count = 1) => UpgradeLevelInternal(id, name, 3, count);
    public bool UpgradeToLevel4(string id, string name, int count = 1) => UpgradeLevelInternal(id, name, 4, count);
    public bool UpgradeToLevel5(string id, string name, int count = 1) => UpgradeLevelInternal(id, name, 5, count);
    public bool UpgradeToLevel6(string id, string name, int count = 1) => UpgradeLevelInternal(id, name, 6, count);
    public bool UpgradeToLevel7(string id, string name, int count = 1) => UpgradeLevelInternal(id, name, 7, count);
    public bool UpgradeToLevel8(string id, string name, int count = 1) => UpgradeLevelInternal(id, name, 8, count);
    public bool UpgradeToLevel9(string id, string name, int count = 1) => UpgradeLevelInternal(id, name, 9, count);
    public bool UpgradeToLevel10(string id, string name, int count = 1) => UpgradeLevelInternal(id, name, 10, count);

    public bool ActivateMagnetBooster(string characterId, string nickname, int count = 1)
    {
        CharacterBase c = EnsureCharacterAlive(characterId, nickname);
        if (c == null) return false;
        if (c.IsDead) { StartCoroutine(DelayedAction(c, () => c.ActivateMagnetBooster(count))); return true; }
        c.ActivateMagnetBooster(count);
        return true;
    }

    public bool ActivateShieldBooster(string characterId, string nickname, int count = 1)
    {
        CharacterBase c = EnsureCharacterAlive(characterId, nickname);
        if (c == null) return false;
        if (c.IsDead) { StartCoroutine(DelayedAction(c, () => c.ActivateShieldBooster(count))); return true; }
        c.ActivateShieldBooster(count);
        return true;
    }

    public bool ActivateHealBooster(string characterId, string nickname, float healAmount)
    {
        CharacterBase c = EnsureCharacterAlive(characterId, nickname);
        if (c == null) return false;
        if (c.IsDead) { StartCoroutine(DelayedAction(c, () => c.ActivateHealBooster(healAmount))); return true; }
        c.ActivateHealBooster(healAmount);
        return true;
    }

    private System.Collections.IEnumerator DelayedAction(CharacterBase c, Action action)
    {
        yield return new WaitForSeconds(0.3f);
        if (c != null && !c.IsDead) action();
    }

    private bool ActivateSkillInternal(string characterId, string nickname, Action<CharacterBase> addStack)
    {
        CharacterBase c = EnsureCharacterAlive(characterId, nickname);
        if (c == null) return false;
        addStack(c);
        return true;
    }

    public bool ActivateSkill1(string id, string name, int count = 1) => ActivateSkillInternal(id, name, c => c.AddSkill1Stack(count));
    public bool ActivateSkill2(string id, string name, int count = 1) => ActivateSkillInternal(id, name, c => c.AddSkill2Stack(count));
    public bool ActivateSkill3(string id, string name, int count = 1) => ActivateSkillInternal(id, name, c => c.AddSkill3Stack(count));
    public bool ActivateSkill4(string id, string name, int count = 1) => ActivateSkillInternal(id, name, c => c.AddSkill4Stack(count));
    public bool ActivateSkill5(string id, string name, int count = 1) => ActivateSkillInternal(id, name, c => c.AddSkill5Stack(count));
    public bool ActivateSkill6(string id, string name, int count = 1) => ActivateSkillInternal(id, name, c => c.AddSkill6Stack(count));
    public bool ActivateSkill7(string id, string name, int count = 1) => ActivateSkillInternal(id, name, c => c.AddSkill7Stack(count));
    public bool ActivateSkill8(string id, string name, int count = 1) => ActivateSkillInternal(id, name, c => c.AddSkill8Stack(count));
    public bool ActivateSkill9(string id, string name, int count = 1) => ActivateSkillInternal(id, name, c => c.AddSkill9Stack(count));

    private CharacterBase EnsureCharacterAlive(string characterId, string nickname)
    {
        CharacterBase c = GetCharacterById(characterId);
        if (c == null || c.IsDead)
        {
            CharacterBase dead = GetCharacterByIdIncludingDead(characterId);
            if (dead?.gameObject != null) Despawn(dead);
            c = RespawnCharacter(characterId, nickname, null, 1);
        }
        return c;
    }

    public bool AddScore(string characterId, string nickname, int points)
    {
        CharacterBase c = EnsureCharacterAlive(characterId, nickname);
        if (c == null) return false;
        c.AddScore(points);
        TotalMatchScore += Mathf.RoundToInt(points * 0.2f);
        return true;
    }

    public void AddKillScore(int killScore)
    {
        TotalMatchScore += Mathf.RoundToInt(killScore * 0.2f);
    }

    public bool AddSwordsToCharacter(string characterId, string nickname, int swordsToAdd)
    {
        CharacterBase c = EnsureCharacterAlive(characterId, nickname);
        if (c == null) return false;
        if (c.IsDead) { StartCoroutine(DelayedAddSwords(c, swordsToAdd)); return true; }
        DoAddSwords(c, swordsToAdd);
        return true;
    }

    private System.Collections.IEnumerator DelayedAddSwords(CharacterBase c, int swordsToAdd)
    {
        yield return new WaitForSeconds(0.3f);
        if (c == null || c.IsDead) yield break;
        DoAddSwords(c, swordsToAdd);
    }

    private void DoAddSwords(CharacterBase c, int swordsToAdd)
    {
        if (c.GetSwordOrbit() == null) return;
        if (c.IsSwordFull) { c.AddToSwordQueue(swordsToAdd); return; }

        int actual = Mathf.Min(swordsToAdd, c.MaxSwordCount - c.SwordCount);
        for (int i = 0; i < actual; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * 2f;
            Sword sword = ItemManager.Instance.Spawn(c.TF.position + new Vector3(offset.x, 0f, offset.y));
            sword?.Collect(c);
        }
        int remaining = swordsToAdd - actual;
        if (remaining > 0) c.AddToSwordQueue(remaining);
    }

    public bool AddElementalSwordsToCharacter(string characterId, string nickname, ElementalSwordType type, int count)
    {
        CharacterBase c = EnsureCharacterAlive(characterId, nickname);
        if (c == null) return false;
        if (c.IsDead) { StartCoroutine(DelayedElementalSwords(c, type, count)); return true; }
        c.AddElementalSword(type, count);
        return true;
    }

    private System.Collections.IEnumerator DelayedElementalSwords(CharacterBase c, ElementalSwordType type, int count)
    {
        yield return new WaitForSeconds(0.3f);
        if (c != null && !c.IsDead) c.AddElementalSword(type, count);
    }

    public bool AddKimSwords(string id, string name, int count = 1)  => AddElementalSwordsToCharacter(id, name, ElementalSwordType.Kim,  count);
    public bool AddMocSwords(string id, string name, int count = 1)  => AddElementalSwordsToCharacter(id, name, ElementalSwordType.Moc,  count);
    public bool AddThuySwords(string id, string name, int count = 1) => AddElementalSwordsToCharacter(id, name, ElementalSwordType.Thuy, count);
    public bool AddHoaSwords(string id, string name, int count = 1)  => AddElementalSwordsToCharacter(id, name, ElementalSwordType.Hoa,  count);
    public bool AddThoSwords(string id, string name, int count = 1)  => AddElementalSwordsToCharacter(id, name, ElementalSwordType.Tho,  count);

    public bool LockTargetAttack(string attackerId, string targetId)
    {
        CharacterBase attacker = GetCharacterById(attackerId);
        if (attacker == null || !attacker.gameObject.activeInHierarchy || attacker.IsDead) return false;
        CharacterBase target = GetCharacterById(targetId);
        if (target == null || !target.gameObject.activeInHierarchy || target.IsDead) return false;
        if (attacker == target) return false;
        attacker.LockTarget(target);
        return true;
    }

    public bool UnlockTargetAttack(string characterId)
    {
        CharacterBase c = GetCharacterById(characterId);
        if (c == null) return false;
        c.UnlockTarget();
        return true;
    }

    public void NotifyLevelUp(string characterName, int level)
    {
        LevelIntroController.Instance?.EnqueueIntro(level, characterName);
    }
}
