using UnityEngine;

public class CharacterAudioSource : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField] private AudioClip collectSwordClip;

    [Header("Distance Settings")]
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private AnimationCurve volumeCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Header("Zoom Settings")]
    [SerializeField] private bool useZoomFactor = true;
    [SerializeField] private float minZoomSize = 15f;
    [SerializeField] private float maxZoomSize = 30f;

    [Header("Sound Cooldowns")]
    [SerializeField] private float attackCooldown = 0.1f;
    [SerializeField] private float collectCooldown = 0.05f;

    private AudioSource loopSource;
    private AudioSource oneShotSource;
    private Transform cameraTransform;
    private Camera mainCamera;
    private bool isInitialized;
    
    private float minDistanceSq;
    private float maxDistanceSq;
    private float distanceRange;
    private float zoomRange;
    private float lastAttackTime = -999f;
    private float lastCollectTime = -999f;

    private void Awake()
    {
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.playOnAwake = false;

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.loop = false;
        oneShotSource.playOnAwake = false;
        
        minDistanceSq = minDistance * minDistance;
        maxDistanceSq = maxDistance * maxDistance;
        distanceRange = maxDistance - minDistance;
        zoomRange = maxZoomSize - minZoomSize;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
            isInitialized = true;
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        float volume = CalculateVolume();
        loopSource.volume = volume;
        oneShotSource.volume = volume;
    }

    private float CalculateVolume()
    {
        float dx = transform.position.x - cameraTransform.position.x;
        float dy = transform.position.y - cameraTransform.position.y;
        float distanceSq = dx * dx + dy * dy;

        float baseVolume;
        if (distanceSq <= minDistanceSq)
            baseVolume = 1f;
        else if (distanceSq >= maxDistanceSq)
            baseVolume = 0f;
        else
        {
            float distance = Mathf.Sqrt(distanceSq);
            float normalizedDistance = (distance - minDistance) / distanceRange;
            baseVolume = volumeCurve.Evaluate(normalizedDistance);
        }

        if (useZoomFactor && mainCamera.orthographic)
        {
            float currentSize = mainCamera.orthographicSize;
            
            if (currentSize >= maxZoomSize)
                return 0f;
            
            if (currentSize > minZoomSize)
                baseVolume *= 1f - (currentSize - minZoomSize) / zoomRange;
        }

        return baseVolume;
    }

    public void PlayFootstep()
    {
        if (footstepClip != null && !loopSource.isPlaying)
        {
            loopSource.clip = footstepClip;
            loopSource.Play();
        }
    }

    public void StopFootstep()
    {
        if (loopSource.isPlaying)
            loopSource.Stop();
    }

    public void PlayAttack()
    {
        if (attackClip == null) return;

        float currentTime = Time.time;
        if (currentTime - lastAttackTime < attackCooldown) return;

        oneShotSource.PlayOneShot(attackClip);
        lastAttackTime = currentTime;
    }

    public void PlayDeath()
    {
        if (deathClip != null)
            oneShotSource.PlayOneShot(deathClip);
    }

    public void PlayLevelUp()
    {
        if (levelUpClip != null)
            oneShotSource.PlayOneShot(levelUpClip);
    }

    public void PlayCollectSword()
    {
        if (collectSwordClip == null) return;

        float currentTime = Time.time;
        if (currentTime - lastCollectTime < collectCooldown) return;

        oneShotSource.PlayOneShot(collectSwordClip);
        lastCollectTime = currentTime;
    }
}
