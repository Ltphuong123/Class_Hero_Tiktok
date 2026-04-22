using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class LeaderboardRow : MonoBehaviour, IPointerClickHandler
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI swordCountText;

    [Header("Images")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image levelIcon;
    [SerializeField] private Image levelTimeFill;

    [Header("Level Icons")]
    [SerializeField] private Sprite[] levelSprites;

    private float cachedHp = -1f;
    private int cachedSwordCount = -1;
    private int cachedLevel = -1;
    private CharacterBase currentCharacter;

    public static event System.Action<CharacterBase> OnRowClicked;

    public void SetData(CharacterRankData data)
    {
        currentCharacter = data.Character;

        if (nameText != null)
            nameText.text = data.Name;

        if (levelIcon != null && cachedLevel != data.Level)
        {
            int index = data.Level - 1;
            if (levelSprites != null && index >= 0 && index < levelSprites.Length)
            {
                levelIcon.sprite = levelSprites[index];
                levelIcon.enabled = true;
            }
            else
            {
                levelIcon.enabled = false;
            }
            cachedLevel = data.Level;
        }

        if (levelTimeFill != null)
        {
            float duration = currentCharacter != null ? currentCharacter.GetLevelDuration() : 0f;
            
            if (duration > 0f && data.LevelTimeRemaining > 0f)
            {
                float ratio = 1-data.LevelTimeRemaining / duration;
                levelTimeFill.fillAmount = ratio;
                levelTimeFill.enabled = true;
            }
            else
            {
                levelTimeFill.enabled = false;
            }
        }

        if (hpText != null && !Mathf.Approximately(cachedHp, data.CurrentHp))
        {
            hpText.text = $"{data.CurrentHp:F0}/{data.MaxHp:F0}";
            cachedHp = data.CurrentHp;
        }

        if (swordCountText != null && cachedSwordCount != data.SwordCount)
        {
            swordCountText.text = $"⚔ {data.SwordCount}";
            cachedSwordCount = data.SwordCount;
        }

        if (avatarImage != null && avatarImage.sprite != data.Avatar)
        {
            avatarImage.enabled = data.Avatar != null;
            if (data.Avatar != null) avatarImage.sprite = data.Avatar;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentCharacter != null)
            OnRowClicked?.Invoke(currentCharacter);
    }
}
