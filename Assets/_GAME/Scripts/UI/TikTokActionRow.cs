using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TikTokActionRow : MonoBehaviour
{
    [Header("Action Settings")]
    [SerializeField] private TMP_Dropdown actionTypeDropdown;
    [SerializeField] private TMP_InputField swordCountInput;
    [SerializeField] private TMP_InputField healAmountInput;
    [SerializeField] private Button deleteButton;

    private TikTokActionConfig actionConfig;
    private Action<TikTokActionRow> onDeleteCallback;

    public TikTokActionConfig ActionConfig => actionConfig;

    public void Initialize(TikTokActionConfig config, Action<TikTokActionRow> onDelete)
    {
        actionConfig = config;
        onDeleteCallback = onDelete;

        if (actionTypeDropdown != null)
            actionTypeDropdown.value = (int)config.actionType;
        
        if (swordCountInput != null)
            swordCountInput.text = config.swordCount.ToString();
        
        if (healAmountInput != null)
            healAmountInput.text = config.healAmount.ToString();

        actionTypeDropdown?.onValueChanged.AddListener(OnActionTypeChanged);
        deleteButton?.onClick.AddListener(OnDelete);

        UpdateInputVisibility();
    }

    private void OnActionTypeChanged(int value)
    {
        UpdateInputVisibility();
    }

    private void UpdateInputVisibility()
    {
        if (actionTypeDropdown == null) return;
        
        TikTokActionType actionType = (TikTokActionType)actionTypeDropdown.value;

        bool showSwordCount = actionType == TikTokActionType.AddSwords;
        bool showHealAmount = actionType == TikTokActionType.HealBooster;

        if (swordCountInput != null && swordCountInput.transform.parent != null)
            swordCountInput.transform.parent.gameObject.SetActive(showSwordCount);
        
        if (healAmountInput != null && healAmountInput.transform.parent != null)
            healAmountInput.transform.parent.gameObject.SetActive(showHealAmount);
    }

    private void OnDelete()
    {
        onDeleteCallback?.Invoke(this);
    }

    public void ApplyChanges()
    {
        if (actionTypeDropdown != null)
            actionConfig.actionType = (TikTokActionType)actionTypeDropdown.value;
        
        if (swordCountInput != null && int.TryParse(swordCountInput.text, out int swordCount))
            actionConfig.swordCount = swordCount;
        
        if (healAmountInput != null && float.TryParse(healAmountInput.text, out float healAmount))
            actionConfig.healAmount = healAmount;
    }
}
