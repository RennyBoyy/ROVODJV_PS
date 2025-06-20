using System.Collections;
using UnityEngine;

public class SkiGameConfigurator : MonoBehaviour
{
    public static SkiGameConfigurator Instance { get; private set; }

    [Header("Player Jump Sound Effects")]
    [SerializeField] private AudioClip[] jumpSounds;

    [Header("Player Skiing Sound Effects")]
    [SerializeField] private AudioClip skiingSound;

    [Header("Player Landing Sound Effects")]
    [SerializeField] private AudioClip[] landingSounds;

    [Header("Player Obstacle Hit Sound Effects")]
    [SerializeField] private AudioClip[] obstacleHitSounds;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Ambient Sounds")]
    [SerializeField] private AudioClip windSound;
    [SerializeField] private AudioClip snowSound;
    [SerializeField] private AudioClip mountainAmbienceSound;

    [Header("Intro + Countdown Sounds")]
    [SerializeField] private AudioClip characterIntroSound;
    [SerializeField] private AudioClip countdownNumberSound;
    [SerializeField] private AudioClip countdownGoSound;

    [Header("Game State Sounds")]
    [SerializeField] private AudioClip loseSound;

    [Header("Player 1 Audio Sources")]
    [SerializeField] private AudioSource player1JumpSource;
    [SerializeField] private AudioSource player1SkiingSource;
    [SerializeField] private AudioSource player1LandingSource;
    [SerializeField] private AudioSource player1ObstacleSource;

    [Header("Player 2 Audio Sources")]
    [SerializeField] private AudioSource player2JumpSource;
    [SerializeField] private AudioSource player2SkiingSource;
    [SerializeField] private AudioSource player2LandingSource;
    [SerializeField] private AudioSource player2ObstacleSource;

    [Header("Ambient + music Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource gameSFXSource;
    [SerializeField] private AudioSource windAmbientSource;
    [SerializeField] private AudioSource snowAmbientSource;
    [SerializeField] private AudioSource mountainAmbientSource;

    [Header("Audio/Volume Settings")]
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float sfxVolume = 1.0f;
    [SerializeField] private float ambientVolume = 0.4f;
    [SerializeField] private float skiingVolume = 0.6f;

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
        PlayBackgroundMusic();
        StartAmbientSounds();
    }

    private void InitializeAudioSources()
    {
        // Initialize shared sources
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (gameSFXSource == null) gameSFXSource = gameObject.AddComponent<AudioSource>();

        // Initialize ambient sources
        if (windAmbientSource == null) windAmbientSource = gameObject.AddComponent<AudioSource>();
        if (snowAmbientSource == null) snowAmbientSource = gameObject.AddComponent<AudioSource>();
        if (mountainAmbientSource == null) mountainAmbientSource = gameObject.AddComponent<AudioSource>();

        // Initialize Player 1 sources
        if (player1JumpSource == null) player1JumpSource = gameObject.AddComponent<AudioSource>();
        if (player1SkiingSource == null) player1SkiingSource = gameObject.AddComponent<AudioSource>();
        if (player1LandingSource == null) player1LandingSource = gameObject.AddComponent<AudioSource>();
        if (player1ObstacleSource == null) player1ObstacleSource = gameObject.AddComponent<AudioSource>();

        // Initialize Player 2 sources
        if (player2JumpSource == null) player2JumpSource = gameObject.AddComponent<AudioSource>();
        if (player2SkiingSource == null) player2SkiingSource = gameObject.AddComponent<AudioSource>();
        if (player2LandingSource == null) player2LandingSource = gameObject.AddComponent<AudioSource>();
        if (player2ObstacleSource == null) player2ObstacleSource = gameObject.AddComponent<AudioSource>();

        // Configure all sources
        ConfigureAudioSource(musicSource, musicVolume, true);
        ConfigureAudioSource(gameSFXSource, sfxVolume, false);

        ConfigureAudioSource(windAmbientSource, ambientVolume, true);
        ConfigureAudioSource(snowAmbientSource, ambientVolume, true);
        ConfigureAudioSource(mountainAmbientSource, ambientVolume, true);

        ConfigureAudioSource(player1JumpSource, sfxVolume, false);
        ConfigureAudioSource(player1SkiingSource, skiingVolume, true); // Skiing is looped
        ConfigureAudioSource(player1LandingSource, sfxVolume, false);
        ConfigureAudioSource(player1ObstacleSource, sfxVolume, false);

        ConfigureAudioSource(player2JumpSource, sfxVolume, false);
        ConfigureAudioSource(player2SkiingSource, skiingVolume, true); // Skiing is looped
        ConfigureAudioSource(player2LandingSource, sfxVolume, false);
        ConfigureAudioSource(player2ObstacleSource, sfxVolume, false);
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

    private void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    #region Player Sound Methods

    public void PlayJumpSound(bool isPlayer1)
    {
        if (jumpSounds == null || jumpSounds.Length == 0) return;

        AudioSource source = isPlayer1 ? player1JumpSource : player2JumpSource;
        AudioClip randomJumpSound = GetRandomClip(jumpSounds);

        if (source != null && randomJumpSound != null)
        {
            source.PlayOneShot(randomJumpSound);
        }
    }

    public void PlayLandingSound(bool isPlayer1)
    {
        if (landingSounds == null || landingSounds.Length == 0) return;

        AudioSource source = isPlayer1 ? player1LandingSource : player2LandingSource;
        AudioClip randomLandingSound = GetRandomClip(landingSounds);

        if (source != null && randomLandingSound != null)
        {
            source.PlayOneShot(randomLandingSound);
        }
    }

    public void PlayObstacleHitSound(bool isPlayer1)
    {
        if (obstacleHitSounds == null || obstacleHitSounds.Length == 0) return;

        AudioSource source = isPlayer1 ? player1ObstacleSource : player2ObstacleSource;
        AudioClip randomHitSound = GetRandomClip(obstacleHitSounds);

        if (source != null && randomHitSound != null)
        {
            source.PlayOneShot(randomHitSound);
        }
    }

    public void StartSkiingSound(bool isPlayer1)
    {
        if (skiingSound == null) return;

        AudioSource source = isPlayer1 ? player1SkiingSource : player2SkiingSource;

        if (source != null && !source.isPlaying)
        {
            source.clip = skiingSound;
            source.Play();
        }
    }

    public void StopSkiingSound(bool isPlayer1)
    {
        AudioSource source = isPlayer1 ? player1SkiingSource : player2SkiingSource;

        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    #endregion

    #region Utility Methods

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;

        // Filter out null clips
        AudioClip[] validClips = System.Array.FindAll(clips, clip => clip != null);

        if (validClips.Length == 0) return null;

        int randomIndex = Random.Range(0, validClips.Length);
        return validClips[randomIndex];
    }

    #endregion

    #region Intro & Countdown Sounds

    public void PlayCharacterIntroSound()
    {
        if (characterIntroSound != null && gameSFXSource != null)
        {
            gameSFXSource.PlayOneShot(characterIntroSound);
        }
    }

    public void PlayCountdownNumberSound()
    {
        if (countdownNumberSound != null && gameSFXSource != null)
        {
            gameSFXSource.PlayOneShot(countdownNumberSound);
        }
    }

    public void PlayCountdownGoSound()
    {
        if (countdownGoSound != null && gameSFXSource != null)
        {
            gameSFXSource.PlayOneShot(countdownGoSound);
        }
    }

    #endregion

    #region Game State Sounds

    public void PlayLoseSound()
    {
        if (gameSFXSource != null && loseSound != null)
        {
            gameSFXSource.PlayOneShot(loseSound);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && gameSFXSource != null)
        {
            gameSFXSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Volume Controls

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);

        if (gameSFXSource != null) gameSFXSource.volume = sfxVolume;
        if (player1JumpSource != null) player1JumpSource.volume = sfxVolume;
        if (player1LandingSource != null) player1LandingSource.volume = sfxVolume;
        if (player1ObstacleSource != null) player1ObstacleSource.volume = sfxVolume;
        if (player2JumpSource != null) player2JumpSource.volume = sfxVolume;
        if (player2LandingSource != null) player2LandingSource.volume = sfxVolume;
        if (player2ObstacleSource != null) player2ObstacleSource.volume = sfxVolume;
    }

    public void SetSkiingVolume(float volume)
    {
        skiingVolume = Mathf.Clamp01(volume);
        if (player1SkiingSource != null) player1SkiingSource.volume = skiingVolume;
        if (player2SkiingSource != null) player2SkiingSource.volume = skiingVolume;
    }

    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        if (windAmbientSource != null) windAmbientSource.volume = ambientVolume;
        if (snowAmbientSource != null) snowAmbientSource.volume = ambientVolume;
        if (mountainAmbientSource != null) mountainAmbientSource.volume = ambientVolume;
    }

    #endregion

    #region Music Controls

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

    #endregion

    #region Ambient Sound Controls

    private void StartAmbientSounds()
    {
        PlayWindSound();
        PlaySnowSound();
        PlayMountainAmbience();
    }

    public void PlayWindSound()
    {
        if (windSound != null && windAmbientSource != null)
        {
            windAmbientSource.clip = windSound;
            windAmbientSource.Play();
        }
    }

    public void PlaySnowSound()
    {
        if (snowSound != null && snowAmbientSource != null)
        {
            snowAmbientSource.clip = snowSound;
            snowAmbientSource.Play();
        }
    }

    public void PlayMountainAmbience()
    {
        if (mountainAmbienceSound != null && mountainAmbientSource != null)
        {
            mountainAmbientSource.clip = mountainAmbienceSound;
            mountainAmbientSource.Play();
        }
    }

    public void StopWindSound()
    {
        if (windAmbientSource != null)
        {
            windAmbientSource.Stop();
        }
    }

    public void StopSnowSound()
    {
        if (snowAmbientSource != null)
        {
            snowAmbientSource.Stop();
        }
    }

    public void StopMountainAmbience()
    {
        if (mountainAmbientSource != null)
        {
            mountainAmbientSource.Stop();
        }
    }

    public void StopAllAmbientSounds()
    {
        StopWindSound();
        StopSnowSound();
        StopMountainAmbience();
    }

    #endregion
}