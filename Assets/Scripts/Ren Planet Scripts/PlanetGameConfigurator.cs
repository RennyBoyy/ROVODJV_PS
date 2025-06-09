using System.Collections;
using UnityEngine;

public class PlanetGameConfigurator : MonoBehaviour
{
    public static PlanetGameConfigurator Instance { get; private set; }

    [Header("Planet Sound Effects")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectSound;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float musicFadeInDuration = 2f;

    [Header("Level Ambient Sounds")]
    [SerializeField] private AudioClip[] levelAmbientSounds;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;

    [Header("Audio Volume Controls")]
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float ambientVolume = 0.5f;
    [SerializeField] private float audioTransitionDuration = 1f;

    private AudioSource currentAmbientSource;
    private bool isTransitioningAudio = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        StartCoroutine(StartBackgroundMusic());
    }

    private void InitializeAudioSources()
    {
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (ambientSource == null) ambientSource = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource(musicSource, musicVolume, true);
        ConfigureAudioSource(sfxSource, sfxVolume, false);
        ConfigureAudioSource(ambientSource, ambientVolume, false);
    }

    private void ConfigureAudioSource(AudioSource source, float volume, bool loop)
    {
        if (source != null)
        {
            source.loop = loop;
            source.playOnAwake = false;
            source.volume = volume;
        }
    }

    private IEnumerator StartBackgroundMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.Play();

            float elapsed = 0f;
            while (elapsed < musicFadeInDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / musicFadeInDuration);
                yield return null;
            }
            musicSource.volume = musicVolume;
        }
    }

    public void PlayHoverSound()
    {
        if (sfxSource != null && hoverSound != null)
        {
            sfxSource.PlayOneShot(hoverSound);
        }
    }

    public void PlaySelectSound()
    {
        if (sfxSource != null && selectSound != null)
        {
            sfxSource.PlayOneShot(selectSound);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void TransitionToAmbientAudio(AudioClip ambientClip)
    {
        StartCoroutine(TransitionToAmbientAudioCoroutine(ambientClip));
    }

    public void TransitionBackToBackgroundMusic()
    {
        StartCoroutine(TransitionBackToBackgroundMusicCoroutine());
    }

    private IEnumerator TransitionToAmbientAudioCoroutine(AudioClip ambientClip)
    {
        if (ambientClip == null)
        {
            Debug.LogWarning("PlanetGameConfigurator: Ambient clip is null, skipping transition");
            yield break;
        }

        isTransitioningAudio = true;

        if (musicSource != null && musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < audioTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / audioTransitionDuration;
                if (musicSource != null)    
                    musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (musicSource != null)
            {
                musicSource.volume = 0f;
                musicSource.Pause();
            }
        }

        if (currentAmbientSource != null)
        {
            currentAmbientSource.Stop();
            Destroy(currentAmbientSource.gameObject);
        }

        GameObject ambientGO = new GameObject("LevelAmbient");
        currentAmbientSource = ambientGO.AddComponent<AudioSource>();
        currentAmbientSource.clip = ambientClip;
        currentAmbientSource.loop = true;
        currentAmbientSource.volume = 0f;
        currentAmbientSource.Play();

        float ambientElapsed = 0f;
        while (ambientElapsed < audioTransitionDuration)
        {
            ambientElapsed += Time.deltaTime;
            float t = ambientElapsed / audioTransitionDuration;
            if (currentAmbientSource != null)    
                currentAmbientSource.volume = Mathf.Lerp(0f, ambientVolume, t);
            yield return null;
        }

        if (currentAmbientSource != null)
            currentAmbientSource.volume = ambientVolume;

        isTransitioningAudio = false;
    }

    private IEnumerator TransitionBackToBackgroundMusicCoroutine()
    {
        isTransitioningAudio = true;

        if (currentAmbientSource != null)
        {
            float startVolume = currentAmbientSource.volume;
            float elapsed = 0f;

            while (elapsed < audioTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / audioTransitionDuration;
                if (currentAmbientSource != null)    
                    currentAmbientSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (currentAmbientSource != null)
            {
                currentAmbientSource.Stop();
                Destroy(currentAmbientSource.gameObject);
                currentAmbientSource = null;
            }
        }

        if (musicSource != null)
        {
            if (!musicSource.isPlaying)
            {
                musicSource.UnPause();
            }

            float elapsed = 0f;
            while (elapsed < audioTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / audioTransitionDuration;
                if (musicSource != null)    
                    musicSource.volume = Mathf.Lerp(0f, musicVolume, t);
                yield return null;
            }

            if (musicSource != null)
                musicSource.volume = musicVolume;
        }

        isTransitioningAudio = false;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null && !isTransitioningAudio && musicSource.isPlaying)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        if (currentAmbientSource != null)
        {
            currentAmbientSource.volume = ambientVolume;
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    public void PlayMusic(AudioClip newMusic)
    {
        if (newMusic != null && musicSource != null)
        {
            musicSource.clip = newMusic;
            musicSource.Play();
        }
    }

    public bool IsTransitioningAudio => isTransitioningAudio;
    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;
    public float AmbientVolume => ambientVolume;
}