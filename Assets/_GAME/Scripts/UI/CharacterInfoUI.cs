using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterInfoUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image hpFill;
    [SerializeField] private Image levelTimeFill;

    private CharacterBase character;

    private void Awake()
    {
        DisableRaycastTargets();
    }

    public void Init(string name, Sprite avatar, float currentHp, float maxHp)
    {
        if (nameText != null) nameText.text = name;
        if (avatarImage != null && avatar != null) avatarImage.sprite = avatar;
        UpdateHp(currentHp, maxHp);
    }

    public void SetCharacter(CharacterBase characterBase)
    {
        character = characterBase;
    }

    public void UpdateHp(float current, float max)
    {
        if (hpFill != null) hpFill.fillAmount = max > 0f ? current / max : 0f;
    }

    private void Update()
    {
        if (character != null && levelTimeFill != null)
        {
            float timeRemaining = character.LevelTimeRemaining;
            float duration = character.GetLevelDuration();
            
            if (duration > 0f)
            {
                float ratio = timeRemaining / duration;
                levelTimeFill.fillAmount = ratio;
                levelTimeFill.enabled = true;
            }
            else
            {
                levelTimeFill.enabled = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    private void DisableRaycastTargets()
    {
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }
}
