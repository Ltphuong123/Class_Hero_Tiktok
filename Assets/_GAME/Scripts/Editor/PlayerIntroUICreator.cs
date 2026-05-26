using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Video;

public static class PlayerIntroUICreator
{
    [MenuItem("Tools/Create PlayerIntro UI")]
    public static void CreateIntroUI()
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

        // ── Root panel (dim overlay) ──────────────────────────────────────────
        GameObject root = new GameObject("IntroPanel");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);
        root.SetActive(false);

        // ── RenderTexture ─────────────────────────────────────────────────────
        RenderTexture rt = new RenderTexture(1920, 1080, 0) { name = "IntroVideoRT" };
        string rtPath = "Assets/_GAME/Assets/IntroVideoRT.renderTexture";
        System.IO.Directory.CreateDirectory("Assets/_GAME/Assets");
        AssetDatabase.CreateAsset(rt, rtPath);

        // ── Video display (RawImage) ──────────────────────────────────────────
        GameObject videoObj = new GameObject("VideoDisplay");
        videoObj.transform.SetParent(root.transform, false);
        RectTransform videoRT = videoObj.AddComponent<RectTransform>();
        videoRT.anchorMin = new Vector2(0.5f, 0.5f);
        videoRT.anchorMax = new Vector2(0.5f, 0.5f);
        videoRT.sizeDelta = new Vector2(900, 506);
        videoRT.anchoredPosition = new Vector2(0, 80);
        RawImage rawImg = videoObj.AddComponent<RawImage>();
        rawImg.texture = rt;

        // ── Rank text ─────────────────────────────────────────────────────────
        GameObject rankObj = new GameObject("RankText");
        rankObj.transform.SetParent(root.transform, false);
        RectTransform rankObjRT = rankObj.AddComponent<RectTransform>();
        rankObjRT.anchorMin = new Vector2(0.5f, 0.5f);
        rankObjRT.anchorMax = new Vector2(0.5f, 0.5f);
        rankObjRT.sizeDelta = new Vector2(700, 60);
        rankObjRT.anchoredPosition = new Vector2(0, -240);
        Text rankTxt = rankObj.AddComponent<Text>();
        rankTxt.text      = "Top #1 BXH Tuần";
        rankTxt.fontSize  = 36;
        rankTxt.color     = new Color(1f, 0.84f, 0f);
        rankTxt.alignment = TextAnchor.MiddleCenter;
        rankTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ── Player name ───────────────────────────────────────────────────────
        GameObject nameObj = new GameObject("PlayerNameText");
        nameObj.transform.SetParent(root.transform, false);
        RectTransform nameObjRT = nameObj.AddComponent<RectTransform>();
        nameObjRT.anchorMin = new Vector2(0.5f, 0.5f);
        nameObjRT.anchorMax = new Vector2(0.5f, 0.5f);
        nameObjRT.sizeDelta = new Vector2(800, 70);
        nameObjRT.anchoredPosition = new Vector2(0, -310);
        Text nameTxt = nameObj.AddComponent<Text>();
        nameTxt.text      = "Tên người chơi";
        nameTxt.fontSize  = 48;
        nameTxt.color     = Color.white;
        nameTxt.fontStyle = FontStyle.Bold;
        nameTxt.alignment = TextAnchor.MiddleCenter;
        nameTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ── VideoPlayer ───────────────────────────────────────────────────────
        VideoPlayer vp = root.AddComponent<VideoPlayer>();
        vp.renderMode   = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.playOnAwake  = false;
        vp.isLooping    = false;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        AudioSource videoAudio = root.AddComponent<AudioSource>();
        vp.SetTargetAudioSource(0, videoAudio);

        // AudioSource riêng cho intro sound
        AudioSource introAudio = root.AddComponent<AudioSource>();
        introAudio.playOnAwake = false;

        // ── PlayerIntroManager ────────────────────────────────────────────────
        PlayerIntroManager manager = Object.FindObjectOfType<PlayerIntroManager>();
        if (manager == null)
        {
            GameObject mgrObj = new GameObject("PlayerIntroManager");
            manager = mgrObj.AddComponent<PlayerIntroManager>();
        }

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("introPanel").objectReferenceValue     = root;
        so.FindProperty("videoDisplay").objectReferenceValue   = rawImg;
        so.FindProperty("playerNameText").objectReferenceValue = nameTxt;
        so.FindProperty("rankText").objectReferenceValue       = rankTxt;
        so.FindProperty("videoPlayer").objectReferenceValue    = vp;
        so.FindProperty("audioSource").objectReferenceValue    = introAudio;
        so.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = manager.gameObject;
        EditorUtility.SetDirty(manager.gameObject);

        Debug.Log("[PlayerIntroUICreator] Done! Gán VideoClip vào VideoPlayer để phát intro.");
    }
}
