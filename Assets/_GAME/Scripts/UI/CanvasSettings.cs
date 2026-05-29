using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CanvasSettings : MonoBehaviour
{
    [Header("Toggle Button")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject mainPanel;
    
    [Header("Tab Buttons")]
    [SerializeField] private Button swordDataTabButton;
    [SerializeField] private Button characterLevelTabButton;
    [SerializeField] private Button characterBaseTabButton;
    [SerializeField] private Button skillDataTabButton;

    [Header("Tab Panels")]
    [SerializeField] private GameObject swordDataPanel;
    [SerializeField] private GameObject characterLevelPanel;
    [SerializeField] private GameObject characterBasePanel;
    [SerializeField] private GameObject skillDataPanel;
    
    [Header("Tab Button Colors")]
    [SerializeField] private Color activeTabColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(0.2f, 0.2f, 0.2f);
    
    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Sword Data Settings")]
    [SerializeField] private SwordDataSO swordData;
    [SerializeField] private Transform swordDataContent;
    [SerializeField] private GameObject swordDataRowPrefab;
    [SerializeField] private Button swordDataSaveButton;
    [SerializeField] private Button swordDataResetButton;

    [Header("Character Level Settings")]
    [SerializeField] private CharacterLevelDataSO levelData;
    [SerializeField] private Transform characterLevelContent;
    [SerializeField] private GameObject characterLevelRowPrefab;
    [SerializeField] private Button characterLevelSaveButton;
    [SerializeField] private Button characterLevelResetButton;

    [Header("Skill Data Settings")]
    [SerializeField] private SkillData[] skills;
    [SerializeField] private Transform skillDataContent;
    [SerializeField] private GameObject skillDataRowPrefab;
    [SerializeField] private Button skillDataSaveButton;
    [SerializeField] private Button skillDataResetButton;

    [Header("Character Base Settings")]
    [SerializeField] private CharacterBaseConfigSO characterBaseConfig;
    [SerializeField] private TMP_InputField maxHpInput;
    [SerializeField] private TMP_InputField overhealThresholdInput;
    [SerializeField] private TMP_InputField overhealScaleInput;
    [SerializeField] private TMP_InputField maxSwordCountInput;
    [SerializeField] private TMP_InputField maxSwordQueueInput;
    [SerializeField] private TMP_InputField lifestealPercentInput;
    [SerializeField] private Button characterBaseSaveButton;
    [SerializeField] private Button characterBaseLoadButton;
    [SerializeField] private Button characterBaseResetButton;

    private List<SwordDataRow> swordDataRows = new List<SwordDataRow>();
    private List<CharacterLevelRow> characterLevelRows = new List<CharacterLevelRow>();
    private List<SkillDataRow> skillDataRows = new List<SkillDataRow>();

    private void Start()
    {
        toggleButton?.onClick.AddListener(TogglePanel);
        
        swordDataTabButton?.onClick.AddListener(() => ShowTab(0));
        characterLevelTabButton?.onClick.AddListener(() => ShowTab(1));
        characterBaseTabButton?.onClick.AddListener(() => ShowTab(2));
        skillDataTabButton?.onClick.AddListener(() => ShowTab(3));
        closeButton?.onClick.AddListener(Close);
        
        swordDataSaveButton?.onClick.AddListener(SaveSwordData);
        swordDataResetButton?.onClick.AddListener(ResetSwordData);
        
        characterLevelSaveButton?.onClick.AddListener(SaveCharacterLevelData);
        characterLevelResetButton?.onClick.AddListener(ResetCharacterLevelData);
        
        if (characterBaseConfig == null)
            characterBaseConfig = CharacterBaseConfigSO.Instance;
        
        characterBaseSaveButton?.onClick.AddListener(SaveCharacterBaseConfig);
        characterBaseLoadButton?.onClick.AddListener(LoadCharacterBaseConfig);
        characterBaseResetButton?.onClick.AddListener(ResetCharacterBaseConfig);

        skillDataSaveButton?.onClick.AddListener(SaveSkillData);
        skillDataResetButton?.onClick.AddListener(ResetSkillData);
        
        ShowTab(0);
    }

    private void ShowTab(int tabIndex)
    {
        swordDataPanel?.SetActive(tabIndex == 0);
        characterLevelPanel?.SetActive(tabIndex == 1);
        characterBasePanel?.SetActive(tabIndex == 2);
        skillDataPanel?.SetActive(tabIndex == 3);

        UpdateTabButtonColors(tabIndex);

        if (tabIndex == 0)
            InitializeSwordDataUI();
        else if (tabIndex == 1)
            InitializeCharacterLevelUI();
        else if (tabIndex == 2)
            LoadCharacterBaseValuesToUI();
        else if (tabIndex == 3)
            InitializeSkillDataUI();
    }

    private void UpdateTabButtonColors(int activeIndex)
    {
        UpdateButtonColor(swordDataTabButton, activeIndex == 0);
        UpdateButtonColor(characterLevelTabButton, activeIndex == 1);
        UpdateButtonColor(characterBaseTabButton, activeIndex == 2);
        UpdateButtonColor(skillDataTabButton, activeIndex == 3);
    }

    private void UpdateButtonColor(Button button, bool isActive)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        colors.normalColor = isActive ? activeTabColor : inactiveTabColor;
        colors.highlightedColor = isActive ? activeTabColor : inactiveTabColor;
        colors.selectedColor = isActive ? activeTabColor : inactiveTabColor;
        button.colors = colors;
    }

    private void InitializeSwordDataUI()
    {
        if (swordData == null || swordDataContent == null || swordDataRowPrefab == null)
        {
            Debug.LogError("SwordData: Missing references!");
            return;
        }

        ClearSwordDataRows();

        var entries = swordData.GetAllEntries();
        
        foreach (var entry in entries)
        {
            GameObject rowObj = Instantiate(swordDataRowPrefab, swordDataContent);
            SwordDataRow row = rowObj.GetComponent<SwordDataRow>();
            
            if (row != null)
            {
                row.Initialize(entry.type, entry.sprite, entry.maxHp, entry.damage,
                    entry.damageReductionBonus, entry.lifestealBonus, entry.orbitSpeedBonus);
                swordDataRows.Add(row);
            }
        }
    }

    private void ClearSwordDataRows()
    {
        foreach (var row in swordDataRows)
        {
            if (row != null && row.gameObject != null)
                Destroy(row.gameObject);
        }
        swordDataRows.Clear();
    }

    private void SaveSwordData()
    {
        if (swordData == null) return;

        foreach (var row in swordDataRows)
        {
            if (row != null)
            {
                swordData.SetStats(row.SwordType, row.MaxHp, row.Damage);
                swordData.SetBonusStats(row.SwordType, row.DamageReductionBonus, row.LifestealBonus, row.OrbitSpeedBonus);
            }
        }

        swordData.SaveToFile();
        Debug.Log("Sword Data saved successfully!");
    }

    private void ResetSwordData()
    {
        if (swordData == null) return;

        swordData.ResetToDefault();
        InitializeSwordDataUI();
        Debug.Log("Sword Data reset to default values!");
    }

    private void InitializeCharacterLevelUI()
    {
        if (levelData == null || characterLevelContent == null || characterLevelRowPrefab == null)
        {
            Debug.LogError("CharacterLevel: Missing references!");
            return;
        }

        ClearCharacterLevelRows();

        var levels = levelData.GetAllLevels();
        
        foreach (var level in levels)
        {
            GameObject rowObj = Instantiate(characterLevelRowPrefab, characterLevelContent);
            CharacterLevelRow row = rowObj.GetComponent<CharacterLevelRow>();
            
            if (row != null)
            {
                row.Initialize(level.level, level.swordType, level.duration, level.speed, level.bodyScale, level.damageReduction, level.orbitRotateSpeed);
                characterLevelRows.Add(row);
            }
        }
    }

    private void ClearCharacterLevelRows()
    {
        foreach (var row in characterLevelRows)
        {
            if (row != null && row.gameObject != null)
                Destroy(row.gameObject);
        }
        characterLevelRows.Clear();
    }

    private void SaveCharacterLevelData()
    {
        if (levelData == null) return;

        foreach (var row in characterLevelRows)
        {
            if (row != null)
                levelData.SetLevelData(row.Level, row.SwordType, row.Duration, row.Speed, row.BodyScale, row.DamageReduction, row.OrbitRotateSpeed);
        }

        levelData.SaveToFile();
        Debug.Log("Character Level Data saved successfully!");
    }

    private void ResetCharacterLevelData()
    {
        if (levelData == null) return;

        levelData.ResetToDefault();
        InitializeCharacterLevelUI();
        Debug.Log("Character Level Data reset to default values!");
    }

    private void LoadCharacterBaseValuesToUI()
    {
        if (characterBaseConfig == null) return;

        maxHpInput.text = characterBaseConfig.maxHp.ToString();
        overhealThresholdInput.text = characterBaseConfig.overhealThreshold.ToString();
        overhealScaleInput.text = characterBaseConfig.overhealScalePerThreshold.ToString();
        maxSwordCountInput.text = characterBaseConfig.maxSwordCount.ToString();
        maxSwordQueueInput.text = characterBaseConfig.maxSwordQueue.ToString();

        if (lifestealPercentInput != null)
            lifestealPercentInput.text = characterBaseConfig.lifestealPercent.ToString();
        
    }

    private void SaveCharacterBaseConfig()
    {
        if (characterBaseConfig == null) return;

        if (float.TryParse(maxHpInput.text, out float maxHp))
            characterBaseConfig.maxHp = maxHp;
        
        if (float.TryParse(overhealThresholdInput.text, out float overhealThreshold))
            characterBaseConfig.overhealThreshold = overhealThreshold;
        
        if (float.TryParse(overhealScaleInput.text, out float overhealScale))
            characterBaseConfig.overhealScalePerThreshold = overhealScale;
        
        if (int.TryParse(maxSwordCountInput.text, out int maxSwordCount))
            characterBaseConfig.maxSwordCount = maxSwordCount;
        
        if (int.TryParse(maxSwordQueueInput.text, out int maxSwordQueue))
            characterBaseConfig.maxSwordQueue = maxSwordQueue;
        
        if (lifestealPercentInput != null && float.TryParse(lifestealPercentInput.text, out float lifestealPercent))
            characterBaseConfig.lifestealPercent = lifestealPercent;

        characterBaseConfig.SaveToJson();
        Debug.Log("Character Base Config saved!");
    }

    private void LoadCharacterBaseConfig()
    {
        if (characterBaseConfig == null) return;

        characterBaseConfig.LoadFromJson();
        LoadCharacterBaseValuesToUI();
        Debug.Log("Character Base Config loaded!");
    }

    private void ResetCharacterBaseConfig()
    {
        if (characterBaseConfig == null) return;

        characterBaseConfig.maxHp = 100f;
        characterBaseConfig.overhealThreshold = 50f;
        characterBaseConfig.overhealScalePerThreshold = 0.1f;
        characterBaseConfig.maxSwordCount = 20;
        characterBaseConfig.maxSwordQueue = 50;
        characterBaseConfig.lifestealPercent = 0.2f;
        LoadCharacterBaseValuesToUI();
        Debug.Log("Character Base Config reset to default!");
    }

    private void InitializeSkillDataUI()
    {
        if (skills == null || skillDataContent == null || skillDataRowPrefab == null)
        {
            Debug.LogError("SkillData: Missing references!");
            return;
        }

        ClearSkillDataRows();

        foreach (var skill in skills)
        {
            if (skill == null) continue;
            GameObject rowObj = Instantiate(skillDataRowPrefab, skillDataContent);
            SkillDataRow row = rowObj.GetComponent<SkillDataRow>();
            if (row != null)
            {
                row.Initialize(skill);
                skillDataRows.Add(row);
            }
        }
    }

    private void ClearSkillDataRows()
    {
        foreach (var row in skillDataRows)
        {
            if (row != null && row.gameObject != null)
                Destroy(row.gameObject);
        }
        skillDataRows.Clear();
    }

    private void SaveSkillData()
    {
        foreach (var row in skillDataRows)
        {
            if (row == null || row.SkillData == null) continue;
            var sd = row.SkillData;
            sd.damage        = row.Damage;
            // sd.cooldown      = row.Cooldown;
            // sd.castDuration  = row.CastDuration;
            // sd.damageRadius  = row.DamageRadius;
            // sd.maxTargets    = row.MaxTargets;
            sd.SaveToJson();
        }
        Debug.Log("Skill Data saved!");
    }

    private void ResetSkillData()
    {
        foreach (var row in skillDataRows)
        {
            if (row?.SkillData == null) continue;
            row.SkillData.DeleteJson();
        }
        InitializeSkillDataUI();
        Debug.Log("Skill Data reset to default!");
    }

    public void Show()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);
        ShowTab(0);
    }

    public void Close()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);
    }

    public void TogglePanel()
    {
        if (mainPanel != null)
        {
            bool isActive = mainPanel.activeSelf;
            if (isActive)
                Close();
            else
                Show();
        }
    }
}
