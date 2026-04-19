using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterInfoUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image hpFill;

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

    public void UpdateHp(float current, float max)
    {
        if (hpFill != null) hpFill.fillAmount = max > 0f ? current / max : 0f;
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
