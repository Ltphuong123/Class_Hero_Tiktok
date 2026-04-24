using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CanvasMainMenu : UICanvas
{
    public void LoadGamePlayScene()
    {
        // Mở loading screen
        CanvasLoading loadingScreen = UIManager.Instance.OpenUI<CanvasLoading>();
        
        if (loadingScreen != null)
        {
            loadingScreen.LoadSceneAsync("GamePlay");
        }
        else
        {
            // Fallback nếu không có loading screen
            SceneManager.LoadScene("GamePlay");
        }
    }
}
