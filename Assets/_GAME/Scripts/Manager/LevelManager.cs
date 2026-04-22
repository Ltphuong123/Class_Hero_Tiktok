using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [Header("Sword Spawning")]
    [SerializeField] private int initialSwordCount = 10;

    [Header("Character Spawning")]
    [SerializeField] private int initialCharacterCount = 5;
    [SerializeField] private string[] characterNames = { "Bot A", "Bot B", "Bot C", "Bot D", "Bot E" };
    [SerializeField] private Sprite[] characterAvatars;

    private void Start()
    {
        SpawnInitialSwords();
        SpawnInitialCharacters();
    }

    private void SpawnInitialSwords()
    {
        MapManager map = MapManager.Instance;
        if (map == null)
        {
            Debug.LogWarning("[LevelManager] MapManager not found. Skipping spawn.");
            return;
        }

        ItemManager itemMgr = ItemManager.Instance;
        if (itemMgr == null)
        {
            Debug.LogWarning("[LevelManager] ItemManager not found. Skipping spawn.");
            return;
        }

        Vector2 min = map.MapMin;
        Vector2 max = map.MapMax;
        float padding = map.CellSize;

        for (int i = 0; i < initialSwordCount; i++)
        {
            Vector3 pos = FindOpenPosition(min, max, padding, map);
            if (pos.z < 0f)
            {
                Debug.LogWarning($"[LevelManager] Could not find open position for sword {i}. Skipping.");
                continue;
            }

            itemMgr.Spawn(pos);
        }

    }

    private void SpawnInitialCharacters()
    {
        MapManager map = MapManager.Instance;
        if (map == null)
        {
            Debug.LogWarning("[LevelManager] MapManager not found. Skipping character spawn.");
            return;
        }

        CharacterManager charMgr = CharacterManager.Instance;
        if (charMgr == null)
        {
            Debug.LogWarning("[LevelManager] CharacterManager not found. Skipping character spawn.");
            return;
        }

        Vector2 min = map.MapMin;
        Vector2 max = map.MapMax;
        float padding = map.CellSize * 2f;

        for (int i = 0; i < initialCharacterCount; i++)
        {
            Vector3 pos = FindOpenPosition(min, max, padding, map);
            if (pos.z < 0f)
            {
                Debug.LogWarning($"[LevelManager] Could not find open position for character {i}. Skipping.");
                continue;
            }

            string id = $"char_{i:000}";
            string name = characterNames.Length > 0 ? characterNames[Random.Range(0, characterNames.Length)] : $"Character {i}";
            Sprite avatar = characterAvatars != null && characterAvatars.Length > 0 ? characterAvatars[Random.Range(0, characterAvatars.Length)] : null;
            int level = 1;

            charMgr.Spawn(pos, id, name, avatar, level);
        }

    }

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
