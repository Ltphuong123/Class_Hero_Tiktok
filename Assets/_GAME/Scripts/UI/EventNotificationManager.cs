using UnityEngine;
using System.Collections.Generic;

public class EventNotificationManager : Singleton<EventNotificationManager>
{
    [SerializeField] private EventNotificationUI notificationUI;

    private Dictionary<string, int> lastKnownLevels = new();

    private void Start()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.OnRankUpdated += CheckLevelChanges;
    }

    private void OnDestroy()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.OnRankUpdated -= CheckLevelChanges;
    }

    private void CheckLevelChanges()
    {
        var rankedCharacters = CharacterManager.Instance.RankedCharacters;
        
        foreach (var data in rankedCharacters)
        {
            if (lastKnownLevels.TryGetValue(data.Id, out int lastLevel))
            {
                if (data.Level > lastLevel)
                    ShowLevelUpNotification(data.Name, data.Level);
            }
            
            lastKnownLevels[data.Id] = data.Level;
        }
    }

    public void ShowCharacterJoinedNotification(string characterName)
    {
        if (notificationUI != null)
            notificationUI.ShowNotification($"<color=#00FF00>{characterName}</color> đã tham gia!");
    }

    public void ShowLevelUpNotification(string characterName, int newLevel)
    {
        if (notificationUI != null)
            notificationUI.ShowNotification($"<color=#FFD700>{characterName}</color> đã lên cấp {newLevel}!");
    }

    public void ShowKillNotification(string killerName, string victimName)
    {
        if (notificationUI != null)
            notificationUI.ShowNotification($"<color=#FF0000>{killerName}</color> đã tiêu diệt <color=#888888>{victimName}</color>");
    }
}
