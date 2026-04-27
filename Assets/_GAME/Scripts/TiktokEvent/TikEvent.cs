using System;

[Serializable]
public class TikEvent
{
    public string type;
    public long ts;

    public TikUser user;

    // comment
    public string comment;

    // like
    public int like_count;
    public int total_likes;

    // gift
    public TikGift gift;
    public int count;   // số lượng quà
    public int total;   // tổng xu = price * count
    public int delta;

    // debug (optional)
    public string rawJson;
}

[Serializable]
public class TikUser
{
    public string user_id;
    public string unique_id;
    public string nickname;
}

[Serializable]
public class TikGift
{
    public int id;
    public string name;
    public int price;   // giá 1 món (diamond)
    
}