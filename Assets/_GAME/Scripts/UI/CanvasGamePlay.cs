using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class CanvasGamePlay : UICanvas
{
    [Header("Timer UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    
    [Header("Countdown Effects")]
    [SerializeField] private AudioSource countdownAudioSource;
    [SerializeField] private AudioClip countdownSound;
    [SerializeField] private float countdownThreshold = 10f;
    [SerializeField] private float punchScale = 1.2f;
    [SerializeField] private float punchDuration = 0.3f;
    
    [Header("Setting Panel")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Button settingButton; // Nút mở setting
    [SerializeField] private Button closeButton; // Nút đóng setting
    [SerializeField] private Button mainMenuButton; // Nút về MainMenu
    [SerializeField] private Button endGameButton; // Nút kết thúc game ngay
    
    private GameManager gameManager;
    private int lastSecond = -1;
    private bool isCountdownActive = false;
    
    private void Start()
    {
        Setup();
    }
    
    public override void Setup()
    {
        base.Setup();
        gameManager = GameManager.Instance;
   
        
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
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideSettingPanel);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(LoadMainMenuScene);
        }
        
        if (endGameButton != null)
        {
            endGameButton.onClick.RemoveAllListeners();
            endGameButton.onClick.AddListener(EndGameNow);
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
        
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
        }
    }
    
    // Ẩn setting panel
    public void HideSettingPanel()
    {
        
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
    }
    
    // Trở về MainMenu
    private void LoadMainMenuScene()
    {
        
        // Reset timeScale về bình thường
        Time.timeScale = 1f;
        
        // Chuyển về MainMenu
        SceneManager.LoadScene("MainMenu");
    }
    
    // Kết thúc game ngay lập tức
    private void EndGameNow()
    {
        if (gameManager != null)
        {
            Debug.Log("[CanvasGamePlay] End Game button clicked");
            
            // Ẩn setting panel nếu đang mở
            if (settingPanel != null)
            {
                settingPanel.SetActive(false);
            }
            
            // Gọi GameManager để end game
            gameManager.ForceEndGame();
        }
    }
    
    private void UpdateTimerDisplay()
    {
        float timeRemaining = gameManager.CurrentGameTime;
        
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        
        string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText.text = timeString;
        
        // Kiểm tra countdown 10 giây cuối
        CheckCountdownEffects(timeRemaining, seconds);
        
        // Đổi màu khi còn ít thời gian
        if (timeRemaining <= countdownThreshold)
        {
            timerText.color = Color.red;
        }
        else if (timeRemaining <= 30f)
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

    private void CheckCountdownEffects(float timeRemaining, int currentSecond)
    {
        // Kích hoạt countdown khi còn <= 10 giây
        if (timeRemaining <= countdownThreshold && timeRemaining > 0f)
        {
            if (!isCountdownActive)
            {
                isCountdownActive = true;
                lastSecond = currentSecond;
            }
            
            // Khi giây thay đổi (đếm ngược)
            if (currentSecond != lastSecond && currentSecond >= 0)
            {
                PlayCountdownEffects();
                lastSecond = currentSecond;
            }
        }
        else
        {
            isCountdownActive = false;
            lastSecond = -1;
        }
    }

    private void PlayCountdownEffects()
    {
        // Hiệu ứng scale cho text
        if (timerText != null)
        {
            timerText.transform.DOKill();
            timerText.transform.localScale = Vector3.one;
            timerText.transform.DOPunchScale(Vector3.one * punchScale, punchDuration, 1, 0.5f);
        }
        
        // Phát âm thanh countdown
        PlayCountdownSound();
    }

    private void PlayCountdownSound()
    {
        if (countdownAudioSource != null && countdownSound != null)
        {
            countdownAudioSource.PlayOneShot(countdownSound);
        }
        else if (countdownSound != null)
        {
            // Fallback: Tạo AudioSource tạm thời
            AudioSource.PlayClipAtPoint(countdownSound, Camera.main.transform.position);
        }
    }
    
    private void OnDestroy()
    {
        // Stop DOTween animations
        if (timerText != null)
        {
            timerText.transform.DOKill();
        }
        
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
        
        if (endGameButton != null)
        {
            endGameButton.onClick.RemoveListener(EndGameNow);
        }
    }
}
