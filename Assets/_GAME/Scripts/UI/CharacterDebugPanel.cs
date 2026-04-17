using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Debug panel hiển thị khi click vào LeaderboardRow.
/// Cho phép cheat: thêm kiếm, máu, đổi loại kiếm, tăng tốc độ.
/// </summary>
public class CharacterDebugPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Buttons")]
    [SerializeField] private Button addSwordButton;
    [SerializeField] private Button addHealthButton;
    [SerializeField] private Button changeSpeedButton;
    [SerializeField] private Button healSwordsButton;
    [SerializeField] private Button closeButton;

    [Header("Sword Type Buttons")]
    [SerializeField] private Button swordDefaultButton;
    [SerializeField] private Button swordFireButton;
    [SerializeField] private Button swordLightningButton;
    [SerializeField] private Button swordMiasmaButton;
    [SerializeField] private Button swordSnowButton;

    [Header("Settings")]
    [SerializeField] private int swordsToAdd = 1;
    [SerializeField] private float healthToAdd = 20f;
    [SerializeField] private float speedMultiplier = 1.5f;

    private CharacterBase targetCharacter;

    private void Awake()
    {
        // Setup button listeners
        if (addSwordButton != null)
            addSwordButton.onClick.AddListener(OnAddSword);
        
        if (addHealthButton != null)
            addHealthButton.onClick.AddListener(OnAddHealth);
        
        if (changeSpeedButton != null)
            changeSpeedButton.onClick.AddListener(OnChangeSpeed);
        
        if (healSwordsButton != null)
            healSwordsButton.onClick.AddListener(OnHealSwords);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        // Sword type buttons
        if (swordDefaultButton != null)
            swordDefaultButton.onClick.AddListener(() => OnChangeSwordType(SwordType.Default));
        
        if (swordFireButton != null)
            swordFireButton.onClick.AddListener(() => OnChangeSwordType(SwordType.Fire));
        
        if (swordLightningButton != null)
            swordLightningButton.onClick.AddListener(() => OnChangeSwordType(SwordType.Lightning));
        
        if (swordMiasmaButton != null)
            swordMiasmaButton.onClick.AddListener(() => OnChangeSwordType(SwordType.Miasma));
        
        if (swordSnowButton != null)
            swordSnowButton.onClick.AddListener(() => OnChangeSwordType(SwordType.Snow));

        Hide();
    }

    public void Show(CharacterBase character)
    {
        if (character == null) return;

        targetCharacter = character;
        
        if (titleText != null)
            titleText.text = $"Debug: {character.CharacterName}";

        if (panel != null)
            panel.SetActive(true);
    }

    public void Hide()
    {
        targetCharacter = null;
        if (panel != null)
            panel.SetActive(false);
    }

    private void OnAddSword()
    {
        if (targetCharacter == null) return;

        SwordOrbit orbit = targetCharacter.GetSwordOrbit();
        if (orbit == null)
        {
            Debug.LogWarning($"[CharacterDebugPanel] {targetCharacter.CharacterName} không có SwordOrbit!");
            return;
        }

        // Sử dụng Helper để spawn swords
        if (CharacterDebugHelper.Instance != null)
        {
            CharacterDebugHelper.Instance.SpawnSwordForCharacter(targetCharacter, swordsToAdd);
        }
        else
        {
            Debug.LogError("[CharacterDebugPanel] CharacterDebugHelper chưa được setup trong scene!");
        }
    }

    private void OnAddHealth()
    {
        if (targetCharacter == null) return;

        targetCharacter.Heal(healthToAdd);
        Debug.Log($"[CharacterDebugPanel] Đã thêm {healthToAdd} HP cho {targetCharacter.CharacterName} (HP: {targetCharacter.CurrentHp}/{targetCharacter.MaxHp})");
    }

    private void OnChangeSpeed()
    {
        if (targetCharacter == null) return;

        // Tăng tốc độ (cần thêm method vào CharacterBase)
        targetCharacter.MultiplySpeed(speedMultiplier);
        Debug.Log($"[CharacterDebugPanel] Đã tăng tốc độ x{speedMultiplier} cho {targetCharacter.CharacterName}");
    }

    private void OnChangeSwordType(SwordType type)
    {
        if (targetCharacter == null) return;

        SwordOrbit orbit = targetCharacter.GetSwordOrbit();
        if (orbit == null) return;

        // Đổi type cho tất cả kiếm (sẽ auto reset HP)
        orbit.SetSwordType(type);
        
        Debug.Log($"[CharacterDebugPanel] Đã đổi loại kiếm thành {type} cho {targetCharacter.CharacterName} (HP đã được reset)");
    }

    private void OnHealSwords()
    {
        if (targetCharacter == null) return;

        SwordOrbit orbit = targetCharacter.GetSwordOrbit();
        if (orbit == null) return;

        // Heal tất cả kiếm của character này
        int healedCount = 0;
        foreach (Transform child in orbit.transform)
        {
            Sword sword = child.GetComponent<Sword>();
            if (sword != null)
            {
                sword.Heal(sword.MaxHp); // Full heal
                healedCount++;
            }
        }

        Debug.Log($"[CharacterDebugPanel] Đã hồi full HP cho {healedCount} kiếm của {targetCharacter.CharacterName}");
    }

    private void Update()
    {
        // Đóng panel khi nhấn ESC
        if (Input.GetKeyDown(KeyCode.Escape) && panel != null && panel.activeSelf)
        {
            Hide();
        }
    }
}
