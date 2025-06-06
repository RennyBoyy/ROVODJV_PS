using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameConfigurator : MonoBehaviour
{
    public static GameConfigurator Instance { get; private set; }

    [Header("General Settings")]
    [Tooltip("Check to skip both Start and End screens and go straight into gameplay.")]
    [SerializeField] private bool skipStartAndEndScreens = false;

    [Tooltip("Drag our Start Screen panel here")]
    [SerializeField] public GameObject startScreenPanel;
    [Tooltip("Drag our In-Game HUD panel here (shared overlay)")]
    [SerializeField] public GameObject hudPanel;
    [Tooltip("Drag our End Screen panel here")]
    [SerializeField] public GameObject endScreenPanel;

    [Tooltip("TextMeshProUGUI that displays the countdown timer")]
    [SerializeField] public TMP_Text timerText;

    [Tooltip("TextMeshProUGUI that shows 'Failed Bite' (shared)")]
    [SerializeField] public TMP_Text failedBiteText;
    [Tooltip("TextMeshProUGUI that shows 'Fish Caught' (shared)")]
    [SerializeField] public TMP_Text fishCaughtText;

    [Tooltip("Sprites for PS4 buttons: 0=Circle, 1=Cross, 2=Square, 3=Triangle")]
    [SerializeField] public Sprite[] buttonIcons;

    [Header("Audio Clips & Settings")]
    [Tooltip("BackGround Music")]
    [SerializeField] public AudioClip backgroundMusic;
    [Tooltip("SFX for any UI click (buttons/transitions)")]
    [SerializeField] public AudioClip clickSFX;
    [Tooltip("SFX for transitions")]
    [SerializeField] public AudioClip transitionSFX;
    [Tooltip("EndGame Music")]
    [SerializeField] public AudioClip endGameMusic;

    [Header("Fade & Transition Timings")]
    [Tooltip("How long to fade between panels (seconds)")]
    [SerializeField] public float screenFadeDuration = 0.75f;
    [Tooltip("Delay between closing Start and showing HUD (seconds)")]
    [SerializeField] public float startToHUDDelay = 0.5f;
    [Tooltip("Delay before playing End Screen audio (seconds)")]
    [SerializeField] public float endScreenDelay = 0.3f;

    [Header("Game Timer Settings")]
    [Tooltip("Starting time for the countdown (seconds)")]
    [SerializeField] public float startTime = 180f;

    [Header("Fishing QTE Timings")]
    [Tooltip("Min seconds before fish bites")]
    [SerializeField] public float minBiteTime = 3f;
    [Tooltip("Max seconds before fish bites")]
    [SerializeField] public float maxBiteTime = 5f;
    [Tooltip("How long the bite prompt stays up (seconds)")]
    [SerializeField] public float bitePromptDuration = 0.5f;
    [Tooltip("Pixels/second that QTE notes scroll left")]
    [SerializeField] public float scrollSpeed = 300f;
    [Tooltip("Sequence length for a small fish")]
    [SerializeField] public int smallFishSequenceLength = 3;
    [Tooltip("Sequence length for a big fish")]
    [SerializeField] public int bigFishSequenceLength = 5;
    [Tooltip("How long 'Fish Caught' text displays (seconds)")]
    [SerializeField] public float fishCaughtDuration = 2f;
    [Tooltip("How long 'Failed Bite' text displays (seconds)")]
    [SerializeField] public float failedBiteDuration = 1f;

    [Header("Fish Point Values")]
    [Tooltip("Points awarded for catching a small fish")]
    [SerializeField] public int smallFishPoints = 1;
    [Tooltip("Points awarded for catching a big fish")]
    [SerializeField] public int bigFishPoints = 2;

    [Header("Player 1 Settings")]
    [Tooltip("Player 1’s TextMeshProUGUI for displaying score")]
    [SerializeField] public TMP_Text scoreP1Text;
    [Tooltip("Player 1’s UI GameObject that shows 'Bite'")]
    [SerializeField] public GameObject bitePromptUI_P1;
    [Tooltip("Player 1’s RectTransform under which QTE notes get instantiated")]
    [SerializeField] public RectTransform qteUIParent_P1;
    [Tooltip("Player 1’s ButtonSlot prefab")]
    [SerializeField] public GameObject buttonSlotTemplate_P1;

    [Header("Player 2 Settings")]
    [Tooltip("Player 2’s TextMeshProUGUI for displaying score")]
    [SerializeField] public TMP_Text scoreP2Text;
    [Tooltip("Player 2’s UI GameObject that shows 'Bite'")]
    [SerializeField] public GameObject bitePromptUI_P2;
    [Tooltip("Player 2’s RectTransform under which QTE notes get instantiated")]
    [SerializeField] public RectTransform qteUIParent_P2;
    [Tooltip("Player 2’s ButtonSlot prefab")]
    [SerializeField] public GameObject buttonSlotTemplate_P2;

    // Internal State
    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private float _remainingTime;
    private bool _timerRunning = false;

    private void Awake()
    {
        // Enforce singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Add/Configure AudioSources
        _musicSource = gameObject.AddComponent<AudioSource>();
        _sfxSource = gameObject.AddComponent<AudioSource>();
        if (backgroundMusic != null)
        {
            _musicSource.clip = backgroundMusic;
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        }

        // Immediately start background music (if assigned)
        if (backgroundMusic != null)
            _musicSource.Play();

        // Hide all panels initially
        if (startScreenPanel != null) startScreenPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (endScreenPanel != null) endScreenPanel.SetActive(false);

        // Hide/deactivate all Player-specific Fishing UI elements
        if (bitePromptUI_P1 != null) bitePromptUI_P1.SetActive(false);
        if (bitePromptUI_P2 != null) bitePromptUI_P2.SetActive(false);
        if (failedBiteText != null) failedBiteText.gameObject.SetActive(false);
        if (fishCaughtText != null) fishCaughtText.gameObject.SetActive(false);
        if (qteUIParent_P1 != null) qteUIParent_P1.gameObject.SetActive(false);
        if (qteUIParent_P2 != null) qteUIParent_P2.gameObject.SetActive(false);
        if (buttonSlotTemplate_P1 != null) buttonSlotTemplate_P1.SetActive(false);
        if (buttonSlotTemplate_P2 != null) buttonSlotTemplate_P2.SetActive(false);

        // Initialize timer state
        _remainingTime = startTime;
        _timerRunning = false;

        if (skipStartAndEndScreens)
        {
            // Directly open HUD and start timer, skipping Start Screen
            StartCoroutine(CloseStartAndOpenHUD());
        }
        else
        {
            // Show the Start Screen at launch
            ShowStartScreen();
        }
    }

    /// <summary>
    /// Fade in & display the Start Screen.
    /// Assign any button callbacks for “Play” inside our Start Screen hierarchy.
    /// </summary>
    public void ShowStartScreen()
    {
        if (startScreenPanel == null)
        {
            Debug.LogError("GameConfigurator: startScreenPanel not assigned.");
            return;
        }

        // Ensure panel is active
        StartCoroutine(FadeInPanel(startScreenPanel, screenFadeDuration));

        // Hook up a Play button (if it exists). Assume there's a Button under startScreenPanel.
        var playBtn = startScreenPanel.GetComponentInChildren<Button>();
        if (playBtn != null)
        {
            playBtn.onClick.RemoveAllListeners();
            playBtn.onClick.AddListener(() =>
            {
                PlaySFX(clickSFX);
                StartCoroutine(CloseStartAndOpenHUD());
            });
        }
    }

    /// <summary>
    /// Call this to trigger the End Screen sequence (fade out HUD, delay, play sounds, fade in End Screen, etc.).
    /// </summary>
    public void ShowEndScreen()
    {
        if (skipStartAndEndScreens)
        {
            // If skipping End Screen, do nothing
            return;
        }

        StartCoroutine(DoEndScreenSequence());
    }

    /// <summary>
    /// Play a one-shot UI SFX (e.g. clicks, transitions).
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    // Fishing UI separated per player

    public void ShowBitePrompt(bool isPlayerOne)
    {
        if (isPlayerOne)
        {
            if (bitePromptUI_P1 != null) bitePromptUI_P1.SetActive(true);
        }
        else
        {
            if (bitePromptUI_P2 != null) bitePromptUI_P2.SetActive(true);
        }
    }

    public void HideBitePrompt(bool isPlayerOne)
    {
        if (isPlayerOne)
        {
            if (bitePromptUI_P1 != null) bitePromptUI_P1.SetActive(false);
        }
        else
        {
            if (bitePromptUI_P2 != null) bitePromptUI_P2.SetActive(false);
        }
    }

    public void ShowFailedBite()
    {
        if (failedBiteText != null) failedBiteText.gameObject.SetActive(true);
    }

    public void HideFailedBite()
    {
        if (failedBiteText != null) failedBiteText.gameObject.SetActive(false);
    }

    public void ShowFishCaught()
    {
        if (fishCaughtText != null) fishCaughtText.gameObject.SetActive(true);
    }

    public void HideFishCaught()
    {
        if (fishCaughtText != null) fishCaughtText.gameObject.SetActive(false);
    }

    public void ShowQTEUI(bool isPlayerOne)
    {
        if (isPlayerOne)
        {
            if (qteUIParent_P1 != null) qteUIParent_P1.gameObject.SetActive(true);
        }
        else
        {
            if (qteUIParent_P2 != null) qteUIParent_P2.gameObject.SetActive(true);
        }
    }

    public void HideQTEUI(bool isPlayerOne)
    {
        if (isPlayerOne)
        {
            if (qteUIParent_P1 != null) qteUIParent_P1.gameObject.SetActive(false);
        }
        else
        {
            if (qteUIParent_P2 != null) qteUIParent_P2.gameObject.SetActive(false);
        }
    }

    // Timer Control

    private IEnumerator TimerTick()
    {
        _timerRunning = true;
        _remainingTime = startTime;

        while (_timerRunning && _remainingTime > 0f)
        {
            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                _timerRunning = false;
                UpdateTimerDisplay();
                OnTimerExpired();
                yield break;
            }

            UpdateTimerDisplay();
            yield return null;
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(_remainingTime / 60f);
        int seconds = Mathf.FloorToInt(_remainingTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void OnTimerExpired()
    {
        Debug.Log("Timer reached 00:00 – stopping game.");
        Time.timeScale = 0f;
        ShowEndScreen();
    }

    // Internal Coroutines

    private IEnumerator CloseStartAndOpenHUD()
    {
        // Fade out Start Screen
        if (startScreenPanel != null)
        {
            yield return FadeOutPanel(startScreenPanel, screenFadeDuration);
            startScreenPanel.SetActive(false);
        }

        // Small delay
        yield return new WaitForSeconds(startToHUDDelay);

        // Activate HUD & start timer
        if (hudPanel != null)
            StartCoroutine(FadeInPanel(hudPanel, screenFadeDuration));

        StartCoroutine(TimerTick());
    }

    private IEnumerator DoEndScreenSequence()
    {
        // Fade out HUD
        if (hudPanel != null)
        {
            yield return FadeOutPanel(hudPanel, screenFadeDuration);
            hudPanel.SetActive(false);
        }

        // Delay then play end sound
        yield return new WaitForSeconds(endScreenDelay);
        PlaySFX(endGameMusic);

        // Fade in End Screen
        if (endScreenPanel != null)
        {
            endScreenPanel.SetActive(true);
            yield return FadeInPanel(endScreenPanel, screenFadeDuration);
        }

        // Hook Retry or Quit buttons (assuming names “RetryButton” & “QuitButton”)
        if (endScreenPanel != null)
        {
            var retryBtn = endScreenPanel.transform.Find("RetryButton")?.GetComponent<Button>();
            if (retryBtn != null)
            {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(() =>
                {
                    PlaySFX(clickSFX);
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                });
            }

            var quitBtn = endScreenPanel.transform.Find("QuitButton")?.GetComponent<Button>();
            if (quitBtn != null)
            {
                quitBtn.onClick.RemoveAllListeners();
                quitBtn.onClick.AddListener(() =>
                {
                    PlaySFX(clickSFX);
                    Application.Quit();
                });
            }
        }
    }

    private IEnumerator FadeInPanel(GameObject panel, float duration)
    {
        if (panel == null) yield break;

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        panel.SetActive(true);
        float timer = 0f;
        while (timer < duration)
        {
            cg.alpha = Mathf.Lerp(0f, 1f, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 1f;
    }

    private IEnumerator FadeOutPanel(GameObject panel, float duration)
    {
        if (panel == null) yield break;

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        float timer = 0f;
        while (timer < duration)
        {
            cg.alpha = Mathf.Lerp(1f, 0f, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 0f;
    }
}
