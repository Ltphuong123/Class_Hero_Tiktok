using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioVolumeUI : MonoBehaviour
{
    [SerializeField] private Slider           volumeSlider;
    [SerializeField] private TextMeshProUGUI  valueText;

    private void Start()
    {
        float saved = CharacterAudioSource.LoadMasterVolume();

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value    = saved;
            volumeSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        UpdateValueText(saved);
    }

    private void OnSliderChanged(float value)
    {
        CharacterAudioSource.SetMasterVolume(value);
        UpdateValueText(value);
    }

    private void UpdateValueText(float value)
    {
        if (valueText != null)
            valueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
