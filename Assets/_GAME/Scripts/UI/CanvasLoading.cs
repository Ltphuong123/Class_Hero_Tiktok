using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CanvasLoading : UICanvas
{
    [Header("Loading UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI loadingText;
    
    [Header("Settings")]
    [SerializeField] private float minimumLoadingTime = 2f;
    
    private void Start()
    {
        // Đảm bảo canvas này không bị destroy khi load scene
        DontDestroyOnLoad(gameObject);
    }
    
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }
    
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // Hiển thị loading screen
        gameObject.SetActive(true);
        
        float startTime = Time.time;
        
        // Bắt đầu load scene async
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        
        float progress = 0f;
        
        while (!operation.isDone)
        {
            // AsyncOperation.progress đi từ 0 đến 0.9
            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // Tính thời gian đã trôi qua
            float elapsedTime = Time.time - startTime;
            
            // Nếu chưa đủ thời gian tối thiểu, fake progress dựa trên thời gian
            if (elapsedTime < minimumLoadingTime)
            {
                // Progress dựa trên thời gian (0 -> 0.9 trong minimumLoadingTime giây)
                float timeProgress = (elapsedTime / minimumLoadingTime) * 0.9f;
                
                // Lấy giá trị nhỏ hơn giữa loadProgress và timeProgress
                progress = Mathf.Min(loadProgress, timeProgress);
            }
            else
            {
                // Sau khi đủ thời gian, dùng progress thật
                progress = loadProgress;
            }
            
            // Cập nhật UI
            if (progressBar != null)
                progressBar.value = progress;
            
            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            
            // Khi load xong 90% VÀ đã đủ thời gian tối thiểu
            if (operation.progress >= 0.9f && elapsedTime >= minimumLoadingTime)
            {
                // Animate đến 100%
                float finalProgress = progress;
                while (finalProgress < 1f)
                {
                    finalProgress += Time.deltaTime * 2f; // Tốc độ animate
                    finalProgress = Mathf.Min(finalProgress, 1f);
                    
                    if (progressBar != null)
                        progressBar.value = finalProgress;
                    
                    if (progressText != null)
                        progressText.text = $"{Mathf.RoundToInt(finalProgress * 100)}%";
                    
                    yield return null;
                }
                
                yield return new WaitForSeconds(0.3f);
                
                // Cho phép chuyển scene
                operation.allowSceneActivation = true;
            }
            
            yield return null;
        }
        
        // Ẩn loading screen sau khi load xong
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }
}
