using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TikTokEventSettingsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TikTokEventConfigSO config;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject eventRowPrefab;
    
    [Header("Buttons")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button addEventButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    private List<TikTokEventRow> eventRows = new List<TikTokEventRow>();

    private void Start()
    {
        toggleButton?.onClick.AddListener(TogglePanel);
        addEventButton?.onClick.AddListener(OnAddEvent);
        saveButton?.onClick.AddListener(OnSave);
        loadButton?.onClick.AddListener(OnLoad);
        resetButton?.onClick.AddListener(OnReset);
        closeButton?.onClick.AddListener(OnClose);

        if (mainPanel != null)
            mainPanel.SetActive(false);
    }

    private void InitializeUI()
    {
        if (config == null || contentParent == null || eventRowPrefab == null)
        {
            Debug.LogError("TikTokEventSettingsUI: Missing references!");
            return;
        }

        ClearRows();

        foreach (var eventConfig in config.events)
        {
            CreateEventRow(eventConfig);
        }
    }

    private void CreateEventRow(TikTokEventConfig eventConfig)
    {
        GameObject rowObj = Instantiate(eventRowPrefab, contentParent);
        TikTokEventRow row = rowObj.GetComponent<TikTokEventRow>();
        
        if (row != null)
        {
            row.Initialize(eventConfig, OnDeleteEvent);
            eventRows.Add(row);
        }
    }

    private void ClearRows()
    {
        foreach (var row in eventRows)
        {
            if (row != null && row.gameObject != null)
                Destroy(row.gameObject);
        }
        eventRows.Clear();
    }

    private void OnAddEvent()
    {
        if (config == null) return;

        TikTokEventConfig newEvent = new TikTokEventConfig
        {
            eventType = TikTokEventType.Like,
            likeThreshold = 5,
            commentCommand = "",
            giftMinPrice = 10,
            actions = new List<TikTokActionConfig>()
        };

        config.events.Add(newEvent);
        CreateEventRow(newEvent);
    }

    private void OnDeleteEvent(TikTokEventRow row)
    {
        if (config == null || row == null) return;

        config.events.Remove(row.EventConfig);
        eventRows.Remove(row);
        Destroy(row.gameObject);
    }

    private void OnSave()
    {
        if (config == null) return;

        foreach (var row in eventRows)
        {
            if (row != null)
                row.ApplyChanges();
        }

        config.SaveToJson();
        Debug.Log("TikTok Event Config saved!");
    }

    private void OnLoad()
    {
        if (config == null) return;

        config.LoadFromJson();
        InitializeUI();
        Debug.Log("TikTok Event Config loaded!");
    }

    private void OnReset()
    {
        if (config == null) return;

        config.ResetToDefault();
        InitializeUI();
        Debug.Log("TikTok Event Config reset to default!");
    }

    private void OnClose()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);
    }

    public void Show()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);
        InitializeUI();
    }

    public void TogglePanel()
    {
        if (mainPanel != null)
        {
            bool isActive = mainPanel.activeSelf;
            if (isActive)
                OnClose();
            else
                Show();
        }
    }
}
