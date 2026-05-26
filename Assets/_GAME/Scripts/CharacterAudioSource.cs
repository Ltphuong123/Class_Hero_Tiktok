using UnityEngine;

public class CharacterAudioSource : MonoBehaviour
{
    private const string VolumePref = "CHARACTER_AUDIO_VOLUME";
    public static float MasterVolume { get; private set; } = 1f;

    public static void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumePref, MasterVolume);
        PlayerPrefs.Save();
    }

    public static float LoadMasterVolume()
    {
        MasterVolume = PlayerPrefs.GetFloat(VolumePref, 1f);
        return MasterVolume;
    }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip footstepClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField] private AudioClip collectSwordClip;

    [Header("Sound Cooldowns")]
    [SerializeField] private float attackCooldown  = 0.1f;
    [SerializeField] private float collectCooldown = 0.05f;

    [Header("Distance Volume")]
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 30f;

    [Header("Height Volume")]
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 50f;

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

        Vector3 camPos  = camTransform.position;
        Vector3 selfPos = transform.position;
        float dx = selfPos.x - camPos.x;
        float dz = selfPos.z - camPos.z;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);

        float distVolume   = 1f - Mathf.InverseLerp(minDistance, maxDistance, dist);
        float heightVolume = 1f - Mathf.InverseLerp(minHeight, maxHeight, camPos.y);
        float volume = distVolume * heightVolume * MasterVolume;

        loopSource.volume    = MasterVolume;
        oneShotSource.volume = MasterVolume;
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
