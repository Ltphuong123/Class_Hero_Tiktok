using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class GameEndUICreator : EditorWindow
{
    [MenuItem("Tools/Create GameEnd UI")]
    public static void CreateGameEndUI()
    {
        // Tìm hoặc tạo Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Root panel
        GameObject root = CreatePanel(canvas.transform, "CanvasGameEnd", new Color(0.08f, 0.08f, 0.12f, 1f));
        StretchFull(root);

        // ---- Title ----
        GameObject title = CreateTMP(root.transform, "TitleText", "KẾT QUẢ", 60, Color.white, TextAlignmentOptions.Center);
        SetRect(title, 0f, 1f, 1f, 1f, 0, -80, 0, -20);

        // ---- Top 3 area ----
        GameObject top3Area = new GameObject("Top3Area");
        top3Area.transform.SetParent(root.transform, false);
        RectTransform top3Rect = top3Area.AddComponent<RectTransform>();
        top3Rect.anchorMin = new Vector2(0f, 1f);
        top3Rect.anchorMax = new Vector2(1f, 1f);
        top3Rect.pivot     = new Vector2(0.5f, 1f);
        top3Rect.anchoredPosition = new Vector2(0, -160);
        top3Rect.sizeDelta = new Vector2(-40, 480);

        HorizontalLayoutGroup top3Layout = top3Area.AddComponent<HorizontalLayoutGroup>();
        top3Layout.spacing = 20;
        top3Layout.childControlWidth  = true;
        top3Layout.childControlHeight = true;
        top3Layout.childForceExpandWidth  = true;
        top3Layout.childForceExpandHeight = true;
        top3Layout.padding = new RectOffset(0, 0, 0, 0);

        // Top 1 (vàng, lớn nhất — ở giữa)
        GameObject top1Panel = CreateTopSlot(top3Area.transform, "Top1Row",
            new Color(0.80f, 0.65f, 0.10f, 1f), new Color(1.0f, 0.84f, 0.0f, 1f), 40);

        // Top 2 (bạc — bên trái)
        GameObject top2Panel = CreateTopSlot(top3Area.transform, "Top2Row",
            new Color(0.45f, 0.45f, 0.50f, 1f), new Color(0.80f, 0.80f, 0.85f, 1f), 34);

        // Top 3 (đồng — bên phải)
        GameObject top3Panel = CreateTopSlot(top3Area.transform, "Top3Row",
            new Color(0.50f, 0.28f, 0.10f, 1f), new Color(0.85f, 0.55f, 0.25f, 1f), 34);

        // Sắp xếp: 2 - 1 - 3
        top2Panel.transform.SetSiblingIndex(0);
        top1Panel.transform.SetSiblingIndex(1);
        top3Panel.transform.SetSiblingIndex(2);

        // ---- Divider ----
        GameObject divider = CreatePanel(root.transform, "Divider", new Color(1f, 1f, 1f, 0.15f));
        SetRect(divider, 0f, 1f, 1f, 1f, 20, -660, -20, -664);

        // ---- Scroll List (rank 4+) ----
        GameObject scrollRoot = CreateScrollView(root.transform, "ScrollList");
        RectTransform scrollRect = scrollRoot.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0.1f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(20, 0);
        scrollRect.offsetMax = new Vector2(-20, -670);
        Transform scrollContent = scrollRoot.transform.Find("Viewport/Content");

        // ---- Back Button ----
        GameObject backBtn = CreateButton(root.transform, "BackButton", "QUAY LẠI", new Color(0.2f, 0.5f, 0.9f));
        SetRect(backBtn, 0.5f, 0f, 0.5f, 0f, -150, 40, 150, 100);

        // ---- Gán CanvasGameEnd component ----
        CanvasGameEnd gameEndScript = root.AddComponent<CanvasGameEnd>();
        SerializedObject so = new SerializedObject(gameEndScript);
        so.FindProperty("top1Row").objectReferenceValue  = top1Panel.GetComponent<GameEndRow>();
        so.FindProperty("top2Row").objectReferenceValue  = top2Panel.GetComponent<GameEndRow>();
        so.FindProperty("top3Row").objectReferenceValue  = top3Panel.GetComponent<GameEndRow>();
        so.FindProperty("content").objectReferenceValue  = scrollContent;
        so.ApplyModifiedProperties();

        // Gán nút back
        Button backBtnComp = backBtn.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            backBtnComp.onClick,
            gameEndScript.LoadMainMenuScene);

        // ---- Tạo GameEndRow prefab cho scroll ----
        GameObject rowPrefabObj = CreateScrollRowPrefab();
        string prefabDir = "Assets/_GAME/Prefabs/UI";
        System.IO.Directory.CreateDirectory(prefabDir);
        string prefabPath = $"{prefabDir}/GameEndRow.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rowPrefabObj, prefabPath);
        DestroyImmediate(rowPrefabObj);

        // Gán rowPrefab
        so = new SerializedObject(gameEndScript);
        so.FindProperty("rowPrefab").objectReferenceValue = savedPrefab.GetComponent<GameEndRow>();
        so.ApplyModifiedProperties();

        Selection.activeGameObject = root;
        Debug.Log("[GameEndUICreator] CanvasGameEnd UI created successfully!");
    }

    // -----------------------------------------------------------------------
    // Top slot (rank 1/2/3) — panel lớn với Avatar, Rank, Name, Kills, Score
    // -----------------------------------------------------------------------
    private static GameObject CreateTopSlot(Transform parent, string name, Color bgColor, Color accentColor, int nameFontSize)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();

        Image bg = panel.AddComponent<Image>();
        bg.color = bgColor;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(10, 10, 16, 16);
        layout.childControlWidth  = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        // Rank badge
        GameObject rankObj = CreateTMP(panel.transform, "RankText", "#1", 48, accentColor, TextAlignmentOptions.Center);
        SetLayoutHeight(rankObj, 56);

        // Avatar
        GameObject avatarObj = new GameObject("AvatarImage");
        avatarObj.transform.SetParent(panel.transform, false);
        RectTransform avatarRect = avatarObj.AddComponent<RectTransform>();
        avatarRect.sizeDelta = new Vector2(0, 120);
        Image avatarImg = avatarObj.AddComponent<Image>();
        avatarImg.color = new Color(1f, 1f, 1f, 0.3f);
        avatarImg.preserveAspect = true;
        LayoutElement avatarLE = avatarObj.AddComponent<LayoutElement>();
        avatarLE.preferredHeight = 120;

        // Name
        GameObject nameObj = CreateTMP(panel.transform, "NameText", "Player", nameFontSize, Color.white, TextAlignmentOptions.Center);
        nameObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        SetLayoutHeight(nameObj, nameFontSize + 10);

        // Separator
        GameObject sep = CreatePanel(panel.transform, "Sep", new Color(1f, 1f, 1f, 0.2f));
        SetLayoutHeight(sep, 2);

        // Kills row
        GameObject killsRow = CreateStatRow(panel.transform, "KillsRow", "Kills:", accentColor, out TextMeshProUGUI killsTMP);
        SetLayoutHeight(killsRow, 36);

        // Score row
        GameObject scoreRow = CreateStatRow(panel.transform, "ScoreRow", "Score:", accentColor, out TextMeshProUGUI scoreTMP);
        SetLayoutHeight(scoreRow, 36);

        // Gán GameEndRow
        GameEndRow rowScript = panel.AddComponent<GameEndRow>();
        SerializedObject so = new SerializedObject(rowScript);
        so.FindProperty("rankText").objectReferenceValue      = rankObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("avatarImage").objectReferenceValue   = avatarImg;
        so.FindProperty("killPointsText").objectReferenceValue = killsTMP;
        so.FindProperty("scoreText").objectReferenceValue     = scoreTMP;
        // nameText là Text legacy — tạo thêm TMP và dùng field nameTextTMP
        so.FindProperty("nameTMPText").objectReferenceValue   = nameObj.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();

        return panel;
    }

    // Scroll row nhỏ (rank 4+)
    private static GameObject CreateScrollRowPrefab()
    {
        GameObject row = new GameObject("GameEndRow");
        RectTransform rect = row.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 70);

        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.20f, 1f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.padding = new RectOffset(16, 16, 8, 8);
        layout.childControlHeight = true;
        layout.childControlWidth  = false;
        layout.childForceExpandHeight = true;

        // Rank
        GameObject rankObj = CreateTMP(row.transform, "RankText", "#4", 26, new Color(1f, 0.84f, 0f), TextAlignmentOptions.Center);
        SetSize(rankObj, 60, 0);

        // Avatar
        GameObject avatarObj = new GameObject("AvatarImage");
        avatarObj.transform.SetParent(row.transform, false);
        Image avatarImg = avatarObj.AddComponent<Image>();
        avatarImg.color = new Color(1f, 1f, 1f, 0.4f);
        avatarImg.preserveAspect = true;
        SetSize(avatarObj, 54, 0);

        // Name
        GameObject nameObj = CreateTMP(row.transform, "NameTMPText", "Player", 22, Color.white, TextAlignmentOptions.MidlineLeft);
        SetSize(nameObj, 300, 0);

        // Kills
        GameObject killsLabel = CreateTMP(row.transform, "KillsLabel", "Kills:", 18, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.MidlineRight);
        SetSize(killsLabel, 60, 0);
        GameObject killsObj = CreateTMP(row.transform, "KillPointsText", "0", 22, Color.white, TextAlignmentOptions.MidlineLeft);
        SetSize(killsObj, 70, 0);

        // Score
        GameObject scoreLabel = CreateTMP(row.transform, "ScoreLabel", "Score:", 18, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.MidlineRight);
        SetSize(scoreLabel, 70, 0);
        GameObject scoreObj = CreateTMP(row.transform, "ScoreText", "0", 22, new Color(0.4f, 1f, 0.6f), TextAlignmentOptions.MidlineLeft);
        SetSize(scoreObj, 90, 0);

        GameEndRow rowScript = row.AddComponent<GameEndRow>();
        SerializedObject so = new SerializedObject(rowScript);
        so.FindProperty("rankText").objectReferenceValue       = rankObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("avatarImage").objectReferenceValue    = avatarImg;
        so.FindProperty("nameTMPText").objectReferenceValue    = nameObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("killPointsText").objectReferenceValue = killsObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("scoreText").objectReferenceValue      = scoreObj.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();

        return row;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    private static GameObject CreateTMP(Transform parent, string name, string text, int fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = align;
        return obj;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        Image img = obj.AddComponent<Image>();
        img.color = color;
        obj.AddComponent<Button>();

        GameObject textObj = CreateTMP(obj.transform, "Text", label, 28, Color.white, TextAlignmentOptions.Center);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        return obj;
    }

    private static GameObject CreateScrollView(Transform parent, string name)
    {
        GameObject sv = new GameObject(name);
        sv.transform.SetParent(parent, false);
        sv.AddComponent<RectTransform>();
        Image svBg = sv.AddComponent<Image>();
        svBg.color = new Color(0f, 0f, 0f, 0.2f);
        ScrollRect scroll = sv.AddComponent<ScrollRect>();

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(sv.transform, false);
        RectTransform vr = viewport.AddComponent<RectTransform>();
        vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one;
        vr.offsetMin = vr.offsetMax = Vector2.zero;
        Image vpImg = viewport.AddComponent<Image>(); vpImg.color = Color.white;
        Mask mask = viewport.AddComponent<Mask>(); mask.showMaskGraphic = false;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform cr = content.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0f, 1f); cr.anchorMax = new Vector2(1f, 1f);
        cr.pivot     = new Vector2(0.5f, 1f);
        cr.sizeDelta = new Vector2(0, 0);
        cr.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vLayout = content.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 6;
        vLayout.padding = new RectOffset(0, 0, 6, 6);
        vLayout.childControlWidth  = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content   = cr;
        scroll.viewport  = vr;
        scroll.horizontal = false;
        scroll.vertical   = true;

        return sv;
    }

    private static GameObject CreateStatRow(Transform parent, string rowName, string labelStr, Color valueColor, out TextMeshProUGUI valueTMP)
    {
        GameObject row = new GameObject(rowName);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        HorizontalLayoutGroup hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.childControlWidth = true; hl.childControlHeight = true;
        hl.childForceExpandWidth = true;

        GameObject lbl = CreateTMP(row.transform, "Label", labelStr, 20, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.MidlineLeft);
        LayoutElement lblLE = lbl.AddComponent<LayoutElement>(); lblLE.flexibleWidth = 1;

        GameObject val = CreateTMP(row.transform, "Value", "0", 22, valueColor, TextAlignmentOptions.MidlineRight);
        LayoutElement valLE = val.AddComponent<LayoutElement>(); valLE.flexibleWidth = 1;
        valueTMP = val.GetComponent<TextMeshProUGUI>();

        return row;
    }

    // RectTransform helpers
    private static void StretchFull(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void SetRect(GameObject obj, float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY,
                                 float left, float top, float right, float bottom)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rt.offsetMin = new Vector2(left,  bottom);
        rt.offsetMax = new Vector2(right, top);
    }

    private static void SetSize(GameObject obj, float w, float h)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void SetLayoutHeight(GameObject obj, float h)
    {
        LayoutElement le = obj.GetComponent<LayoutElement>();
        if (le == null) le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = h;
    }
}
