using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TopCharacterData
{
    public int rank;
    public string characterId;
    public string characterName;
    public Sprite avatar;
    public int level;
    public int swordCount;
    public float hp;
    public float maxHp;
    
    public TopCharacterData(int rank, CharacterRankData data)
    {
        this.rank = rank;
        this.characterId = data.Id;
        this.characterName = data.Name;
        this.avatar = data.Avatar;
        this.level = data.Level;
        this.swordCount = data.SwordCount;
        this.hp = data.CurrentHp;
        this.maxHp = data.MaxHp;
    }
}

public static class GameEndData
{
    private static List<TopCharacterData> topCharacters = new List<TopCharacterData>();
    
    public static List<TopCharacterData> TopCharacters => topCharacters;
    
    public static void SetTopCharacters(List<CharacterRankData> rankedCharacters)
    {
        topCharacters.Clear();
        
        // Lấy top 3
        int count = Mathf.Min(3, rankedCharacters.Count);
        for (int i = 0; i < count; i++)
        {
            topCharacters.Add(new TopCharacterData(i + 1, rankedCharacters[i]));
        }
        
        Debug.Log($"[GameEndData] Saved {topCharacters.Count} top characters");
    }
    
    public static void Clear()
    {
        topCharacters.Clear();
    }
}
