using UnityEngine;

public class PoolControl : MonoBehaviour
{
    [SerializeField] private PoolAmount[] poolAmounts;

    private void Awake() {

        GameUnit[] gameUnits = Resources.LoadAll<GameUnit>("Pool/");
        for (int i = 0; i < gameUnits.Length; i++)
        {
            if (!SimplePool.GetPool(gameUnits[i].PoolType))
            {
                SimplePool.Preload(gameUnits[i], 0, new GameObject(gameUnits[i].name).transform);
            }
        }

        for (int i = 0; i < poolAmounts.Length; i++)
        {
            if (!SimplePool.GetPool(gameUnits[i].PoolType))
            {
                SimplePool.Preload(poolAmounts[i].prefab, poolAmounts[i].amount, poolAmounts[i].parent);
            }
        }
    }

}


[System.Serializable]
public class PoolAmount
{
    public GameUnit prefab;
    public Transform parent;
    public int amount;
}
public enum PoolType
{
    None = 0,
    Sword = 1,
    Character1 = 2,
    Character2 = 3,
    Character3 = 4,
    Character4 = 5,
    Character5 = 6,
    Character6 = 7,
    Character7 = 8,
    Character8 = 9,
    Character9 = 10,
}
