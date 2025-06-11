using System.Collections;
using System.Collections.Generic;
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

    [Header("Shared Button Icons")]
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

    [Header("Fish Models")]
    [Tooltip("Assign all small fish prefab GameObjects here")]
    [SerializeField] public List<GameObject> smallFishModels;
    [Tooltip("Assign all big fish prefab GameObjects here")]
    [SerializeField] public List<GameObject> bigFishModels;
    [Tooltip("How long (seconds) the fish model stays active")]
    [SerializeField] public float fishModelActiveTime = 2f;

    [Header("Fish Spawn Transforms")]
    [Tooltip("Where to spawn small fish for Player 1")]
    [SerializeField] public Transform smallFishSpawn_P1;
    [Tooltip("Where to spawn big fish for Player 1")]
    [SerializeField] public Transform bigFishSpawn_P1;
    [Tooltip("Where to spawn small fish for Player 2")]
    [SerializeField] public Transform smallFishSpawn_P2;
    [Tooltip("Where to spawn big fish for Player 2")]
    [SerializeField] public Transform bigFishSpawn_P2;

    [Header("Player 1 Settings")]
    [Tooltip("Player 1’s TextMeshProUGUI for displaying score")]
    [SerializeField] public TMP_Text scoreP1Text;
    [Tooltip("Player 1’s UI GameObject that shows 'Bite'")]
    [SerializeField] public GameObject bitePromptUI_P1;
    [Tooltip("Player 1’s TextMeshProUGUI for 'Failed Bite'")]
    [SerializeField] public TMP_Text failedBiteText_P1;
    [Tooltip("Player 1’s TextMeshProUGUI for 'Fish Caught'")]
    [SerializeField] public TMP_Text fishCaughtText_P1;
    [Tooltip("Player 1’s RectTransform under which QTE notes get instantiated")]
    [SerializeField] public RectTransform qteUIParent_P1;
    [Tooltip("Player 1’s ButtonSlot prefab")]
    [SerializeField] public GameObject buttonSlotTemplate_P1;

    [Header("Player 2 Settings")]
    [Tooltip("Player 2’s TextMeshProUGUI for displaying score")]
    [SerializeField] public TMP_Text scoreP2Text;
    [Tooltip("Player 2’s UI GameObject that shows 'Bite'")]
    [SerializeField] public GameObject bitePromptUI_P2;
    [Tooltip("Player 2’s TextMeshProUGUI for 'Failed Bite'")]
    [SerializeField] public TMP_Text failedBiteText_P2;
    [Tooltip("Player 2’s TextMeshProUGUI for 'Fish Caught'")]
    [SerializeField] public TMP_Text fishCaughtText_P2;
    [Tooltip("Player 2’s RectTransform under which QTE notes get instantiated")]
    [SerializeField] public RectTransform qteUIParent_P2;
    [Tooltip("Player 2’s ButtonSlot prefab")]
    [SerializeField] public GameObject buttonSlotTemplate_P2;

    // Internal state
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

        // Configure AudioSources
        _musicSource = gameObject.AddComponent<AudioSource>();
        _sfxSource = gameObject.AddComponent<AudioSource>();
        if (backgroundMusic != null)
        {
            _musicSource.clip = backgroundMusic;
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        }

        // Start BGM if assigned
        if (backgroundMusic != null)
            _musicSource.Play();

        // Hide panels at startup
        if (startScreenPanel != null) startScreenPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (endScreenPanel != null) endScreenPanel.SetActive(false);

        // Hide/deactivate player-specific UI
        if (bitePromptUI_P1 != null) bitePromptUI_P1.SetActive(false);
        if (bitePromptUI_P2 != null) bitePromptUI_P2.SetActive(false);
        if (failedBiteText_P1 != null) failedBiteText_P1.gameObject.SetActive(false);
        if (failedBiteText_P2 != null) failedBiteText_P2.gameObject.SetActive(false);
        if (fishCaughtText_P1 != null) fishCaughtText_P1.gameObject.SetActive(false);
        if (fishCaughtText_P2 != null) fishCaughtText_P2.gameObject.SetActive(false);
        if (qteUIParent_P1 != null) qteUIParent_P1.gameObject.SetActive(false);
        if (qteUIParent_P2 != null) qteUIParent_P2.gameObject.SetActive(false);
        if (buttonSlotTemplate_P1 != null) buttonSlotTemplate_P1.SetActive(false);
        if (buttonSlotTemplate_P2 != null) buttonSlotTemplate_P2.SetActive(false);

        // Initialize timer
        _remainingTime = startTime;
        _timerRunning = false;

        if (skipStartAndEndScreens)
        {
            // Skip to HUD + timer
            StartCoroutine(CloseStartAndOpenHUD());
        }
        else
        {
            // Show Start Screen
            ShowStartScreen();
        }
    }

    public void ShowStartScreen()
    {
        if (startScreenPanel == null)
        {
            Debug.LogError("GameConfigurator: startScreenPanel not assigned.");
            return;
        }

        StartCoroutine(FadeInPanel(startScreenPanel, screenFadeDuration));

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

    public void ShowEndScreen()
    {
        if (skipStartAndEndScreens) return;
        StartCoroutine(DoEndScreenSequence());
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    // --------- Fishing UI (per player) -------------

    public void ShowBitePrompt(bool isPlayerOne)
    {
        var go = isPlayerOne ? bitePromptUI_P1 : bitePromptUI_P2;
        if (go != null) go.SetActive(true);
    }

    public void HideBitePrompt(bool isPlayerOne)
    {
        var go = isPlayerOne ? bitePromptUI_P1 : bitePromptUI_P2;
        if (go != null) go.SetActive(false);
    }

    public void ShowFailedBite(bool isPlayerOne)
    {
        var txt = isPlayerOne ? failedBiteText_P1 : failedBiteText_P2;
        if (txt != null) txt.gameObject.SetActive(true);
    }

    public void HideFailedBite(bool isPlayerOne)
    {
        var txt = isPlayerOne ? failedBiteText_P1 : failedBiteText_P2;
        if (txt != null) txt.gameObject.SetActive(false);
    }

    public void ShowFishCaught(bool isPlayerOne)
    {
        var txt = isPlayerOne ? fishCaughtText_P1 : fishCaughtText_P2;
        if (txt != null) txt.gameObject.SetActive(true);
    }

    public void HideFishCaught(bool isPlayerOne)
    {
        var txt = isPlayerOne ? fishCaughtText_P1 : fishCaughtText_P2;
        if (txt != null) txt.gameObject.SetActive(false);
    }

    public void ShowQTEUI(bool isPlayerOne)
    {
        var t = isPlayerOne ? qteUIParent_P1 : qteUIParent_P2;
        if (t != null) t.gameObject.SetActive(true);
    }

    public void HideQTEUI(bool isPlayerOne)
    {
        var t = isPlayerOne ? qteUIParent_P1 : qteUIParent_P2;
        if (t != null) t.gameObject.SetActive(false);
    }

    /// <summary>
    /// Spawn a random small/big fish at the chosen player’s spawn Transform
    /// (as set in the Inspector), keep it alive for fishModelActiveTime, then destroy.
    /// </summary>
    public void ShowFishModel(bool isBigFish, bool isPlayerOne)
    {
        StartCoroutine(FishModelRoutine(isBigFish, isPlayerOne));
    }

    private IEnumerator FishModelRoutine(bool isBigFish, bool isPlayerOne)
    {
        var list = isBigFish ? bigFishModels : smallFishModels;
        if (list == null || list.Count == 0) yield break;

        GameObject prefab = list[Random.Range(0, list.Count)];
        if (prefab == null) yield break;

        Transform spawnTransform = null;
        if (isPlayerOne)
            spawnTransform = isBigFish ? bigFishSpawn_P1 : smallFishSpawn_P1;
        else
            spawnTransform = isBigFish ? bigFishSpawn_P2 : smallFishSpawn_P2;

        if (spawnTransform == null) yield break;

        GameObject instance = Instantiate(prefab, spawnTransform.position, spawnTransform.rotation);
        yield return new WaitForSeconds(fishModelActiveTime);
        Destroy(instance);
    }

    // --------- Timer Control -------------

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
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void OnTimerExpired()
    {
        Debug.Log("Timer reached 00:00 – stopping game.");
        Time.timeScale = 0f;
        ShowEndScreen();
    }

    private IEnumerator CloseStartAndOpenHUD()
    {
        if (startScreenPanel != null)
        {
            yield return FadeOutPanel(startScreenPanel, screenFadeDuration);
            startScreenPanel.SetActive(false);
        }
        yield return new WaitForSeconds(startToHUDDelay);
        if (hudPanel != null)
            StartCoroutine(FadeInPanel(hudPanel, screenFadeDuration));
        StartCoroutine(TimerTick());
    }

    private IEnumerator DoEndScreenSequence()
    {
        if (hudPanel != null)
        {
            yield return FadeOutPanel(hudPanel, screenFadeDuration);
            hudPanel.SetActive(false);
        }
        yield return new WaitForSeconds(endScreenDelay);
        PlaySFX(endGameMusic);

        if (endScreenPanel != null)
        {
            endScreenPanel.SetActive(true);
            yield return FadeInPanel(endScreenPanel, screenFadeDuration);
        }

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
