// using UnityEngine;
// using UnityEditor;
// using UnityEngine.UI;
// using TMPro;

// public class CanvasSettingsCreator : EditorWindow
// {
//     [MenuItem("Tools/Create Canvas Settings")]
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

//         GameObject mainPanel = new GameObject("CanvasSettings");
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
//         contentRect.sizeDelta = new Vector2(1000, 700);
//         contentRect.anchoredPosition = Vector2.zero;
        
//         Image contentBg = contentPanel.AddComponent<Image>();
//         contentBg.color = new Color(0.15f, 0.15f, 0.15f);

//         GameObject title = CreateText(contentPanel.transform, "Title", "GAME SETTINGS", 28);
//         SetPosition(title, 0, -40, 800, 50);

//         GameObject closeBtn = CreateButton(contentPanel.transform, "CloseButton", "X", new Color(0.8f, 0.2f, 0.2f));
//         RectTransform closeBtnRect = closeBtn.GetComponent<RectTransform>();
//         closeBtnRect.anchorMin = new Vector2(1f, 1f);
//         closeBtnRect.anchorMax = new Vector2(1f, 1f);
//         closeBtnRect.sizeDelta = new Vector2(50, 50);
//         closeBtnRect.anchoredPosition = new Vector2(-25, -25);

//         GameObject tabBar = new GameObject("TabBar");
//         tabBar.transform.SetParent(contentPanel.transform, false);
//         RectTransform tabBarRect = tabBar.AddComponent<RectTransform>();
//         tabBarRect.anchorMin = new Vector2(0.5f, 1f);
//         tabBarRect.anchorMax = new Vector2(0.5f, 1f);
//         tabBarRect.sizeDelta = new Vector2(900, 60);
//         tabBarRect.anchoredPosition = new Vector2(0, -100);
        
//         HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
//         tabLayout.spacing = 10;
//         tabLayout.childControlWidth = true;
//         tabLayout.childControlHeight = true;
//         tabLayout.childForceExpandWidth = true;
//         tabLayout.childForceExpandHeight = true;

//         GameObject tab1 = CreateButton(tabBar.transform, "SwordDataTab", "Sword Data", new Color(0.3f, 0.6f, 1f));
//         GameObject tab2 = CreateButton(tabBar.transform, "CharacterLevelTab", "Character Level", new Color(0.2f, 0.2f, 0.2f));
//         GameObject tab3 = CreateButton(tabBar.transform, "CharacterBaseTab", "Character Base", new Color(0.2f, 0.2f, 0.2f));

//         GameObject swordDataPanel = CreateSwordDataPanel(contentPanel.transform);
//         GameObject characterLevelPanel = CreateCharacterLevelPanel(contentPanel.transform);
//         GameObject characterBasePanel = CreateCharacterBasePanel(contentPanel.transform);

//         CanvasSettings settingsScript = mainPanel.AddComponent<CanvasSettings>();
        
//         SerializedObject so = new SerializedObject(settingsScript);
//         so.FindProperty("swordDataTabButton").objectReferenceValue = tab1.GetComponent<Button>();
//         so.FindProperty("characterLevelTabButton").objectReferenceValue = tab2.GetComponent<Button>();
//         so.FindProperty("characterBaseTabButton").objectReferenceValue = tab3.GetComponent<Button>();
//         so.FindProperty("swordDataPanel").objectReferenceValue = swordDataPanel;
//         so.FindProperty("characterLevelPanel").objectReferenceValue = characterLevelPanel;
//         so.FindProperty("characterBasePanel").objectReferenceValue = characterBasePanel;
//         so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
        
//         so.FindProperty("swordDataContent").objectReferenceValue = swordDataPanel.transform.Find("SwordDataScroll/Viewport/Content");
//         so.FindProperty("swordDataSaveButton").objectReferenceValue = swordDataPanel.transform.Find("ButtonBar/SaveButton").GetComponent<Button>();
//         so.FindProperty("swordDataResetButton").objectReferenceValue = swordDataPanel.transform.Find("ButtonBar/ResetButton").GetComponent<Button>();
        
//         so.FindProperty("characterLevelContent").objectReferenceValue = characterLevelPanel.transform.Find("CharacterLevelScroll/Viewport/Content");
//         so.FindProperty("characterLevelSaveButton").objectReferenceValue = characterLevelPanel.transform.Find("ButtonBar/SaveButton").GetComponent<Button>();
//         so.FindProperty("characterLevelResetButton").objectReferenceValue = characterLevelPanel.transform.Find("ButtonBar/ResetButton").GetComponent<Button>();
        
//         so.FindProperty("maxHpInput").objectReferenceValue = characterBasePanel.transform.Find("MaxHpInputContainer/MaxHpInput").GetComponent<TMP_InputField>();
//         so.FindProperty("overhealThresholdInput").objectReferenceValue = characterBasePanel.transform.Find("OverhealThresholdInputContainer/OverhealThresholdInput").GetComponent<TMP_InputField>();
//         so.FindProperty("overhealScaleInput").objectReferenceValue = characterBasePanel.transform.Find("OverhealScaleInputContainer/OverhealScaleInput").GetComponent<TMP_InputField>();
//         so.FindProperty("maxSwordCountInput").objectReferenceValue = characterBasePanel.transform.Find("MaxSwordCountInputContainer/MaxSwordCountInput").GetComponent<TMP_InputField>();
//         so.FindProperty("maxSwordQueueInput").objectReferenceValue = characterBasePanel.transform.Find("MaxSwordQueueInputContainer/MaxSwordQueueInput").GetComponent<TMP_InputField>();
//         so.FindProperty("meteorDamageInput").objectReferenceValue = characterBasePanel.transform.Find("MeteorDamageInputContainer/MeteorDamageInput").GetComponent<TMP_InputField>();
//         so.FindProperty("characterBaseSaveButton").objectReferenceValue = characterBasePanel.transform.Find("ButtonBar/SaveButton").GetComponent<Button>();
//         so.FindProperty("characterBaseLoadButton").objectReferenceValue = characterBasePanel.transform.Find("ButtonBar/LoadButton").GetComponent<Button>();
//         so.FindProperty("characterBaseResetButton").objectReferenceValue = characterBasePanel.transform.Find("ButtonBar/ResetButton").GetComponent<Button>();
        
//         so.ApplyModifiedProperties();

//         Selection.activeGameObject = mainPanel;
//         Debug.Log("Canvas Settings created successfully!");
//     }

//     private static GameObject CreateSwordDataPanel(Transform parent)
//     {
//         GameObject panel = new GameObject("SwordDataPanel");
//         panel.transform.SetParent(parent, false);
        
//         RectTransform rect = panel.AddComponent<RectTransform>();
//         rect.anchorMin = new Vector2(0.5f, 0f);
//         rect.anchorMax = new Vector2(0.5f, 1f);
//         rect.sizeDelta = new Vector2(900, -180);
//         rect.anchoredPosition = new Vector2(0, -40);
        
//         GameObject scrollView = CreateScrollView(panel.transform, "SwordDataScroll");
//         GameObject buttonBar = CreateButtonBar(panel.transform);
        
//         return panel;
//     }

//     private static GameObject CreateCharacterLevelPanel(Transform parent)
//     {
//         GameObject panel = new GameObject("CharacterLevelPanel");
//         panel.transform.SetParent(parent, false);
        
//         RectTransform rect = panel.AddComponent<RectTransform>();
//         rect.anchorMin = new Vector2(0.5f, 0f);
//         rect.anchorMax = new Vector2(0.5f, 1f);
//         rect.sizeDelta = new Vector2(900, -180);
//         rect.anchoredPosition = new Vector2(0, -40);
        
//         GameObject scrollView = CreateScrollView(panel.transform, "CharacterLevelScroll");
//         GameObject buttonBar = CreateButtonBar(panel.transform);
        
//         return panel;
//     }

//     private static GameObject CreateCharacterBasePanel(Transform parent)
//     {
//         GameObject panel = new GameObject("CharacterBasePanel");
//         panel.transform.SetParent(parent, false);
        
//         RectTransform rect = panel.AddComponent<RectTransform>();
//         rect.anchorMin = new Vector2(0.5f, 0f);
//         rect.anchorMax = new Vector2(0.5f, 1f);
//         rect.sizeDelta = new Vector2(900, -180);
//         rect.anchoredPosition = new Vector2(0, -40);

//         float yPos = -30;
//         float spacing = 60;

//         GameObject healthHeader = CreateText(panel.transform, "HealthHeader", "=== HEALTH SETTINGS ===", 18);
//         SetPosition(healthHeader, 0, yPos, 800, 30);
//         yPos -= spacing;

//         GameObject maxHpField = CreateInputField(panel.transform, "MaxHpInput", "Max HP:", "100");
//         SetPosition(maxHpField, 0, yPos, 700, 50);
//         yPos -= spacing;

//         GameObject overhealThresholdField = CreateInputField(panel.transform, "OverhealThresholdInput", "Overheal Threshold:", "50");
//         SetPosition(overhealThresholdField, 0, yPos, 700, 50);
//         yPos -= spacing;

//         GameObject overhealScaleField = CreateInputField(panel.transform, "OverhealScaleInput", "Overheal Scale:", "0.1");
//         SetPosition(overhealScaleField, 0, yPos, 700, 50);
//         yPos -= spacing;

//         GameObject swordHeader = CreateText(panel.transform, "SwordHeader", "=== SWORD SETTINGS ===", 18);
//         SetPosition(swordHeader, 0, yPos, 800, 30);
//         yPos -= spacing;

//         GameObject maxSwordCountField = CreateInputField(panel.transform, "MaxSwordCountInput", "Max Sword Count:", "20");
//         SetPosition(maxSwordCountField, 0, yPos, 700, 50);
//         yPos -= spacing;

//         GameObject maxSwordQueueField = CreateInputField(panel.transform, "MaxSwordQueueInput", "Max Sword Queue:", "50");
//         SetPosition(maxSwordQueueField, 0, yPos, 700, 50);
//         yPos -= spacing;

//         GameObject meteorHeader = CreateText(panel.transform, "MeteorHeader", "=== METEOR SETTINGS ===", 18);
//         SetPosition(meteorHeader, 0, yPos, 800, 30);
//         yPos -= spacing;

//         GameObject meteorDamageField = CreateInputField(panel.transform, "MeteorDamageInput", "Meteor Damage:", "50");
//         SetPosition(meteorDamageField, 0, yPos, 700, 50);
//         yPos -= spacing;

//         GameObject buttonBar = CreateButtonBar(panel.transform);
        
//         return panel;
//     }

//     private static GameObject CreateScrollView(Transform parent, string name)
//     {
//         GameObject scrollView = new GameObject(name);
//         scrollView.transform.SetParent(parent, false);
        
//         RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
//         scrollRect.anchorMin = new Vector2(0.5f, 0f);
//         scrollRect.anchorMax = new Vector2(0.5f, 1f);
//         scrollRect.sizeDelta = new Vector2(850, -80);
//         scrollRect.anchoredPosition = new Vector2(0, 20);
        
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
//         contentRect.sizeDelta = new Vector2(0, 1000);
//         contentRect.anchoredPosition = Vector2.zero;
        
//         VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
//         layout.spacing = 5;
//         layout.padding = new RectOffset(10, 10, 10, 10);
//         layout.childControlWidth = true;
//         layout.childControlHeight = false;
//         layout.childForceExpandWidth = true;
        
//         ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
//         fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
//         scroll.content = contentRect;
//         scroll.viewport = viewportRect;
//         scroll.horizontal = false;
//         scroll.vertical = true;
        
//         return scrollView;
//     }

//     private static GameObject CreateButtonBar(Transform parent)
//     {
//         GameObject buttonBar = new GameObject("ButtonBar");
//         buttonBar.transform.SetParent(parent, false);
        
//         RectTransform barRect = buttonBar.AddComponent<RectTransform>();
//         barRect.anchorMin = new Vector2(0.5f, 0f);
//         barRect.anchorMax = new Vector2(0.5f, 0f);
//         barRect.sizeDelta = new Vector2(400, 50);
//         barRect.anchoredPosition = new Vector2(0, 30);
        
//         HorizontalLayoutGroup layout = buttonBar.AddComponent<HorizontalLayoutGroup>();
//         layout.spacing = 20;
//         layout.childControlWidth = true;
//         layout.childControlHeight = true;
//         layout.childForceExpandWidth = true;
//         layout.childForceExpandHeight = true;

//         CreateButton(buttonBar.transform, "SaveButton", "Save", new Color(0.2f, 0.8f, 0.2f));
//         CreateButton(buttonBar.transform, "LoadButton", "Load", new Color(0.2f, 0.5f, 0.8f));
//         CreateButton(buttonBar.transform, "ResetButton", "Reset", new Color(0.8f, 0.3f, 0.2f));
        
//         return buttonBar;
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

//     private static GameObject CreateInputField(Transform parent, string name, string labelText, string placeholder)
//     {
//         GameObject container = new GameObject(name + "Container");
//         container.transform.SetParent(parent, false);
        
//         RectTransform containerRect = container.AddComponent<RectTransform>();

//         GameObject label = CreateText(container.transform, "Label", labelText, 16);
//         RectTransform labelRect = label.GetComponent<RectTransform>();
//         labelRect.anchorMin = new Vector2(0f, 0.5f);
//         labelRect.anchorMax = new Vector2(0f, 0.5f);
//         labelRect.sizeDelta = new Vector2(250, 30);
//         labelRect.anchoredPosition = new Vector2(125, 0);
//         label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

//         GameObject inputObj = new GameObject(name);
//         inputObj.transform.SetParent(container.transform, false);
        
//         RectTransform inputRect = inputObj.AddComponent<RectTransform>();
//         inputRect.anchorMin = new Vector2(1f, 0.5f);
//         inputRect.anchorMax = new Vector2(1f, 0.5f);
//         inputRect.sizeDelta = new Vector2(200, 35);
//         inputRect.anchoredPosition = new Vector2(-100, 0);
        
//         Image inputImage = inputObj.AddComponent<Image>();
//         inputImage.color = new Color(0.1f, 0.1f, 0.1f);
        
//         TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        
//         GameObject textArea = new GameObject("TextArea");
//         textArea.transform.SetParent(inputObj.transform, false);
//         RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
//         textAreaRect.anchorMin = Vector2.zero;
//         textAreaRect.anchorMax = Vector2.one;
//         textAreaRect.offsetMin = new Vector2(5, 0);
//         textAreaRect.offsetMax = new Vector2(-5, 0);
        
//         GameObject textObj = new GameObject("Text");
//         textObj.transform.SetParent(textArea.transform, false);
//         TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
//         inputText.fontSize = 14;
//         inputText.color = Color.white;
//         RectTransform textRect = textObj.GetComponent<RectTransform>();
//         textRect.anchorMin = Vector2.zero;
//         textRect.anchorMax = Vector2.one;
//         textRect.offsetMin = Vector2.zero;
//         textRect.offsetMax = Vector2.zero;
        
//         GameObject placeholderObj = new GameObject("Placeholder");
//         placeholderObj.transform.SetParent(textArea.transform, false);
//         TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
//         placeholderText.text = placeholder;
//         placeholderText.fontSize = 14;
//         placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
//         RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
//         placeholderRect.anchorMin = Vector2.zero;
//         placeholderRect.anchorMax = Vector2.one;
//         placeholderRect.offsetMin = Vector2.zero;
//         placeholderRect.offsetMax = Vector2.zero;
        
//         inputField.textViewport = textAreaRect;
//         inputField.textComponent = inputText;
//         inputField.placeholder = placeholderText;
        
//         return container;
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
