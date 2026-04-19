using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CharacterLeaderboardUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Content")]
    [SerializeField] private Transform content;
    [SerializeField] private LeaderboardRow rowPrefab;

    [Header("Header Info")]
    [SerializeField] private TextMeshProUGUI aliveCountText;

    [Header("Debug Panel")]
    [SerializeField] private CharacterDebugPanel debugPanel;

    private readonly List<LeaderboardRow> rows = new();
    private bool isVisible = true;

    private void OnEnable()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.OnRankUpdated += RefreshUI;

        // Subscribe to row click events
        LeaderboardRow.OnRowClicked += OnRowClicked;
    }

    private void OnDisable()
    {
        if (CharacterManager.Instance != null)
            CharacterManager.Instance.OnRankUpdated -= RefreshUI;

        // Unsubscribe from row click events
        LeaderboardRow.OnRowClicked -= OnRowClicked;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetVisible(!isVisible);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (panel != null) panel.SetActive(visible);
    }

    public void ToggleVisible() => SetVisible(!isVisible);

    private void OnRowClicked(CharacterBase character)
    {
        if (debugPanel != null)
        {
            debugPanel.Show(character);
        }
        else
        {
            Debug.LogWarning("[CharacterLeaderboardUI] Debug Panel chưa được gán!");
        }
    }

    private void RefreshUI()
    {
        if (!isVisible) return;

        var ranked = CharacterManager.Instance.RankedCharacters;
        int count = ranked.Count;

        if (aliveCountText != null)
            aliveCountText.text = $"Alive: {count}";

        // Tạo rows mới nếu cần (batch instantiate)
        int rowsToCreate = count - rows.Count;
        if (rowsToCreate > 0)
        {
            for (int i = 0; i < rowsToCreate; i++)
            {
                LeaderboardRow row = Instantiate(rowPrefab, content);
                row.gameObject.SetActive(false); // Tắt ngay để tránh layout recalc
                rows.Add(row);
            }
        }

        // Update rows (chỉ update khi cần)
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
