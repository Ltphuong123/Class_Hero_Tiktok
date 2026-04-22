using UnityEngine;

public class ParticleUnit : MonoBehaviour
{
    [SerializeField] private ParticleType particleType;
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private float duration = 2f;
    
    private Transform tf;

    public ParticleType ParticleType => particleType;

    public Transform TF
    {
        get
        {
            if (tf == null) tf = transform;
            return tf;
        }
    }

    private void Awake()
    {
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();
        
        if (particleSystem != null)
        {
            var main = particleSystem.main;
            main.stopAction = ParticleSystemStopAction.None;
        }
    }

    public void OnSpawn()
    {
        gameObject.SetActive(true);
        if (particleSystem != null) particleSystem.Play();
        Invoke(nameof(AutoDespawn), duration);
    }

    private void AutoDespawn()
    {
        ParticlePool.Despawn(this);
    }

    public void OnDespawn()
    {
        if (particleSystem != null) particleSystem.Stop();
        CancelInvoke(nameof(AutoDespawn));
        gameObject.SetActive(false);
    }
}
