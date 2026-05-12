using UnityEngine;

public class CharacterAudioSource : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField] private AudioClip collectSwordClip;

    [Header("Sound Cooldowns")]
    [SerializeField] private float attackCooldown  = 0.1f;
    [SerializeField] private float collectCooldown = 0.05f;

    [Header("Zoom Volume")]
    [SerializeField] private float minHeight     = 5f;
    [SerializeField] private float maxHeight     = 50f;
    [SerializeField] [Range(0f, 1f)] private float zoomInfluence = 0.5f;

    [SerializeField] private AudioSource loopSource;
    [SerializeField] private AudioSource oneShotSource;

    private Transform camTransform;
    private float lastAttackTime  = -999f;
    private float lastCollectTime = -999f;

    private void Start()
    {
        Camera cam = Camera.main;
        if (cam != null) camTransform = cam.transform;
    }

    private void Update()
    {
        if (camTransform == null) return;

        float t      = Mathf.InverseLerp(minHeight, maxHeight, camTransform.position.y);
        float volume = Mathf.Lerp(1f, 1f - zoomInfluence, t);

        loopSource.volume    = volume;
        oneShotSource.volume = volume;
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
