using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class LevelData
{
    [Tooltip("Level (1, 2, 3, ...)")]
    public int level;
    
    [Tooltip("Loại kiếm cho level này")]
    public SwordType swordType;
    
    [Tooltip("Thời gian duy trì level này (giây)")]
    public float duration;
    
    [Tooltip("Tốc độ di chuyển cho level này")]
    public float speed = 5f;
    
    [Tooltip("Chỉ số scale cơ thể (1.0 = bình thường)")]
    public float bodyScale = 1f;
}

[CreateAssetMenu(fileName = "CharacterLevelData", menuName = "Game/Character Level Data")]
public class CharacterLevelDataSO : ScriptableObject
{
    [System.Serializable]
    private class LevelDataSave
    {
        public List<LevelData> levels = new List<LevelData>();
    }

    [Tooltip("Danh sách các level và thông tin tương ứng")]
    public LevelData[] levels;

    private static string SaveFilePath
    {
        get
        {
#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "_GAME/So/CharacterLevelData.json");
#else
            return Path.Combine(Application.dataPath, "../CharacterLevelData.json");
#endif
        }
    }

    private void OnEnable()
    {
        LoadFromFile();
    }

    public LevelData GetLevelData(int level)
    {
        if (levels == null || levels.Length == 0) return null;

        foreach (var data in levels)
        {
            if (data.level == level)
                return data;
        }

        return null;
    }

    public SwordType GetSwordType(int level)
    {
        LevelData data = GetLevelData(level);
        return data != null ? data.swordType : SwordType.Default;
    }

    public float GetDuration(int level)
    {
        LevelData data = GetLevelData(level);
        return data != null ? data.duration : 0f;
    }

    public float GetSpeed(int level)
    {
        LevelData data = GetLevelData(level);
        return data != null ? data.speed : 5f;
    }

    public float GetBodyScale(int level)
    {
        LevelData data = GetLevelData(level);
        return data != null ? data.bodyScale : 1f;
    }

    public int GetMaxLevel()
    {
        if (levels == null || levels.Length == 0) return 1;

        int max = 1;
        foreach (var data in levels)
        {
            if (data.level > max)
                max = data.level;
        }
        return max;
    }

    public void SetLevelData(int level, SwordType swordType, float duration, float speed, float bodyScale)
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].level == level)
            {
                levels[i].swordType = swordType;
                levels[i].duration = duration;
                levels[i].speed = speed;
                levels[i].bodyScale = bodyScale;
                return;
            }
        }
    }

    public LevelData[] GetAllLevels()
    {
        return levels;
    }

    public void SaveToFile()
    {
        try
        {
            string directory = Path.GetDirectoryName(SaveFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            LevelDataSave saveData = new LevelDataSave();
            
            if (levels != null)
            {
                foreach (var level in levels)
                {
                    saveData.levels.Add(level);
                }
            }

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SaveFilePath, json);
            
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            
            Debug.Log($"Character Level Data saved to: {SaveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save Character Level Data: {e.Message}");
        }
    }

    public void LoadFromFile()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                LevelDataSave saveData = JsonUtility.FromJson<LevelDataSave>(json);
                
                if (saveData != null && saveData.levels != null && saveData.levels.Count > 0)
                {
                    for (int i = 0; i < saveData.levels.Count && i < levels.Length; i++)
                    {
                        levels[i] = saveData.levels[i];
                    }
                    
                    Debug.Log($"Character Level Data loaded from: {SaveFilePath}");
                }
            }
            else
            {
                Debug.Log("No saved Character Level Data found. Using default values.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load Character Level Data: {e.Message}");
        }
    }

    public void ResetToDefault()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i].duration = 10f;
            levels[i].speed = 5f;
            levels[i].bodyScale = 1f;
            levels[i].swordType = SwordType.Default;
        }
        SaveToFile();
    }
}
