using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SwordDataRow : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI typeNameText;
    [SerializeField] private TMP_InputField maxHpInput;
    [SerializeField] private TMP_InputField damageInput;

    private SwordType swordType;
    private float maxHp;
    private float damage;

    public SwordType SwordType => swordType;
    public float MaxHp => maxHp;
    public float Damage => damage;

    public void Initialize(SwordType type, Sprite icon, float hp, float dmg)
    {
        swordType = type;
        maxHp = hp;
        damage = dmg;

        if (typeNameText != null)
            typeNameText.text = type.ToString();

        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        if (maxHpInput != null)
        {
            maxHpInput.text = hp.ToString("F0");
            maxHpInput.onEndEdit.AddListener(OnMaxHpChanged);
        }

        if (damageInput != null)
        {
            damageInput.text = dmg.ToString("F0");
            damageInput.onEndEdit.AddListener(OnDamageChanged);
        }
    }

    private void OnMaxHpChanged(string value)
    {
        if (float.TryParse(value, out float newValue))
        {
            maxHp = Mathf.Max(1f, newValue);
            maxHpInput.text = maxHp.ToString("F0");
        }
        else
        {
            maxHpInput.text = maxHp.ToString("F0");
        }
    }

    private void OnDamageChanged(string value)
    {
        if (float.TryParse(value, out float newValue))
        {
            damage = Mathf.Max(1f, newValue);
            damageInput.text = damage.ToString("F0");
        }
        else
        {
            damageInput.text = damage.ToString("F0");
        }
    }

    private void OnDestroy()
    {
        if (maxHpInput != null)
            maxHpInput.onEndEdit.RemoveListener(OnMaxHpChanged);

        if (damageInput != null)
            damageInput.onEndEdit.RemoveListener(OnDamageChanged);
    }
}
