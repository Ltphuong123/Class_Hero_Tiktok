using UnityEngine;

[CreateAssetMenu(fileName = "SwordData", menuName = "Game/Sword Data")]
public class SwordDataSO : ScriptableObject
{
    [System.Serializable]
    public struct SwordEntry
    {
        public SwordType type;
        public Sprite sprite;
    }

    [SerializeField] private SwordEntry[] entries;

    public Sprite GetSprite(SwordType type)
    {
        for (int i = 0; i < entries.Length; i++)
            if (entries[i].type == type) return entries[i].sprite;
        return null;
    }
}
