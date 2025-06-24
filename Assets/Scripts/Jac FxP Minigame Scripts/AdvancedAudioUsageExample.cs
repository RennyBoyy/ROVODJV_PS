using UnityEngine;

// Example script showing how to use the AdvancedAudioManager
// This demonstrates how to replace the old FruityGameConfigurator calls
public class AdvancedAudioUsageExample : MonoBehaviour
{
    [Header("Audio Manager Reference")]
    [SerializeField] private AdvancedAudioManager audioManager;
    
    void Start()
    {
        // Get reference to the AdvancedAudioManager if not assigned
        if (audioManager == null)
            audioManager = AdvancedAudioManager.Instance;
    }
    
    // Example: How to replace old audio calls in PlayerScript
    public void ExamplePlayerAudioCalls()
    {
        // OLD WAY (FruityGameConfigurator):
        // FruityGameConfigurator.Instance?.PlayJumpSound(IsPlayer1());
        // FruityGameConfigurator.Instance?.PlayThrowSound(IsPlayer1());
        // FruityGameConfigurator.Instance?.PlayEmptyThrowSound(IsPlayer1());
        // FruityGameConfigurator.Instance?.PlayReloadSound(IsPlayer1());
        
        // NEW WAY (AdvancedAudioManager):
        audioManager.PlayPlayerSound("Jump");
        audioManager.PlayPlayerSound("Throw");
        audioManager.PlayPlayerSound("EmptyThrow");
        audioManager.PlayPlayerSound("Reload");
    }
    
    // Example: How to replace old audio calls in MonsterBad
    public void ExampleEnemyAudioCalls()
    {
        // OLD WAY:
        // FruityGameConfigurator.Instance?.PlayScarecrowSpawnSound();
        // FruityGameConfigurator.Instance?.PlayScarecrowDeathSound();
        // FruityGameConfigurator.Instance?.PlayScarecrowEatingSound();
        // FruityGameConfigurator.Instance?.PlayPumpkinEatenSound();
        
        // NEW WAY:
        audioManager.PlayEnemySound("Spawn");
        audioManager.PlayEnemySound("Death");
        audioManager.PlayEnemySound("Eating");
        audioManager.PlayEnemySound("PumpkinEaten");
    }
    
    // Example: How to replace old audio calls in GameIntroManager
    public void ExampleGameStateAudioCalls()
    {
        // OLD WAY:
        // FruityGameConfigurator.Instance?.PlayCountdownNumberSound();
        // FruityGameConfigurator.Instance?.PlayCountdownGoSound();
        // FruityGameConfigurator.Instance?.PlayLoseSound();
        
        // NEW WAY:
        audioManager.PlayGameStateSound("CountdownNumber");
        audioManager.PlayGameStateSound("CountdownGo");
        audioManager.PlayGameStateSound("Lose");
    }
    
    // Example: Volume control
    public void ExampleVolumeControl()
    {
        // Set volumes (0-1 range)
        audioManager.SetMusicVolume(0.5f);
        audioManager.SetSFXVolume(0.8f);
        audioManager.SetAmbientVolume(0.3f);
    }
    
    // Example: Music control with fade effects
    public void ExampleMusicControl()
    {
        // Play music with fade in
        AudioClip newMusic = null; // Assign your music clip
        audioManager.PlayMusic(newMusic, true); // true = fade in
        
        // Stop music with fade out
        audioManager.StopMusic(true); // true = fade out
    }
    
    // Example: Ambient sounds
    public void ExampleAmbientSounds()
    {
        // Start ambient sounds with fade in
        audioManager.PlayAmbientSound("BirdChirping", true);
        audioManager.PlayAmbientSound("WindmillCreaking", true);
        audioManager.PlayAmbientSound("WindSound", true);
        
        // Stop all ambient sounds
        audioManager.StopAllAmbientSounds();
    }
    
    // Example: Utility methods
    public void ExampleUtilityMethods()
    {
        // Pause all sounds
        audioManager.PauseAllSounds();
        
        // Resume all sounds
        audioManager.ResumeAllSounds();
        
        // Stop all sounds
        audioManager.StopAllSounds();
    }
}

// Example: How to modify existing scripts to use the new system
public class PlayerScriptAudioExample : MonoBehaviour
{
    private AdvancedAudioManager audioManager;
    
    void Start()
    {
        audioManager = AdvancedAudioManager.Instance;
    }
    
    // Example modification of PlayerScript.Shoot() method
    public void Shoot(bool hasAmmo)
    {
        if (!hasAmmo)
        {
            // OLD: FruityGameConfigurator.Instance?.PlayEmptyThrowSound(IsPlayer1());
            audioManager.PlayPlayerSound("EmptyThrow");
            return;
        }
        
        // ... shooting logic ...
        
        // OLD: FruityGameConfigurator.Instance?.PlayThrowSound(IsPlayer1());
        audioManager.PlayPlayerSound("Throw");
    }
    
    // Example modification of PlayerScript movement
    public void OnJump()
    {
        // OLD: FruityGameConfigurator.Instance?.PlayJumpSound(IsPlayer1());
        audioManager.PlayPlayerSound("Jump");
    }
    
    // Example modification of PlayerScript reload
    public void OnReload()
    {
        // OLD: FruityGameConfigurator.Instance?.PlayReloadSound(IsPlayer1());
        audioManager.PlayPlayerSound("Reload");
    }
}

// Example: How to modify MonsterBad script
public class MonsterBadAudioExample : MonoBehaviour
{
    private AdvancedAudioManager audioManager;
    
    void Start()
    {
        audioManager = AdvancedAudioManager.Instance;
    }
    
    // Example modification of MonsterBad.OnTriggerEnter()
    public void OnEnemySpawn()
    {
        // OLD: FruityGameConfigurator.Instance?.PlayScarecrowSpawnSound();
        audioManager.PlayEnemySound("Spawn");
    }
    
    public void OnEnemyDeath()
    {
        // OLD: FruityGameConfigurator.Instance?.PlayScarecrowDeathSound();
        audioManager.PlayEnemySound("Death");
    }
    
    public void OnEnemyEating()
    {
        // OLD: FruityGameConfigurator.Instance?.PlayScarecrowEatingSound();
        audioManager.PlayEnemySound("Eating");
    }
    
    public void OnPumpkinEaten()
    {
        // OLD: FruityGameConfigurator.Instance?.PlayPumpkinEatenSound();
        audioManager.PlayEnemySound("PumpkinEaten");
    }
} 