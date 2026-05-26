using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TopCharacterData
{
    public int rank;
    public string characterId;
    public string characterName;
    public Sprite avatar;
    public int killPoints;
    public int score;
    public int bonusScore;

    public TopCharacterData(int rank, CharacterRankData data)
    {
        this.rank          = rank;
        this.characterId   = data.Id;
        this.characterName = data.Name;
        this.avatar        = data.Avatar;
        this.killPoints    = data.KillPoints;
        this.score         = data.Score;
    }
}

public static class GameEndData
{
    private static List<TopCharacterData> topCharacters = new List<TopCharacterData>();

    public static List<TopCharacterData> TopCharacters  => topCharacters;
    public static int                    TotalMatchScore { get; private set; }

    public static void SetTopCharacters(List<CharacterRankData> rankedCharacters, int totalMatchScore = 0)
    {
        topCharacters.Clear();
        for (int i = 0; i < rankedCharacters.Count; i++)
            topCharacters.Add(new TopCharacterData(i + 1, rankedCharacters[i]));

        TotalMatchScore = totalMatchScore;
        Debug.Log($"[GameEndData] Saved {topCharacters.Count} characters, TotalMatchScore={totalMatchScore}");
    }

    public static void Clear()
    {
        topCharacters.Clear();
        TotalMatchScore = 0;
    }
}
