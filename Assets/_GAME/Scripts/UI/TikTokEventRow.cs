using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class TikTokEventRow : MonoBehaviour
{
    [Header("Event Settings")]
    [SerializeField] private TMP_Dropdown eventTypeDropdown;
    [SerializeField] private TMP_InputField likeThresholdInput;
    [SerializeField] private TMP_InputField commentCommandInput;
    [SerializeField] private TMP_InputField giftMinPriceInput;
    
    [Header("Actions")]
    [SerializeField] private Transform actionsParent;
    [SerializeField] private GameObject actionRowPrefab;
    [SerializeField] private Button addActionButton;
    [SerializeField] private Button deleteEventButton;

    private TikTokEventConfig eventConfig;
    private Action<TikTokEventRow> onDeleteCallback;
    private List<TikTokActionRow> actionRows = new List<TikTokActionRow>();

    public TikTokEventConfig EventConfig => eventConfig;

    public void Initialize(TikTokEventConfig config, Action<TikTokEventRow> onDelete)
    {
        eventConfig = config;
        onDeleteCallback = onDelete;
        
        if (eventTypeDropdown != null)
            eventTypeDropdown.value = (int)config.eventType;
        
        if (likeThresholdInput != null)
            likeThresholdInput.text = config.likeThreshold.ToString();
        
        if (commentCommandInput != null)
            commentCommandInput.text = config.commentCommand;
        
        if (giftMinPriceInput != null)
            giftMinPriceInput.text = config.giftMinPrice.ToString();

        eventTypeDropdown?.onValueChanged.AddListener(OnEventTypeChanged);
        addActionButton?.onClick.AddListener(OnAddAction);
        deleteEventButton?.onClick.AddListener(OnDeleteEvent);

        UpdateInputVisibility();
        InitializeActions();
    }

    private void OnEventTypeChanged(int value)
    {
        UpdateInputVisibility();
    }

    private void UpdateInputVisibility()
    {
        if (eventTypeDropdown == null) return;
        
        TikTokEventType eventType = (TikTokEventType)eventTypeDropdown.value;

        if (likeThresholdInput != null && likeThresholdInput.transform.parent != null)
            likeThresholdInput.transform.parent.gameObject.SetActive(eventType == TikTokEventType.Like);
        
        if (commentCommandInput != null && commentCommandInput.transform.parent != null)
            commentCommandInput.transform.parent.gameObject.SetActive(eventType == TikTokEventType.Comment);
        
        if (giftMinPriceInput != null && giftMinPriceInput.transform.parent != null)
            giftMinPriceInput.transform.parent.gameObject.SetActive(eventType == TikTokEventType.Gift);
    }

    private void InitializeActions()
    {
        ClearActions();

        foreach (var actionConfig in eventConfig.actions)
        {
            CreateActionRow(actionConfig);
        }
    }

    private void CreateActionRow(TikTokActionConfig actionConfig)
    {
        if (actionRowPrefab == null || actionsParent == null) return;

        GameObject rowObj = Instantiate(actionRowPrefab, actionsParent);
        TikTokActionRow row = rowObj.GetComponent<TikTokActionRow>();
        
        if (row != null)
        {
            row.Initialize(actionConfig, OnDeleteAction);
            actionRows.Add(row);
        }
    }

    private void ClearActions()
    {
        foreach (var row in actionRows)
        {
            if (row != null && row.gameObject != null)
                Destroy(row.gameObject);
        }
        actionRows.Clear();
    }

    private void OnAddAction()
    {
        TikTokActionConfig newAction = new TikTokActionConfig
        {
            actionType = TikTokActionType.AddSwords,
            swordCount = 1,
            healAmount = 5f
        };

        eventConfig.actions.Add(newAction);
        CreateActionRow(newAction);
    }

    private void OnDeleteAction(TikTokActionRow row)
    {
        if (row == null) return;

        eventConfig.actions.Remove(row.ActionConfig);
        actionRows.Remove(row);
        Destroy(row.gameObject);
    }

    private void OnDeleteEvent()
    {
        onDeleteCallback?.Invoke(this);
    }

    public void ApplyChanges()
    {
        if (eventTypeDropdown != null)
            eventConfig.eventType = (TikTokEventType)eventTypeDropdown.value;
        
        if (likeThresholdInput != null && int.TryParse(likeThresholdInput.text, out int likeThreshold))
            eventConfig.likeThreshold = likeThreshold;
        
        if (commentCommandInput != null)
            eventConfig.commentCommand = commentCommandInput.text;
        
        if (giftMinPriceInput != null && int.TryParse(giftMinPriceInput.text, out int giftMinPrice))
            eventConfig.giftMinPrice = giftMinPrice;

        foreach (var row in actionRows)
        {
            if (row != null)
                row.ApplyChanges();
        }
    }
}
