using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CanvasGamePlay : UICanvas
{
    [Header("Timer UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    
    [Header("Setting Panel")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Button settingButton; // Nút mở setting
    [SerializeField] private Button closeButton; // Nút đóng setting
    [SerializeField] private Button mainMenuButton; // Nút về MainMenu
    
    private GameManager gameManager;
    
    private void Start()
    {
        Setup();
    }
    
    public override void Setup()
    {
        base.Setup();
        gameManager = GameManager.Instance;
        
        Debug.Log($"CanvasGamePlay Setup - GameManager: {gameManager != null}, TimerText: {timerText != null}");
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found!");
        }
        
        if (timerText == null)
        {
            Debug.LogError("TimerText not assigned in Inspector!");
        }
        
        // Ẩn setting panel khi khởi tạo
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
        
        // Gán sự kiện cho các nút
        if (settingButton != null)
        {
            settingButton.onClick.RemoveAllListeners();
            settingButton.onClick.AddListener(ShowSettingPanel);
            Debug.Log("[CanvasGamePlay] settingButton listener added");
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideSettingPanel);
            Debug.Log("[CanvasGamePlay] closeButton listener added");
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(LoadMainMenuScene);
            Debug.Log("[CanvasGamePlay] mainMenuButton listener added");
        }
    }
    
    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }
        
        if (gameManager != null && timerText != null)
        {
            UpdateTimerDisplay();
        }
    }

    public void LoadGamePlayScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
    
    // Hiển thị setting panel
    public void ShowSettingPanel()
    {
        Debug.Log("[CanvasGamePlay] ShowSettingPanel called");
        
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
        }
    }
    
    // Ẩn setting panel
    public void HideSettingPanel()
    {
        Debug.Log("[CanvasGamePlay] HideSettingPanel called");
        
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
    }
    
    // Trở về MainMenu
    private void LoadMainMenuScene()
    {
        Debug.Log("[CanvasGamePlay] LoadMainMenuScene called");
        
        // Reset timeScale về bình thường
        Time.timeScale = 1f;
        
        // Chuyển về MainMenu
        SceneManager.LoadScene("MainMenu");
    }
    
    private void UpdateTimerDisplay()
    {
        float timeRemaining = gameManager.CurrentGameTime;
        
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        
        string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText.text = timeString;
        
        // Debug mỗi 60 frames
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"UI Update: Time={timeRemaining}, Display={timeString}");
        }
        
        // Đổi màu khi còn ít thời gian
        if (timeRemaining <= 30f)
        {
            timerText.color = Color.red;
        }
        else if (timeRemaining <= 60f)
        {
            timerText.color = Color.yellow;
        }
        else
        {
            timerText.color = Color.white;
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup listeners
        if (settingButton != null)
        {
            settingButton.onClick.RemoveListener(ShowSettingPanel);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideSettingPanel);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(LoadMainMenuScene);
        }
    }
}
