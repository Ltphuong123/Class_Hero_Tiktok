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
        int giftId = ev.gift.id;

        Debug.Log($"[Gift] {nickname} id={giftId}  price={giftPrice}  delta={delta}");


        var idConfigs = eventConfig.GetEventConfigsByGiftId(giftId);
        if (idConfigs != null && idConfigs.Count > 0)
        {
            foreach (var config in idConfigs)
                ExecuteActions(config.actions, userId, nickname, delta);
        }
        else
        {
            // 2. Fallback: tính theo giá (tier cao nhất khớp)
            int highestMinPrice = 0;
            foreach (var config in eventConfig.events)
            {
                if (config.eventType != TikTokEventType.Gift) continue;
                if (giftPrice < config.giftMinPrice) continue;
                if (config.giftMinPrice > highestMinPrice)
                    highestMinPrice = config.giftMinPrice;
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

        int scoreGain = delta * giftPrice * 5;
        if (scoreGain > 0)
            characterManager.AddScore(userId, nickname, scoreGain);
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

            case TikTokActionType.UpgradeToLevel6:
                success = characterManager.UpgradeToLevel6(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 6, count);
                break;

            case TikTokActionType.UpgradeToLevel7:
                success = characterManager.UpgradeToLevel7(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 7, count);
                break;

            case TikTokActionType.UpgradeToLevel8:
                success = characterManager.UpgradeToLevel8(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 8, count);
                break;

            case TikTokActionType.UpgradeToLevel9:
                success = characterManager.UpgradeToLevel9(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 9, count);
                break;

            case TikTokActionType.UpgradeToLevel10:
                success = characterManager.UpgradeToLevel10(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 10, count);
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

            case TikTokActionType.HealBooster:
                float healAmount = actionConfig.healAmount * count;
                success = characterManager.ActivateHealBooster(userId, nickname, healAmount);
                if (success && notificationManager != null)
                    notificationManager.ShowHealBoosterNotification(nickname, healAmount);
                break;

            case TikTokActionType.UseSkill1:
                characterManager.ActivateSkill1(userId, nickname, count);
                break;

            case TikTokActionType.UseSkill2:
                characterManager.ActivateSkill2(userId, nickname, count);
                break;

            case TikTokActionType.UseSkill3:
                characterManager.ActivateSkill3(userId, nickname, count);
                break;

            case TikTokActionType.UseSkill4:
                success = characterManager.ActivateSkill4(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowSkill4Notification(nickname, count);
                break;

            case TikTokActionType.UseSkill5:
                success = characterManager.ActivateSkill5(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowSkill5Notification(nickname, count);
                break;

            case TikTokActionType.UseSkill6:
                success = characterManager.ActivateSkill6(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowSkill6Notification(nickname, count);
                break;

            case TikTokActionType.UseSkill7:
                success = characterManager.ActivateSkill7(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowSkill7Notification(nickname, count);
                break;

            case TikTokActionType.UseSkill8:
                success = characterManager.ActivateSkill8(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowSkill8Notification(nickname, count);
                break;

            case TikTokActionType.UseSkill9:
                success = characterManager.ActivateSkill9(userId, nickname, count);
                if (success && notificationManager != null)
                    notificationManager.ShowSkill9Notification(nickname, count);
                break;

            case TikTokActionType.AddKimSword:
                success = characterManager.AddKimSwords(userId, nickname, actionConfig.swordCount * count);
                if (success && notificationManager != null)
                    notificationManager.ShowAddSwordsNotification(nickname, actionConfig.swordCount * count);
                break;

            case TikTokActionType.AddMocSword:
                success = characterManager.AddMocSwords(userId, nickname, actionConfig.swordCount * count);
                if (success && notificationManager != null)
                    notificationManager.ShowAddSwordsNotification(nickname, actionConfig.swordCount * count);
                break;

            case TikTokActionType.AddThuySword:
                success = characterManager.AddThuySwords(userId, nickname, actionConfig.swordCount * count);
                if (success && notificationManager != null)
                    notificationManager.ShowAddSwordsNotification(nickname, actionConfig.swordCount * count);
                break;

            case TikTokActionType.AddHoaSword:
                success = characterManager.AddHoaSwords(userId, nickname, actionConfig.swordCount * count);
                if (success && notificationManager != null)
                    notificationManager.ShowAddSwordsNotification(nickname, actionConfig.swordCount * count);
                break;

            case TikTokActionType.AddThoSword:
                success = characterManager.AddThoSwords(userId, nickname, actionConfig.swordCount * count);
                if (success && notificationManager != null)
                    notificationManager.ShowAddSwordsNotification(nickname, actionConfig.swordCount * count);
                break;
        }
    }


}
