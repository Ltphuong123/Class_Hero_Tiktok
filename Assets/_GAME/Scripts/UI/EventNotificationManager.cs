using UnityEngine;
using System.Collections.Generic;

public class EventNotificationManager : Singleton<EventNotificationManager>
{
    [SerializeField] private EventNotificationUI notificationUI;

    [Header("Event Icons")]
    [SerializeField] private Sprite joinIcon;
    [SerializeField] private Sprite levelUpIcon;
    [SerializeField] private Sprite killIcon;
    [SerializeField] private Sprite swordIcon;
    [SerializeField] private Sprite magnetIcon;
    [SerializeField] private Sprite shieldIcon;
    [SerializeField] private Sprite meteorIcon;
    [SerializeField] private Sprite respawnIcon;
    [SerializeField] private Sprite healIcon;

    [Header("Background Colors")]
    [SerializeField] private Color joinColor = new Color(0.1f, 0.4f, 0.1f, 0.5f);        // Xanh lá tối
    [SerializeField] private Color levelUpColor = new Color(0.5f, 0.42f, 0f, 0.5f);      // Vàng gold tối
    [SerializeField] private Color killColor = new Color(0.4f, 0.1f, 0.1f, 0.5f);        // Đỏ tối
    [SerializeField] private Color swordColor = new Color(0.15f, 0.3f, 0.5f, 0.5f);      // Xanh dương tối
    [SerializeField] private Color magnetColor = new Color(0f, 0.4f, 0.4f, 0.5f);        // Cyan tối
    [SerializeField] private Color shieldColor = new Color(0.5f, 0.32f, 0f, 0.5f);       // Cam tối
    [SerializeField] private Color meteorColor = new Color(0.4f, 0f, 0.2f, 0.5f);        // Đỏ tím tối
    [SerializeField] private Color respawnColor = new Color(0.25f, 0.25f, 0.5f, 0.5f);   // Tím tối
    [SerializeField] private Color healColor = new Color(0f, 0.5f, 0.2f, 0.5f);          // Xanh lá sáng

    [Header("Settings")]
    [SerializeField] private int maxMessageLength = 30;

    private string TruncateMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // Remove rich text tags để đếm ký tự thực
        string plainText = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty);
        
        if (plainText.Length <= maxMessageLength)
            return message;

        // Tìm vị trí cắt trong plain text
        int cutPosition = maxMessageLength - 3; // -3 cho "..."
        
        // Tính toán vị trí cắt trong message gốc (có rich text)
        int plainIndex = 0;
        int richIndex = 0;
        bool inTag = false;
        
        while (richIndex < message.Length && plainIndex < cutPosition)
        {
            if (message[richIndex] == '<')
                inTag = true;
            else if (message[richIndex] == '>')
                inTag = false;
            else if (!inTag)
                plainIndex++;
            
            richIndex++;
        }
        
        // Đóng các tag còn mở
        string truncated = message.Substring(0, richIndex);
        int openTags = 0;
        for (int i = 0; i < truncated.Length; i++)
        {
            if (truncated[i] == '<')
            {
                if (i + 1 < truncated.Length && truncated[i + 1] != '/')
                    openTags++;
                else if (i + 1 < truncated.Length && truncated[i + 1] == '/')
                    openTags--;
            }
        }
        
        // Thêm closing tags
        for (int i = 0; i < openTags; i++)
            truncated += "</color>";
        
        return truncated + "...";
    }

    public void ShowCharacterJoinedNotification(string characterName)
    {
        if (notificationUI != null)
        {
            string message = TruncateMessage($"<color=#00FF00>{characterName}</color> joined!");
            notificationUI.ShowNotification(message, joinIcon, joinColor);
        }
    }

    public void ShowKillNotification(string killerName, string victimName)
    {
        if (notificationUI != null)
        {
            string message = TruncateMessage($"<color=#00FF00>{killerName}</color> killed <color=#FF0000>{victimName}</color>");
            notificationUI.ShowNotification(message, killIcon, killColor);
        }
    }

    public void ShowAddSwordsNotification(string characterName, int count)
    {
        if (notificationUI != null)
        {
            string countText = count > 1 ? $" x{count}" : "";
            string message = TruncateMessage($"<color=#00FF00>{characterName}</color> +swords{countText}");
            notificationUI.ShowNotification(message, swordIcon, swordColor);
        }
    }

    public void ShowUpgradeNotification(string characterName, int level, int count)
    {
        if (notificationUI != null)
        {
            string countText = count > 1 ? $" x{count}" : "";
            string message = TruncateMessage($"<color=#FFD700>{characterName}</color> Lv{level}{countText}");
            notificationUI.ShowNotification(message, levelUpIcon, levelUpColor);
        }
    }

    public void ShowRespawnNotification(string characterName)
    {
        if (notificationUI != null)
        {
            string message = TruncateMessage($"<color=#00FF00>{characterName}</color> respawned!");
            notificationUI.ShowNotification(message, respawnIcon, respawnColor);
        }
    }

    public void ShowMagnetBoosterNotification(string characterName, int count)
    {
        if (notificationUI != null)
        {
            string countText = count > 1 ? $" x{count}" : "";
            string message = TruncateMessage($"<color=#00FFFF>{characterName}</color> Magnet{countText}");
            notificationUI.ShowNotification(message, magnetIcon, magnetColor);
        }
    }

    public void ShowShieldBoosterNotification(string characterName, int count)
    {
        if (notificationUI != null)
        {
            string countText = count > 1 ? $" x{count}" : "";
            string message = TruncateMessage($"<color=#FFA500>{characterName}</color> Shield{countText}");
            notificationUI.ShowNotification(message, shieldIcon, shieldColor);
        }
    }

    public void ShowMeteorBoosterNotification(string characterName, int count)
    {
        if (notificationUI != null)
        {
            string countText = count > 1 ? $" x{count}" : "";
            string message = TruncateMessage($"<color=#FF0000>{characterName}</color> Meteor{countText}");
            notificationUI.ShowNotification(message, meteorIcon, meteorColor);
        }
    }

    public void ShowHealBoosterNotification(string characterName, float healAmount)
    {
        if (notificationUI != null)
        {
            string message = TruncateMessage($"<color=#00FF88>{characterName}</color> +{healAmount:F0} HP");
            notificationUI.ShowNotification(message, healIcon, healColor);
        }
    }
}
