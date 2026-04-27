using UnityEngine;
using System.IO;

[CreateAssetMenu(fileName = "CharacterBaseConfig", menuName = "Game/Character Base Config")]
public class CharacterBaseConfigSO : ScriptableObject
{
    [Header("Health Settings")]
    [Tooltip("Máu tối đa")]
    public float maxHp = 100f;
    
    [Tooltip("Ngưỡng overheal (máu vượt quá maxHp)")]
    public float overhealThreshold = 50f;
    
    [Tooltip("Tỷ lệ scale cơ thể mỗi ngưỡng overheal")]
    public float overhealScalePerThreshold = 0.1f;

    [Header("Sword Settings")]
    [Tooltip("Số kiếm tối đa")]
    public int maxSwordCount = 20;
    
    [Tooltip("Hàng đợi kiếm tối đa")]
    public int maxSwordQueue = 50;

    [Header("Meteor Settings")]
    [Tooltip("Sát thương meteor")]
    public float meteorDamage = 50f;

    [Header("AI Combat Settings")]
    [Tooltip("Tự động lock target khi bị tấn công")]
    public bool enableAutoLockOnAttacked = false;
    
    [Tooltip("Tự động unlock target khi hết kiếm (chỉ auto lock)")]
    public bool enableAutoUnlockOnNoSwords = true;

    private static CharacterBaseConfigSO instance;
    public static CharacterBaseConfigSO Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<CharacterBaseConfigSO>("CharacterBaseConfig");
                if (instance != null)
                    instance.LoadFromJson();
            }
            return instance;
        }
    }

    private void OnEnable()
    {
        LoadFromJson();
    }

    private void Awake()
    {
        LoadFromJson();
    }

    private string GetFilePath()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "_GAME/So/CharacterBaseConfig.json");
#else
        return Path.Combine(Application.dataPath, "../CharacterBaseConfig.json");
#endif
    }

    public void SaveToJson()
    {
        string filePath = GetFilePath();
        
        CharacterBaseConfigData data = new CharacterBaseConfigData
        {
            maxHp = this.maxHp,
            overhealThreshold = this.overhealThreshold,
            overhealScalePerThreshold = this.overhealScalePerThreshold,
            maxSwordCount = this.maxSwordCount,
            maxSwordQueue = this.maxSwordQueue,
            meteorDamage = this.meteorDamage,
            enableAutoLockOnAttacked = this.enableAutoLockOnAttacked,
            enableAutoUnlockOnNoSwords = this.enableAutoUnlockOnNoSwords
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
        
        Debug.Log($"Character Base Config saved to: {filePath}");
    }

    public void LoadFromJson()
    {
        string filePath = GetFilePath();
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Config file not found: {filePath}. Using default values.");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            CharacterBaseConfigData data = JsonUtility.FromJson<CharacterBaseConfigData>(json);

            this.maxHp = data.maxHp;
            this.overhealThreshold = data.overhealThreshold;
            this.overhealScalePerThreshold = data.overhealScalePerThreshold;
            this.maxSwordCount = data.maxSwordCount;
            this.maxSwordQueue = data.maxSwordQueue;
            this.meteorDamage = data.meteorDamage;
            this.enableAutoLockOnAttacked = data.enableAutoLockOnAttacked;
            this.enableAutoUnlockOnNoSwords = data.enableAutoUnlockOnNoSwords;
            
            // Đồng bộ với biến static trong CharacterBase
            CharacterBase.EnableAutoLockOnAttacked = this.enableAutoLockOnAttacked;
            CharacterBase.EnableAutoUnlockOnNoSwords = this.enableAutoUnlockOnNoSwords;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            
            Debug.Log($"Character Base Config loaded from: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load Character Base Config: {e.Message}");
        }
    }

    [System.Serializable]
    private class CharacterBaseConfigData
    {
        public float maxHp;
        public float overhealThreshold;
        public float overhealScalePerThreshold;
        public int maxSwordCount;
        public int maxSwordQueue;
        public float meteorDamage;
        public bool enableAutoLockOnAttacked;
        public bool enableAutoUnlockOnNoSwords;
    }
}
