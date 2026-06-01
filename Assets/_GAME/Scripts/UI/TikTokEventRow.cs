using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class TikTokEventRow : MonoBehaviour
{
    [Header("Event Settings")]
    [SerializeField] private TMP_Dropdown   eventTypeDropdown;
    [SerializeField] private TMP_InputField likeThresholdInput;
    [SerializeField] private TMP_InputField commentCommandInput;
    [SerializeField] private TMP_InputField giftMinPriceInput;

    [Header("Gift By ID")]
    [SerializeField] private GiftDatabase    giftDatabase;
    [SerializeField] private GiftPickerPanel giftPickerPanel;
    [SerializeField] private TextMeshProUGUI selectedGiftNameLabel;
    [SerializeField] private TextMeshProUGUI selectedGiftInfoLabel;  // "ID: x  •  x 💎"
    [SerializeField] private RawImage        selectedGiftIcon;
    [SerializeField] private Button          pickGiftButton;
    
    [Header("Actions")]
    [SerializeField] private Transform actionsParent;
    [SerializeField] private GameObject actionRowPrefab;
    [SerializeField] private Button addActionButton;
    [SerializeField] private Button deleteEventButton;

    private TikTokEventConfig      eventConfig;
    private Action<TikTokEventRow> onDeleteCallback;
    private List<TikTokActionRow>  actionRows = new List<TikTokActionRow>();
    private int selectedGiftId;

    public TikTokEventConfig EventConfig => eventConfig;

    public void Initialize(TikTokEventConfig config, Action<TikTokEventRow> onDelete)
    {
        eventConfig = config;
        onDeleteCallback = onDelete;
        
        if (eventTypeDropdown != null)
        {
            eventTypeDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (TikTokEventType t in System.Enum.GetValues(typeof(TikTokEventType)))
                options.Add(new TMP_Dropdown.OptionData(t.ToString()));
            eventTypeDropdown.AddOptions(options);
            eventTypeDropdown.value = (int)config.eventType;
        }
        
        if (likeThresholdInput != null)
            likeThresholdInput.text = config.likeThreshold.ToString();
        
        if (commentCommandInput != null)
            commentCommandInput.text = config.commentCommand;
        
        if (giftMinPriceInput != null)
            giftMinPriceInput.text = config.giftMinPrice.ToString();

        selectedGiftId = config.giftId;
        UpdateSelectedGiftDisplay();
        pickGiftButton?.onClick.AddListener(OpenGiftPicker);

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

        if (pickGiftButton != null && pickGiftButton.transform.parent != null)
            pickGiftButton.transform.parent.gameObject.SetActive(eventType == TikTokEventType.GiftById);
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

    private void OpenGiftPicker()
    {
        if (giftPickerPanel == null || giftDatabase == null) return;
        giftPickerPanel.Open(giftDatabase, OnGiftPicked);
    }

    private void OnGiftPicked(GiftInfo gift)
    {
        selectedGiftId = gift.id;
        UpdateSelectedGiftDisplay();
    }

    private void UpdateSelectedGiftDisplay()
    {
        GiftInfo gift = giftDatabase?.GetById(selectedGiftId);

        if (selectedGiftNameLabel != null)
            selectedGiftNameLabel.text = gift != null ? gift.name : (selectedGiftId > 0 ? "???" : "Chưa chọn quà");

        if (selectedGiftInfoLabel != null)
            selectedGiftInfoLabel.text = gift != null ? $"ID: {gift.id}  •  {gift.diamond} 💎" : (selectedGiftId > 0 ? $"ID: {selectedGiftId}" : "");

        if (selectedGiftIcon != null)
        {
            if (gift != null && !string.IsNullOrEmpty(gift.imageUrl))
                StartCoroutine(LoadGiftIcon(gift.imageUrl));
            else
            {
                selectedGiftIcon.texture = null;
                selectedGiftIcon.color   = new Color(0.35f, 0.35f, 0.4f);
            }
        }
    }

    private System.Collections.IEnumerator LoadGiftIcon(string url)
    {
        // Dùng lại cache của GiftPickerItem để tránh load lại
        if (GiftPickerItem.TryGetCached(url, out var cached))
        {
            if (selectedGiftIcon != null) { selectedGiftIcon.texture = cached; selectedGiftIcon.color = Color.white; }
            yield break;
        }

        string loadUrl = url.EndsWith(".webp", System.StringComparison.OrdinalIgnoreCase)
            ? url.Substring(0, url.Length - 5) + ".jpeg"
            : url;

        using var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(loadUrl);
        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success && selectedGiftIcon != null)
        {
            var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
            GiftPickerItem.AddToCache(url, tex);
            selectedGiftIcon.texture = tex;
            selectedGiftIcon.color   = Color.white;
        }
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

        eventConfig.giftId = selectedGiftId;

        foreach (var row in actionRows)
        {
            if (row != null)
                row.ApplyChanges();
        }
    }
}
