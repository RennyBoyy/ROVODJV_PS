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

    [Header("Ambient Sounds")]
    [SerializeField] private AudioClip[] birdChirpingSounds;
    [SerializeField] private AudioClip[] windmillCreakingSounds;
    [SerializeField] private AudioClip[] windSounds;
    [SerializeField] private float minAmbientInterval = 5f;
    [SerializeField] private float maxAmbientInterval = 15f;

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
    [SerializeField] private AudioSource ambientSource;

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
        if (ambientSource == null) ambientSource = gameObject.AddComponent<AudioSource>();

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
        ConfigureAudioSource(ambientSource, ambientVolume, false);

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

        Debug.Log($"Skipping {clip.name}, as both audio sources for ts sound are busy");
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
        if (ambientSource != null)
        {
            ambientSource.volume = ambientVolume;
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

    private void StartAmbientSounds()
    {
        StartCoroutine(PlayAmbientSounds());
    }

    private IEnumerator PlayAmbientSounds()
    {
        while (true)
        {
            float waitTime = Random.Range(minAmbientInterval, maxAmbientInterval);
            yield return new WaitForSeconds(waitTime);

            PlayRandomAmbientSound();
        }
    }

    private void PlayRandomAmbientSound()
    {
        System.Collections.Generic.List<AudioClip> allAmbientSounds = new System.Collections.Generic.List<AudioClip>();

        if (birdChirpingSounds != null)
            allAmbientSounds.AddRange(birdChirpingSounds);
        if (windmillCreakingSounds != null)
            allAmbientSounds.AddRange(windmillCreakingSounds);
        if (windSounds != null)
            allAmbientSounds.AddRange(windSounds);

        if (allAmbientSounds.Count > 0)
        {
            AudioClip randomClip = allAmbientSounds[Random.Range(0, allAmbientSounds.Count)];
            PlayAmbientSound(randomClip);
        }
    }

    private void PlayAmbientSound(AudioClip clip)
    {
        if (clip != null && ambientSource != null)
        {
            ambientSource.PlayOneShot(clip);
        }
    }

    public void PlayRandomBirdChirping()
    {
        if (birdChirpingSounds != null && birdChirpingSounds.Length > 0)
        {
            AudioClip randomBird = birdChirpingSounds[Random.Range(0, birdChirpingSounds.Length)];
            PlayAmbientSound(randomBird);
        }
    }

    public void PlayRandomWindmillCreaking()
    {
        if (windmillCreakingSounds != null && windmillCreakingSounds.Length > 0)
        {
            AudioClip randomCreaking = windmillCreakingSounds[Random.Range(0, windmillCreakingSounds.Length)];
            PlayAmbientSound(randomCreaking);
        }
    }

    public void PlayRandomWindSound()
    {
        if (windSounds != null && windSounds.Length > 0)
        {
            AudioClip randomWind = windSounds[Random.Range(0, windSounds.Length)];
            PlayAmbientSound(randomWind);
        }
    }
}