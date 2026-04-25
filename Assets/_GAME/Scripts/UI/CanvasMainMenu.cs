using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class CanvasMainMenu : UICanvas
{
    [Header("License API")]
    public string apiUrl = "http://localhost:5047/api/license/check";
    
    [Header("UI Elements")]
    public GameObject objectToHide; // GameObject cần ẩn khi load game
    
    private const string SavedKeyPref = "LICENSE_KEY";

    public void LoadGamePlayScene()
    {
        // Kiểm tra key trước khi load game
        StartCoroutine(CheckLicenseAndLoadGame());
    }

    IEnumerator CheckLicenseAndLoadGame()
    {
        // Lấy key đã lưu
        string savedKey = PlayerPrefs.GetString(SavedKeyPref, "");

        if (string.IsNullOrEmpty(savedKey))
        {
            // Không có key → về màn Login
            LoadLoginScene();
            yield break;
        }

        // Kiểm tra key với server
        yield return StartCoroutine(CheckKeyWithServer(savedKey));
    }

    IEnumerator CheckKeyWithServer(string key)
    {
        string deviceId = GetDeviceId();

        CheckRequest requestData = new CheckRequest
        {
            key = key,
            deviceId = deviceId
        };

        string json = JsonUtility.ToJson(requestData);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("License check failed: " + request.error);
            LoadLoginScene();
            yield break;
        }

        string responseText = request.downloadHandler.text;
        LicenseResponse res = JsonUtility.FromJson<LicenseResponse>(responseText);

        if (res != null && res.success)
        {
            // Key hợp lệ → Load GamePlay
            LoadGamePlaySceneInternal();
        }
        else
        {
            // Key không hợp lệ → Xóa key và về Login
            PlayerPrefs.DeleteKey(SavedKeyPref);
            PlayerPrefs.Save();
            LoadLoginScene();
        }
    }

    void LoadGamePlaySceneInternal()
    {
        // Ẩn GameObject trước khi load game
        if (objectToHide != null)
        {
            objectToHide.SetActive(false);
        }
        
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

    void LoadLoginScene()
    {
        // Load về màn hình Login
        if (Application.CanStreamedLevelBeLoaded("Login"))
        {
            SceneManager.LoadScene("Login");
        }
        else
        {
            Debug.LogError("LoginScene không tồn tại! Vui lòng tạo scene Login.");
        }
    }

    string GetDeviceId()
    {
        string id = SystemInfo.deviceUniqueIdentifier;

        if (string.IsNullOrEmpty(id))
        {
            id = SystemInfo.deviceName + "_" + SystemInfo.operatingSystem;
        }

        return id;
    }
}
