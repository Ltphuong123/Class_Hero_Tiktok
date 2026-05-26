using System.Collections.Generic;

[System.Serializable]
public class RankEntry
{
    public int    rank;
    public string tiktokUserId;
    public string nickname;
    public string avatarUrl;
    public int    score;
    public int    totalKills;
}

[System.Serializable]
public class RankEntryList
{
    public List<RankEntry> items;
}

[System.Serializable]
public class PlayerRankEntry
{
    public int    rank;
    public string tiktokUserId;
    public string nickname;
    public string avatarUrl;
    public int    warPoints;
    public int    totalKills;
}
