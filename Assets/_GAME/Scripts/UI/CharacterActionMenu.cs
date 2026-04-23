using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterActionMenu : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject menuPanel;

    [Header("Buttons")]
    [SerializeField] private Button addSwordButton;
    [SerializeField] private Button magnetBoosterButton;
    [SerializeField] private Button shieldBoosterButton;
    [SerializeField] private Button freezeBoosterButton;
    [SerializeField] private Button meteorBoosterButton;
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private Button level3Button;
    [SerializeField] private Button level4Button;
    [SerializeField] private Button level5Button;
    [SerializeField] private Button closeButton;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Header("Settings")]
    [SerializeField] private int swordsToAdd = 1;

    private CharacterBase currentCharacter;

    private void Awake()
    {
        if (addSwordButton != null)
            addSwordButton.onClick.AddListener(OnAddSwordClicked);

        if (magnetBoosterButton != null)
            magnetBoosterButton.onClick.AddListener(OnMagnetBoosterClicked);

        if (shieldBoosterButton != null)
            shieldBoosterButton.onClick.AddListener(OnShieldBoosterClicked);

        if (freezeBoosterButton != null)
            freezeBoosterButton.onClick.AddListener(OnFreezeBoosterClicked);

        if (meteorBoosterButton != null)
            meteorBoosterButton.onClick.AddListener(OnMeteorBoosterClicked);

        if (level1Button != null)
            level1Button.onClick.AddListener(() => OnLevelButtonClicked(1));

        if (level2Button != null)
            level2Button.onClick.AddListener(() => OnLevelButtonClicked(2));

        if (level3Button != null)
            level3Button.onClick.AddListener(() => OnLevelButtonClicked(3));

        if (level4Button != null)
            level4Button.onClick.AddListener(() => OnLevelButtonClicked(4));

        if (level5Button != null)
            level5Button.onClick.AddListener(() => OnLevelButtonClicked(5));

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        Hide();
    }

    private void OnDestroy()
    {
        if (addSwordButton != null)
            addSwordButton.onClick.RemoveListener(OnAddSwordClicked);

        if (magnetBoosterButton != null)
            magnetBoosterButton.onClick.RemoveListener(OnMagnetBoosterClicked);

        if (shieldBoosterButton != null)
            shieldBoosterButton.onClick.RemoveListener(OnShieldBoosterClicked);

        if (freezeBoosterButton != null)
            freezeBoosterButton.onClick.RemoveListener(OnFreezeBoosterClicked);

        if (meteorBoosterButton != null)
            meteorBoosterButton.onClick.RemoveListener(OnMeteorBoosterClicked);

        if (level1Button != null)
            level1Button.onClick.RemoveAllListeners();

        if (level2Button != null)
            level2Button.onClick.RemoveAllListeners();

        if (level3Button != null)
            level3Button.onClick.RemoveAllListeners();

        if (level4Button != null)
            level4Button.onClick.RemoveAllListeners();

        if (level5Button != null)
            level5Button.onClick.RemoveAllListeners();

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
    }

    public void Show(CharacterBase character)
    {
        if (character == null) return;

        currentCharacter = character;

        if (characterNameText != null)
            characterNameText.text = character.CharacterName;

        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    public void Hide()
    {
        currentCharacter = null;

        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    private void OnAddSwordClicked()
    {
        if (currentCharacter == null) return;

        SwordOrbit orbit = currentCharacter.GetSwordOrbit();
        if (orbit == null) return;

        for (int i = 0; i < swordsToAdd; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle.normalized * 2f;
            Vector3 spawnPos = currentCharacter.TF.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
            Sword sword = ItemManager.Instance.Spawn(spawnPos);
            
            if (sword != null)
                sword.Collect(currentCharacter);
        }
    }

    private void OnMagnetBoosterClicked()
    {
        if (currentCharacter == null) return;
        currentCharacter.ActivateMagnetBooster();
    }

    private void OnShieldBoosterClicked()
    {
        if (currentCharacter == null) return;
        currentCharacter.ActivateShieldBooster();
    }

    private void OnFreezeBoosterClicked()
    {
        if (currentCharacter == null) return;
        currentCharacter.ActivateFreezeBooster();
    }

    private void OnMeteorBoosterClicked()
    {
        if (currentCharacter == null) return;
        currentCharacter.ActivateMeteorBooster();
    }

    private void OnLevelButtonClicked(int level)
    {
        if (currentCharacter == null) return;

        CharacterLevelDataSO levelData = currentCharacter.GetLevelData();

        if (levelData != null)
        {
            float duration = levelData.GetDuration(level);
            currentCharacter.AddLevelReserveTime(level, duration);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && menuPanel != null && menuPanel.activeSelf)
            Hide();
    }
}
