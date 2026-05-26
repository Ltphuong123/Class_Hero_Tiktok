using UnityEngine;
using TMPro;

public class SkillDataRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TMP_InputField  damageInput;

    private SkillData skillData;
    private string    initialDamageText;

    public SkillData SkillData => skillData;
    public float     Damage    { get; private set; }

    public void Initialize(SkillData data)
    {
        skillData = data;
        Damage    = data.damage;

        if (skillNameText != null)
            skillNameText.text = data.name;

        initialDamageText = data.damage.ToString("F1");
        if (damageInput == null) return;
        damageInput.text = initialDamageText;
        damageInput.onEndEdit.AddListener(value =>
        {
            if (float.TryParse(value, out float parsed))
            {
                Damage           = Mathf.Max(0f, parsed);
                damageInput.text = Damage.ToString("F1");
            }
            else
            {
                damageInput.text = initialDamageText;
            }
        });
    }
}
