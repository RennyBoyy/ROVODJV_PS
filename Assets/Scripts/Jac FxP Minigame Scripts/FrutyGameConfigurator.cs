using System.Collections;
using UnityEngine;

public class FruityGameConfigurator : MonoBehaviour
{
    public static FruityGameConfigurator Instance { get; private set; }

    [Header("Player Sound Effects")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip throwSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptyThrowSound;

    [Header("Scarecrow/Enemy Sound Effects")]
    [SerializeField] private AudioClip scarecrowSpawnSound;
    [SerializeField] private AudioClip scarecrowEatingSound;
    [SerializeField] private AudioClip pumpkinEatenSound;
    [SerializeField] private AudioClip scarecrowDeathSound;

    [Header("Game State Sounds")]
    [SerializeField] private AudioClip loseSound;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Ambient Sounds (Looped)")]
    [SerializeField] private AudioClip birdChirpingSound;
    [SerializeField] private AudioClip windmillCreakingSound;
    [SerializeField] private AudioClip windSound;

    [Header("Intro & Countdown Sounds")]
    [SerializeField] private AudioClip characterIntroSound;
    [SerializeField] private AudioClip countdownNumberSound;
    [SerializeField] private AudioClip countdownGoSound;

    [Header("Player 1 Audio Sources")]
    [SerializeField] private AudioSource player1MovementSource;
    [SerializeField] private AudioSource player1ThrowingSource;

    [Header("Player 2 Audio Sources")]
    [SerializeField] private AudioSource player2MovementSource;
    [SerializeField] private AudioSource player2ThrowingSource;

    [Header("Enemy Audio Sources")]
    [SerializeField] private AudioSource enemySpawnSource1;
    [SerializeField] private AudioSource enemySpawnSource2;
    [SerializeField] private AudioSource enemyDeathSource1;
    [SerializeField] private AudioSource enemyDeathSource2;
    [SerializeField] private AudioSource enemyEatSource1;
    [SerializeField] private AudioSource enemyEatSource2;

    [Header("Shared Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource gameSFXSource;
    [SerializeField] private AudioSource birdAmbientSource;
    [SerializeField] private AudioSource windmillAmbientSource;
    [SerializeField] private AudioSource windAmbientSource;

    [Header("Audio Settings")]
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float sfxVolume = 1.0f;
    [SerializeField] private float ambientVolume = 0.4f;

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
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (gameSFXSource == null) gameSFXSource = gameObject.AddComponent<AudioSource>();

        if (birdAmbientSource == null) birdAmbientSource = gameObject.AddComponent<AudioSource>();
        if (windmillAmbientSource == null) windmillAmbientSource = gameObject.AddComponent<AudioSource>();
        if (windAmbientSource == null) windAmbientSource = gameObject.AddComponent<AudioSource>();

        if (player1MovementSource == null) player1MovementSource = gameObject.AddComponent<AudioSource>();
        if (player1ThrowingSource == null) player1ThrowingSource = gameObject.AddComponent<AudioSource>();

        if (player2MovementSource == null) player2MovementSource = gameObject.AddComponent<AudioSource>();
        if (player2ThrowingSource == null) player2ThrowingSource = gameObject.AddComponent<AudioSource>();

        if (enemySpawnSource1 == null) enemySpawnSource1 = gameObject.AddComponent<AudioSource>();
        if (enemySpawnSource2 == null) enemySpawnSource2 = gameObject.AddComponent<AudioSource>();
        if (enemyDeathSource1 == null) enemyDeathSource1 = gameObject.AddComponent<AudioSource>();
        if (enemyDeathSource2 == null) enemyDeathSource2 = gameObject.AddComponent<AudioSource>();
        if (enemyEatSource1 == null) enemyEatSource1 = gameObject.AddComponent<AudioSource>();
        if (enemyEatSource2 == null) enemyEatSource2 = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource(musicSource, musicVolume, true);
        ConfigureAudioSource(gameSFXSource, sfxVolume, false);

        ConfigureAudioSource(birdAmbientSource, ambientVolume, true);
        ConfigureAudioSource(windmillAmbientSource, ambientVolume, true);
        ConfigureAudioSource(windAmbientSource, ambientVolume, true);

        ConfigureAudioSource(player1MovementSource, sfxVolume, false);
        ConfigureAudioSource(player1ThrowingSource, sfxVolume, false);
        ConfigureAudioSource(player2MovementSource, sfxVolume, false);
        ConfigureAudioSource(player2ThrowingSource, sfxVolume, false);

        ConfigureAudioSource(enemySpawnSource1, sfxVolume, false);
        ConfigureAudioSource(enemySpawnSource2, sfxVolume, false);
        ConfigureAudioSource(enemyDeathSource1, sfxVolume, false);
        ConfigureAudioSource(enemyDeathSource2, sfxVolume, false);
        ConfigureAudioSource(enemyEatSource1, sfxVolume, false);
        ConfigureAudioSource(enemyEatSource2, sfxVolume, false);
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

    public void PlayJumpSound(bool isPlayer1)
    {
        AudioSource source = isPlayer1 ? player1MovementSource : player2MovementSource;
        if (source != null && jumpSound != null)
        {
            source.PlayOneShot(jumpSound);
        }
    }

    public void PlayThrowSound(bool isPlayer1)
    {
        AudioSource source = isPlayer1 ? player1ThrowingSource : player2ThrowingSource;
        if (source != null && throwSound != null)
        {
            source.PlayOneShot(throwSound);
        }
    }

    public void PlayReloadSound(bool isPlayer1)
    {
        AudioSource source = isPlayer1 ? player1ThrowingSource : player2ThrowingSource;
        if (source != null && reloadSound != null)
        {
            source.PlayOneShot(reloadSound);
        }
    }

    public void PlayEmptyThrowSound(bool isPlayer1)
    {
        AudioSource source = isPlayer1 ? player1ThrowingSource : player2ThrowingSource;
        if (source != null && emptyThrowSound != null)
        {
            source.PlayOneShot(emptyThrowSound);
        }
    }

    public void PlayScarecrowSpawnSound()
    {
        PlayEnemySound(scarecrowSpawnSound, enemySpawnSource1, enemySpawnSource2);
    }

    public void PlayScarecrowEatingSound()
    {
        PlayEnemySound(scarecrowEatingSound, enemyEatSource1, enemyEatSource2);
    }

    public void PlayPumpkinEatenSound()
    {
        PlayEnemySound(pumpkinEatenSound, enemyEatSource1, enemyEatSource2);
    }

    public void PlayScarecrowDeathSound()
    {
        PlayEnemySound(scarecrowDeathSound, enemyDeathSource1, enemyDeathSource2);
    }

    private void PlayEnemySound(AudioClip clip, AudioSource source1, AudioSource source2)
    {
        if (clip == null) return;

        if (source1 != null && !source1.isPlaying)
        {
            source1.PlayOneShot(clip);
            return;
        }

        if (source2 != null && !source2.isPlaying)
        {
            source2.PlayOneShot(clip);
            return;
        }

        Debug.Log($"Skipping {clip.name}, as both audio sources for this sound are bussy");
    }

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
        if (player1MovementSource != null) player1MovementSource.volume = sfxVolume;
        if (player1ThrowingSource != null) player1ThrowingSource.volume = sfxVolume;
        if (player2MovementSource != null) player2MovementSource.volume = sfxVolume;
        if (player2ThrowingSource != null) player2ThrowingSource.volume = sfxVolume;
        if (enemySpawnSource1 != null) enemySpawnSource1.volume = sfxVolume;
        if (enemySpawnSource2 != null) enemySpawnSource2.volume = sfxVolume;
        if (enemyDeathSource1 != null) enemyDeathSource1.volume = sfxVolume;
        if (enemyDeathSource2 != null) enemyDeathSource2.volume = sfxVolume;
        if (enemyEatSource1 != null) enemyEatSource1.volume = sfxVolume;
        if (enemyEatSource2 != null) enemyEatSource2.volume = sfxVolume;
    }

    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        if (birdAmbientSource != null) birdAmbientSource.volume = ambientVolume;
        if (windmillAmbientSource != null) windmillAmbientSource.volume = ambientVolume;
        if (windAmbientSource != null) windAmbientSource.volume = ambientVolume;
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

    private void StartAmbientSounds()
    {
        PlayBirdChirping();
        PlayWindmillCreaking();
        PlayWindSound();
    }

    public void PlayBirdChirping()
    {
        if (birdChirpingSound != null && birdAmbientSource != null)
        {
            birdAmbientSource.clip = birdChirpingSound;
            birdAmbientSource.Play();
        }
    }

    public void PlayWindmillCreaking()
    {
        if (windmillCreakingSound != null && windmillAmbientSource != null)
        {
            windmillAmbientSource.clip = windmillCreakingSound;
            windmillAmbientSource.Play();
        }
    }

    public void PlayWindSound()
    {
        if (windSound != null && windAmbientSource != null)
        {
            windAmbientSource.clip = windSound;
            windAmbientSource.Play();
        }
    }

    public void StopBirdChirping()
    {
        if (birdAmbientSource != null)
        {
            birdAmbientSource.Stop();
        }
    }

    public void StopWindmillCreaking()
    {
        if (windmillAmbientSource != null)
        {
            windmillAmbientSource.Stop();
        }
    }

    public void StopWindSound()
    {
        if (windAmbientSource != null)
        {
            windAmbientSource.Stop();
        }
    }

    public void StopAllAmbientSounds()
    {
        StopBirdChirping();
        StopWindmillCreaking();
        StopWindSound();
    }
}