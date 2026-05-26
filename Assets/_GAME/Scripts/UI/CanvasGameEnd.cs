using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CanvasGameEnd : UICanvas
{
    [Header("Top 1-2-3 (riêng)")]
    [SerializeField] private GameEndRow top1Row;
    [SerializeField] private GameEndRow top2Row;
    [SerializeField] private GameEndRow top3Row;

    [Header("Scroll List (rank 4 trở đi)")]
    [SerializeField] private Transform content;
    [SerializeField] private GameEndRow rowPrefab;

    private readonly List<GameEndRow> rows = new();

    private static readonly float[] RankBonusPercent = { 0.40f, 0.25f, 0.15f, 0.12f, 0.08f };

    private void Start()
    {
        ApplyRankBonus(GameEndData.TopCharacters);
        DisplayAllCharacters();

        RankServerService rankService = gameObject.AddComponent<RankServerService>();
        rankService.SubmitResults(GameEndData.TopCharacters);
    }

    private static void ApplyRankBonus(List<TopCharacterData> players)
    {
        int totalScore = GameEndData.TotalMatchScore;

        if (totalScore <= 0) return;

        for (int i = 0; i < players.Count && i < RankBonusPercent.Length; i++)
        {
            int bonus = Mathf.RoundToInt(totalScore * RankBonusPercent[i]);
            players[i].bonusScore = bonus;
            players[i].score     += bonus;
        }
    }

    private void DisplayAllCharacters()
    {
        List<TopCharacterData> all = GameEndData.TopCharacters;

        // Top 1-2-3
        SetTopSlot(top1Row, all, 0);
        SetTopSlot(top2Row, all, 1);
        SetTopSlot(top3Row, all, 2);

        // Rank 4 trở đi vào scroll list
        int scrollCount = all.Count > 3 ? all.Count - 3 : 0;

        while (rows.Count < scrollCount)
        {
            GameEndRow row = Instantiate(rowPrefab, content);
            rows.Add(row);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (i < scrollCount)
            {
                rows[i].gameObject.SetActive(true);
                rows[i].SetData(all[i + 3]);
            }
            else
            {
                rows[i].gameObject.SetActive(false);
            }
        }

        Debug.Log($"[CanvasGameEnd] Top3 + {scrollCount} in scroll list");
    }

    private void SetTopSlot(GameEndRow slot, List<TopCharacterData> all, int index)
    {
        if (slot == null) return;
        if (index < all.Count)
        {
            slot.gameObject.SetActive(true);
            slot.SetData(all[index]);
        }
        else
        {
            slot.gameObject.SetActive(false);
        }
    }

    public void LoadMainMenuScene()
    {
        Debug.Log("[CanvasGameEnd] Loading MainMenu scene...");
        Time.timeScale = 1f;
        GameEndData.Clear();
        SceneManager.LoadScene("MainMenu");
    }
}
