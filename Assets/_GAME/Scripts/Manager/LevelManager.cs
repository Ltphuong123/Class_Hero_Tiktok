using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{

    // khoi tao chi so
    private void OnInit()
    {

    }

    public void OnPlay()
    {
        OnDespawn();

        OnLoadLevel();
        GameManager.Instance.GamePlay();
        
        OnInit();
    }
    
    public void OnStart()
    {
        GameManager.Instance.GameStart();
    }

    private void OnLoadLevel()
    {

    }

    public void OnWin()
    {
        GameManager.Instance.GameWin();
    }

    public void OnLose()
    {
        GameManager.Instance.GameLose();
    }

    public void OnNextLevel()
    {
        OnPlay();
    }

    public void OnHome()
    {
        OnDespawn();
        GameManager.Instance.GameHome();
    }


    private void OnDespawn()
    {
    
    }
    
}
