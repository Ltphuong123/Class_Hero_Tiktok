// using UnityEngine;
// using UnityEditor;
// using UnityEngine.UI;
// using TMPro;

// public class TikTokEventPrefabCreator : EditorWindow
// {
//     [MenuItem("Tools/Create TikTok Event Prefabs")]
//     public static void CreatePrefabs()
//     {
//         CreateActionRowPrefab();
//         CreateEventRowPrefab();
        
//         Debug.Log("TikTok Event Prefabs created successfully!");
//         Debug.Log("Prefabs saved to: Assets/_GAME/Prefabs/UI/");
//     }

//     private static void CreateActionRowPrefab()
//     {
//         GameObject actionRow = new GameObject("TikTokActionRow");
        
//         RectTransform rect = actionRow.AddComponent<RectTransform>();
//         rect.sizeDelta = new Vector2(800, 60);
        
//         Image bg = actionRow.AddComponent<Image>();
//         bg.color = new Color(0.25f, 0.25f, 0.25f);
        
//         HorizontalLayoutGroup layout = actionRow.AddComponent<HorizontalLayoutGroup>();
//         layout.spacing = 10;
//         layout.padding = new RectOffset(10, 10, 10, 10);
//         layout.childControlWidth = false;
//         layout.childControlHeight = true;
//         layout.childForceExpandHeight = true;

//         GameObject actionTypeLabel = CreateText(actionRow.transform, "ActionTypeLabel", "Action:", 14);
//         SetSize(actionTypeLabel, 60, 40);

//         GameObject actionTypeDropdown = CreateDropdown(actionRow.transform, "ActionTypeDropdown", 
//             new string[] { "Spawn", "Respawn", "AddSwords", "UpgradeToLevel2", "UpgradeToLevel3", 
//                           "UpgradeToLevel4", "UpgradeToLevel5", "MagnetBooster", "ShieldBooster", 
//                           "MeteorBooster", "HealBooster" });
//         SetSize(actionTypeDropdown, 180, 40);

//         GameObject swordCountContainer = new GameObject("SwordCountContainer");
//         swordCountContainer.transform.SetParent(actionRow.transform, false);
//         RectTransform swordRect = swordCountContainer.AddComponent<RectTransform>();
//         swordRect.sizeDelta = new Vector2(200, 40);
//         HorizontalLayoutGroup swordLayout = swordCountContainer.AddComponent<HorizontalLayoutGroup>();
//         swordLayout.spacing = 5;
//         swordLayout.childControlWidth = false;
//         swordLayout.childControlHeight = true;
        
//         GameObject swordLabel = CreateText(swordCountContainer.transform, "Label", "Swords:", 14);
//         SetSize(swordLabel, 70, 40);
//         GameObject swordInput = CreateInputField(swordCountContainer.transform, "SwordCountInput", "1");
//         SetSize(swordInput, 120, 40);

//         GameObject healAmountContainer = new GameObject("HealAmountContainer");
//         healAmountContainer.transform.SetParent(actionRow.transform, false);
//         RectTransform healRect = healAmountContainer.AddComponent<RectTransform>();
//         healRect.sizeDelta = new Vector2(200, 40);
//         HorizontalLayoutGroup healLayout = healAmountContainer.AddComponent<HorizontalLayoutGroup>();
//         healLayout.spacing = 5;
//         healLayout.childControlWidth = false;
//         healLayout.childControlHeight = true;
        
//         GameObject healLabel = CreateText(healAmountContainer.transform, "Label", "Heal:", 14);
//         SetSize(healLabel, 50, 40);
//         GameObject healInput = CreateInputField(healAmountContainer.transform, "HealAmountInput", "5");
//         SetSize(healInput, 140, 40);

//         GameObject deleteBtn = CreateButton(actionRow.transform, "DeleteButton", "X", new Color(0.8f, 0.2f, 0.2f));
//         SetSize(deleteBtn, 40, 40);

//         TikTokActionRow rowScript = actionRow.AddComponent<TikTokActionRow>();
        
//         SerializedObject so = new SerializedObject(rowScript);
//         so.FindProperty("actionTypeDropdown").objectReferenceValue = actionTypeDropdown.GetComponent<TMP_Dropdown>();
//         so.FindProperty("swordCountInput").objectReferenceValue = swordInput.GetComponent<TMP_InputField>();
//         so.FindProperty("healAmountInput").objectReferenceValue = healInput.GetComponent<TMP_InputField>();
//         so.FindProperty("deleteButton").objectReferenceValue = deleteBtn.GetComponent<Button>();
//         so.ApplyModifiedProperties();

//         string prefabPath = "Assets/_GAME/Prefabs/UI/TikTokActionRow.prefab";
//         System.IO.Directory.CreateDirectory("Assets/_GAME/Prefabs/UI");
//         PrefabUtility.SaveAsPrefabAsset(actionRow, prefabPath);
//         DestroyImmediate(actionRow);
        
//         Debug.Log($"Action Row Prefab created: {prefabPath}");
//     }

//     private static void CreateEventRowPrefab()
//     {
//         GameObject eventRow = new GameObject("TikTokEventRow");
        
//         RectTransform rect = eventRow.AddComponent<RectTransform>();
//         rect.sizeDelta = new Vector2(1100, 400);
        
//         Image bg = eventRow.AddComponent<Image>();
//         bg.color = new Color(0.2f, 0.2f, 0.2f);
        
//         VerticalLayoutGroup mainLayout = eventRow.AddComponent<VerticalLayoutGroup>();
//         mainLayout.spacing = 10;
//         mainLayout.padding = new RectOffset(15, 15, 15, 15);
//         mainLayout.childControlWidth = true;
//         mainLayout.childControlHeight = false;
//         mainLayout.childForceExpandWidth = true;

//         GameObject headerRow = new GameObject("HeaderRow");
//         headerRow.transform.SetParent(eventRow.transform, false);
//         RectTransform headerRect = headerRow.AddComponent<RectTransform>();
//         headerRect.sizeDelta = new Vector2(0, 50);
//         HorizontalLayoutGroup headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
//         headerLayout.spacing = 10;
//         headerLayout.childControlWidth = false;
//         headerLayout.childControlHeight = true;
//         headerLayout.childForceExpandHeight = true;

//         GameObject enabledToggle = CreateToggle(headerRow.transform, "EnabledToggle", "Enabled");
//         SetSize(enabledToggle, 100, 40);

//         GameObject eventTypeLabel = CreateText(headerRow.transform, "EventTypeLabel", "Event Type:", 14);
//         SetSize(eventTypeLabel, 100, 40);

//         GameObject eventTypeDropdown = CreateDropdown(headerRow.transform, "EventTypeDropdown", 
//             new string[] { "Like", "Comment", "Share", "Gift" });
//         SetSize(eventTypeDropdown, 150, 40);

//         GameObject deleteBtn = CreateButton(headerRow.transform, "DeleteEventButton", "Delete Event", new Color(0.8f, 0.2f, 0.2f));
//         SetSize(deleteBtn, 120, 40);

//         GameObject inputsRow = new GameObject("InputsRow");
//         inputsRow.transform.SetParent(eventRow.transform, false);
//         RectTransform inputsRect = inputsRow.AddComponent<RectTransform>();
//         inputsRect.sizeDelta = new Vector2(0, 50);
//         HorizontalLayoutGroup inputsLayout = inputsRow.AddComponent<HorizontalLayoutGroup>();
//         inputsLayout.spacing = 15;
//         inputsLayout.childControlWidth = false;
//         inputsLayout.childControlHeight = true;

//         GameObject likeContainer = CreateInputContainer(inputsRow.transform, "LikeThresholdContainer", "Like Threshold:", "5");
//         SetSize(likeContainer, 250, 40);

//         GameObject commentContainer = CreateInputContainer(inputsRow.transform, "CommentCommandContainer", "Command:", "1");
//         SetSize(commentContainer, 250, 40);

//         GameObject giftContainer = CreateInputContainer(inputsRow.transform, "GiftMinPriceContainer", "Min Price:", "10");
//         SetSize(giftContainer, 250, 40);

//         GameObject actionsHeader = CreateText(eventRow.transform, "ActionsHeader", "=== ACTIONS ===", 16);
//         RectTransform actionsHeaderRect = actionsHeader.GetComponent<RectTransform>();
//         actionsHeaderRect.sizeDelta = new Vector2(0, 30);

//         GameObject actionsScrollView = CreateActionsScrollView(eventRow.transform);
//         RectTransform actionsScrollRect = actionsScrollView.GetComponent<RectTransform>();
//         actionsScrollRect.sizeDelta = new Vector2(0, 200);

//         GameObject addActionBtn = CreateButton(eventRow.transform, "AddActionButton", "Add Action", new Color(0.3f, 0.6f, 1f));
//         RectTransform addActionRect = addActionBtn.GetComponent<RectTransform>();
//         addActionRect.sizeDelta = new Vector2(0, 40);

//         TikTokEventRow rowScript = eventRow.AddComponent<TikTokEventRow>();
        
//         SerializedObject so = new SerializedObject(rowScript);
//         so.FindProperty("enabledToggle").objectReferenceValue = enabledToggle.GetComponent<Toggle>();
//         so.FindProperty("eventTypeDropdown").objectReferenceValue = eventTypeDropdown.GetComponent<TMP_Dropdown>();
//         so.FindProperty("likeThresholdInput").objectReferenceValue = likeContainer.transform.Find("LikeThresholdInput").GetComponent<TMP_InputField>();
//         so.FindProperty("commentCommandInput").objectReferenceValue = commentContainer.transform.Find("CommentCommandInput").GetComponent<TMP_InputField>();
//         so.FindProperty("giftMinPriceInput").objectReferenceValue = giftContainer.transform.Find("GiftMinPriceInput").GetComponent<TMP_InputField>();
//         so.FindProperty("actionsParent").objectReferenceValue = actionsScrollView.transform.Find("Viewport/Content");
//         so.FindProperty("addActionButton").objectReferenceValue = addActionBtn.GetComponent<Button>();
//         so.FindProperty("deleteEventButton").objectReferenceValue = deleteBtn.GetComponent<Button>();
//         so.ApplyModifiedProperties();

//         string prefabPath = "Assets/_GAME/Prefabs/UI/TikTokEventRow.prefab";
//         PrefabUtility.SaveAsPrefabAsset(eventRow, prefabPath);
//         DestroyImmediate(eventRow);
        
//         Debug.Log($"Event Row Prefab created: {prefabPath}");
//     }

//     private static GameObject CreateActionsScrollView(Transform parent)
//     {
//         GameObject scrollView = new GameObject("ActionsScrollView");
//         scrollView.transform.SetParent(parent, false);
        
//         Image scrollBg = scrollView.AddComponent<Image>();
//         scrollBg.color = new Color(0.15f, 0.15f, 0.15f);
        
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
//         contentRect.sizeDelta = new Vector2(0, 500);
//         contentRect.anchoredPosition = Vector2.zero;
        
//         VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
//         layout.spacing = 5;
//         layout.padding = new RectOffset(5, 5, 5, 5);
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

//     private static GameObject CreateInputContainer(Transform parent, string name, string labelText, string placeholder)
//     {
//         GameObject container = new GameObject(name);
//         container.transform.SetParent(parent, false);
        
//         HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
//         layout.spacing = 5;
//         layout.childControlWidth = false;
//         layout.childControlHeight = true;
        
//         GameObject label = CreateText(container.transform, "Label", labelText, 14);
//         SetSize(label, 120, 40);
        
//         string inputName = name.Replace("Container", "");
//         GameObject input = CreateInputField(container.transform, inputName, placeholder);
//         SetSize(input, 120, 40);
        
//         return container;
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

//     private static GameObject CreateInputField(Transform parent, string name, string placeholder)
//     {
//         GameObject obj = new GameObject(name);
//         obj.transform.SetParent(parent, false);
        
//         Image bg = obj.AddComponent<Image>();
//         bg.color = new Color(0.1f, 0.1f, 0.1f);
        
//         TMP_InputField inputField = obj.AddComponent<TMP_InputField>();
        
//         GameObject textArea = new GameObject("TextArea");
//         textArea.transform.SetParent(obj.transform, false);
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
        
//         return obj;
//     }

//     private static GameObject CreateDropdown(Transform parent, string name, string[] options)
//     {
//         GameObject obj = new GameObject(name);
//         obj.transform.SetParent(parent, false);
        
//         Image bg = obj.AddComponent<Image>();
//         bg.color = new Color(0.1f, 0.1f, 0.1f);
        
//         TMP_Dropdown dropdown = obj.AddComponent<TMP_Dropdown>();
        
//         GameObject label = new GameObject("Label");
//         label.transform.SetParent(obj.transform, false);
//         TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
//         labelText.fontSize = 14;
//         labelText.color = Color.white;
//         labelText.alignment = TextAlignmentOptions.Center;
//         RectTransform labelRect = label.GetComponent<RectTransform>();
//         labelRect.anchorMin = Vector2.zero;
//         labelRect.anchorMax = Vector2.one;
//         labelRect.offsetMin = new Vector2(10, 0);
//         labelRect.offsetMax = new Vector2(-25, 0);
        
//         GameObject arrow = new GameObject("Arrow");
//         arrow.transform.SetParent(obj.transform, false);
//         Image arrowImg = arrow.AddComponent<Image>();
//         arrowImg.color = Color.white;
//         RectTransform arrowRect = arrow.GetComponent<RectTransform>();
//         arrowRect.anchorMin = new Vector2(1f, 0.5f);
//         arrowRect.anchorMax = new Vector2(1f, 0.5f);
//         arrowRect.sizeDelta = new Vector2(20, 20);
//         arrowRect.anchoredPosition = new Vector2(-15, 0);
        
//         GameObject template = new GameObject("Template");
//         template.transform.SetParent(obj.transform, false);
//         RectTransform templateRect = template.AddComponent<RectTransform>();
//         templateRect.anchorMin = new Vector2(0f, 0f);
//         templateRect.anchorMax = new Vector2(1f, 0f);
//         templateRect.pivot = new Vector2(0.5f, 1f);
//         templateRect.sizeDelta = new Vector2(0, 150);
//         templateRect.anchoredPosition = new Vector2(0, 2);
        
//         Image templateBg = template.AddComponent<Image>();
//         templateBg.color = new Color(0.1f, 0.1f, 0.1f);
        
//         ScrollRect templateScroll = template.AddComponent<ScrollRect>();
        
//         GameObject viewport = new GameObject("Viewport");
//         viewport.transform.SetParent(template.transform, false);
//         RectTransform viewportRect = viewport.AddComponent<RectTransform>();
//         viewportRect.anchorMin = Vector2.zero;
//         viewportRect.anchorMax = Vector2.one;
//         viewportRect.offsetMin = Vector2.zero;
//         viewportRect.offsetMax = Vector2.zero;
//         Image viewportMask = viewport.AddComponent<Image>();
//         Mask mask = viewport.AddComponent<Mask>();
//         mask.showMaskGraphic = false;
        
//         GameObject content = new GameObject("Content");
//         content.transform.SetParent(viewport.transform, false);
//         RectTransform contentRect = content.AddComponent<RectTransform>();
//         contentRect.anchorMin = new Vector2(0f, 1f);
//         contentRect.anchorMax = new Vector2(1f, 1f);
//         contentRect.pivot = new Vector2(0.5f, 1f);
//         contentRect.sizeDelta = new Vector2(0, 28);
        
//         GameObject item = new GameObject("Item");
//         item.transform.SetParent(content.transform, false);
//         RectTransform itemRect = item.AddComponent<RectTransform>();
//         itemRect.sizeDelta = new Vector2(0, 20);
        
//         Toggle itemToggle = item.AddComponent<Toggle>();
//         Image itemBg = item.AddComponent<Image>();
//         itemBg.color = new Color(0.2f, 0.2f, 0.2f);
        
//         GameObject itemLabel = new GameObject("Item Label");
//         itemLabel.transform.SetParent(item.transform, false);
//         TextMeshProUGUI itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
//         itemLabelText.fontSize = 14;
//         itemLabelText.color = Color.white;
//         RectTransform itemLabelRect = itemLabel.GetComponent<RectTransform>();
//         itemLabelRect.anchorMin = Vector2.zero;
//         itemLabelRect.anchorMax = Vector2.one;
//         itemLabelRect.offsetMin = new Vector2(10, 0);
//         itemLabelRect.offsetMax = new Vector2(-10, 0);
        
//         itemToggle.targetGraphic = itemBg;
//         itemToggle.isOn = true;
        
//         templateScroll.content = contentRect;
//         templateScroll.viewport = viewportRect;
//         templateScroll.horizontal = false;
//         templateScroll.vertical = true;
        
//         dropdown.targetGraphic = bg;
//         dropdown.template = templateRect;
//         dropdown.captionText = labelText;
//         dropdown.itemText = itemLabelText;
        
//         dropdown.options.Clear();
//         foreach (string option in options)
//         {
//             dropdown.options.Add(new TMP_Dropdown.OptionData(option));
//         }
        
//         template.SetActive(false);
        
//         return obj;
//     }

//     private static GameObject CreateToggle(Transform parent, string name, string labelText)
//     {
//         GameObject obj = new GameObject(name);
//         obj.transform.SetParent(parent, false);
        
//         Toggle toggle = obj.AddComponent<Toggle>();
        
//         GameObject bg = new GameObject("Background");
//         bg.transform.SetParent(obj.transform, false);
//         RectTransform bgRect = bg.AddComponent<RectTransform>();
//         bgRect.anchorMin = new Vector2(0f, 0.5f);
//         bgRect.anchorMax = new Vector2(0f, 0.5f);
//         bgRect.sizeDelta = new Vector2(20, 20);
//         bgRect.anchoredPosition = new Vector2(10, 0);
//         Image bgImg = bg.AddComponent<Image>();
//         bgImg.color = new Color(0.2f, 0.2f, 0.2f);
        
//         GameObject checkmark = new GameObject("Checkmark");
//         checkmark.transform.SetParent(bg.transform, false);
//         RectTransform checkRect = checkmark.AddComponent<RectTransform>();
//         checkRect.anchorMin = Vector2.zero;
//         checkRect.anchorMax = Vector2.one;
//         checkRect.offsetMin = Vector2.zero;
//         checkRect.offsetMax = Vector2.zero;
//         Image checkImg = checkmark.AddComponent<Image>();
//         checkImg.color = new Color(0.2f, 0.8f, 0.2f);
        
//         GameObject label = new GameObject("Label");
//         label.transform.SetParent(obj.transform, false);
//         RectTransform labelRect = label.AddComponent<RectTransform>();
//         labelRect.anchorMin = new Vector2(0f, 0f);
//         labelRect.anchorMax = new Vector2(1f, 1f);
//         labelRect.offsetMin = new Vector2(30, 0);
//         labelRect.offsetMax = Vector2.zero;
//         TextMeshProUGUI labelTMP = label.AddComponent<TextMeshProUGUI>();
//         labelTMP.text = labelText;
//         labelTMP.fontSize = 14;
//         labelTMP.color = Color.white;
//         labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        
//         toggle.targetGraphic = bgImg;
//         toggle.graphic = checkImg;
//         toggle.isOn = true;
        
//         return obj;
//     }

//     private static GameObject CreateButton(Transform parent, string name, string text, Color color)
//     {
//         GameObject obj = new GameObject(name);
//         obj.transform.SetParent(parent, false);
        
//         Image image = obj.AddComponent<Image>();
//         image.color = color;
        
//         Button button = obj.AddComponent<Button>();
        
//         GameObject textObj = CreateText(obj.transform, "Text", text, 14);
//         RectTransform textRect = textObj.GetComponent<RectTransform>();
//         textRect.anchorMin = Vector2.zero;
//         textRect.anchorMax = Vector2.one;
//         textRect.offsetMin = Vector2.zero;
//         textRect.offsetMax = Vector2.zero;
        
//         return obj;
//     }

//     private static void SetSize(GameObject obj, float width, float height)
//     {
//         RectTransform rect = obj.GetComponent<RectTransform>();
//         if (rect == null) rect = obj.AddComponent<RectTransform>();
//         rect.sizeDelta = new Vector2(width, height);
//     }
// }
