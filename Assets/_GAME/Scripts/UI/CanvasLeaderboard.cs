using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasLeaderboard : UICanvas
{
    [Header("Tabs")]
    [SerializeField] private Button matchTabBtn;
    [SerializeField] private Button weeklyTabBtn;
    [SerializeField] private Button monthlyTabBtn;

    [Header("Panels")]
    [SerializeField] private GameObject gameEndPanel;
    [SerializeField] private GameObject rankListPanel;

    [Header("List")]
    [SerializeField] private Transform          content;
    [SerializeField] private LeaderboardRankRow rowPrefab;

    [Header("States")]
    [SerializeField] private GameObject loadingObj;
    [SerializeField] private GameObject emptyObj;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private RankServerService rankService;
    private readonly List<LeaderboardRankRow> rows = new();

    private List<RankEntry> weeklyCache  = null;
    private List<RankEntry> monthlyCache = null;

    private Text matchTabText;
    private Text weeklyTabText;
    private Text monthlyTabText;

    private static readonly Color TabTextActive   = Color.white;
    private static readonly Color TabTextInactive = Color.gray;

    private enum Tab { Match, Weekly, Monthly }

    public System.Action onClosed;

    public void ClearCache()
    {
        weeklyCache  = null;
        monthlyCache = null;
    }

    private void Awake()
    {
        rankService = gameObject.AddComponent<RankServerService>();

        if (matchTabBtn  != null) { matchTabText  = matchTabBtn.GetComponentInChildren<Text>();  matchTabBtn.onClick.AddListener(ShowMatch); }
        if (weeklyTabBtn != null) { weeklyTabText  = weeklyTabBtn.GetComponentInChildren<Text>(); weeklyTabBtn.onClick.AddListener(ShowWeekly); }
        if (monthlyTabBtn!= null) { monthlyTabText = monthlyTabBtn.GetComponentInChildren<Text>();monthlyTabBtn.onClick.AddListener(ShowMonthly); }

        closeButton?.onClick.AddListener(Close);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        onClosed?.Invoke();
    }

    private void OnEnable()
    {
        if (matchTabBtn != null) matchTabBtn.gameObject.SetActive(true);
        if (gameEndPanel != null)
            ShowMatch();
        else
            ShowWeekly();
    }

    public void OpenForGamePlay()
    {
        if (matchTabBtn != null) matchTabBtn.gameObject.SetActive(false);
        ShowWeekly();
    }

    // ── Tabs ─────────────────────────────────────────────────────────────────

    public void ShowMatch()
    {
        if (gameEndPanel == null) return;
        SetTabVisual(Tab.Match);
        gameEndPanel.SetActive(true);
        rankListPanel?.SetActive(false);
    }

    public void ShowWeekly()
    {
        SetTabVisual(Tab.Weekly);
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (rankListPanel != null) rankListPanel.SetActive(true);
        if (weeklyCache != null) { OnLoaded(weeklyCache); return; }
        SetLoading(true);
        rankService.FetchWeekly(data => { weeklyCache = data; OnLoaded(data); }, OnError);
    }

    public void ShowMonthly()
    {
        SetTabVisual(Tab.Monthly);
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (rankListPanel != null) rankListPanel.SetActive(true);
        if (monthlyCache != null) { OnLoaded(monthlyCache); return; }
        SetLoading(true);
        rankService.FetchMonthly(data => { monthlyCache = data; OnLoaded(data); }, OnError);
    }

    // ── Populate ─────────────────────────────────────────────────────────────

    private void OnLoaded(List<RankEntry> data)
    {
        SetLoading(false);
        if (emptyObj != null) emptyObj.SetActive(data.Count == 0);

        while (rows.Count < data.Count)
            rows.Add(Instantiate(rowPrefab, content));

        for (int i = 0; i < rows.Count; i++)
        {
            bool active = i < data.Count;
            rows[i].gameObject.SetActive(active);
            if (active) rows[i].SetData(data[i]);
        }
    }

    private void OnError(string error)
    {
        SetLoading(false);
        if (emptyObj != null) emptyObj.SetActive(true);
        Debug.LogWarning($"[Leaderboard] {error}");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetLoading(bool loading)
    {
        if (loadingObj != null) loadingObj.SetActive(loading);
        if (content    != null) content.gameObject.SetActive(!loading);
    }

    private void SetTabVisual(Tab active)
    {
        if (matchTabText  != null) matchTabText.color  = active == Tab.Match    ? TabTextActive : TabTextInactive;
        if (weeklyTabText  != null) weeklyTabText.color  = active == Tab.Weekly ? TabTextActive : TabTextInactive;
        if (monthlyTabText != null) monthlyTabText.color = active == Tab.Monthly? TabTextActive : TabTextInactive;
    }
}
