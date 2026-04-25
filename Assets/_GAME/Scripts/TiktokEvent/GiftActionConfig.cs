using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
public enum GiftActionType
{
    AddSwords,
    UpgradeLevel2,
    UpgradeLevel3,
    UpgradeLevel4,
    UpgradeLevel5,
    RespawnCharacter,
    ActivateMagnet,
    ActivateShield,
    ActivateMeteor
}

[Serializable]
public class GiftActionThreshold
{
    public int priceThreshold;
    public GiftActionType actionType;

    public GiftActionThreshold(int price, GiftActionType action)
    {
        priceThreshold = price;
        actionType = action;
    }
}

[Serializable]
public class GiftActionConfigData
{
    public List<GiftActionThreshold> thresholds;
}

[CreateAssetMenu(fileName = "GiftActionConfig", menuName = "Game/Gift Action Config")]
public class GiftActionConfig : ScriptableObject
{
    [Header("Gift Action Thresholds")]
    [Tooltip("Danh sách các mốc giá và hành động tương ứng. Sắp xếp từ cao xuống thấp.")]
    public List<GiftActionThreshold> thresholds = new List<GiftActionThreshold>
    {
        new GiftActionThreshold(60, GiftActionType.ActivateMeteor),
        new GiftActionThreshold(50, GiftActionType.ActivateShield),
        new GiftActionThreshold(40, GiftActionType.ActivateMagnet),
        new GiftActionThreshold(30, GiftActionType.RespawnCharacter),
        new GiftActionThreshold(20, GiftActionType.UpgradeLevel5),
        new GiftActionThreshold(15, GiftActionType.UpgradeLevel4),
        new GiftActionThreshold(10, GiftActionType.UpgradeLevel3),
        new GiftActionThreshold(5, GiftActionType.UpgradeLevel2),
        new GiftActionThreshold(0, GiftActionType.AddSwords)
    };

    private static string SaveFilePath
    {
        get
        {
#if UNITY_EDITOR
            // Trong Editor: lưu vào Assets/_GAME/So
            return Path.Combine(Application.dataPath, "_GAME/So/GiftActionConfig.json");
#else
            // Trong Build: lưu vào thư mục game
            return Path.Combine(Application.dataPath, "../GiftActionConfig.json");
#endif
        }
    }

    private void OnEnable()
    {
        LoadFromFile();
    }

    public void SortThresholds()
    {
        thresholds.Sort((a, b) => b.priceThreshold.CompareTo(a.priceThreshold));
    }

    public GiftActionType GetActionForPrice(int price)
    {
        SortThresholds();
        
        foreach (var threshold in thresholds)
        {
            if (price >= threshold.priceThreshold)
                return threshold.actionType;
        }
        
        return GiftActionType.AddSwords;
    }

    public void SaveToFile()
    {
        try
        {
            // Tạo folder nếu chưa tồn tại
            string directory = Path.GetDirectoryName(SaveFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            GiftActionConfigData data = new GiftActionConfigData
            {
                thresholds = new List<GiftActionThreshold>(thresholds)
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json);
            
#if UNITY_EDITOR
            // Refresh AssetDatabase để file hiện trong Unity Editor
            UnityEditor.AssetDatabase.Refresh();
#endif
            
            Debug.Log($"Gift Action Config saved to: {SaveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save Gift Action Config: {e.Message}");
        }
    }

    public void LoadFromFile()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                GiftActionConfigData data = JsonUtility.FromJson<GiftActionConfigData>(json);
                
                if (data != null && data.thresholds != null && data.thresholds.Count > 0)
                {
                    thresholds = data.thresholds;
                    Debug.Log($"Gift Action Config loaded from: {SaveFilePath}");
                }
            }
            else
            {
                Debug.Log("No saved Gift Action Config found. Using default values.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load Gift Action Config: {e.Message}");
        }
    }

    public void ResetToDefault()
    {
        thresholds = new List<GiftActionThreshold>
        {
            new GiftActionThreshold(60, GiftActionType.ActivateMeteor),
            new GiftActionThreshold(50, GiftActionType.ActivateShield),
            new GiftActionThreshold(40, GiftActionType.ActivateMagnet),
            new GiftActionThreshold(30, GiftActionType.RespawnCharacter),
            new GiftActionThreshold(20, GiftActionType.UpgradeLevel5),
            new GiftActionThreshold(15, GiftActionType.UpgradeLevel4),
            new GiftActionThreshold(10, GiftActionType.UpgradeLevel3),
            new GiftActionThreshold(5, GiftActionType.UpgradeLevel2),
            new GiftActionThreshold(0, GiftActionType.AddSwords)
        };
        SaveToFile();
    }
}
