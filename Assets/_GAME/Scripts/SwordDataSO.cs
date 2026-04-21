using UnityEngine;

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
}
