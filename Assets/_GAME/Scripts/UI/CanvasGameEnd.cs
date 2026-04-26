using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CanvasGameEnd : UICanvas
{
    [Header("Top 3 Display")]
    [SerializeField] private GameObject[] topCharacterPanels;
    [SerializeField] private Image[] avatarImages;
    [SerializeField] private TextMeshProUGUI[] nameTexts;
    [SerializeField] private TextMeshProUGUI[] levelTexts;
    [SerializeField] private TextMeshProUGUI[] swordCountTexts;
    [SerializeField] private TextMeshProUGUI[] killPointsTexts;
    [SerializeField] private TextMeshProUGUI[] rankTexts;
    
    private void Start()
    {
        // Hiển thị top 3 characters khi canvas được mở
        DisplayTopCharacters();
    }
    
    private void DisplayTopCharacters()
    {
        List<TopCharacterData> topCharacters = GameEndData.TopCharacters;
        
        Debug.Log($"[CanvasGameEnd] Displaying {topCharacters.Count} top characters");
        
        // Hiển thị từng character trong top 3
        for (int i = 0; i < 3; i++)
        {
            if (i < topCharacters.Count)
            {
                // Có data cho vị trí này
                TopCharacterData data = topCharacters[i];
                
                if (topCharacterPanels != null && i < topCharacterPanels.Length && topCharacterPanels[i] != null)
                {
                    topCharacterPanels[i].SetActive(true);
                }
                
                // Set avatar
                if (avatarImages != null && i < avatarImages.Length && avatarImages[i] != null)
                {
                    avatarImages[i].sprite = data.avatar;
                    avatarImages[i].enabled = data.avatar != null;
                }
                
                // Set name
                if (nameTexts != null && i < nameTexts.Length && nameTexts[i] != null)
                {
                    nameTexts[i].text = data.characterName;
                }
                
                // Set level
                if (levelTexts != null && i < levelTexts.Length && levelTexts[i] != null)
                {
                    levelTexts[i].text = $"Level {data.level}";
                }
                
                // Set sword count
                if (swordCountTexts != null && i < swordCountTexts.Length && swordCountTexts[i] != null)
                {
                    int totalSwords = data.swordCount + data.swordQueue;
                    swordCountTexts[i].text = $"{totalSwords} Swords";
                }
                
                // Set kill points
                if (killPointsTexts != null && i < killPointsTexts.Length && killPointsTexts[i] != null)
                {
                    killPointsTexts[i].text = $"{data.killPoints}";
                }
                
                // Set rank
                if (rankTexts != null && i < rankTexts.Length && rankTexts[i] != null)
                {
                    rankTexts[i].text = $"#{data.rank}";
                }
                
                Debug.Log($"[CanvasGameEnd] Rank {data.rank}: {data.characterName} - Level {data.level} - {data.swordCount} swords - {data.killPoints} points");
            }
            else
            {
                // Không có data cho vị trí này, ẩn panel
                if (topCharacterPanels != null && i < topCharacterPanels.Length && topCharacterPanels[i] != null)
                {
                    topCharacterPanels[i].SetActive(false);
                }
            }
        }
    }
    
    public void LoadMainMenuScene()
    {
        // Reset timeScale về bình thường (phòng trường hợp bị pause)
        Time.timeScale = 1f;
        
        // Clear data trước khi chuyển scene
        GameEndData.Clear();
        
        SceneManager.LoadScene("MainMenu");
    }
}
