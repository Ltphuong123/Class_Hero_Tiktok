using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

[CreateAssetMenu(fileName = "SwordData", menuName = "Game/Sword Data")]
public class SwordDataSO : ScriptableObject
{
    [System.Serializable]
    public struct SwordEntry
    {
        public SwordType type;
        public Sprite sprite;
        public float maxHp;
        public float damage;
    }

    [System.Serializable]
    private class SwordDataSave
    {
        public List<SwordStatsSave> stats = new List<SwordStatsSave>();
    }

    [System.Serializable]
    private class SwordStatsSave
    {
        public SwordType type;
        public float maxHp;
        public float damage;
    }

    [SerializeField] private SwordEntry[] entries;

    private static string SaveFilePath
    {
        get
        {
#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "_GAME/So/SwordData.json");
#else
            return Path.Combine(Application.dataPath, "../SwordData.json");
#endif
        }
    }

    private void OnEnable()
    {
        LoadFromFile();
    }

    public Sprite GetSprite(SwordType type)
    {
        for (int i = 0; i < entries.Length; i++)
            if (entries[i].type == type) return entries[i].sprite;
        return null;
    }

    public SwordEntry GetEntry(SwordType type)
    {
        for (int i = 0; i < entries.Length; i++)
            if (entries[i].type == type) return entries[i];
        
        return new SwordEntry
        {
            type = type,
            sprite = null,
            maxHp = 100f,
            damage = 15f
        };
    }

    public float GetMaxHp(SwordType type)
    {
        var entry = GetEntry(type);
        return entry.maxHp > 0f ? entry.maxHp : 100f;
    }

    public float GetDamage(SwordType type)
    {
        var entry = GetEntry(type);
        return entry.damage > 0f ? entry.damage : 15f;
    }

    public void SetStats(SwordType type, float maxHp, float damage)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].type == type)
            {
                entries[i].maxHp = maxHp;
                entries[i].damage = damage;
                return;
            }
        }
    }

    public SwordEntry[] GetAllEntries()
    {
        return entries;
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

            SwordDataSave saveData = new SwordDataSave();
            
            foreach (var entry in entries)
            {
                saveData.stats.Add(new SwordStatsSave
                {
                    type = entry.type,
                    maxHp = entry.maxHp,
                    damage = entry.damage
                });
            }

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SaveFilePath, json);
            
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            
            Debug.Log($"Sword Data saved to: {SaveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save Sword Data: {e.Message}");
        }
    }

    public void LoadFromFile()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                SwordDataSave saveData = JsonUtility.FromJson<SwordDataSave>(json);
                
                if (saveData != null && saveData.stats != null)
                {
                    foreach (var stat in saveData.stats)
                    {
                        SetStats(stat.type, stat.maxHp, stat.damage);
                    }
                    
                    Debug.Log($"Sword Data loaded from: {SaveFilePath}");
                }
            }
            else
            {
                Debug.Log("No saved Sword Data found. Using default values.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load Sword Data: {e.Message}");
        }
    }

    public void ResetToDefault()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i].maxHp = 100f;
            entries[i].damage = 15f;
        }
        SaveToFile();
    }
}
