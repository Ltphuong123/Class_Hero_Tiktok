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

    [Header("Lifesteal Settings")]
    [Tooltip("Tỷ lệ hút máu khi gây sát thương (0.0 - 1.0)")]
    public float lifestealPercent = 0.2f;

    [Header("Elemental Orbit Speed")]
    [Tooltip("Tốc độ quay orbit Kim (tối thiểu 30)")]
    public float kimOrbitSpeed  = 180f;
    [Tooltip("Tốc độ quay orbit Mộc (tối thiểu 30)")]
    public float mocOrbitSpeed  = 180f;
    [Tooltip("Tốc độ quay orbit Thủy (tối thiểu 30)")]
    public float thuyOrbitSpeed = 180f;
    [Tooltip("Tốc độ quay orbit Hỏa (tối thiểu 30)")]
    public float hoaOrbitSpeed  = 180f;
    [Tooltip("Tốc độ quay orbit Thổ (tối thiểu 30)")]
    public float thoOrbitSpeed  = 180f;

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
            lifestealPercent = this.lifestealPercent,
            kimOrbitSpeed  = this.kimOrbitSpeed,
            mocOrbitSpeed  = this.mocOrbitSpeed,
            thuyOrbitSpeed = this.thuyOrbitSpeed,
            hoaOrbitSpeed  = this.hoaOrbitSpeed,
            thoOrbitSpeed  = this.thoOrbitSpeed,
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
            this.lifestealPercent = data.lifestealPercent;
            this.kimOrbitSpeed  = data.kimOrbitSpeed  > 0f ? data.kimOrbitSpeed  : 180f;
            this.mocOrbitSpeed  = data.mocOrbitSpeed  > 0f ? data.mocOrbitSpeed  : 180f;
            this.thuyOrbitSpeed = data.thuyOrbitSpeed > 0f ? data.thuyOrbitSpeed : 180f;
            this.hoaOrbitSpeed  = data.hoaOrbitSpeed  > 0f ? data.hoaOrbitSpeed  : 180f;
            this.thoOrbitSpeed  = data.thoOrbitSpeed  > 0f ? data.thoOrbitSpeed  : 180f;
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
        public float lifestealPercent;
        public float kimOrbitSpeed;
        public float mocOrbitSpeed;
        public float thuyOrbitSpeed;
        public float hoaOrbitSpeed;
        public float thoOrbitSpeed;
    }
}
