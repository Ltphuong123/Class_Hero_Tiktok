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
    [SerializeField] private TMP_InputField damageReductionBonusInput;
    [SerializeField] private TMP_InputField lifestealBonusInput;
    [SerializeField] private TMP_InputField orbitSpeedBonusInput;

    private SwordType swordType;
    private float maxHp;
    private float damage;
    private float damageReductionBonus;
    private float lifestealBonus;
    private float orbitSpeedBonus;

    public SwordType SwordType => swordType;
    public float MaxHp => maxHp;
    public float Damage => damage;
    public float DamageReductionBonus => damageReductionBonus;
    public float LifestealBonus => lifestealBonus;
    public float OrbitSpeedBonus => orbitSpeedBonus;

    public void Initialize(SwordType type, Sprite icon, float hp, float dmg,
        float dmgReductionBonus = 0f, float lifesteal = 0f, float orbitSpeed = 0f)
    {
        swordType = type;
        maxHp = hp;
        damage = dmg;
        damageReductionBonus = dmgReductionBonus;
        lifestealBonus = lifesteal;
        orbitSpeedBonus = orbitSpeed;

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

        if (damageReductionBonusInput != null)
        {
            damageReductionBonusInput.text = (dmgReductionBonus * 100f).ToString("F1");
            damageReductionBonusInput.onEndEdit.AddListener(OnDamageReductionBonusChanged);
        }

        if (lifestealBonusInput != null)
        {
            lifestealBonusInput.text = (lifesteal * 100f).ToString("F1");
            lifestealBonusInput.onEndEdit.AddListener(OnLifestealBonusChanged);
        }

        if (orbitSpeedBonusInput != null)
        {
            orbitSpeedBonusInput.text = (orbitSpeed * 100f).ToString("F1");
            orbitSpeedBonusInput.onEndEdit.AddListener(OnOrbitSpeedBonusChanged);
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

    private void OnDamageReductionBonusChanged(string value)
    {
        if (float.TryParse(value, out float newValue))
        {
            damageReductionBonus = newValue / 100f;
            damageReductionBonusInput.text = (damageReductionBonus * 100f).ToString("F1");
        }
        else
        {
            damageReductionBonusInput.text = (damageReductionBonus * 100f).ToString("F1");
        }
    }

    private void OnLifestealBonusChanged(string value)
    {
        if (float.TryParse(value, out float newValue))
        {
            lifestealBonus = newValue / 100f;
            lifestealBonusInput.text = (lifestealBonus * 100f).ToString("F1");
        }
        else
        {
            lifestealBonusInput.text = (lifestealBonus * 100f).ToString("F1");
        }
    }

    private void OnOrbitSpeedBonusChanged(string value)
    {
        if (float.TryParse(value, out float newValue))
        {
            orbitSpeedBonus = newValue / 100f;
            orbitSpeedBonusInput.text = (orbitSpeedBonus * 100f).ToString("F1");
        }
        else
        {
            orbitSpeedBonusInput.text = (orbitSpeedBonus * 100f).ToString("F1");
        }
    }

    private void OnDestroy()
    {
        if (maxHpInput != null)
            maxHpInput.onEndEdit.RemoveListener(OnMaxHpChanged);

        if (damageInput != null)
            damageInput.onEndEdit.RemoveListener(OnDamageChanged);

        if (damageReductionBonusInput != null)
            damageReductionBonusInput.onEndEdit.RemoveListener(OnDamageReductionBonusChanged);

        if (lifestealBonusInput != null)
            lifestealBonusInput.onEndEdit.RemoveListener(OnLifestealBonusChanged);

        if (orbitSpeedBonusInput != null)
            orbitSpeedBonusInput.onEndEdit.RemoveListener(OnOrbitSpeedBonusChanged);
    }
}
