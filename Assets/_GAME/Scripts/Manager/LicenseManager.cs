using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LicenseManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField keyInput;
    public TMP_Text statusText;
    public Button checkButton;
    public Button logoutButton;

    [Header("API")]
    public string apiUrl = "http://localhost:5047/api/license/check";

    [Header("Scene")]
    public string gameSceneName = "GameScene";

    private const string SavedKeyPref = "LICENSE_KEY";

    void Start()
    {
        checkButton.onClick.AddListener(OnCheckClick);
        logoutButton.onClick.AddListener(OnLogoutClick);

        // Nếu có key đã lưu thì điền sẵn vào input
        string savedKey = PlayerPrefs.GetString(SavedKeyPref, "");
        
        if (!string.IsNullOrEmpty(savedKey))
        {
            keyInput.text = savedKey;
            statusText.text = "Key đã lưu. Nhấn Check để đăng nhập";
        }
        else
        {
            keyInput.text = "";
            statusText.text = "Nhập key để tiếp tục";
        }
    }

    void OnCheckClick()
    {
        string key = keyInput.text.Trim();

        if (string.IsNullOrEmpty(key))
        {
            statusText.text = "Chưa nhập key!";
            return;
        }

        StartCoroutine(CheckKey(key));
    }

    void OnLogoutClick()
    {
        PlayerPrefs.DeleteKey(SavedKeyPref);
        PlayerPrefs.Save();

        keyInput.text = "";
        statusText.text = "Đã xóa key.";
    }

    IEnumerator CheckKey(string key)
    {
        statusText.text = "Đang kiểm tra...";
        checkButton.interactable = false;

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

        checkButton.interactable = true;

        if (request.result != UnityWebRequest.Result.Success)
        {
            statusText.text = "Không kết nối được server!";
            Debug.LogError(request.error);
            yield break;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log("Server response: " + responseText);

        LicenseResponse res = JsonUtility.FromJson<LicenseResponse>(responseText);

        if (res != null && res.success)
        {
            // 🔹 Lưu key để CanvasMainMenu có thể kiểm tra
            PlayerPrefs.SetString(SavedKeyPref, key);
            PlayerPrefs.Save();

            statusText.text = "✅ " + res.message;

            yield return new WaitForSeconds(0.5f);

            EnterGame();
        }
        else
        {
            statusText.text = "❌ " + (res != null ? res.message : "Key không hợp lệ");
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

    void EnterGame()
    {
        if (Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.Log("Đăng nhập thành công (chưa có GameScene)");
            statusText.text = "Đăng nhập thành công!";
        }
    }
}

[System.Serializable]
public class CheckRequest
{
    public string key;
    public string deviceId;
}

[System.Serializable]
public class LicenseResponse
{
    public bool success;
    public string code;
    public string message;
}