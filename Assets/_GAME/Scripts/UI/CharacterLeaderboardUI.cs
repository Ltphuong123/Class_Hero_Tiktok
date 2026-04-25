using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterLeaderboardUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Special Panel")]
    [SerializeField] private GameObject specialPanel;
    [SerializeField] private KeyCode specialPanelKey = KeyCode.P;

    [Header("Buttons")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Content")]
    [SerializeField] private Transform content;
    [SerializeField] private LeaderboardRow rowPrefab;

    [Header("Header Info")]
    [SerializeField] private TextMeshProUGUI aliveCountText;

    private readonly List<LeaderboardRow> rows = new();
    private bool isVisible = false;

    private void Awake()
    {
        if (openButton != null)
            openButton.onClick.AddListener(OnOpenButtonClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(OnOpenButtonClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
    }

    private void Start()
    {
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.OnRankUpdated += RefreshUI;
    }

    private void OnDisable()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.OnRankUpdated -= RefreshUI;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleVisible();

        if (Input.GetKeyDown(specialPanelKey))
            ToggleSpecialPanel();
    }

    private void OnOpenButtonClicked()
    {
        SetVisible(true);
    }

    private void OnCloseButtonClicked()
    {
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        
        if (panel != null)
            panel.SetActive(visible);

        if (openButton != null)
            openButton.gameObject.SetActive(!visible);

        if (visible)
            RefreshUI();
    }

    public void ToggleVisible() => SetVisible(!isVisible);

    public void ToggleSpecialPanel()
    {
        if (specialPanel != null)
        {
            bool newState = !specialPanel.activeSelf;
            specialPanel.SetActive(newState);
        }
    }

    private void RefreshUI()
    {
        if (!isVisible) return;

        var ranked = CharacterManager.Instance.RankedCharacters;
        int count = ranked.Count;

        if (aliveCountText != null)
            aliveCountText.text = $"Alive: {count}";

        int rowsToCreate = count - rows.Count;
        if (rowsToCreate > 0)
        {
            for (int i = 0; i < rowsToCreate; i++)
            {
                LeaderboardRow row = Instantiate(rowPrefab, content);
                row.gameObject.SetActive(false);
                rows.Add(row);
            }
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (i < count)
            {
                if (!rows[i].gameObject.activeSelf)
                    rows[i].gameObject.SetActive(true);
                rows[i].SetData(ranked[i]);
            }
            else
            {
                if (rows[i].gameObject.activeSelf)
                    rows[i].gameObject.SetActive(false);
            }
        }
    }
}
