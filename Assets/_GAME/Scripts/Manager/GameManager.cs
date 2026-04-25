using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public enum GameState {MainMenu=1, GamePlay = 2, Win = 3, Lose = 4, Setting = 5, Pause =6}

public class GameManager : Singleton<GameManager>
{
    [Header("Game Timer")]
    [SerializeField] private float gameTimeInSeconds = 300f; // 5 phút mặc định
    
    [Header("TikTok Integration")]
    [SerializeField] private TikTokUdpReceiver tiktokReceiver;
    [SerializeField] private float tiktokStartDelay = 1f; // Delay trước khi start UDP receiver
    
    private static GameState gameState;
    private float currentGameTime;
    private bool isGameRunning;
    
    public float CurrentGameTime => currentGameTime;
    public float GameTimeInSeconds => gameTimeInSeconds;
    public bool IsGameRunning => isGameRunning;
    public event Action OnGameTimeUp;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Load game time từ PlayerPrefs
        gameTimeInSeconds = GiftActionConfigEditor.GetSavedGameTime();

        //tranh viec nguoi choi cham da diem vao man hinh
        Input.multiTouchEnabled = false;
        //target frame rate ve 60 fps
        Application.targetFrameRate = 60;
        //tranh viec tat man hinh
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //xu tai tho
        int maxScreenHeight = 1280;
        float ratio = (float)Screen.currentResolution.width / (float)Screen.currentResolution.height;
        if (Screen.currentResolution.height > maxScreenHeight)
        {
            Screen.SetResolution(Mathf.RoundToInt(ratio * (float)maxScreenHeight), maxScreenHeight, true);
        }
    }

    private void Update()
    {

        if (isGameRunning && gameState == GameState.GamePlay)
        {
            currentGameTime -= Time.deltaTime;
            
            if (currentGameTime <= 0f)
            {
                currentGameTime = 0f;
                OnTimeUp();
            }
        }
    }
    
    public void ChangeState(GameState state)
    {
        gameState = state;
    }
    public bool IsState(GameState state) => gameState == state;
    public GameState GetState()
    {
        return gameState;
    }

    //vao game
    private void Start()
    {
        
        // Đảm bảo timeScale = 1
        Time.timeScale = 1f;
        
        // Tự động bắt đầu đếm thời gian khi game start
        currentGameTime = gameTimeInSeconds;
        isGameRunning = true;
        ChangeState(GameState.GamePlay);
        
        // Delay 1 giây trước khi start TikTok UDP receiver
        StartCoroutine(StartTikTokReceiverDelayed());
    }
    
    private IEnumerator StartTikTokReceiverDelayed()
    {
        // Chờ delay time
        yield return new WaitForSeconds(tiktokStartDelay);
        
        // Tìm TikTokUdpReceiver nếu chưa assign
        if (tiktokReceiver == null)
        {
            tiktokReceiver = FindObjectOfType<TikTokUdpReceiver>();
        }
        
        // Start receiver
        if (tiktokReceiver != null)
        {
            tiktokReceiver.StartReceiver();
            Debug.Log($"[GameManager] TikTok UDP Receiver started after {tiktokStartDelay}s delay");
        }
        else
        {
            Debug.LogWarning("[GameManager] TikTokUdpReceiver not found in scene");
        }
    }

    // bat dau game
    public void GamePlay()
    {
        if (!isGameRunning)
        {
            currentGameTime = gameTimeInSeconds;
            isGameRunning = true;
            ChangeState(GameState.GamePlay);
        }
    }


    // bat dau game
    public void GameStart()
    {
        currentGameTime = gameTimeInSeconds;
        isGameRunning = true;
        ChangeState(GameState.GamePlay);
    }

    //dung game
    public void GamePause()
    {
        isGameRunning = false;
        ChangeState(GameState.Pause);
        Time.timeScale = 0f;
    }

    //tiep tuc game
    public void GameResume()
    {
        isGameRunning = true;
        ChangeState(GameState.GamePlay);
        Time.timeScale = 1f;
    }

    //thang
    public void GameWin()
    {
        isGameRunning = false;
        ChangeState(GameState.Win);
    }

    //thua
    public void GameLose()
    {
        isGameRunning = false;
        ChangeState(GameState.Lose);
    }

    private void OnTimeUp()
    {
        isGameRunning = false;
        
        // Lưu top 3 characters vào GameEndData
        if (CharacterManager.Instance != null)
        {
            var rankedCharacters = CharacterManager.Instance.RankedCharacters;
            GameEndData.SetTopCharacters(new List<CharacterRankData>(rankedCharacters));
        }
        
        // Tạm dừng game ngay lập tức
        Time.timeScale = 0f;
        
        // Chờ 1 giây rồi mới fade và chuyển scene
        StartCoroutine(FadeToGameEndCoroutine());
    }
    
    private IEnumerator FadeToGameEndCoroutine()
    {
        // Chờ 1 giây (sử dụng unscaled time vì game đã pause)
        float waitTime = 0f;
        while (waitTime < 1f)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Sau 1 giây, kiểm tra xem có CanvasFade không
        bool hasFadeCanvas = false;
        try
        {
            CanvasFade fadeCanvas = UIManager.Instance.GetUI<CanvasFade>();
            if (fadeCanvas != null)
            {
                hasFadeCanvas = true;
                fadeCanvas.gameObject.SetActive(true);
                fadeCanvas.FadeToBlackAndLoadScene("GameEnd", 1f);
            }
        }
        catch (System.Exception e)
        {
            hasFadeCanvas = false;
        }
        
        if (!hasFadeCanvas)
        {
            // Fallback nếu không có fade canvas
            
            // Reset timeScale trước khi load scene
            Time.timeScale = 1f;
            LoadScene("GameEnd");
        }
    }

    // cai dat
    public void GameSettings()
    {
    }

    //tro ve home
    public void GameHome()
    {
    }
    
    // Load scene
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    
    public void LoadGamePlayScene()
    {
        LoadScene("GamePlay");
    }
    
    public void LoadMainMenuScene()
    {
        LoadScene("MainMenu");
    }

}