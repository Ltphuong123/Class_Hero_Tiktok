using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterLevelRow : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TMP_Dropdown swordTypeDropdown;
    [SerializeField] private TMP_InputField durationInput;
    [SerializeField] private TMP_InputField speedInput;
    [SerializeField] private TMP_InputField bodyScaleInput;

    private int level;
    private SwordType swordType;
    private float duration;
    private float speed;
    private float bodyScale;

    public int Level => level;
    public SwordType SwordType => swordType;
    public float Duration => duration;
    public float Speed => speed;
    public float BodyScale => bodyScale;

    public void Initialize(int lvl, SwordType type, float dur, float spd, float scale)
    {
        level = lvl;
        swordType = type;
        duration = dur;
        speed = spd;
        bodyScale = scale;

        if (levelText != null)
            levelText.text = $"Level {lvl}";

        if (swordTypeDropdown != null)
        {
            swordTypeDropdown.ClearOptions();
            
            var options = new System.Collections.Generic.List<string>();
            options.Add("Default");
            options.Add("Fire");
            options.Add("Lightning");
            options.Add("Miasma");
            options.Add("Snow");
            
            swordTypeDropdown.AddOptions(options);
            swordTypeDropdown.value = (int)type;
            swordTypeDropdown.onValueChanged.AddListener(OnSwordTypeChanged);
        }

        if (durationInput != null)
        {
            durationInput.text = dur.ToString("F1");
            durationInput.onEndEdit.AddListener(OnDurationChanged);
        }

        if (speedInput != null)
        {
            speedInput.text = spd.ToString("F1");
            speedInput.onEndEdit.AddListener(OnSpeedChanged);
        }

        if (bodyScaleInput != null)
        {
            bodyScaleInput.text = scale.ToString("F2");
            bodyScaleInput.onEndEdit.AddListener(OnBodyScaleChanged);
        }
    }

    private void OnSwordTypeChanged(int value)
    {
        swordType = (SwordType)value;
    }

    private void OnDurationChanged(string value)
    {
        if (float.TryParse(value, out float newValue))
        {
            duration = Mathf.Max(0f, newValue);
            durationInput.text = duration.ToString("F1");
        }
        else
        {
            durationInput.text = duration.ToString("F1");
        }
    }

    private void OnSpeedChanged(string value)
    {
        if (float.TryParse(value, out float newValue))
        {
            speed = Mathf.Max(0.1f, newValue);
            speedInput.text = speed.ToString("F1");
        }
        else
        {
            speedInput.text = speed.ToString("F1");
        }
    }

    private void OnBodyScaleChanged(string value)
    {
        if (float.TryParse(value, out float newValue))
        {
            bodyScale = Mathf.Max(0.1f, newValue);
            bodyScaleInput.text = bodyScale.ToString("F2");
        }
        else
        {
            bodyScaleInput.text = bodyScale.ToString("F2");
        }
    }

    private void OnDestroy()
    {
        if (swordTypeDropdown != null)
            swordTypeDropdown.onValueChanged.RemoveListener(OnSwordTypeChanged);

        if (durationInput != null)
            durationInput.onEndEdit.RemoveListener(OnDurationChanged);

        if (speedInput != null)
            speedInput.onEndEdit.RemoveListener(OnSpeedChanged);

        if (bodyScaleInput != null)
            bodyScaleInput.onEndEdit.RemoveListener(OnBodyScaleChanged);
    }
}
