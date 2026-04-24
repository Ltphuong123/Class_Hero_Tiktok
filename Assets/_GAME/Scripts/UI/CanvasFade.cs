using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CanvasFade : UICanvas
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;
    
    private void Start()
    {
        // Đảm bảo canvas này không bị destroy khi load scene
        DontDestroyOnLoad(gameObject);
        
        // Đảm bảo fade image trong suốt ban đầu
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }
    
    public void FadeToBlackAndLoadScene(string sceneName, float duration = 1f)
    {
        fadeDuration = duration;
        StartCoroutine(FadeAndLoadCoroutine(sceneName));
    }


    
    private IEnumerator FadeAndLoadCoroutine(string sceneName)
    {
        if (fadeImage == null)
        {
            Debug.LogError("Fade Image is not assigned!");
            Time.timeScale = 1f; // Reset timeScale
            SceneManager.LoadScene(sceneName);
            yield break;
        }
        
        // Hiển thị canvas
        gameObject.SetActive(true);
        
        // Fade to black (sử dụng unscaledDeltaTime để hoạt động khi game pause)
        float elapsed = 0f;
        Color c = fadeImage.color;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Sử dụng unscaledDeltaTime
            c.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        
        // Đảm bảo alpha = 1
        c.a = 1f;
        fadeImage.color = c;
        
        // Chờ một chút trước khi load scene
        float waitTime = 0f;
        while (waitTime < 0.2f)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Reset timeScale trước khi load scene
        Time.timeScale = 1f;
        
        // Load scene
        SceneManager.LoadScene(sceneName);
        
        // Sau khi load xong, fade in (optional)
        waitTime = 0f;
        while (waitTime < 0.5f)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Fade in
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        
        // Destroy canvas sau khi fade in xong
        Destroy(gameObject);
    }
}
