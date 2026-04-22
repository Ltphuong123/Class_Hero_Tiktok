using UnityEngine;

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
    [Tooltip("Danh sách các level và thông tin tương ứng")]
    public LevelData[] levels;

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
}
