using UnityEngine;

[CreateAssetMenu(fileName = "SwordData", menuName = "Game/Sword Data")]
public class SwordDataSO : ScriptableObject
{
    [System.Serializable]
    public struct SwordEntry
    {
        public SwordType type;
        public Sprite sprite;
        [Header("Stats")]
        public float maxHp;           // HP tối đa của kiếm
        public float damage;          // Damage gây cho character
        public float swordDamage;     // Damage gây cho kiếm khác khi va chạm
    }

    [SerializeField] private SwordEntry[] entries;

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
        
        // Default values nếu không tìm thấy
        return new SwordEntry
        {
            type = type,
            sprite = null,
            maxHp = 100f,
            damage = 10f,
            swordDamage = 20f
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
        return entry.damage > 0f ? entry.damage : 10f;
    }

    public float GetSwordDamage(SwordType type)
    {
        var entry = GetEntry(type);
        return entry.swordDamage > 0f ? entry.swordDamage : 20f;
    }
}
