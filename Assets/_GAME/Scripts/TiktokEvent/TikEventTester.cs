using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TikEventTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TikTokGameHandler gameHandler;

    [Header("UI Elements")]
    [SerializeField] private TMP_InputField userIdInput;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TMP_InputField giftPriceInput;
    [SerializeField] private TMP_InputField giftDeltaInput;
    [SerializeField] private TMP_InputField commentInput;
    [SerializeField] private Button sendGiftButton;
    [SerializeField] private Button sendCommentButton;
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        if (sendGiftButton != null)
            sendGiftButton.onClick.AddListener(SendTestGift);

        if (sendCommentButton != null)
            sendCommentButton.onClick.AddListener(SendTestComment);

        // Set default values
        if (userIdInput != null)
            userIdInput.text = "test_user_123";

        if (nicknameInput != null)
            nicknameInput.text = "TestPlayer";

        if (giftPriceInput != null)
            giftPriceInput.text = "1";

        if (giftDeltaInput != null)
            giftDeltaInput.text = "1";
    }

    private void SendTestGift()
    {
        if (gameHandler == null)
        {
            ShowStatus("GameHandler not assigned!", true);
            return;
        }

        string userId = userIdInput != null ? userIdInput.text : "test_user";
        string nickname = nicknameInput != null ? nicknameInput.text : "TestPlayer";
        int price = giftPriceInput != null && int.TryParse(giftPriceInput.text, out int p) ? p : 1;
        int delta = giftDeltaInput != null && int.TryParse(giftDeltaInput.text, out int d) ? d : 1;

        TikEvent tikEvent = CreateGiftEvent(userId, nickname, price, delta);
        SendEvent(tikEvent);

        ShowStatus($"Sent Gift: Price={price}, Delta={delta}, Total={price * delta}", false);
    }

    private void SendTestComment()
    {
        if (gameHandler == null)
        {
            ShowStatus("GameHandler not assigned!", true);
            return;
        }

        string userId = userIdInput != null ? userIdInput.text : "test_user";
        string nickname = nicknameInput != null ? nicknameInput.text : "TestPlayer";
        string comment = commentInput != null ? commentInput.text : "Test comment";

        TikEvent tikEvent = CreateCommentEvent(userId, nickname, comment);
        SendEvent(tikEvent);

        ShowStatus($"Sent Comment: {comment}", false);
    }

    private TikEvent CreateGiftEvent(string userId, string nickname, int price, int delta)
    {
        TikEvent tikEvent = new TikEvent
        {
            type = "gift_tick",
            ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            user = new TikUser
            {
                user_id = userId,
                unique_id = userId,
                nickname = nickname
            },
            gift = new TikGift
            {
                id = price,
                name = $"Gift_{price}",
                price = price
            },
            delta = delta,
            total = price * delta
        };

        return tikEvent;
    }

    private TikEvent CreateCommentEvent(string userId, string nickname, string comment)
    {
        TikEvent tikEvent = new TikEvent
        {
            type = "comment",
            ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            user = new TikUser
            {
                user_id = userId,
                unique_id = userId,
                nickname = nickname
            },
            comment = comment
        };

        return tikEvent;
    }

    private void SendEvent(TikEvent tikEvent)
    {
        if (gameHandler != null && gameHandler.receiver != null)
        {
            // Trigger event through receiver's public method
            gameHandler.receiver.TriggerTestEvent(tikEvent);
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }

        Debug.Log($"[TikEventTester] {message}");
    }

    // Public methods for testing from code
    public void SendGift(int price, int delta = 1)
    {
        string userId = userIdInput != null ? userIdInput.text : "test_user";
        string nickname = nicknameInput != null ? nicknameInput.text : "TestPlayer";

        TikEvent tikEvent = CreateGiftEvent(userId, nickname, price, delta);
        SendEvent(tikEvent);

        ShowStatus($"Sent Gift: Price={price}, Delta={delta}", false);
    }

    public void SendComment(string comment)
    {
        string userId = userIdInput != null ? userIdInput.text : "test_user";
        string nickname = nicknameInput != null ? nicknameInput.text : "TestPlayer";

        TikEvent tikEvent = CreateCommentEvent(userId, nickname, comment);
        SendEvent(tikEvent);

        ShowStatus($"Sent Comment: {comment}", false);
    }
}
