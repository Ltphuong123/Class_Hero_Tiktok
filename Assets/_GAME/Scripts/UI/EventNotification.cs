using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class EventNotification : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    public event Action<EventNotification> OnHideComplete;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show(string message)
    {
        if (messageText != null) messageText.text = message;
        
        gameObject.SetActive(true);
        
        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, fadeInDuration).OnComplete(() =>
        {
            DOVirtual.DelayedCall(displayDuration, () =>
            {
                canvasGroup.DOFade(0f, fadeOutDuration).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    OnHideComplete?.Invoke(this);
                });
            });
        });
    }
}
