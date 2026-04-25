using UnityEngine;

public class TikTokGameHandler : MonoBehaviour
{
    [Header("References")]
    public TikTokUdpReceiver receiver;
    [SerializeField] private CharacterManager characterManager;
    [SerializeField] private GiftActionConfig giftActionConfig;

    void Start()
    {
        receiver.OnEvent += HandleEvent;
        
        if (characterManager == null)
        {
            characterManager = CharacterManager.Instance;
        }

        // Load config from file
        if (giftActionConfig != null)
        {
            giftActionConfig.LoadFromFile();
        }
    }

    void OnDestroy()
    {
        receiver.OnEvent -= HandleEvent;
    }

    void HandleEvent(TikEvent ev)
    {
        switch (ev.type)
        {
            case "connect":
                break;

            case "disconnect":
                break;

            case "comment":
                HandleComment(ev);
                break;

            case "like":
                break;

            case "follow":
                break;

            case "share":
                break;

            case "gift_final":
                HandleGift(ev);
                break;
        }
    }

    void HandleComment(TikEvent ev)
    {
        if (characterManager == null)
            return;

        string comment = ev.comment?.Trim();
        string userId = ev.user.user_id;
        string nickname = ev.user.nickname;

        characterManager.SpawnFromTikTok(userId, nickname, null, 1);
    }

    void HandleGift(TikEvent ev)
    {
        int count = ev.count;
        int totalCoin = ev.total;
        int price = ev.gift.price;
        string userId = ev.user.user_id;
        string nickname = ev.user.nickname;

        if (characterManager == null)
            return;

        if (giftActionConfig == null)
        {
            Debug.LogWarning("GiftActionConfig is not assigned!");
            return;
        }

        // Lấy action type từ config dựa trên price
        GiftActionType actionType = giftActionConfig.GetActionForPrice(price);

        // Thực hiện action tương ứng
        ExecuteGiftAction(actionType, userId, nickname, count);
    }

    private void ExecuteGiftAction(GiftActionType actionType, string userId, string nickname, int count)
    {
        bool success = false;
        
        switch (actionType)
        {
            case GiftActionType.AddSwords:
                success = characterManager.AddSwordsToCharacter(userId, count);
                if (success && EventNotificationManager.Instance != null)
                    EventNotificationManager.Instance.ShowAddSwordsNotification(nickname, count);
                break;

            case GiftActionType.UpgradeLevel2:
                success = characterManager.UpgradeToLevel2(userId, count);
                if (success && EventNotificationManager.Instance != null)
                    EventNotificationManager.Instance.ShowUpgradeNotification(nickname, 2, count);
                break;

            case GiftActionType.UpgradeLevel3:
                success = characterManager.UpgradeToLevel3(userId, count);
                if (success && EventNotificationManager.Instance != null)
                    EventNotificationManager.Instance.ShowUpgradeNotification(nickname, 3, count);
                break;

            case GiftActionType.UpgradeLevel4:
                success = characterManager.UpgradeToLevel4(userId, count);
                if (success && EventNotificationManager.Instance != null)
                    EventNotificationManager.Instance.ShowUpgradeNotification(nickname, 4, count);
                break;

            case GiftActionType.UpgradeLevel5:
                success = characterManager.UpgradeToLevel5(userId, count);
                if (success && EventNotificationManager.Instance != null)
                    EventNotificationManager.Instance.ShowUpgradeNotification(nickname, 5, count);
                break;

            case GiftActionType.RespawnCharacter:
                CharacterBase respawnedChar = characterManager.RespawnCharacter(userId, nickname);
                success = respawnedChar != null;
                if (success && EventNotificationManager.Instance != null)
                    EventNotificationManager.Instance.ShowRespawnNotification(nickname);
                break;

            case GiftActionType.ActivateMagnet:
                success = characterManager.ActivateMagnetBooster(userId, count);
                if (success && EventNotificationManager.Instance != null)
                    EventNotificationManager.Instance.ShowMagnetBoosterNotification(nickname, count);
                break;

            case GiftActionType.ActivateShield:
                success = characterManager.ActivateShieldBooster(userId, count);
                if (success && EventNotificationManager.Instance != null)
                    EventNotificationManager.Instance.ShowShieldBoosterNotification(nickname, count);
                break;

            case GiftActionType.ActivateMeteor:
                success = characterManager.ActivateMeteorBooster(userId, count);
                if (success && EventNotificationManager.Instance != null)
                    EventNotificationManager.Instance.ShowMeteorBoosterNotification(nickname, count);
                break;
        }
    }
}
