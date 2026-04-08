using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;


    [SerializeField] private AudioClip backGroundClip;


    [Header("Volumes")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float bgmVolume = 0.5f;

    private bool isSoundEnabled = true;
    private bool isMusicEnabled = true;

    private const string SOUND_KEY = "SoundEnabled";
    private const string MUSIC_KEY = "MusicEnabled";

    private void Awake()
    {
        isSoundEnabled = PlayerPrefs.GetInt(SOUND_KEY, 1) == 1;
        isMusicEnabled = PlayerPrefs.GetInt(MUSIC_KEY, 1) == 1;

        sfxSource.volume = sfxVolume;
        bgmSource.volume = bgmVolume;

        if (isMusicEnabled)
        {
            PlayBackgroundMusic();
        }
    }

    public void ToggleSound(bool enable)
    {
        isSoundEnabled = enable;
        PlayerPrefs.SetInt(SOUND_KEY, enable ? 1 : 0);
        PlayerPrefs.Save();

        if (!isSoundEnabled)
        {
            sfxSource.Stop();
        }
    }

    public void ToggleMusic(bool enable)
    {
        isMusicEnabled = enable;
        PlayerPrefs.SetInt(MUSIC_KEY, enable ? 1 : 0);
        PlayerPrefs.Save();

        if (!isMusicEnabled)
        {
            bgmSource.Stop();
        }
        else
        {
            if (!bgmSource.isPlaying && bgmSource.clip != null)
            {
                bgmSource.Play();
            }
            else if (!bgmSource.isPlaying && bgmSource.clip == null)
            {
                PlayBackgroundMusic();
            }
        }
    }

    public bool GetToggleMusic()
    {
        return isMusicEnabled;
    }
    public bool GetToggleSound()
    {
        return isSoundEnabled;
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }

    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }

    public void PlayBackgroundMusic() => PlayBGM(backGroundClip, true);

    private void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (!isMusicEnabled || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (!isSoundEnabled || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
