using UnityEngine;
using System.Collections.Generic;
using System.IO;

public enum TikTokEventType
{
    Like,
    Comment,
    Share,
    Gift
}

public enum TikTokActionType
{
    Spawn,
    Respawn,
    AddSwords,
    UpgradeToLevel2,
    UpgradeToLevel3,
    UpgradeToLevel4,
    UpgradeToLevel5,
    MagnetBooster,
    ShieldBooster,
    MeteorBooster,
    HealBooster
}

[System.Serializable]
public class TikTokActionConfig
{
    [Tooltip("Loại hành động")]
    public TikTokActionType actionType;
    
    [Header("Action Inputs")]
    [Tooltip("Số kiếm (cho AddSwords)")]
    public int swordCount = 1;
    
    [Tooltip("Lượng máu hồi (cho HealBooster)")]
    public float healAmount = 5f;
}

[System.Serializable]
public class TikTokEventConfig
{
    [Tooltip("Loại sự kiện")]
    public TikTokEventType eventType;
    
    [Header("Event Inputs")]
    [Tooltip("Like: Mỗi X likes trigger 1 lần")]
    public int likeThreshold = 5;
    
    [Tooltip("Comment: Nội dung command")]
    public string commentCommand = "1";
    
    [Tooltip("Gift: Price tối thiểu để trigger")]
    public int giftMinPrice = 10;
    
    [Header("Actions")]
    [Tooltip("Danh sách hành động khi event trigger")]
    public List<TikTokActionConfig> actions = new List<TikTokActionConfig>();
}

[CreateAssetMenu(fileName = "TikTokEventConfig", menuName = "Game/TikTok Event Config")]
public class TikTokEventConfigSO : ScriptableObject
{
    [Tooltip("Danh sách tất cả events")]
    public List<TikTokEventConfig> events = new List<TikTokEventConfig>();

    private void OnEnable()
    {
        LoadFromJson();
    }

    public TikTokEventConfig GetEventConfig(TikTokEventType eventType)
    {
        return events.Find(e => e.eventType == eventType);
    }

    public TikTokEventConfig GetEventConfigByCommand(string command)
    {
        return events.Find(e => 
            e.eventType == TikTokEventType.Comment && 
            e.commentCommand == command
        );
    }

    private string GetFilePath()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "_GAME/So/TikTokEventConfig.json");
#else
        return Path.Combine(Application.dataPath, "../TikTokEventConfig.json");
#endif
    }

    public void SaveToJson()
    {
        string filePath = GetFilePath();
        
        TikTokEventConfigData data = new TikTokEventConfigData
        {
            events = this.events
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
        
        Debug.Log($"TikTok Event Config saved to: {filePath}");
    }

    public void LoadFromJson()
    {
        string filePath = GetFilePath();
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Config file not found: {filePath}");
            return;
        }

        string json = File.ReadAllText(filePath);
        TikTokEventConfigData data = JsonUtility.FromJson<TikTokEventConfigData>(json);

        this.events = data.events;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        
        Debug.Log($"TikTok Event Config loaded from: {filePath}");
    }

    public void ResetToDefault()
    {
        events.Clear();
        
        events.Add(new TikTokEventConfig
        {
            eventType = TikTokEventType.Like,
            likeThreshold = 5,
            actions = new List<TikTokActionConfig>
            {
                new TikTokActionConfig
                {
                    actionType = TikTokActionType.AddSwords,
                    swordCount = 1
                }
            }
        });
        
        events.Add(new TikTokEventConfig
        {
            eventType = TikTokEventType.Comment,
            commentCommand = "1",
            actions = new List<TikTokActionConfig>
            {
                new TikTokActionConfig
                {
                    actionType = TikTokActionType.Spawn
                }
            }
        });
        
        events.Add(new TikTokEventConfig
        {
            eventType = TikTokEventType.Comment,
            commentCommand = "2",
            actions = new List<TikTokActionConfig>
            {
                new TikTokActionConfig
                {
                    actionType = TikTokActionType.Respawn
                }
            }
        });
        
        events.Add(new TikTokEventConfig
        {
            eventType = TikTokEventType.Share,
            actions = new List<TikTokActionConfig>
            {
                new TikTokActionConfig
                {
                    actionType = TikTokActionType.HealBooster,
                    healAmount = 5f
                }
            }
        });
        
        events.Add(new TikTokEventConfig
        {
            eventType = TikTokEventType.Gift,
            giftMinPrice = 10,
            actions = new List<TikTokActionConfig>
            {
                new TikTokActionConfig
                {
                    actionType = TikTokActionType.AddSwords,
                    swordCount = 1
                }
            }
        });

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [System.Serializable]
    private class TikTokEventConfigData
    {
        public List<TikTokEventConfig> events;
    }
}
