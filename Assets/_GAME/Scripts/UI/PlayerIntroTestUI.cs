using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerIntroTestUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject    panel;
    [SerializeField] private KeyCode       toggleKey = KeyCode.F9;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TMP_InputField weeklyRankInput;
    [SerializeField] private TMP_InputField weeklyPtsInput;
    [SerializeField] private TMP_InputField monthlyRankInput;
    [SerializeField] private TMP_InputField monthlyPtsInput;

    [Header("Button")]
    [SerializeField] private Button testButton;

    private void Awake()
    {
        if (testButton != null)
            testButton.onClick.AddListener(OnTestClick);

        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            TogglePanel();
    }

    private void TogglePanel()
    {
        if (panel != null)
            panel.SetActive(!panel.activeSelf);
    }

    private void OnTestClick()
    {
        string nickname    = nicknameInput   != null ? nicknameInput.text.Trim()   : "TestPlayer";
        int weeklyRank     = Parse(weeklyRankInput);
        int weeklyPts      = Parse(weeklyPtsInput);
        int monthlyRank    = Parse(monthlyRankInput);
        int monthlyPts     = Parse(monthlyPtsInput);

        if (string.IsNullOrEmpty(nickname)) nickname = "TestPlayer";

        if (PlayerIntroManager.Instance == null)
        {
            Debug.LogWarning("[IntroTestUI] PlayerIntroManager.Instance is null");
            return;
        }

        PlayerIntroManager.Instance.TryQueueBestIntro(nickname, weeklyRank, weeklyPts, monthlyRank, monthlyPts);
    }

    private static int Parse(TMP_InputField field)
    {
        if (field == null) return 0;
        return int.TryParse(field.text, out int v) ? v : 0;
    }
}
