using UnityEngine;

// ScriptableObject for Player Audio Data
[CreateAssetMenu(fileName = "PlayerAudioData", menuName = "Audio/Player Audio Data")]
public class PlayerAudioData : ScriptableObject
{
    [Header("Jump Sound")]
    public string jumpSoundName = "Jump";
    public AudioClip jumpSoundClip;
    [Range(0f, 1f)] public float jumpSoundVolume = 1f;
    [Range(0.1f, 3f)] public float jumpSoundPitch = 1f;
    public AudioPriority jumpSoundPriority = AudioPriority.Normal;
    
    [Header("Throw Sound")]
    public string throwSoundName = "Throw";
    public AudioClip throwSoundClip;
    [Range(0f, 1f)] public float throwSoundVolume = 1f;
    [Range(0.1f, 3f)] public float throwSoundPitch = 1f;
    public AudioPriority throwSoundPriority = AudioPriority.Normal;
    
    [Header("Empty Throw Sound")]
    public string emptyThrowSoundName = "EmptyThrow";
    public AudioClip emptyThrowSoundClip;
    [Range(0f, 1f)] public float emptyThrowSoundVolume = 1f;
    [Range(0.1f, 3f)] public float emptyThrowSoundPitch = 1f;
    public AudioPriority emptyThrowSoundPriority = AudioPriority.High;
    
    [Header("Reload Sound")]
    public string reloadSoundName = "Reload";
    public AudioClip reloadSoundClip;
    [Range(0f, 1f)] public float reloadSoundVolume = 1f;
    [Range(0.1f, 3f)] public float reloadSoundPitch = 1f;
    public AudioPriority reloadSoundPriority = AudioPriority.Normal;
}

// ScriptableObject for Enemy Audio Data
[CreateAssetMenu(fileName = "EnemyAudioData", menuName = "Audio/Enemy Audio Data")]
public class EnemyAudioData : ScriptableObject
{
    [Header("Spawn Sound")]
    public string spawnSoundName = "Spawn";
    public AudioClip spawnSoundClip;
    [Range(0f, 1f)] public float spawnSoundVolume = 1f;
    [Range(0.1f, 3f)] public float spawnSoundPitch = 1f;
    public AudioPriority spawnSoundPriority = AudioPriority.Normal;
    
    [Header("Death Sound")]
    public string deathSoundName = "Death";
    public AudioClip deathSoundClip;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;
    [Range(0.1f, 3f)] public float deathSoundPitch = 1f;
    public AudioPriority deathSoundPriority = AudioPriority.High;
    
    [Header("Eating Sound")]
    public string eatingSoundName = "Eating";
    public AudioClip eatingSoundClip;
    [Range(0f, 1f)] public float eatingSoundVolume = 1f;
    [Range(0.1f, 3f)] public float eatingSoundPitch = 1f;
    public AudioPriority eatingSoundPriority = AudioPriority.High;
    
    [Header("Pumpkin Eaten Sound")]
    public string pumpkinEatenSoundName = "PumpkinEaten";
    public AudioClip pumpkinEatenSoundClip;
    [Range(0f, 1f)] public float pumpkinEatenSoundVolume = 1f;
    [Range(0.1f, 3f)] public float pumpkinEatenSoundPitch = 1f;
    public AudioPriority pumpkinEatenSoundPriority = AudioPriority.High;
}

// ScriptableObject for Game State Audio Data
[CreateAssetMenu(fileName = "GameStateAudioData", menuName = "Audio/Game State Audio Data")]
public class GameStateAudioData : ScriptableObject
{
    [Header("Lose Sound")]
    public string loseSoundName = "Lose";
    public AudioClip loseSoundClip;
    [Range(0f, 1f)] public float loseSoundVolume = 1f;
    public AudioPriority loseSoundPriority = AudioPriority.Critical;
    
    [Header("Countdown Number Sound")]
    public string countdownNumberSoundName = "CountdownNumber";
    public AudioClip countdownNumberSoundClip;
    [Range(0f, 1f)] public float countdownNumberSoundVolume = 1f;
    public AudioPriority countdownNumberSoundPriority = AudioPriority.Critical;
    
    [Header("Countdown Go Sound")]
    public string countdownGoSoundName = "CountdownGo";
    public AudioClip countdownGoSoundClip;
    [Range(0f, 1f)] public float countdownGoSoundVolume = 1f;
    public AudioPriority countdownGoSoundPriority = AudioPriority.Critical;
    
    [Header("Character Intro Sound")]
    public string characterIntroSoundName = "CharacterIntro";
    public AudioClip characterIntroSoundClip;
    [Range(0f, 1f)] public float characterIntroSoundVolume = 1f;
    public AudioPriority characterIntroSoundPriority = AudioPriority.Critical;
}

// ScriptableObject for Ambient Audio Data
[CreateAssetMenu(fileName = "AmbientAudioData", menuName = "Audio/Ambient Audio Data")]
public class AmbientAudioData : ScriptableObject
{
    [Header("Bird Chirping")]
    public string birdChirpingName = "BirdChirping";
    public AudioClip birdChirpingClip;
    [Range(0f, 1f)] public float birdChirpingVolume = 0.4f;
    public AudioPriority birdChirpingPriority = AudioPriority.Low;
    
    [Header("Windmill Creaking")]
    public string windmillCreakingName = "WindmillCreaking";
    public AudioClip windmillCreakingClip;
    [Range(0f, 1f)] public float windmillCreakingVolume = 0.4f;
    public AudioPriority windmillCreakingPriority = AudioPriority.Low;
    
    [Header("Wind Sound")]
    public string windSoundName = "WindSound";
    public AudioClip windSoundClip;
    [Range(0f, 1f)] public float windSoundVolume = 0.4f;
    public AudioPriority windSoundPriority = AudioPriority.Low;
} 