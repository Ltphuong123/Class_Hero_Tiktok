using UnityEngine;

public class TikTokGameHandler : MonoBehaviour
{
    [Header("References")]
    public TikTokUdpReceiver receiver;
    [SerializeField] private CharacterManager characterManager;
    [SerializeField] private GiftActionConfig giftActionConfig;

    private void Start()
    {
        receiver.OnEvent += HandleEvent;
        
        if (characterManager == null)
            characterManager = CharacterManager.Instance;

        if (giftActionConfig != null)
            giftActionConfig.LoadFromFile();
    }

    private void OnDestroy()
    {
        receiver.OnEvent -= HandleEvent;
    }

    private void HandleEvent(TikEvent ev)
    {
        switch (ev.type)
        {
            case "comment":
                HandleComment(ev);
                break;
            case "gift_final":
                HandleGift(ev);
                break;
            case "like":
                HandleLike(ev);
                break;
        }
    }

    private void HandleComment(TikEvent ev)
    {
        if (characterManager == null) return;

        string comment = ev.comment?.Trim();
        if (string.IsNullOrEmpty(comment)) return;

        string userId = ev.user.user_id;
        string nickname = ev.user.nickname;

        // Chuyển comment về lowercase để so sánh
        string commentLower = comment.ToLower();

        if (comment == "1")
        {
            characterManager.SpawnFromTikTok(userId, nickname, null, 1);
            return;
        }

        if (comment == "2")
        {
            CharacterBase respawnedChar = characterManager.RespawnCharacter(userId, nickname, null, 1);
            if (respawnedChar != null && EventNotificationManager.Instance != null)
                EventNotificationManager.Instance.ShowRespawnNotification(nickname);
            return;
        }

        // Xử lý attack command: "atk ID" hoặc "atkID" (không phân biệt hoa thường)
        if (commentLower.StartsWith("atk ") || commentLower.StartsWith("atk"))
        {
            ProcessAttackCommand(comment, userId, nickname);
            return;
        }

        // Xử lý stop command (không phân biệt hoa thường)
        if (commentLower == "stop")
        {
            characterManager.UnlockTargetAttack(userId);
            return;
        }
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

    private void HandleGift(TikEvent ev)
    {
        if (characterManager == null || giftActionConfig == null) return;

        string userId = ev.user.user_id;
        string nickname = ev.user.nickname;
        int count = ev.count;
        int price = ev.gift.price;

        GiftActionType actionType = giftActionConfig.GetActionForPrice(price);
        ExecuteGiftAction(actionType, userId, nickname, count);
    }

    private void ExecuteGiftAction(GiftActionType actionType, string userId, string nickname, int count)
    {
        bool success = false;
        EventNotificationManager notificationManager = EventNotificationManager.Instance;
        
        switch (actionType)
        {
            case GiftActionType.AddSwords:
                success = characterManager.AddSwordsToCharacter(userId, count);
                if (success && notificationManager != null)
                    notificationManager.ShowAddSwordsNotification(nickname, count);
                break;

            case GiftActionType.UpgradeLevel2:
                success = characterManager.UpgradeToLevel2(userId, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 2, count);
                break;

            case GiftActionType.UpgradeLevel3:
                success = characterManager.UpgradeToLevel3(userId, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 3, count);
                break;

            case GiftActionType.UpgradeLevel4:
                success = characterManager.UpgradeToLevel4(userId, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 4, count);
                break;

            case GiftActionType.UpgradeLevel5:
                success = characterManager.UpgradeToLevel5(userId, count);
                if (success && notificationManager != null)
                    notificationManager.ShowUpgradeNotification(nickname, 5, count);
                break;

            case GiftActionType.RespawnCharacter:
                // CharacterBase respawnedChar = characterManager.RespawnCharacter(userId, nickname);
                // if (respawnedChar != null && notificationManager != null)
                //     notificationManager.ShowRespawnNotification(nickname);
                break;

            case GiftActionType.ActivateMagnet:
                success = characterManager.ActivateMagnetBooster(userId, count);
                if (success && notificationManager != null)
                    notificationManager.ShowMagnetBoosterNotification(nickname, count);
                break;

            case GiftActionType.ActivateShield:
                success = characterManager.ActivateShieldBooster(userId, count);
                if (success && notificationManager != null)
                    notificationManager.ShowShieldBoosterNotification(nickname, count);
                break;

            case GiftActionType.ActivateMeteor:
                success = characterManager.ActivateMeteorBooster(userId, count);
                if (success && notificationManager != null)
                    notificationManager.ShowMeteorBoosterNotification(nickname, count);
                break;
        }
    }

    private void HandleLike(TikEvent ev)
    {
        if (characterManager == null) return;

        string userId = ev.user.user_id;
        string nickname = ev.user.nickname;
        
        Debug.Log($"❤️ {nickname} +{ev.like_count} (total {ev.total_likes})");
        
        // Tính số kiếm dựa trên like_count
        int swordsToAdd;
        if (ev.like_count < 5)
        {
            swordsToAdd = 1; // Nếu like_count < 5 thì cho 1 kiếm
        }
        else
        {
            swordsToAdd = ev.like_count / 5; // Mỗi 5 like_count thì cho 1 kiếm
        }
        
        // Đảm bảo ít nhất cho 1 kiếm
        if (swordsToAdd < 1) swordsToAdd = 1;
        
        bool success = characterManager.AddSwordsToCharacter(userId, swordsToAdd);
        
        if (success && EventNotificationManager.Instance != null)
        {
            EventNotificationManager.Instance.ShowAddSwordsNotification(nickname, swordsToAdd);
        }
    }
}
