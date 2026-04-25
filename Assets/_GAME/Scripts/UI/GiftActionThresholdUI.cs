using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GiftActionThresholdUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField priceInput;
    [SerializeField] private TMP_Dropdown actionDropdown;
    [SerializeField] private Button removeButton;

    private GiftActionThreshold threshold;
    private GiftActionConfigEditor editor;

    public GiftActionThreshold Threshold => threshold;

    public void Initialize(GiftActionThreshold thresholdData, GiftActionConfigEditor configEditor)
    {
        threshold = thresholdData;
        editor = configEditor;

        if (priceInput != null)
            priceInput.text = threshold.priceThreshold.ToString();

        if (actionDropdown != null)
        {
            actionDropdown.ClearOptions();
            actionDropdown.AddOptions(new System.Collections.Generic.List<string>(System.Enum.GetNames(typeof(GiftActionType))));
            actionDropdown.value = (int)threshold.actionType;
        }

        if (removeButton != null)
            removeButton.onClick.AddListener(OnRemoveClicked);
    }

    public void UpdateThreshold()
    {
        if (threshold == null)
            return;

        if (priceInput != null && int.TryParse(priceInput.text, out int price))
            threshold.priceThreshold = price;

        if (actionDropdown != null)
            threshold.actionType = (GiftActionType)actionDropdown.value;
    }

    private void OnRemoveClicked()
    {
        if (editor != null)
            editor.RemoveThreshold(this);
    }
}
