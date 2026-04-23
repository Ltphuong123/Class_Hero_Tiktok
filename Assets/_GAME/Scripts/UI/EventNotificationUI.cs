using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class EventNotificationUI : MonoBehaviour
{
    [SerializeField] private EventNotification notificationPrefab;
    [SerializeField] private Transform notificationContainer;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float notificationSpacing = 10f;
    [SerializeField] private float repositionDuration = 0.3f;

    private Queue<EventNotification> notificationPool = new();
    private List<EventNotification> activeNotifications = new();

    private void Awake()
    {
        if (notificationContainer == null)
            notificationContainer = transform;
            
        if (scrollRect == null)
            scrollRect = GetComponentInParent<ScrollRect>();
    }

    public void ShowNotification(string message)
    {
        EventNotification notification = GetNotification();
        notification.OnHideComplete += OnNotificationHidden;
        notification.Show(message);
        
        activeNotifications.Insert(0, notification);
        notification.transform.SetAsFirstSibling();
        
        RectTransform rt = notification.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
        
        RepositionNotifications();
        
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private void OnNotificationHidden(EventNotification notification)
    {
        notification.OnHideComplete -= OnNotificationHidden;
        activeNotifications.Remove(notification);
        ReturnNotification(notification);
        RepositionNotifications();
    }

    private EventNotification GetNotification()
    {
        if (notificationPool.Count > 0)
        {
            EventNotification notification = notificationPool.Dequeue();
            notification.gameObject.SetActive(true);
            return notification;
        }

        return Instantiate(notificationPrefab, notificationContainer);
    }

    private void ReturnNotification(EventNotification notification)
    {
        notification.gameObject.SetActive(false);
        notificationPool.Enqueue(notification);
    }

    private void RepositionNotifications()
    {
        float totalHeight = 0f;
        
        for (int i = 0; i < activeNotifications.Count; i++)
        {
            RectTransform rt = activeNotifications[i].GetComponent<RectTransform>();
            if (rt != null)
            {
                float targetY = -totalHeight;
                
                rt.DOKill();
                rt.DOAnchorPosY(targetY, repositionDuration).SetEase(Ease.OutQuad);
                
                totalHeight += rt.sizeDelta.y + notificationSpacing;
            }
        }
        
        if (notificationContainer is RectTransform containerRect)
        {
            Vector2 size = containerRect.sizeDelta;
            size.y = Mathf.Max(totalHeight, 0f);
            containerRect.sizeDelta = size;
        }
    }
}
