using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[System.Serializable]
public class IntroRankConfig
{
    public Sprite background;
    public Text   playerNameText;
    public Text   rankText;
    public Text   scoreText;
}

public class PlayerIntroManager : Singleton<PlayerIntroManager>
{
    [Header("Threshold")]
    [SerializeField] private int weeklyThreshold  = 20;
    [SerializeField] private int monthlyThreshold = 10;

    [Header("Video Clips (index 0 = rank #1)")]
    [SerializeField] private VideoClip[] weeklyClips;
    [SerializeField] private VideoClip[] monthlyClips;

    [Header("Rank Configs - Tuần (index 0 = rank #1)")]
    [SerializeField] private IntroRankConfig[] weeklyConfigs;

    [Header("Rank Configs - Tháng (index 0 = rank #1)")]
    [SerializeField] private IntroRankConfig[] monthlyConfigs;

    [Header("UI")]
    [SerializeField] private GameObject    introPanel;
    [SerializeField] private Image         bgImage;
    [SerializeField] private RawImage      videoDisplay;

    [Header("Info Panel")]
    [SerializeField] private RectTransform infoPanelRT;
    [SerializeField] private CanvasGroup   infoPanelGroup;

    [Header("Info Animation")]
    [SerializeField] private float animInDuration  = 0.5f;
    [SerializeField] private float animOutDuration = 0.3f;
    [SerializeField] private float slideOffset     = 300f;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private float       fallbackDuration = 3f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   introSound;

    private struct IntroData
    {
        public string          nickname;
        public string          label;
        public int             rank;
        public int             warPoints;
        public VideoClip       clip;
        public IntroRankConfig config;
    }

    private readonly Queue<IntroData> queue = new();
    private bool    isPlaying;
    private bool    videoEnded;
    private Vector2 infoPanelRestPos;

    protected override void Awake()
    {
        base.Awake();
        if (infoPanelRT != null) infoPanelRestPos = infoPanelRT.anchoredPosition;
        if (introPanel  != null) introPanel.SetActive(false);
        if (videoPlayer != null) videoPlayer.loopPointReached += _ => videoEnded = true;
    }

    public void TryQueueBestIntro(string nickname,
                                  int weeklyRank,  int weeklyPoints,
                                  int monthlyRank, int monthlyPoints)
    {
        bool weeklyOk  = weeklyRank  > 0 && weeklyRank  <= weeklyThreshold;
        bool monthlyOk = monthlyRank > 0 && monthlyRank <= monthlyThreshold;

        if (!weeklyOk && !monthlyOk) return;

        IntroData data;

        if (weeklyOk && monthlyOk)
        {
            if (weeklyRank < monthlyRank)
                data = Build(nickname, "Tuần",  weeklyRank,  weeklyPoints,
                             GetAt(weeklyClips, weeklyRank), GetAt(weeklyConfigs, weeklyRank));
            else
                data = Build(nickname, "Tháng", monthlyRank, monthlyPoints,
                             GetAt(monthlyClips, monthlyRank), GetAt(monthlyConfigs, monthlyRank));
        }
        else if (weeklyOk)
            data = Build(nickname, "Tuần",  weeklyRank,  weeklyPoints,
                         GetAt(weeklyClips, weeklyRank), GetAt(weeklyConfigs, weeklyRank));
        else
            data = Build(nickname, "Tháng", monthlyRank, monthlyPoints,
                         GetAt(monthlyClips, monthlyRank), GetAt(monthlyConfigs, monthlyRank));

        queue.Enqueue(data);
        if (!isPlaying) StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isPlaying = true;
        while (queue.Count > 0)
            yield return StartCoroutine(PlayIntro(queue.Dequeue()));
        isPlaying = false;
    }

    private void HideAllTexts()
    {
        HideConfigTexts(weeklyConfigs);
        HideConfigTexts(monthlyConfigs);
    }

    private static void HideConfigTexts(IntroRankConfig[] configs)
    {
        if (configs == null) return;
        foreach (var c in configs)
        {
            if (c == null) continue;
            if (c.playerNameText != null) c.playerNameText.gameObject.SetActive(false);
            if (c.rankText       != null) c.rankText.gameObject.SetActive(false);
            if (c.scoreText      != null) c.scoreText.gameObject.SetActive(false);
        }
    }

    private IEnumerator PlayIntro(IntroData data)
    {
        IntroRankConfig cfg = data.config;

        HideAllTexts();

        // Background
        if (bgImage != null)
        {
            bgImage.enabled = cfg != null && cfg.background != null;
            if (bgImage.enabled) bgImage.sprite = cfg.background;
        }

        // Texts từ config của rank đó
        Text nameText = cfg?.playerNameText;
        Text rnkText  = cfg?.rankText;
        Text scText   = cfg?.scoreText;

        if (nameText != null)
        {
            nameText.gameObject.SetActive(true);
            nameText.text = data.nickname;
        }
        if (rnkText != null)
        {
            bool showRank = data.label == "Tuần" && data.rank >= 6;
            rnkText.gameObject.SetActive(showRank);
            if (showRank) rnkText.text = $"TOP {data.rank}";
        }
        if (scText != null)
        {
            scText.gameObject.SetActive(true);
            scText.text = FormatScore(data.warPoints);
        }

        if (introPanel != null) introPanel.SetActive(true);

        // Sound
        if (audioSource != null && introSound != null)
            audioSource.PlayOneShot(introSound);

        // ── Animate info panel vào ────────────────────────────────────────────
        if (infoPanelRT != null && infoPanelGroup != null)
        {
            infoPanelRT.DOKill();
            infoPanelGroup.DOKill();

            infoPanelRT.anchoredPosition = infoPanelRestPos + Vector2.down * slideOffset;
            infoPanelGroup.alpha = 0f;

            infoPanelRT.DOAnchorPos(infoPanelRestPos, animInDuration).SetEase(Ease.OutBack);
            infoPanelGroup.DOFade(1f, animInDuration * 0.7f);

            yield return new WaitForSeconds(animInDuration);
        }

        // Video hoặc fallback
        if (videoPlayer != null && data.clip != null)
        {
            videoEnded       = false;
            videoPlayer.clip = data.clip;
            videoPlayer.Stop();
            videoPlayer.Play();
            yield return new WaitUntil(() => videoEnded);
        }
        else
        {
            yield return new WaitForSeconds(fallbackDuration);
        }

        // ── Animate info panel ra ─────────────────────────────────────────────
        if (infoPanelRT != null && infoPanelGroup != null)
        {
            infoPanelRT.DOAnchorPosY(infoPanelRestPos.y - slideOffset, animOutDuration).SetEase(Ease.InBack);
            infoPanelGroup.DOFade(0f, animOutDuration);
            yield return new WaitForSeconds(animOutDuration);
        }

        if (introPanel != null) introPanel.SetActive(false);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static T GetAt<T>(T[] arr, int rank) where T : class
    {
        if (arr == null || arr.Length == 0) return null;
        int idx = rank - 1;
        return idx >= 0 && idx < arr.Length ? arr[idx] : null;
    }

    private static IntroData Build(string nickname, string label, int rank, int warPoints,
                                   VideoClip clip, IntroRankConfig config)
        => new() { nickname = nickname, label = label, rank = rank,
                   warPoints = warPoints, clip = clip, config = config };

    private static string FormatScore(int value)
    {
        if (value >= 1_000_000) return $"{value / 1_000_000f:0.#}M";
        if (value >= 1_000)     return $"{value / 1_000f:0.#}k";
        return value.ToString();
    }
}
