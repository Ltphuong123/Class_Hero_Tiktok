using UnityEngine;

public class GameUnit : MonoBehaviour
{
    [SerializeField] private PoolType poolType;
    private Transform tf;

    public PoolType PoolType
    {
        get => poolType;
    }

    public Transform TF
    {
        get
        {
            if (tf == null)
            {
                tf = transform;
            }
            return tf;
        }
    }
}

