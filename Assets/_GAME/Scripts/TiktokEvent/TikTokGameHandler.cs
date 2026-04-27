using UnityEngine;
using System.Collections.Generic;

public class TikTokGameHandler : MonoBehaviour
{
    [Header("References")]
    public TikTokUdpReceiver receiver;
    [SerializeField] private CharacterManager characterManager;
    [SerializeField] private TikTokEventConfigSO eventConfig;

    private void Start()
    {
        receiver.OnEvent += HandleEvent;
        
        if (characterManager == null)
            characterManager = CharacterManager.Instance;
        
        if (eventConfig != null)
            eventConfig.LoadFromJson();
    }

    private void OnDestroy()
    {
        receiver.OnEvent -= HandleEvent;
    }

    private void ProcessAttackCommand(string comment, string userId, string nickname)
    {
        string targetIdStr = "";
        string commentLower = comment.ToLower();
        
        // Xử lý format "atk ID" (có khoảng trắng)
        if (commentLower.StartsWith("atk "))
        {
            targetIdStr = comment.Substring(4).Trim();
        }
        // Xử lý format "atkID" (không có khoảng trắng)
        else if (commentLower.StartsWith("atk"))
        {
            targetIdStr = comment.Substring(3).Trim();
        }
        
        if (string.IsNullOrEmpty(targetIdStr) || !int.TryParse(targetIdStr, out int targetNumericId))
            return;

        CharacterBase attacker = characterManager.GetCharacterById(userId);
        if (attacker == null) return;

        CharacterBase target = characterManager.GetCharacterByNumericId(targetNumericId);
        if (target == null) return;

        attacker.LockTarget(target);
    }

    private void HandleEvent(TikEvent ev)
    {
        switch (ev.type)
        {
            case "comment":
                HandleComment(ev);
                break;
            case "gift_tick":
                HandleGift(ev);
                break;
            case "like":
                HandleLike(ev);
                break;
            case "share":
                HandleShare(ev);
                break;
        }
    }

    private void HandleComment(TikEvent ev)
    {
        if (characterManager == null || eventConfig == null) return;

        string comment = ev.comment?.Trim();
        if (string.IsNullOrEmpty(comment)) return;

        string userId = ev.user.user_id;
        string nickname = ev.user.nickname;

        string commentLower = comment.ToLower();

        if (commentLower.StartsWith("atk ") || commentLower.StartsWith("atk"))
        {
            ProcessAttackCommand(comment, userId, nickname);
            return;
        }

        if (commentLower == "stop")
        {
            characterManager.UnlockTargetAttack(userId);
            return;
        }

        TikTokEventConfig config = eventConfig.GetEventConfigByCommand(comment);
        if (config != null)
        {
            ExecuteActions(config.actions, userId, nickname, 1);
        }
    }

    private void HandleGift(TikEvent ev)
    {
        if (characterManager == null || eventConfig == null) return;

        string userId = ev.user.user_id;
        string nickname = ev.user.nickname;
        int delta = ev.delta;
        int giftPrice = ev.gift.price;

        int highestMinPrice = 0;

        foreach (var config in eventConfig.events)
        {
            if (config.eventType != TikTokEventType.Gift) continue;
            if (giftPrice < config.giftMinPrice) continue;
            
            if (config.giftMinPrice > highestMinPrice)
            {
                highestMinPrice = config.giftMinPrice;
            }
        }

        if (highestMinPrice > 0)
        {
            foreach (var config in eventConfig.events)
            {
                if (config.eventType != TikTokEventType.Gift) continue;
                if (config.giftMinPrice != highestMinPrice) continue;
                
                ExecuteActions(config.actions, userId, nickname, delta);
            }
        }
    }

    private void HandleLike(TikEvent ev)
    {
        if (characterManager == null || eventConfig == null) return;

        TikTokEventConfig config = eventConfig.GetEventConfig(TikTokEventType.Like);
        if (config == null) return;

        string userId = ev.user.user_id;
        string nickname = ev.user.nickname;
        
        int triggerCount = ev.like_count / config.likeThreshold;
        if (triggerCount < 1) triggerCount = 1;
        
        ExecuteActions(config.actions, userId, nickname, triggerCount);
    }

    private void HandleShare(TikEvent ev)
    {
        if (characterManager == null || eventConfig == null) return;

        TikTokEventConfig config = eventConfig.GetEventConfig(TikTokEventType.Share);
        if (config == null) return;

        string userId = ev.user.user_id;
        string nickname = ev.user.nickname;
        
        ExecuteActions(config.actions, userId, nickname, 1);
    }

    private void ExecuteActions(List<TikTokActionConfig> actions, string userId, string nickname, int count)
    {
        if (actions == null || actions.Count == 0) return;

        foreach (var actionConfig in actions)
        {
            ExecuteAction(actionConfig, userId, nickname, count);
        }
    }

    private void ExecuteAction(TikTokActionConfig actionConfig, string userId, string nickname, int count)
    {
        bool success = false;
        EventNotificationManager notificationManager = EventNotificationManager.Instance;
        
        switch (actionConfig.actionType)
        {
            case TikTokActionType.Spawn:
                characterManager.SpawnFromTikTok(userId, nickname, null, 1);
                break;

            case TikTokActionType.Respawn:
                CharacterBase respawnedChar = characterManager.RespawnCharacter(userId, nickname, null, 1);
                if (respawnedChar != null && notificationManager != null)
                    notificationManager.ShowRespawnNotification(nickname);
                break;

            case TikTokActionType.AddSwords:
                int swordCount = actionConfig.swordCount * count;
                success = characterManager.AddSwordsToCharacter(userId, nickname, swordCount);
                if (success && notificationManager != null)
                    notificationManager.ShowAddSwordsNotification(nickname, swordCount);
                break;

            case TikTokActionType.UpgradeToLevel2:
                success = characterManager.UpgradeToLevel2(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 2, count);
                break;

            case TikTokActionType.UpgradeToLevel3:
                success = characterManager.UpgradeToLevel3(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 3, count);
                break;

            case TikTokActionType.UpgradeToLevel4:
                success = characterManager.UpgradeToLevel4(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 4, count);
                break;

            case TikTokActionType.UpgradeToLevel5:
                success = characterManager.UpgradeToLevel5(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 5, count);
                break;

            case TikTokActionType.MagnetBooster:
                success = characterManager.ActivateMagnetBooster(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowMagnetBoosterNotification(nickname, count);
                break;

            case TikTokActionType.ShieldBooster:
                success = characterManager.ActivateShieldBooster(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowShieldBoosterNotification(nickname, count);
                break;

            case TikTokActionType.MeteorBooster:
                success = characterManager.ActivateMeteorBooster(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowMeteorBoosterNotification(nickname, count);
                break;

            case TikTokActionType.HealBooster:
                float healAmount = actionConfig.healAmount * count;
                success = characterManager.ActivateHealBooster(userId, nickname, healAmount);
                if (success && notificationManager != null)
                    notificationManager.ShowHealBoosterNotification(nickname, healAmount);
                break;
        }
    }


}
