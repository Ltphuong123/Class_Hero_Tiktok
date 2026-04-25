using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class EventNotification : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    public event Action<EventNotification> OnHideComplete;

    private float originalHeight;
    private bool isHiding;

    public bool IsHiding => isHiding;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        
        if (rectTransform != null)
            originalHeight = rectTransform.sizeDelta.y;
    }

    public void Show(string message, Sprite icon = null, Color? backgroundColor = null)
    {
        if (messageText != null) messageText.text = message;
        
        // Hiển thị icon nếu có
        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        // Đổi màu background nếu có
        if (backgroundImage != null && backgroundColor.HasValue)
        {
            backgroundImage.color = backgroundColor.Value;
        }
        
        gameObject.SetActive(true);
        isHiding = false;
        
        if (rectTransform != null && originalHeight > 0)
        {
            Vector2 size = rectTransform.sizeDelta;
            size.y = originalHeight;
            rectTransform.sizeDelta = size;
        }
        
        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, fadeInDuration).OnComplete(() =>
        {
            DOVirtual.DelayedCall(displayDuration, StartHide);
        });
    }

    private void StartHide()
    {
        isHiding = true;
        canvasGroup.DOFade(0f, fadeOutDuration).OnComplete(CompleteHide);
    }

    private void CompleteHide()
    {
        gameObject.SetActive(false);
        isHiding = false;
        OnHideComplete?.Invoke(this);
    }

    public void ForceHide()
    {
        canvasGroup.DOKill();
        gameObject.SetActive(false);
        isHiding = false;
    }
}
