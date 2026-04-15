using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [Header("Sword Spawning")]
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private int initialSwordCount = 10;

    private void Start()
    {
        SpawnInitialSwords();
    }

    private void SpawnInitialSwords()
    {
        if (swordPrefab == null)
        {
            Debug.LogWarning("[LevelManager] Sword prefab not assigned. Skipping spawn.");
            return;
        }

        MapManager map = MapManager.Instance;
        if (map == null)
        {
            Debug.LogWarning("[LevelManager] MapManager not found. Skipping spawn.");
            return;
        }

        Vector2 min = map.MapMin;
        Vector2 max = map.MapMax;
        float padding = map.CellSize; // keep swords away from edges

        for (int i = 0; i < initialSwordCount; i++)
        {
            Vector3 pos = FindOpenPosition(min, max, padding, map);
            if (pos.z < 0f)
            {
                Debug.LogWarning($"[LevelManager] Could not find open position for sword {i}. Skipping.");
                continue;
            }

            ItemManager itemMgr = ItemManager.Instance;
            if (itemMgr != null)
            {
                itemMgr.SpawnItem(swordPrefab, pos);
            }
            else
            {
                GameObject go = Instantiate(swordPrefab, pos, Quaternion.identity);
                Sword sword = go.GetComponent<Sword>();
                if (sword != null) sword.OnSpawn(pos);
            }
        }

        Debug.Log($"[LevelManager] Spawned {initialSwordCount} swords on the map.");
    }

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

    /// <summary>
    /// Tìm vị trí ngẫu nhiên không trúng tường. Thử tối đa 30 lần.
    /// Trả về z = -1 nếu không tìm được.
    /// </summary>
    private static Vector3 FindOpenPosition(Vector2 min, Vector2 max, float padding, MapManager map)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 pos = new Vector3(
                Random.Range(min.x + padding, max.x - padding),
                Random.Range(min.y + padding, max.y - padding),
                0f
            );
            if (!map.IsWall(pos))
                return pos;
        }
        return new Vector3(0f, 0f, -1f);
    }
}
