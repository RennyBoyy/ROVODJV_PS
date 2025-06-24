using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Main Advanced Audio Manager
public class AdvancedAudioManager : MonoBehaviour
{
    public static AdvancedAudioManager Instance { get; private set; }
    
    [Header("Audio Data Assets")]
    [SerializeField] private PlayerAudioData playerAudio;
    [SerializeField] private EnemyAudioData enemyAudio;
    [SerializeField] private GameStateAudioData gameStateAudio;
    [SerializeField] private AmbientAudioData ambientAudio;
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambientSource;
    
    [Header("Audio Settings")]
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float sfxVolume = 1.0f;
    [SerializeField] private float ambientVolume = 0.4f;
    [SerializeField] private int maxConcurrentSounds = 12;
    
    private AudioPool sfxPool;
    private Dictionary<string, SoundData> soundDictionary = new Dictionary<string, SoundData>();
    private List<AudioSource> activeSources = new List<AudioSource>();
    
    // Sound data structure for internal use
    [System.Serializable]
    public class SoundData
    {
        public string name;
        public AudioClip clip;
        public float volume = 1f;
        public float pitch = 1f;
        public bool loop = false;
        public AudioPriority priority = AudioPriority.Normal;
        public bool interruptible = true;
        public int maxConcurrent = 2;
        public AudioCategory category = AudioCategory.Player;
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        BuildSoundDictionary();
        StartAmbientSounds();
    }
    
    private void InitializeAudioManager()
    {
        // Add AudioPool component if not present
        sfxPool = GetComponent<AudioPool>();
        if (sfxPool == null)
        {
            sfxPool = gameObject.AddComponent<AudioPool>();
            Debug.Log("AudioPool component added to AdvancedAudioManager");
        }
        
        // Add AudioFadeManager component if not present
        if (GetComponent<AudioFadeManager>() == null)
        {
            gameObject.AddComponent<AudioFadeManager>();
            Debug.Log("AudioFadeManager component added to AdvancedAudioManager");
        }
        
        // Create audio sources if not assigned
        if (musicSource == null)
        {
            GameObject musicGO = new GameObject("MusicSource");
            musicGO.transform.SetParent(transform);
            musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            Debug.Log("Music AudioSource created");
        }
        
        if (ambientSource == null)
        {
            GameObject ambientGO = new GameObject("AmbientSource");
            ambientGO.transform.SetParent(transform);
            ambientSource = ambientGO.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            Debug.Log("Ambient AudioSource created");
        }
        
        // Configure sources
        musicSource.volume = musicVolume;
        ambientSource.volume = ambientVolume;
        
        Debug.Log("AdvancedAudioManager initialized successfully");
    }
    
    private void BuildSoundDictionary()
    {
        soundDictionary.Clear();
        
        // Add player sounds
        if (playerAudio != null)
        {
            AddSoundData(playerAudio.jumpSoundName, playerAudio.jumpSoundClip, playerAudio.jumpSoundVolume, playerAudio.jumpSoundPitch, playerAudio.jumpSoundPriority, AudioCategory.Player);
            AddSoundData(playerAudio.throwSoundName, playerAudio.throwSoundClip, playerAudio.throwSoundVolume, playerAudio.throwSoundPitch, playerAudio.throwSoundPriority, AudioCategory.Player);
            AddSoundData(playerAudio.emptyThrowSoundName, playerAudio.emptyThrowSoundClip, playerAudio.emptyThrowSoundVolume, playerAudio.emptyThrowSoundPitch, playerAudio.emptyThrowSoundPriority, AudioCategory.Player);
            AddSoundData(playerAudio.reloadSoundName, playerAudio.reloadSoundClip, playerAudio.reloadSoundVolume, playerAudio.reloadSoundPitch, playerAudio.reloadSoundPriority, AudioCategory.Player);
        }
        
        // Add enemy sounds
        if (enemyAudio != null)
        {
            AddSoundData(enemyAudio.spawnSoundName, enemyAudio.spawnSoundClip, enemyAudio.spawnSoundVolume, enemyAudio.spawnSoundPitch, enemyAudio.spawnSoundPriority, AudioCategory.Enemy);
            AddSoundData(enemyAudio.deathSoundName, enemyAudio.deathSoundClip, enemyAudio.deathSoundVolume, enemyAudio.deathSoundPitch, enemyAudio.deathSoundPriority, AudioCategory.Enemy);
            AddSoundData(enemyAudio.eatingSoundName, enemyAudio.eatingSoundClip, enemyAudio.eatingSoundVolume, enemyAudio.eatingSoundPitch, enemyAudio.eatingSoundPriority, AudioCategory.Enemy);
            AddSoundData(enemyAudio.pumpkinEatenSoundName, enemyAudio.pumpkinEatenSoundClip, enemyAudio.pumpkinEatenSoundVolume, enemyAudio.pumpkinEatenSoundPitch, enemyAudio.pumpkinEatenSoundPriority, AudioCategory.Enemy);
        }
        
        // Add game state sounds
        if (gameStateAudio != null)
        {
            AddSoundData(gameStateAudio.loseSoundName, gameStateAudio.loseSoundClip, gameStateAudio.loseSoundVolume, 1f, gameStateAudio.loseSoundPriority, AudioCategory.GameState);
            AddSoundData(gameStateAudio.countdownNumberSoundName, gameStateAudio.countdownNumberSoundClip, gameStateAudio.countdownNumberSoundVolume, 1f, gameStateAudio.countdownNumberSoundPriority, AudioCategory.GameState);
            AddSoundData(gameStateAudio.countdownGoSoundName, gameStateAudio.countdownGoSoundClip, gameStateAudio.countdownGoSoundVolume, 1f, gameStateAudio.countdownGoSoundPriority, AudioCategory.GameState);
            AddSoundData(gameStateAudio.characterIntroSoundName, gameStateAudio.characterIntroSoundClip, gameStateAudio.characterIntroSoundVolume, 1f, gameStateAudio.characterIntroSoundPriority, AudioCategory.GameState);
        }
        
        // Add ambient sounds
        if (ambientAudio != null)
        {
            AddSoundData(ambientAudio.birdChirpingName, ambientAudio.birdChirpingClip, ambientAudio.birdChirpingVolume, 1f, ambientAudio.birdChirpingPriority, AudioCategory.Ambient, true);
            AddSoundData(ambientAudio.windmillCreakingName, ambientAudio.windmillCreakingClip, ambientAudio.windmillCreakingVolume, 1f, ambientAudio.windmillCreakingPriority, AudioCategory.Ambient, true);
            AddSoundData(ambientAudio.windSoundName, ambientAudio.windSoundClip, ambientAudio.windSoundVolume, 1f, ambientAudio.windSoundPriority, AudioCategory.Ambient, true);
        }
    }
    
    private void AddSoundData(string name, AudioClip clip, float volume, float pitch, AudioPriority priority, AudioCategory category, bool loop = false)
    {
        if (!string.IsNullOrEmpty(name) && clip != null)
        {
            var soundData = new SoundData
            {
                name = name,
                clip = clip,
                volume = volume,
                pitch = pitch,
                loop = loop,
                priority = priority,
                interruptible = priority != AudioPriority.Critical,
                maxConcurrent = priority == AudioPriority.Critical ? 1 : 2,
                category = category
            };
            
            soundDictionary[name] = soundData;
        }
    }
    
    // Public methods for playing sounds
    public void PlayPlayerSound(string soundName, bool fadeIn = false)
    {
        PlaySound(soundName, fadeIn);
    }
    
    public void PlayEnemySound(string soundName, bool fadeIn = false)
    {
        PlaySound(soundName, fadeIn);
    }
    
    public void PlayGameStateSound(string soundName, bool fadeIn = false)
    {
        PlaySound(soundName, fadeIn);
    }
    
    public void PlayAmbientSound(string soundName, bool fadeIn = false)
    {
        PlaySound(soundName, fadeIn);
    }
    
    // Main sound playing method with prioritization
    public void PlaySound(string soundName, bool fadeIn = false)
    {
        if (!soundDictionary.ContainsKey(soundName))
        {
            Debug.LogWarning($"Sound '{soundName}' not found in audio dictionary!");
            return;
        }
        
        var soundData = soundDictionary[soundName];
        
        // Check if we need to stop lower priority sounds
        if (activeSources.Count >= maxConcurrentSounds)
        {
            StopLowestPrioritySounds(soundData.priority);
        }
        
        // Check concurrent sound limit for this specific sound
        int currentPlaying = CountCurrentlyPlaying(soundName);
        if (currentPlaying >= soundData.maxConcurrent)
        {
            Debug.Log($"Max concurrent sounds reached for '{soundName}' ({soundData.maxConcurrent})");
            return;
        }
        
        // Get audio source from pool
        AudioSource source = sfxPool.GetAudioSource();
        source.clip = soundData.clip;
        source.volume = soundData.volume * sfxVolume;
        source.pitch = soundData.pitch;
        source.loop = soundData.loop;
        
        activeSources.Add(source);
        
        if (fadeIn)
        {
            StartCoroutine(AudioFadeManager.Instance.FadeIn(source, 0.5f, soundData.volume * sfxVolume));
        }
        else
        {
            source.Play();
        }
    }
    
    private void StopLowestPrioritySounds(AudioPriority newPriority)
    {
        var sortedSources = activeSources
            .Where(s => s != null && s.isPlaying)
            .OrderBy(s => GetSoundPriority(s))
            .ToList();
            
        int soundsToStop = activeSources.Count - maxConcurrentSounds + 1;
        
        for (int i = 0; i < soundsToStop && i < sortedSources.Count; i++)
        {
            var source = sortedSources[i];
            if (GetSoundPriority(source) < (int)newPriority)
            {
                source.Stop();
                activeSources.Remove(source);
                sfxPool.ReturnAudioSource(source);
            }
        }
    }
    
    private int GetSoundPriority(AudioSource source)
    {
        foreach (var soundData in soundDictionary.Values)
        {
            if (soundData.clip == source.clip)
                return (int)soundData.priority;
        }
        return 0; // Default to low priority
    }
    
    private int CountCurrentlyPlaying(string soundName)
    {
        if (!soundDictionary.ContainsKey(soundName)) return 0;
        
        var targetClip = soundDictionary[soundName].clip;
        return activeSources.Count(s => s != null && s.isPlaying && s.clip == targetClip);
    }
    
    // Volume control methods
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        // Update all active sources
        foreach (var source in activeSources)
        {
            if (source != null)
            {
                // Find the original sound data to get base volume
                foreach (var soundData in soundDictionary.Values)
                {
                    if (soundData.clip == source.clip)
                    {
                        source.volume = soundData.volume * sfxVolume;
                        break;
                    }
                }
            }
        }
    }
    
    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        if (ambientSource != null)
            ambientSource.volume = ambientVolume;
    }
    
    // Music control methods
    public void PlayMusic(AudioClip music, bool fadeIn = true)
    {
        if (musicSource != null && music != null)
        {
            if (fadeIn)
            {
                StartCoroutine(AudioFadeManager.Instance.Crossfade(musicSource, musicSource, 1f));
            }
            else
            {
                musicSource.clip = music;
                musicSource.Play();
            }
        }
    }
    
    public void StopMusic(bool fadeOut = true)
    {
        if (musicSource != null)
        {
            if (fadeOut)
            {
                StartCoroutine(AudioFadeManager.Instance.FadeOut(musicSource, 1f));
            }
            else
            {
                musicSource.Stop();
            }
        }
    }
    
    // Ambient sound methods
    private void StartAmbientSounds()
    {
        if (ambientAudio != null)
        {
            PlayAmbientSound("BirdChirping", true);
            PlayAmbientSound("WindmillCreaking", true);
            PlayAmbientSound("WindSound", true);
        }
    }
    
    public void StopAllAmbientSounds()
    {
        var ambientSources = activeSources.Where(s => 
            soundDictionary.Values.Any(sd => sd.clip == s.clip && sd.category == AudioCategory.Ambient)).ToList();
        
        foreach (var source in ambientSources)
        {
            source.Stop();
            activeSources.Remove(source);
            sfxPool.ReturnAudioSource(source);
        }
    }
    
    // Utility methods
    public void StopAllSounds()
    {
        foreach (var source in activeSources.ToList())
        {
            if (source != null)
            {
                source.Stop();
                sfxPool.ReturnAudioSource(source);
            }
        }
        activeSources.Clear();
    }
    
    public void PauseAllSounds()
    {
        foreach (var source in activeSources)
        {
            if (source != null && source.isPlaying)
                source.Pause();
        }
    }
    
    public void ResumeAllSounds()
    {
        foreach (var source in activeSources)
        {
            if (source != null)
                source.UnPause();
        }
    }
} 