using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class PlayerIntroTestUICreator
{
    [MenuItem("Tools/Create PlayerIntro Test UI")]
    public static void Create()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
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

        // ── Root panel ────────────────────────────────────────────────────────
        GameObject root = new GameObject("IntroTestUI");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0f, 0f);
        rootRT.anchorMax = new Vector2(0f, 0f);
        rootRT.pivot     = new Vector2(0f, 0f);
        rootRT.sizeDelta        = new Vector2(420, 420);
        rootRT.anchoredPosition = new Vector2(20, 20);

        Image rootBg = root.AddComponent<Image>();
        rootBg.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);

        // ── Panel (child, referenced by script) ───────────────────────────────
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        // ── Title ─────────────────────────────────────────────────────────────
        GameObject title = CreateLabel(panel.transform, "TitleText", "INTRO TEST", 28,
                                       new Vector2(0.5f, 1f), new Vector2(380, 50), new Vector2(0, -30));

        // ── Input fields ──────────────────────────────────────────────────────
        float startY = -90f;
        float step   = -62f;

        TMP_InputField nicknameInput    = CreateInputField(panel.transform, "Nickname",     "Nickname",     startY + step * 0);
        TMP_InputField weeklyRankInput  = CreateInputField(panel.transform, "Weekly Rank",  "Top Tuần",     startY + step * 1);
        TMP_InputField weeklyPtsInput   = CreateInputField(panel.transform, "Weekly Pts",   "Điểm Tuần",    startY + step * 2);
        TMP_InputField monthlyRankInput = CreateInputField(panel.transform, "Monthly Rank", "Top Tháng",    startY + step * 3);
        TMP_InputField monthlyPtsInput  = CreateInputField(panel.transform, "Monthly Pts",  "Điểm Tháng",   startY + step * 4);

        weeklyRankInput.contentType  = TMP_InputField.ContentType.IntegerNumber;
        weeklyPtsInput.contentType   = TMP_InputField.ContentType.IntegerNumber;
        monthlyRankInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        monthlyPtsInput.contentType  = TMP_InputField.ContentType.IntegerNumber;

        // ── Test button ───────────────────────────────────────────────────────
        GameObject btnObj = new GameObject("TestButton");
        btnObj.transform.SetParent(panel.transform, false);
        RectTransform btnRT = btnObj.AddComponent<RectTransform>();
        btnRT.anchorMin        = new Vector2(0.5f, 1f);
        btnRT.anchorMax        = new Vector2(0.5f, 1f);
        btnRT.pivot            = new Vector2(0.5f, 1f);
        btnRT.sizeDelta        = new Vector2(300, 55);
        btnRT.anchoredPosition = new Vector2(0, startY + step * 5 - 10);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.7f, 0.3f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.85f, 0.4f, 1f);
        cb.pressedColor     = new Color(0.15f, 0.55f, 0.25f, 1f);
        btn.colors = cb;

        GameObject btnLabel = new GameObject("Text");
        btnLabel.transform.SetParent(btnObj.transform, false);
        RectTransform btnLabelRT = btnLabel.AddComponent<RectTransform>();
        btnLabelRT.anchorMin = Vector2.zero;
        btnLabelRT.anchorMax = Vector2.one;
        btnLabelRT.offsetMin = btnLabelRT.offsetMax = Vector2.zero;
        TextMeshProUGUI btnTxt = btnLabel.AddComponent<TextMeshProUGUI>();
        btnTxt.text      = "TEST INTRO";
        btnTxt.fontSize  = 22;
        btnTxt.color     = Color.white;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;

        // ── PlayerIntroTestUI component ───────────────────────────────────────
        PlayerIntroTestUI testUI = root.AddComponent<PlayerIntroTestUI>();
        SerializedObject so = new SerializedObject(testUI);
        so.FindProperty("panel").objectReferenceValue           = panel;
        so.FindProperty("nicknameInput").objectReferenceValue   = nicknameInput;
        so.FindProperty("weeklyRankInput").objectReferenceValue = weeklyRankInput;
        so.FindProperty("weeklyPtsInput").objectReferenceValue  = weeklyPtsInput;
        so.FindProperty("monthlyRankInput").objectReferenceValue= monthlyRankInput;
        so.FindProperty("monthlyPtsInput").objectReferenceValue = monthlyPtsInput;
        so.FindProperty("testButton").objectReferenceValue      = btn;
        so.ApplyModifiedProperties();

        root.SetActive(true);
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);

        Debug.Log("[PlayerIntroTestUICreator] Done! Nhấn F9 lúc runtime để bật/tắt panel.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject CreateLabel(Transform parent, string name, string text, int fontSize,
                                          Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        return obj;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, float posY)
    {
        // Row container
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0.5f, 1f);
        rowRT.anchorMax        = new Vector2(0.5f, 1f);
        rowRT.pivot            = new Vector2(0.5f, 1f);
        rowRT.sizeDelta        = new Vector2(380, 52);
        rowRT.anchoredPosition = new Vector2(0, posY);

        // Background
        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Input field
        TMP_InputField input = row.AddComponent<TMP_InputField>();

        // Text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(row.transform, false);
        RectTransform taRT = textArea.AddComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(10, 4);
        taRT.offsetMax = new Vector2(-10, -4);
        textArea.AddComponent<RectMask2D>();

        // Placeholder
        GameObject phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(textArea.transform, false);
        RectTransform phRT = phObj.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = phRT.offsetMax = Vector2.zero;
        TextMeshProUGUI ph = phObj.AddComponent<TextMeshProUGUI>();
        ph.text      = placeholder;
        ph.fontSize  = 18;
        ph.color     = new Color(0.5f, 0.5f, 0.5f, 1f);
        ph.alignment = TextAlignmentOptions.MidlineLeft;

        // Input text
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(textArea.transform, false);
        RectTransform txtRT = txtObj.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.fontSize  = 18;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.MidlineLeft;

        input.textViewport   = taRT;
        input.textComponent  = txt;
        input.placeholder    = ph;

        return input;
    }
}
