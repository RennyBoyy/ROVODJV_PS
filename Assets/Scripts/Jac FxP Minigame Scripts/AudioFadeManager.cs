using System.Collections;
using UnityEngine;

// Audio Fade Manager for smooth transitions
public class AudioFadeManager : MonoBehaviour
{
    public static AudioFadeManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Fade in
    public IEnumerator FadeIn(AudioSource source, float duration, float targetVolume = 1f)
    {
        float startVolume = 0f;
        source.volume = startVolume;
        source.Play();
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }
        source.volume = targetVolume;
    }
    
    // Fade out
    public IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        source.Stop();
        source.volume = startVolume; // Reset for next use
    }
    
    // Crossfade between two sources
    public IEnumerator Crossfade(AudioSource source1, AudioSource source2, float duration)
    {
        StartCoroutine(FadeOut(source1, duration));
        yield return StartCoroutine(FadeIn(source2, duration));
    }
} 