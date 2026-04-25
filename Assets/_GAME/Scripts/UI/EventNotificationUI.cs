using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class EventNotificationUI : MonoBehaviour
{
    [SerializeField] private EventNotification notificationPrefab;
    [SerializeField] private Transform notificationContainer;
    [SerializeField] private float notificationSpacing = 10f;
    [SerializeField] private float repositionDuration = 0.3f;

    private Queue<EventNotification> notificationPool = new();
    private List<EventNotification> activeNotifications = new();

    private void Awake()
    {
        if (notificationContainer == null) notificationContainer = transform;
        
        if (notificationContainer is RectTransform containerRect)
        {
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = Vector2.zero;
        }
    }

    public void ShowNotification(string message, Sprite icon = null, Color? backgroundColor = null)
    {
        EventNotification notification = GetNotification();
        notification.OnHideComplete += OnNotificationHidden;
        
        RectTransform rt = notification.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = Vector2.zero;
        }
        
        notification.Show(message, icon, backgroundColor);
        
        activeNotifications.Insert(0, notification);
        notification.transform.SetAsFirstSibling();
        
        RepositionNotifications(false);
    }

    private void OnNotificationHidden(EventNotification notification)
    {
        notification.OnHideComplete -= OnNotificationHidden;
        
        int index = activeNotifications.IndexOf(notification);
        bool isLastNotification = index == activeNotifications.Count - 1;
        
        activeNotifications.Remove(notification);
        ReturnNotification(notification);
        
        if (!isLastNotification)
            RepositionNotifications(true);
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

    private void RepositionNotifications(bool animate)
    {
        float totalHeight = 0f;
        
        for (int i = 0; i < activeNotifications.Count; i++)
        {
            EventNotification notification = activeNotifications[i];
            if (notification.IsHiding) continue;
            
            RectTransform rt = notification.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                
                float targetY = -totalHeight;
                
                if (animate)
                {
                    rt.DOKill();
                    rt.DOAnchorPosY(targetY, repositionDuration).SetEase(Ease.OutQuad);
                }
                else
                {
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, targetY);
                }
                
                totalHeight += rt.sizeDelta.y + notificationSpacing;
            }
        }
        
        if (notificationContainer is RectTransform containerRect)
        {
            Vector2 size = containerRect.sizeDelta;
            size.y = Mathf.Max(totalHeight, 100f);
            containerRect.sizeDelta = size;
        }
    }
}
