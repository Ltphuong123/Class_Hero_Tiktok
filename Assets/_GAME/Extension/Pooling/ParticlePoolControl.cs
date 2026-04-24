using UnityEngine;

public class ParticlePoolControl : MonoBehaviour
{
    [SerializeField] private ParticlePoolAmount[] particlePoolAmounts;
    [SerializeField] private Transform particleParent;

    private void Awake()
    {
        // Cleanup invalid pools trước khi init
        ParticlePool.CleanupInvalidPools();
        
        if (particleParent == null)
        {
            GameObject parentObj = new GameObject("ParticlePool");
            particleParent = parentObj.transform;
            DontDestroyOnLoad(parentObj);
        }

        ParticleUnit[] particleUnits = Resources.LoadAll<ParticleUnit>("ParticlePool/");
        for (int i = 0; i < particleUnits.Length; i++)
        {
            if (!ParticlePool.HasPool(particleUnits[i].ParticleType))
            {
                GameObject typeParent = new GameObject(particleUnits[i].ParticleType.ToString());
                typeParent.transform.SetParent(particleParent);
                ParticlePool.Preload(particleUnits[i], 10, typeParent.transform);
            }
        }

        if (particlePoolAmounts != null)
        {
            for (int i = 0; i < particlePoolAmounts.Length; i++)
            {
                if (particlePoolAmounts[i].prefab == null) continue;
                if (ParticlePool.HasPool(particlePoolAmounts[i].prefab.ParticleType)) continue;

                Transform parent = particlePoolAmounts[i].parent;
                if (parent == null)
                {
                    GameObject typeParent = new GameObject(particlePoolAmounts[i].prefab.ParticleType.ToString());
                    typeParent.transform.SetParent(particleParent);
                    parent = typeParent.transform;
                }

                ParticlePool.Preload(particlePoolAmounts[i].prefab, particlePoolAmounts[i].amount, parent);
            }
        }
    }

    private void OnDestroy()
    {
        // Cleanup khi scene bị destroy
        if (Application.isPlaying)
        {
            ParticlePool.CollectAll();
        }
    }

    private void OnApplicationQuit()
    {
        ParticlePool.ClearOnQuit();
    }
}

[System.Serializable]
public class ParticlePoolAmount
{
    public ParticleUnit prefab;
    public Transform parent;
    public int amount = 5;
}
