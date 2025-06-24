using System.Collections.Generic;
using UnityEngine;

// Audio Pool for efficient AudioSource management
public class AudioPool : MonoBehaviour
{
    [SerializeField] private int poolSize = 15;
    private Queue<AudioSource> availableSources = new Queue<AudioSource>();
    private List<AudioSource> activeSources = new List<AudioSource>();
    
    void Start()
    {
        // Pre-create AudioSources
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 1f;
            availableSources.Enqueue(source);
        }
    }
    
    public AudioSource GetAudioSource()
    {
        AudioSource source;
        if (availableSources.Count > 0)
        {
            source = availableSources.Dequeue();
        }
        else
        {
            // Create new one if pool is empty
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 1f;
        }
        
        activeSources.Add(source);
        return source;
    }
    
    public void ReturnAudioSource(AudioSource source)
    {
        if (source == null) return;
        
        source.Stop();
        source.clip = null;
        source.loop = false;
        activeSources.Remove(source);
        availableSources.Enqueue(source);
    }
    
    // Auto-return when sound finishes
    void Update()
    {
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            if (activeSources[i] != null && !activeSources[i].isPlaying && !activeSources[i].loop)
            {
                ReturnAudioSource(activeSources[i]);
            }
        }
    }
} 