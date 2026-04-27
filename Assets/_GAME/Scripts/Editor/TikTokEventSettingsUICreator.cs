// using UnityEngine;
// using UnityEditor;
// using UnityEngine.UI;
// using TMPro;

// public class TikTokEventSettingsUICreator : EditorWindow
// {
//     [MenuItem("Tools/Create TikTok Event Settings UI")]
//     public static void CreateUI()
//     {
//         Canvas canvas = FindObjectOfType<Canvas>();
//         if (canvas == null)
//         {
//             GameObject canvasObj = new GameObject("Canvas");
//             canvas = canvasObj.AddComponent<Canvas>();
//             canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//             canvasObj.AddComponent<CanvasScaler>();
//             canvasObj.AddComponent<GraphicRaycaster>();
//         }

//         GameObject mainPanel = new GameObject("TikTokEventSettingsUI");
//         mainPanel.transform.SetParent(canvas.transform, false);
        
//         RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
//         mainRect.anchorMin = Vector2.zero;
//         mainRect.anchorMax = Vector2.one;
//         mainRect.offsetMin = Vector2.zero;
//         mainRect.offsetMax = Vector2.zero;
        
//         Image mainBg = mainPanel.AddComponent<Image>();
//         mainBg.color = new Color(0f, 0f, 0f, 0.8f);

//         GameObject contentPanel = new GameObject("ContentPanel");
//         contentPanel.transform.SetParent(mainPanel.transform, false);
//         RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
//         contentRect.anchorMin = new Vector2(0.5f, 0.5f);
//         contentRect.anchorMax = new Vector2(0.5f, 0.5f);
//         contentRect.sizeDelta = new Vector2(1200, 800);
//         contentRect.anchoredPosition = Vector2.zero;
        
//         Image contentBg = contentPanel.AddComponent<Image>();
//         contentBg.color = new Color(0.15f, 0.15f, 0.15f);

//         GameObject title = CreateText(contentPanel.transform, "Title", "TIKTOK EVENT SETTINGS", 28);
//         SetPosition(title, 0, -40, 1000, 50);

//         GameObject closeBtn = CreateButton(contentPanel.transform, "CloseButton", "X", new Color(0.8f, 0.2f, 0.2f));
//         RectTransform closeBtnRect = closeBtn.GetComponent<RectTransform>();
//         closeBtnRect.anchorMin = new Vector2(1f, 1f);
//         closeBtnRect.anchorMax = new Vector2(1f, 1f);
//         closeBtnRect.sizeDelta = new Vector2(50, 50);
//         closeBtnRect.anchoredPosition = new Vector2(-25, -25);

//         GameObject scrollView = CreateScrollView(contentPanel.transform);

//         GameObject buttonBar = new GameObject("ButtonBar");
//         buttonBar.transform.SetParent(contentPanel.transform, false);
//         RectTransform barRect = buttonBar.AddComponent<RectTransform>();
//         barRect.anchorMin = new Vector2(0.5f, 0f);
//         barRect.anchorMax = new Vector2(0.5f, 0f);
//         barRect.sizeDelta = new Vector2(600, 50);
//         barRect.anchoredPosition = new Vector2(0, 30);
        
//         HorizontalLayoutGroup layout = buttonBar.AddComponent<HorizontalLayoutGroup>();
//         layout.spacing = 20;
//         layout.childControlWidth = true;
//         layout.childControlHeight = true;
//         layout.childForceExpandWidth = true;
//         layout.childForceExpandHeight = true;

//         GameObject addBtn = CreateButton(buttonBar.transform, "AddEventButton", "Add Event", new Color(0.3f, 0.6f, 1f));
//         GameObject saveBtn = CreateButton(buttonBar.transform, "SaveButton", "Save", new Color(0.2f, 0.8f, 0.2f));
//         GameObject loadBtn = CreateButton(buttonBar.transform, "LoadButton", "Load", new Color(0.2f, 0.5f, 0.8f));
//         GameObject resetBtn = CreateButton(buttonBar.transform, "ResetButton", "Reset", new Color(0.8f, 0.3f, 0.2f));

//         TikTokEventSettingsUI uiScript = mainPanel.AddComponent<TikTokEventSettingsUI>();
        
//         SerializedObject so = new SerializedObject(uiScript);
//         so.FindProperty("contentParent").objectReferenceValue = scrollView.transform.Find("Viewport/Content");
//         so.FindProperty("addEventButton").objectReferenceValue = addBtn.GetComponent<Button>();
//         so.FindProperty("saveButton").objectReferenceValue = saveBtn.GetComponent<Button>();
//         so.FindProperty("loadButton").objectReferenceValue = loadBtn.GetComponent<Button>();
//         so.FindProperty("resetButton").objectReferenceValue = resetBtn.GetComponent<Button>();
//         so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
//         so.ApplyModifiedProperties();

//         Selection.activeGameObject = mainPanel;
//         Debug.Log("TikTok Event Settings UI created successfully!");
//         Debug.Log("Don't forget to:");
//         Debug.Log("1. Assign TikTokEventConfigSO to 'config' field");
//         Debug.Log("2. Create and assign Event Row Prefab");
//     }

//     private static GameObject CreateScrollView(Transform parent)
//     {
//         GameObject scrollView = new GameObject("ScrollView");
//         scrollView.transform.SetParent(parent, false);
        
//         RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
//         scrollRect.anchorMin = new Vector2(0.5f, 0f);
//         scrollRect.anchorMax = new Vector2(0.5f, 1f);
//         scrollRect.sizeDelta = new Vector2(1150, -180);
//         scrollRect.anchoredPosition = new Vector2(0, -10);
        
//         Image scrollBg = scrollView.AddComponent<Image>();
//         scrollBg.color = new Color(0.1f, 0.1f, 0.1f);
        
//         ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        
//         GameObject viewport = new GameObject("Viewport");
//         viewport.transform.SetParent(scrollView.transform, false);
//         RectTransform viewportRect = viewport.AddComponent<RectTransform>();
//         viewportRect.anchorMin = Vector2.zero;
//         viewportRect.anchorMax = Vector2.one;
//         viewportRect.offsetMin = Vector2.zero;
//         viewportRect.offsetMax = Vector2.zero;
        
//         Image viewportMask = viewport.AddComponent<Image>();
//         viewportMask.color = Color.white;
//         Mask mask = viewport.AddComponent<Mask>();
//         mask.showMaskGraphic = false;
        
//         GameObject content = new GameObject("Content");
//         content.transform.SetParent(viewport.transform, false);
//         RectTransform contentRect = content.AddComponent<RectTransform>();
//         contentRect.anchorMin = new Vector2(0f, 1f);
//         contentRect.anchorMax = new Vector2(1f, 1f);
//         contentRect.pivot = new Vector2(0.5f, 1f);
//         contentRect.sizeDelta = new Vector2(0, 2000);
//         contentRect.anchoredPosition = Vector2.zero;
        
//         VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
//         layoutGroup.spacing = 10;
//         layoutGroup.padding = new RectOffset(10, 10, 10, 10);
//         layoutGroup.childControlWidth = true;
//         layoutGroup.childControlHeight = false;
//         layoutGroup.childForceExpandWidth = true;
        
//         ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
//         fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
//         scroll.content = contentRect;
//         scroll.viewport = viewportRect;
//         scroll.horizontal = false;
//         scroll.vertical = true;
        
//         return scrollView;
//     }

//     private static GameObject CreateText(Transform parent, string name, string text, int fontSize)
//     {
//         GameObject obj = new GameObject(name);
//         obj.transform.SetParent(parent, false);
        
//         TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
//         tmp.text = text;
//         tmp.fontSize = fontSize;
//         tmp.alignment = TextAlignmentOptions.Center;
//         tmp.color = Color.white;
        
//         return obj;
//     }

//     private static GameObject CreateButton(Transform parent, string name, string text, Color color)
//     {
//         GameObject obj = new GameObject(name);
//         obj.transform.SetParent(parent, false);
        
//         Image image = obj.AddComponent<Image>();
//         image.color = color;
        
//         Button button = obj.AddComponent<Button>();
        
//         GameObject textObj = CreateText(obj.transform, "Text", text, 16);
//         RectTransform textRect = textObj.GetComponent<RectTransform>();
//         textRect.anchorMin = Vector2.zero;
//         textRect.anchorMax = Vector2.one;
//         textRect.offsetMin = Vector2.zero;
//         textRect.offsetMax = Vector2.zero;
        
//         return obj;
//     }

//     private static void SetPosition(GameObject obj, float x, float y, float width, float height)
//     {
//         RectTransform rect = obj.GetComponent<RectTransform>();
//         rect.anchorMin = new Vector2(0.5f, 1f);
//         rect.anchorMax = new Vector2(0.5f, 1f);
//         rect.sizeDelta = new Vector2(width, height);
//         rect.anchoredPosition = new Vector2(x, y);
//     }
// }
