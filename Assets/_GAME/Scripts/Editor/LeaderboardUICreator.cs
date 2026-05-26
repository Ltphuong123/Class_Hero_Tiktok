using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class LeaderboardUICreator : EditorWindow
{
    [MenuItem("Tools/Create Leaderboard UI")]
    public static void CreateLeaderboardUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject cObj = new GameObject("Canvas");
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = cObj.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cObj.AddComponent<GraphicRaycaster>();
        }

        // Root
        GameObject root = CreatePanel(canvas.transform, "CanvasLeaderboard", new Color(0.08f, 0.08f, 0.12f, 1f));
        StretchFull(root);

        // Title
        GameObject title = CreateText(root.transform, "TitleText", "BXH", 52, Color.white);
        SetAnchored(title, 0f, 1f, 1f, 1f, 0, -70, 0, -16);

        // ── Tab bar ──────────────────────────────────────────────────────────
        GameObject tabBar = new GameObject("TabBar");
        tabBar.transform.SetParent(root.transform, false);
        RectTransform tabBarRT = tabBar.AddComponent<RectTransform>();
        tabBarRT.anchorMin = new Vector2(0f, 1f);
        tabBarRT.anchorMax = new Vector2(1f, 1f);
        tabBarRT.pivot     = new Vector2(0.5f, 1f);
        tabBarRT.anchoredPosition = new Vector2(0, -90);
        tabBarRT.sizeDelta = new Vector2(-40, 70);
        HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 16;
        tabLayout.childControlWidth  = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth  = true;
        tabLayout.childForceExpandHeight = true;

        // Match tab (active by default)
        GameObject matchTab = CreateButtonWithBg(tabBar.transform, "MatchTab", "TRẬN NÀY",
            new Color(0.20f, 0.50f, 0.90f, 1f), 26, out Image matchBg);

        // Weekly tab
        GameObject weeklyTab = CreateButtonWithBg(tabBar.transform, "WeeklyTab", "TUẦN NÀY",
            new Color(0.18f, 0.18f, 0.22f, 1f), 26, out Image weeklyBg);

        // Monthly tab
        GameObject monthlyTab = CreateButtonWithBg(tabBar.transform, "MonthlyTab", "THÁNG NÀY",
            new Color(0.18f, 0.18f, 0.22f, 1f), 26, out Image monthlyBg);

        // ── Scroll List ───────────────────────────────────────────────────────
        GameObject scrollRoot = CreateScrollView(root.transform, "ScrollList");
        RectTransform scrollRT = scrollRoot.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0f, 0.08f);
        scrollRT.anchorMax = new Vector2(1f, 1f);
        scrollRT.offsetMin = new Vector2(20, 0);
        scrollRT.offsetMax = new Vector2(-20, -175);
        Transform scrollContent = scrollRoot.transform.Find("Viewport/Content");

        // Loading text
        GameObject loadingObj = CreateText(root.transform, "LoadingText", "Đang tải...", 32, new Color(0.6f, 0.6f, 0.6f));
        RectTransform loadRT = loadingObj.GetComponent<RectTransform>();
        loadRT.anchorMin = new Vector2(0.5f, 0.5f);
        loadRT.anchorMax = new Vector2(0.5f, 0.5f);
        loadRT.sizeDelta = new Vector2(400, 60);
        loadRT.anchoredPosition = Vector2.zero;
        loadingObj.SetActive(false);

        // Empty text
        GameObject emptyObj = CreateText(root.transform, "EmptyText", "Chưa có dữ liệu", 30, new Color(0.5f, 0.5f, 0.5f));
        RectTransform emptyRT = emptyObj.GetComponent<RectTransform>();
        emptyRT.anchorMin = new Vector2(0.5f, 0.5f);
        emptyRT.anchorMax = new Vector2(0.5f, 0.5f);
        emptyRT.sizeDelta = new Vector2(500, 60);
        emptyRT.anchoredPosition = Vector2.zero;
        emptyObj.SetActive(false);

        // ── Tạo LeaderboardRankRow prefab ─────────────────────────────────────
        GameObject rowObj = CreateRowPrefabObj();
        string prefabDir  = "Assets/_GAME/Prefabs/UI";
        System.IO.Directory.CreateDirectory(prefabDir);
        string prefabPath = $"{prefabDir}/LeaderboardRankRow.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rowObj, prefabPath);
        DestroyImmediate(rowObj);

        // ── Wire CanvasLeaderboard ─────────────────────────────────────────────
        CanvasLeaderboard script = root.AddComponent<CanvasLeaderboard>();
        SerializedObject so = new SerializedObject(script);
        so.FindProperty("matchTabBtn").objectReferenceValue   = matchTab.GetComponent<Button>();
        so.FindProperty("weeklyTabBtn").objectReferenceValue  = weeklyTab.GetComponent<Button>();
        so.FindProperty("monthlyTabBtn").objectReferenceValue = monthlyTab.GetComponent<Button>();
        so.FindProperty("matchTabBg").objectReferenceValue    = matchBg;
        so.FindProperty("weeklyTabBg").objectReferenceValue   = weeklyBg;
        so.FindProperty("monthlyTabBg").objectReferenceValue  = monthlyBg;
        so.FindProperty("content").objectReferenceValue       = scrollContent;
        so.FindProperty("rowPrefab").objectReferenceValue     = savedPrefab.GetComponent<LeaderboardRankRow>();
        so.FindProperty("loadingObj").objectReferenceValue    = loadingObj;
        so.FindProperty("emptyObj").objectReferenceValue      = emptyObj;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = root;
        Debug.Log("[LeaderboardUICreator] CanvasLeaderboard created!");
    }

    // ── Row prefab ───────────────────────────────────────────────────────────
    private static GameObject CreateRowPrefabObj()
    {
        GameObject row = new GameObject("LeaderboardRankRow");
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80);

        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.20f, 1f);

        HorizontalLayoutGroup hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 14;
        hl.padding = new RectOffset(16, 16, 10, 10);
        hl.childControlHeight = true;
        hl.childControlWidth  = false;
        hl.childForceExpandHeight = true;

        // Rank
        GameObject rankObj = CreateText(row.transform, "RankText", "#1", 28, new Color(1f, 0.84f, 0f));
        SetSize(rankObj, 70, 0);

        // Avatar
        GameObject avatarObj = new GameObject("AvatarImage");
        avatarObj.transform.SetParent(row.transform, false);
        Image avatarImg = avatarObj.AddComponent<Image>();
        avatarImg.color = new Color(1f, 1f, 1f, 0.4f);
        avatarImg.preserveAspect = true;
        SetSize(avatarObj, 60, 0);

        // Nickname
        GameObject nameObj = CreateText(row.transform, "NicknameText", "Player", 24, Color.white);
        SetSize(nameObj, 320, 0);

        // Kills label + value
        GameObject killLbl = CreateText(row.transform, "KillsLabel", "Kills:", 18, new Color(0.6f, 0.6f, 0.6f));
        SetSize(killLbl, 55, 0);
        GameObject killObj = CreateText(row.transform, "KillsText", "0", 22, Color.white);
        SetSize(killObj, 60, 0);

        // Score label + value
        GameObject scoreLbl = CreateText(row.transform, "ScoreLabel", "Score:", 18, new Color(0.6f, 0.6f, 0.6f));
        SetSize(scoreLbl, 65, 0);
        GameObject scoreObj = CreateText(row.transform, "ScoreText", "0", 22, new Color(0.4f, 1f, 0.6f));
        SetSize(scoreObj, 100, 0);

        // Wire LeaderboardRankRow
        LeaderboardRankRow rowScript = row.AddComponent<LeaderboardRankRow>();
        SerializedObject so = new SerializedObject(rowScript);
        so.FindProperty("rankText").objectReferenceValue     = rankObj.GetComponent<Text>();
        so.FindProperty("avatarImage").objectReferenceValue  = avatarImg;
        so.FindProperty("nicknameText").objectReferenceValue = nameObj.GetComponent<Text>();
        so.FindProperty("scoreText").objectReferenceValue    = scoreObj.GetComponent<Text>();
        so.FindProperty("killsText").objectReferenceValue    = killObj.GetComponent<Text>();
        so.ApplyModifiedProperties();

        return row;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        obj.AddComponent<Image>().color = color;
        return obj;
    }

    private static GameObject CreateText(Transform parent, string name, string text, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        Text t = obj.AddComponent<Text>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return obj;
    }

    private static GameObject CreateButtonWithBg(Transform parent, string name, string label,
        Color bgColor, int fontSize, out Image bgImage)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        bgImage = obj.AddComponent<Image>();
        bgImage.color = bgColor;
        obj.AddComponent<Button>();

        GameObject textObj = CreateText(obj.transform, "Text", label, fontSize, Color.white);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        return obj;
    }

    private static GameObject CreateScrollView(Transform parent, string name)
    {
        GameObject sv = new GameObject(name);
        sv.transform.SetParent(parent, false);
        sv.AddComponent<RectTransform>();
        sv.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        ScrollRect scroll = sv.AddComponent<ScrollRect>();

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(sv.transform, false);
        RectTransform vr = viewport.AddComponent<RectTransform>();
        vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one;
        vr.offsetMin = vr.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = Color.white;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewport.transform, false);
        RectTransform cr = contentObj.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0f, 1f); cr.anchorMax = new Vector2(1f, 1f);
        cr.pivot = new Vector2(0.5f, 1f);
        cr.sizeDelta = cr.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(0, 0, 6, 6);
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = cr; scroll.viewport = vr;
        scroll.horizontal = false; scroll.vertical = true;
        return sv;
    }

    private static void StretchFull(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void SetAnchored(GameObject obj,
        float axMin, float ayMin, float axMax, float ayMax,
        float left, float top, float right, float bottom)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(axMin, ayMin); rt.anchorMax = new Vector2(axMax, ayMax);
        rt.offsetMin = new Vector2(left, bottom);  rt.offsetMax = new Vector2(right, top);
    }

    private static void SetSize(GameObject obj, float w, float h)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
    }
}
