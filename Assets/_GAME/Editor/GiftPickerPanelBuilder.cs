#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tạo prefab GiftPickerPanel và GiftPickerItem.
/// Menu: Game → UI → Create Gift Picker Prefabs
/// </summary>
public static class GiftPickerPanelBuilder
{
    private const string PrefabDir  = "Assets/_GAME/Prefabs/UI";
    private const string PanelPath  = PrefabDir + "/GiftPickerPanel.prefab";
    private const string ItemPath   = PrefabDir + "/GiftPickerItem.prefab";

    [MenuItem("Game/UI/Create Gift Picker Prefabs")]
    public static void Build()
    {
        EnsureFolder(PrefabDir);

        // 1. GiftPickerItem (cần tạo trước để panel reference được)
        var itemGO   = BuildItem();
        var itemAsset = PrefabUtility.SaveAsPrefabAsset(itemGO, ItemPath);
        Object.DestroyImmediate(itemGO);

        // 2. GiftPickerPanel
        var panelGO  = BuildPanel(itemAsset);
        PrefabUtility.SaveAsPrefabAsset(panelGO, PanelPath);
        Object.DestroyImmediate(panelGO);

        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
        Debug.Log($"[GiftPickerBuilder] Prefabs saved to {PrefabDir}");
    }

    // ─────────────────────────────────────────────────────────────
    //  PANEL
    // ─────────────────────────────────────────────────────────────
    private static GameObject BuildPanel(GameObject itemPrefab)
    {
        // Root — full-screen overlay
        var root     = MakeGO("GiftPickerPanel");
        var rootRect = root.AddComponent<RectTransform>();
        Stretch(rootRect);

        // Semi-transparent background (click-to-close area)
        var overlay = MakeImage(root, "Overlay", new Color(0f, 0f, 0f, 0.65f));
        Stretch(overlay.rectTransform);

        // Card container  750 × 580
        var card = MakeImage(root, "Card", new Color(0.12f, 0.13f, 0.17f, 0.98f));
        SetAnchored(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(750, 580));

        var cardVLG = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardVLG.padding   = new RectOffset(14, 14, 14, 14);
        cardVLG.spacing   = 8;
        cardVLG.childControlWidth = cardVLG.childForceExpandWidth = true;
        cardVLG.childControlHeight = cardVLG.childForceExpandHeight = false;

        // ── Title bar ──────────────────────────────────────────
        var titleBar = MakeGO("TitleBar", card.transform);
        titleBar.AddComponent<RectTransform>();
        ForceHeight(titleBar, 46);
        var titleHLG = titleBar.AddComponent<HorizontalLayoutGroup>();
        titleHLG.childAlignment     = TextAnchor.MiddleLeft;
        titleHLG.spacing            = 8;
        titleHLG.childControlHeight = true;
        titleHLG.childForceExpandWidth  = false;
        titleHLG.childForceExpandHeight = true;

        // Title text
        var titleTxt = MakeTMP(titleBar, "TitleText", "🎁  Chọn quà TikTok", 19, FontStyles.Bold);
        Flex(titleTxt.gameObject);

        // Close button
        var closeBtn = MakeButton(titleBar, "CloseButton", "✕",
                                  new Color(0.65f, 0.18f, 0.18f));
        FixSize(closeBtn.gameObject, 40, 40);
        closeBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18;

        // ── Search bar ─────────────────────────────────────────
        var searchGO = BuildSearchField(card.transform);
        ForceHeight(searchGO, 40);

        // ── Scroll view ────────────────────────────────────────
        var scrollGO = BuildScrollView(card.transform);
        var scrollLE = scrollGO.AddComponent<LayoutElement>();
        scrollLE.flexibleHeight = 1;
        scrollLE.minHeight      = 60;

        // ── Wire GiftPickerPanel component ─────────────────────
        var comp = root.AddComponent<GiftPickerPanel>();
        var so   = new SerializedObject(comp);
        so.FindProperty("searchInput").objectReferenceValue =
            searchGO.GetComponentInChildren<TMP_InputField>();
        so.FindProperty("listContent").objectReferenceValue =
            scrollGO.transform.Find("Viewport/Content");
        so.FindProperty("itemPrefab").objectReferenceValue = itemPrefab;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.ApplyModifiedProperties();

        return root;
    }

    // ─────────────────────────────────────────────────────────────
    //  ITEM
    // ─────────────────────────────────────────────────────────────
    private static GameObject BuildItem()
    {
        var root     = MakeImage(null, "GiftPickerItem", new Color(0.17f, 0.18f, 0.24f));
        root.rectTransform.sizeDelta = new Vector2(700, 72);

        var hlg = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding          = new RectOffset(10, 10, 8, 8);
        hlg.spacing          = 12;
        hlg.childAlignment   = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Icon  56×56
        var iconGO  = MakeGO("Icon", root.transform);
        iconGO.AddComponent<RectTransform>().sizeDelta = new Vector2(56, 56);
        FixSize(iconGO, 56, 56);
        var rawImg  = iconGO.AddComponent<RawImage>();
        rawImg.color = new Color(0.35f, 0.35f, 0.4f);

        // Info group  (flexible)
        var info    = MakeGO("Info", root.transform);
        var infoRect = info.AddComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(0, 56);
        var infoLE  = info.AddComponent<LayoutElement>();
        infoLE.flexibleWidth  = 1;
        infoLE.preferredWidth = 400;
        var infoVLG = info.AddComponent<VerticalLayoutGroup>();
        infoVLG.childAlignment    = TextAnchor.MiddleLeft;
        infoVLG.childControlHeight = false;
        infoVLG.childForceExpandHeight = false;
        infoVLG.spacing = 2;

        var nameTxt   = MakeTMP(info, "NameText",   "Gift Name",       15, FontStyles.Bold);
        ForceHeight(nameTxt.gameObject, 24);

        var detailTxt = MakeTMP(info, "DetailText", "ID: 0  |  0 💎",  12, FontStyles.Normal);
        detailTxt.color = new Color(0.68f, 0.68f, 0.68f);
        ForceHeight(detailTxt.gameObject, 20);

        // Transparent click button (covers whole item)
        var btnGO   = MakeGO("SelectButton", root.transform);
        var btnRect = btnGO.AddComponent<RectTransform>();
        Stretch(btnRect);
        btnRect.SetAsLastSibling();
        var btnImg  = btnGO.AddComponent<Image>();
        btnImg.color = Color.clear;
        var btn     = btnGO.AddComponent<Button>();
        var cb      = btn.colors;
        cb.normalColor      = Color.clear;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        cb.pressedColor     = new Color(1f, 1f, 1f, 0.18f);
        btn.colors = cb;

        // ── Wire GiftPickerItem component ──────────────────────
        var comp = root.gameObject.AddComponent<GiftPickerItem>();
        var so   = new SerializedObject(comp);
        so.FindProperty("icon").objectReferenceValue        = rawImg;
        so.FindProperty("nameText").objectReferenceValue    = nameTxt;
        so.FindProperty("detailText").objectReferenceValue  = detailTxt;
        so.FindProperty("selectButton").objectReferenceValue = btn;
        so.ApplyModifiedProperties();

        return root.gameObject;
    }

    // ─────────────────────────────────────────────────────────────
    //  SUB-BUILDERS
    // ─────────────────────────────────────────────────────────────
    private static GameObject BuildSearchField(Transform parent)
    {
        var bg     = MakeImage(parent.gameObject, "SearchBar", new Color(0.08f, 0.09f, 0.12f));
        bg.rectTransform.sizeDelta = new Vector2(0, 40);

        var inputField = bg.gameObject.AddComponent<TMP_InputField>();

        // Text area with mask
        var areaGO = MakeGO("Text Area", bg.transform);
        var areaR  = areaGO.AddComponent<RectTransform>();
        Stretch(areaR);
        areaR.offsetMin = new Vector2(12, 4);
        areaR.offsetMax = new Vector2(-12, -4);
        areaGO.AddComponent<RectMask2D>();

        // Placeholder
        var ph = MakeTMP(areaGO, "Placeholder",
                         "🔍  Tìm theo tên hoặc ID...", 13, FontStyles.Italic);
        ph.color = new Color(0.48f, 0.48f, 0.48f);
        Stretch(ph.gameObject.GetComponent<RectTransform>());

        // Input text
        var txt = MakeTMP(areaGO, "Text", "", 13, FontStyles.Normal);
        Stretch(txt.gameObject.GetComponent<RectTransform>());

        inputField.textViewport  = areaR;
        inputField.textComponent = txt;
        inputField.placeholder   = ph;

        return bg.gameObject;
    }

    private static GameObject BuildScrollView(Transform parent)
    {
        var go   = MakeGO("ScrollView", parent);
        go.AddComponent<RectTransform>();
        var bgImg = go.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.09f, 0.12f, 0.6f);

        var scroll = go.AddComponent<ScrollRect>();
        scroll.horizontal         = false;
        scroll.vertical           = true;
        scroll.scrollSensitivity  = 35;
        scroll.movementType       = ScrollRect.MovementType.Clamped;

        // Viewport
        var vp     = MakeGO("Viewport", go.transform);
        var vpRect = vp.AddComponent<RectTransform>();
        Stretch(vpRect);
        vp.AddComponent<RectMask2D>();

        // Content
        var content = MakeGO("Content", vp.transform);
        var cRect   = content.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot     = new Vector2(0.5f, 1f);
        cRect.offsetMin = cRect.offsetMax = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing          = 4;
        vlg.padding          = new RectOffset(4, 4, 4, 4);
        vlg.childControlWidth    = vlg.childForceExpandWidth = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandHeight = false;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRect;
        scroll.content  = cRect;

        return go;
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────
    private static GameObject MakeGO(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static Image MakeImage(GameObject parent, string name, Color color)
    {
        var go = MakeGO(name, parent?.transform);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI MakeTMP(GameObject parent, string name, string text,
                                            float size, FontStyles style)
    {
        var go  = MakeGO(name, parent.transform);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = Color.white;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.alignment    = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }

    private static Button MakeButton(GameObject parent, string name, string label, Color bg)
    {
        var img  = MakeImage(parent, name, bg);
        var btn  = img.gameObject.AddComponent<Button>();
        var cb   = btn.colors;
        cb.highlightedColor = bg * 1.25f; cb.pressedColor = bg * 0.75f;
        btn.colors = cb;

        var lbl = MakeTMP(img.gameObject, "Label", label, 15, FontStyles.Bold);
        Stretch(lbl.gameObject.GetComponent<RectTransform>());
        lbl.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static void SetAnchored(RectTransform r, Vector2 anchorMin, Vector2 anchorMax,
                                     Vector2 size)
    {
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = Vector2.zero;
    }

    private static void ForceHeight(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = h;
    }

    private static void FixSize(GameObject go, float w, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = w;
        le.minHeight = le.preferredHeight = h;
    }

    private static void Flex(GameObject go)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
    }

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
