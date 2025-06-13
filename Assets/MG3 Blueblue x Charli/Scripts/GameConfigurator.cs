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
    [SerializeField] private bool skipStartAndEndScreens = false;
    [SerializeField] public GameObject startScreenPanel;
    [SerializeField] public GameObject hudPanel;
    [SerializeField] public GameObject endScreenPanel;
    [SerializeField] public TMP_Text timerText;

    [Header("Shared Button Icons")]
    [SerializeField] public Sprite[] buttonIcons;

    [Header("Audio Clips & Settings")]
    [Tooltip("Background Music")]
    [SerializeField] public AudioClip backgroundMusic;
    [Tooltip("SFX for cast action")]
    [SerializeField] public AudioClip castSFX;
    [Tooltip("SFX for bite prompt")]
    [SerializeField] public AudioClip biteSFX;
    [Tooltip("SFX for each QTE button press")]
    [SerializeField] public AudioClip sequenceSFX;
    [Tooltip("SFX for successful catch")]
    [SerializeField] public AudioClip successSFX;
    [Tooltip("SFX for reel-in animation")]
    [SerializeField] public AudioClip reelInSFX;
    [Tooltip("SFX for failed bite")]
    [SerializeField] public AudioClip failedBiteSFX;
    [Tooltip("SFX for UI transitions / button clicks")]
    [SerializeField] public AudioClip transitionSFX;
    [Tooltip("EndGame Music")]
    [SerializeField] public AudioClip endGameMusic;

    [Header("Fade & Transition Timings")]
    [SerializeField] public float screenFadeDuration = 0.75f;
    [SerializeField] public float startToHUDDelay = 0.5f;
    [SerializeField] public float endScreenDelay = 0.3f;

    [Header("Game Timer Settings")]
    [SerializeField] public float startTime = 180f;

    [Header("Fishing QTE Timings")]
    [SerializeField] public float minBiteTime = 3f;
    [SerializeField] public float maxBiteTime = 5f;
    [SerializeField] public float bitePromptDuration = 0.5f;
    [SerializeField] public int smallFishSequenceLength = 3;
    [SerializeField] public int bigFishSequenceLength = 5;
    [SerializeField] public float fishCaughtDuration = 2f;
    [SerializeField] public float failedBiteDuration = 1f;

    [Header("Speed Increase Settings")]
    [Tooltip("How much to bump scroll up speed each time a QTE sequence starts (0–15)")]
    [Range(0f, 15f)]
    [SerializeField] public float speedIncreasePerSequence = 5f;
    private const float baseScrollSpeed = 300f;

    [Header("Fish Point Values")]
    [SerializeField] public int smallFishPoints = 1;
    [SerializeField] public int bigFishPoints = 2;

    [Header("Fish Models")]
    [SerializeField] public List<GameObject> smallFishModels;
    [SerializeField] public List<GameObject> bigFishModels;
    [SerializeField] public float fishModelActiveTime = 2f;

    [Header("Fish Spawn Transforms")]
    [SerializeField] public Transform fishSpawn_P1;
    [SerializeField] public Transform fishSpawn_P2;

    [Header("Fish Move End Transforms")]
    [SerializeField] public Transform smallFishMoveEnd_P1;
    [SerializeField] public Transform bigFishMoveEnd_P1;
    [SerializeField] public Transform smallFishMoveEnd_P2;
    [SerializeField] public Transform bigFishMoveEnd_P2;

    [Header("Fish Move Settings")]
    [SerializeField] public float fishMoveDuration = 1f;
    [SerializeField] public float fishMoveHoldTime = 0.5f;

    [Header("Player 1 UI")]
    [SerializeField] public TMP_Text scoreP1Text;
    [SerializeField] public GameObject bitePromptUI_P1;
    [SerializeField] public TMP_Text failedBiteText_P1;
    [SerializeField] public TMP_Text fishCaughtText_P1;
    [SerializeField] public RectTransform qteUIParent_P1;
    [SerializeField] public GameObject buttonSlotTemplate_P1;

    [Header("Player 2 UI")]
    [SerializeField] public TMP_Text scoreP2Text;
    [SerializeField] public GameObject bitePromptUI_P2;
    [SerializeField] public TMP_Text failedBiteText_P2;
    [SerializeField] public TMP_Text fishCaughtText_P2;
    [SerializeField] public RectTransform qteUIParent_P2;
    [SerializeField] public GameObject buttonSlotTemplate_P2;

    // Internal state
    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private float _remainingTime;
    private bool _timerRunning;

    private void Awake()
    {
        // Singleton
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

        // Audio setup
        _musicSource = gameObject.AddComponent<AudioSource>();
        _sfxSource = gameObject.AddComponent<AudioSource>();
        if (backgroundMusic != null)
        {
            _musicSource.clip = backgroundMusic;
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.Play();
        }

        HideAllUI();
        _remainingTime = startTime;
        _timerRunning = false;

        if (skipStartAndEndScreens)
            StartCoroutine(CloseStartAndOpenHUD());
        else
            ShowStartScreen();
    }

    private void HideAllUI()
    {
        startScreenPanel?.SetActive(false);
        hudPanel?.SetActive(false);
        endScreenPanel?.SetActive(false);
        bitePromptUI_P1?.SetActive(false);
        bitePromptUI_P2?.SetActive(false);
        failedBiteText_P1?.gameObject.SetActive(false);
        failedBiteText_P2?.gameObject.SetActive(false);
        fishCaughtText_P1?.gameObject.SetActive(false);
        fishCaughtText_P2?.gameObject.SetActive(false);
        qteUIParent_P1?.gameObject.SetActive(false);
        qteUIParent_P2?.gameObject.SetActive(false);
        buttonSlotTemplate_P1?.SetActive(false);
        buttonSlotTemplate_P2?.SetActive(false);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    public void ShowStartScreen()
    {
        StartCoroutine(FadeInPanel(startScreenPanel, screenFadeDuration));
        var btn = startScreenPanel.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                PlaySFX(transitionSFX);
                StartCoroutine(CloseStartAndOpenHUD());
            });
        }
    }

    public void ShowEndScreen()
    {
        if (skipStartAndEndScreens) return;
        StartCoroutine(DoEndScreenSequence());
    }

    public void ShowBitePrompt(bool p1)
    {
        (p1 ? bitePromptUI_P1 : bitePromptUI_P2)?.SetActive(true);
        PlaySFX(biteSFX);
    }

    public void HideBitePrompt(bool p1) => (p1 ? bitePromptUI_P1 : bitePromptUI_P2)?.SetActive(false);

    public void ShowFailedBite(bool p1)
    {
        (p1 ? failedBiteText_P1 : failedBiteText_P2)?.gameObject.SetActive(true);
        PlaySFX(failedBiteSFX);
    }

    public void HideFailedBite(bool p1) => (p1 ? failedBiteText_P1 : failedBiteText_P2)?.gameObject.SetActive(false);
    public void ShowFishCaught(bool p1) => (p1 ? fishCaughtText_P1 : fishCaughtText_P2)?.gameObject.SetActive(true);
    public void HideFishCaught(bool p1) => (p1 ? fishCaughtText_P1 : fishCaughtText_P2)?.gameObject.SetActive(false);
    public void ShowQTEUI(bool p1) => (p1 ? qteUIParent_P1 : qteUIParent_P2)?.gameObject.SetActive(true);
    public void HideQTEUI(bool p1) => (p1 ? qteUIParent_P1 : qteUIParent_P2)?.gameObject.SetActive(false);

    public float GetScrollSpeed(int sequenceCount)
        => baseScrollSpeed + speedIncreasePerSequence * sequenceCount;

    public void ShowFishMove(bool isBig, bool p1)
        => StartCoroutine(FishMoveRoutine(isBig, p1));

    private IEnumerator FishMoveRoutine(bool isBig, bool p1)
    {
        var list = isBig ? bigFishModels : smallFishModels;
        var spawn = p1 ? fishSpawn_P1 : fishSpawn_P2;
        var endT = isBig
                     ? (p1 ? bigFishMoveEnd_P1 : bigFishMoveEnd_P2)
                     : (p1 ? smallFishMoveEnd_P1 : smallFishMoveEnd_P2);
        if (list == null || list.Count == 0 || spawn == null || endT == null) yield break;

        var prefab = list[Random.Range(0, list.Count)];
        var inst = Instantiate(prefab, spawn.position, Quaternion.Euler(0f, 180f, 0f));
        float t = 0f;
        while (t < fishMoveDuration)
        {
            inst.transform.position = Vector3.Lerp(spawn.position, endT.position, t / fishMoveDuration);
            t += Time.deltaTime;
            yield return null;
        }
        inst.transform.position = endT.position;

        float spin = 0f;
        while (spin < fishMoveHoldTime)
        {
            inst.transform.rotation = Quaternion.Euler(0f, 180f + 360f * (spin / fishMoveHoldTime), 0f);
            spin += Time.deltaTime;
            yield return null;
        }
        Destroy(inst);
    }

    private IEnumerator TimerTick()
    {
        _timerRunning = true;
        _remainingTime = startTime;
        while (_timerRunning && _remainingTime > 0f)
        {
            _remainingTime -= Time.deltaTime;
            int m = Mathf.FloorToInt(_remainingTime / 60f);
            int s = Mathf.FloorToInt(_remainingTime % 60f);
            timerText.text = $"{m:00}:{s:00}";
            yield return null;
        }
        timerText.text = "00:00";
        OnTimerExpired();
    }

    private void OnTimerExpired()
    {
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
        if (hudPanel != null) StartCoroutine(FadeInPanel(hudPanel, screenFadeDuration));
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

            var retry = endScreenPanel.transform.Find("RetryButton")?.GetComponent<Button>();
            if (retry != null)
            {
                retry.onClick.RemoveAllListeners();
                retry.onClick.AddListener(() =>
                {
                    PlaySFX(transitionSFX);
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                });
            }
            var quit = endScreenPanel.transform.Find("QuitButton")?.GetComponent<Button>();
            if (quit != null)
            {
                quit.onClick.RemoveAllListeners();
                quit.onClick.AddListener(() =>
                {
                    PlaySFX(transitionSFX);
                    Application.Quit();
                });
            }
        }
    }

    private IEnumerator FadeInPanel(GameObject panel, float duration)
    {
        if (panel == null) yield break;
        var cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        panel.SetActive(true);
        float t = 0f;
        while (t < duration)
        {
            cg.alpha = Mathf.Lerp(0f, 1f, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 1f;
    }

    private IEnumerator FadeOutPanel(GameObject panel, float duration)
    {
        if (panel == null) yield break;
        var cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        float t = 0f;
        while (t < duration)
        {
            cg.alpha = Mathf.Lerp(1f, 0f, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 0f;
    }
}
