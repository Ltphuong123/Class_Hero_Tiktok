using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState {MainMenu=1, GamePlay = 2, Win = 3, Lose = 4, Setting = 5, Pause =6}

public class GameManager : Singleton<GameManager>
{
    private static GameState gameState;
    private void Awake()
    {
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

    }

    // bat dau game
    public void GamePlay()
    {
    }


    // bat dau game
    public void GameStart()
    {
    }

    //dung game
    public void GamePause()
    {
    }

    //tiep tuc game
    public void GameResume()
    {
    }

    //thang
    public void GameWin()
    {
    }

    //thua
    public void GameLose()
    {
    }


    // cai dat
    public void GameSettings()
    {
    }

    //tro ve home
    public void GameHome()
    {
    }

}