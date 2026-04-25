using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GiftActionConfigEditor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GiftActionConfig config;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject thresholdItemPrefab;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button addButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    [Header("Game Time Settings")]
    [SerializeField] private TMP_InputField gameTimeInput;

    private const string GAME_TIME_KEY = "GameTimeInSeconds";
    private const int DEFAULT_GAME_TIME = 300;

    private List<GiftActionThresholdUI> thresholdUIItems = new List<GiftActionThresholdUI>();

    private void Start()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveConfig);

        if (addButton != null)
            addButton.onClick.AddListener(AddNewThreshold);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefault);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseEditor);

        LoadConfig();
        LoadGameTime();
    }

    private void LoadGameTime()
    {
        if (gameTimeInput != null)
        {
            int savedTime = PlayerPrefs.GetInt(GAME_TIME_KEY, DEFAULT_GAME_TIME);
            gameTimeInput.text = savedTime.ToString();
        }
    }

    private void SaveGameTime()
    {
        if (gameTimeInput != null && int.TryParse(gameTimeInput.text, out int gameTime))
        {
            if (gameTime < 1)
                gameTime = DEFAULT_GAME_TIME;

            PlayerPrefs.SetInt(GAME_TIME_KEY, gameTime);
            PlayerPrefs.Save();
            Debug.Log($"Game time saved: {gameTime} seconds");
        }
    }

    private void LoadConfig()
    {
        if (config == null)
        {
            Debug.LogError("GiftActionConfig is not assigned!");
            return;
        }

        // Clear existing UI items
        foreach (var item in thresholdUIItems)
        {
            if (item != null && item.gameObject != null)
                Destroy(item.gameObject);
        }
        thresholdUIItems.Clear();

        // Create UI items for each threshold
        foreach (var threshold in config.thresholds)
        {
            CreateThresholdUI(threshold);
        }
    }

    private void CreateThresholdUI(GiftActionThreshold threshold)
    {
        if (thresholdItemPrefab == null || contentParent == null)
            return;

        GameObject itemObj = Instantiate(thresholdItemPrefab, contentParent);
        GiftActionThresholdUI thresholdUI = itemObj.GetComponent<GiftActionThresholdUI>();

        if (thresholdUI != null)
        {
            thresholdUI.Initialize(threshold, this);
            thresholdUIItems.Add(thresholdUI);
        }
    }

    private void AddNewThreshold()
    {
        if (config == null)
            return;

        GiftActionThreshold newThreshold = new GiftActionThreshold(1, GiftActionType.AddSwords);
        config.thresholds.Add(newThreshold);
        CreateThresholdUI(newThreshold);
    }

    public void RemoveThreshold(GiftActionThresholdUI thresholdUI)
    {
        if (config == null || thresholdUI == null)
            return;

        config.thresholds.Remove(thresholdUI.Threshold);
        thresholdUIItems.Remove(thresholdUI);
        Destroy(thresholdUI.gameObject);
    }

    private void SaveConfig()
    {
        if (config == null)
            return;

        // Update config from UI
        for (int i = 0; i < thresholdUIItems.Count; i++)
        {
            if (i < config.thresholds.Count)
            {
                thresholdUIItems[i].UpdateThreshold();
            }
        }

        // Sort thresholds
        config.SortThresholds();

        // Save to file (persistent)
        config.SaveToFile();

        // Save game time to PlayerPrefs
        SaveGameTime();

        // Save to ScriptableObject asset (editor only)
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(config);
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log("Gift Action Config saved!");
        
        // Reload UI to show sorted order
        LoadConfig();
    }

    private void ResetToDefault()
    {
        if (config == null)
            return;

        config.ResetToDefault();
        LoadConfig();
        Debug.Log("Gift Action Config reset to default!");
    }

    private void CloseEditor()
    {
        gameObject.SetActive(false);
    }

    public void OpenEditor()
    {
        gameObject.SetActive(true);
        LoadConfig();
        LoadGameTime();
    }

    public static int GetSavedGameTime()
    {
        return PlayerPrefs.GetInt(GAME_TIME_KEY, DEFAULT_GAME_TIME);
    }
}
