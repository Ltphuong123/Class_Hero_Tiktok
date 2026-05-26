using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class AudioVolumeUICreator
{
    [MenuItem("Tools/Create Audio Volume UI")]
    public static void Create()
    {
        // Canvas riêng — không bị ảnh hưởng bởi parent khác
        GameObject cObj = new GameObject("Canvas_AudioVolume");
        Canvas canvas = cObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler cs = cObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080, 1920);
        cObj.AddComponent<GraphicRaycaster>();

        // ── Root panel ────────────────────────────────────────────────────────
        GameObject root = new GameObject("AudioVolumeUI");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin        = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax        = new Vector2(0.5f, 0.5f);
        rootRT.pivot            = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta        = new Vector2(500, 160);
        rootRT.anchoredPosition = Vector2.zero;

        Image rootBg = root.AddComponent<Image>();
        rootBg.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);

        // ── Title ─────────────────────────────────────────────────────────────
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(root.transform, false);
        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin        = new Vector2(0f, 1f);
        titleRT.anchorMax        = new Vector2(1f, 1f);
        titleRT.pivot            = new Vector2(0.5f, 1f);
        titleRT.sizeDelta        = new Vector2(0, 50);
        titleRT.anchoredPosition = new Vector2(0, -5);
        TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "ÂM LƯỢNG NHÂN VẬT";
        titleTxt.fontSize  = 22;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color     = Color.white;
        titleTxt.alignment = TextAlignmentOptions.Center;

        // ── Slider row ────────────────────────────────────────────────────────
        GameObject rowObj = new GameObject("SliderRow");
        rowObj.transform.SetParent(root.transform, false);
        RectTransform rowRT = rowObj.AddComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0f, 0f);
        rowRT.anchorMax        = new Vector2(1f, 0f);
        rowRT.pivot            = new Vector2(0.5f, 0f);
        rowRT.sizeDelta        = new Vector2(-40, 50);
        rowRT.anchoredPosition = new Vector2(0, 20);

        // ── Value text ────────────────────────────────────────────────────────
        GameObject valueObj = new GameObject("ValueText");
        valueObj.transform.SetParent(rowObj.transform, false);
        RectTransform valueRT = valueObj.AddComponent<RectTransform>();
        valueRT.anchorMin        = new Vector2(1f, 0f);
        valueRT.anchorMax        = new Vector2(1f, 1f);
        valueRT.pivot            = new Vector2(1f, 0.5f);
        valueRT.sizeDelta        = new Vector2(70, 0);
        valueRT.anchoredPosition = Vector2.zero;
        TextMeshProUGUI valueTxt = valueObj.AddComponent<TextMeshProUGUI>();
        valueTxt.text      = "100%";
        valueTxt.fontSize  = 20;
        valueTxt.color     = Color.white;
        valueTxt.alignment = TextAlignmentOptions.MidlineRight;

        // ── Slider ────────────────────────────────────────────────────────────
        GameObject sliderObj = new GameObject("VolumeSlider");
        sliderObj.transform.SetParent(rowObj.transform, false);
        RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
        sliderRT.anchorMin        = new Vector2(0f, 0f);
        sliderRT.anchorMax        = new Vector2(1f, 1f);
        sliderRT.offsetMin        = Vector2.zero;
        sliderRT.offsetMax        = new Vector2(-80, 0);
        Slider slider = sliderObj.AddComponent<Slider>();

        // Background track
        GameObject bgTrack = new GameObject("Background");
        bgTrack.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRT = bgTrack.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.25f);
        bgRT.anchorMax = new Vector2(1f, 0.75f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        Image bgImg = bgTrack.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRT.offsetMin = new Vector2(5, 0);
        fillAreaRT.offsetMax = new Vector2(-15, 0);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        fillRT.sizeDelta = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.7f, 1f, 1f);

        // Handle area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10, 0);
        handleAreaRT.offsetMax = new Vector2(-10, 0);

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleArea.transform, false);
        RectTransform handleRT = handleObj.AddComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0f, 0f);
        handleRT.anchorMax = new Vector2(0f, 1f);
        handleRT.sizeDelta = new Vector2(24, 0);
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = Color.white;

        // Wire slider
        slider.fillRect        = fillRT;
        slider.handleRect      = handleRT;
        slider.targetGraphic   = handleImg;
        slider.direction       = Slider.Direction.LeftToRight;
        slider.minValue        = 0f;
        slider.maxValue        = 1f;
        slider.value           = 1f;

        // ── AudioVolumeUI component ───────────────────────────────────────────
        AudioVolumeUI ui = root.AddComponent<AudioVolumeUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("volumeSlider").objectReferenceValue = slider;
        so.FindProperty("valueText").objectReferenceValue    = valueTxt;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);

        Debug.Log("[AudioVolumeUICreator] Done!");
    }
}
