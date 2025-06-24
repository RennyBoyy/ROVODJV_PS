using UnityEngine;

// Priority system for audio
public enum AudioPriority
{
    Low = 0,      // Background/ambient sounds
    Normal = 1,   // Regular gameplay sounds  
    High = 2,     // Important feedback
    Critical = 3  // Game-changing events
}

// Audio categories for organization
public enum AudioCategory
{
    Player, Enemy, GameState, Ambient, Music
} 