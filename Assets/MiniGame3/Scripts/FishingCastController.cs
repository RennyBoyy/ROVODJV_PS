using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Animator))]
public class FishingCastController : MonoBehaviour
{
    [Tooltip("Check this on the Player1 GameObject, leave unchecked on Player2")]
    [SerializeField] private bool isPlayerOne = false;

    // Core inputs & animator
    private InputAction _castAction;
    private Animator _animator;
    private bool _pulledDown = false;
    private bool _isFishing = false;

    // Cached animation lengths
    private float _castLength = 0f;
    private float _reelInLength = 0f;
    private float _failedReelLength = 0f;

    // Shared UI (from GameConfigurator)
    private TMP_Text failedBiteText;
    private TMP_Text fishCaughtText;
    private Sprite[] buttonIcons;

    // Pick correct score?text for this player
    private TMP_Text scoreTextForThisPlayer;

    // Player?specific templates (from GameConfigurator)
    private GameObject buttonSlotTemplateForThisPlayer;
    private RectTransform qteUIParentForThisPlayer;

    // QTE logic state
    private bool _waitingForBitePress = false;
    private float _bitePromptEndTime = 0f;

    private bool _qteActive = false;
    private List<MovingNote> activeNotes = new List<MovingNote>();
    private List<int> _currentSequence = new List<int>();
    private List<float> _spawnTimes = new List<float>();
    private int _spawnIndex = 0;

    // Score tracking
    private int _fishCount = 0;

    // Indicate big or small fish for scoring
    private bool _isCurrentFishBig = false;

    // Timings (pulled from GameConfigurator)
    private float minBiteTime;
    private float maxBiteTime;
    private float bitePromptDuration;
    private float scrollSpeed;
    private int smallFishSequenceLength;
    private int bigFishSequenceLength;
    private float fishCaughtDuration;
    private float failedBiteDuration;

    // Success window
    private const float successXMin = 200f;
    private const float successXMax = 300f;

    // MovingNote helper
    private class MovingNote
    {
        public RectTransform rect;
        public int expectedButton;
        public bool hasBeenHit;
    }

    private void Awake()
    {
        // — Gamepad setup —
        if (isPlayerOne && Gamepad.all.Count < 1)
        {
            Debug.Log("No controller for Player1");
            enabled = false;
            return;
        }
        if (!isPlayerOne && Gamepad.all.Count < 2)
        {
            Debug.Log("No controller for Player2");
            enabled = false;
            return;
        }

        var pi = GetComponent<PlayerInput>();
        if (isPlayerOne)
            pi.SwitchCurrentControlScheme("PS4_P1", Gamepad.all[0]);
        else
            pi.SwitchCurrentControlScheme("PS4_P2", Gamepad.all[1]);

        _castAction = pi.actions["Cast"];

        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("Animator not found");
            enabled = false;
            return;
        }

        // Cache animation clip lengths
        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == "Cast") _castLength = clip.length;
            if (clip.name == "Reel In") _reelInLength = clip.length;
            if (clip.name == "FailedReel") _failedReelLength = clip.length;
        }

        // — Pull in shared references & timings from GameConfigurator —
        var cfg = GameConfigurator.Instance;
        if (cfg == null)
        {
            Debug.LogError("FishingCastController requires a GameConfigurator in the scene.");
            enabled = false;
            return;
        }

        failedBiteText = cfg.failedBiteText;
        fishCaughtText = cfg.fishCaughtText;
        buttonIcons = cfg.buttonIcons;

        // Choose the correct score?text for this player
        scoreTextForThisPlayer = isPlayerOne
                                 ? cfg.scoreP1Text
                                 : cfg.scoreP2Text;

        // Choose the correct button?slot template and QTE parent
        buttonSlotTemplateForThisPlayer = isPlayerOne
                                          ? cfg.buttonSlotTemplate_P1
                                          : cfg.buttonSlotTemplate_P2;
        qteUIParentForThisPlayer = isPlayerOne
                                   ? cfg.qteUIParent_P1
                                   : cfg.qteUIParent_P2;

        // Pull in timings
        minBiteTime = cfg.minBiteTime;
        maxBiteTime = cfg.maxBiteTime;
        bitePromptDuration = cfg.bitePromptDuration;
        scrollSpeed = cfg.scrollSpeed;
        smallFishSequenceLength = cfg.smallFishSequenceLength;
        bigFishSequenceLength = cfg.bigFishSequenceLength;
        fishCaughtDuration = cfg.fishCaughtDuration;
        failedBiteDuration = cfg.failedBiteDuration;

        // Hide shared UI initially
        if (failedBiteText != null) failedBiteText.gameObject.SetActive(false);
        if (fishCaughtText != null) fishCaughtText.gameObject.SetActive(false);

        // Hide player?specific bite and QTE panels
        if (isPlayerOne)
        {
            if (cfg.bitePromptUI_P1 != null) cfg.bitePromptUI_P1.SetActive(false);
            if (cfg.qteUIParent_P1 != null) cfg.qteUIParent_P1.gameObject.SetActive(false);
        }
        else
        {
            if (cfg.bitePromptUI_P2 != null) cfg.bitePromptUI_P2.SetActive(false);
            if (cfg.qteUIParent_P2 != null) cfg.qteUIParent_P2.gameObject.SetActive(false);
        }

        // Initialize this player’s score display
        if (scoreTextForThisPlayer != null)
            scoreTextForThisPlayer.text = _fishCount.ToString();
    }

    private void OnEnable()
    {
        if (_castAction != null)
            _castAction.performed += OnCastPerformed;
    }

    private void OnDisable()
    {
        if (_castAction != null)
            _castAction.performed -= OnCastPerformed;
    }

    private void Update()
    {
        // Spawning QTE notes
        if (_qteActive && _spawnIndex < _currentSequence.Count)
        {
            if (Time.time >= _spawnTimes[_spawnIndex])
            {
                SpawnSingleNote(_currentSequence[_spawnIndex]);
                _spawnIndex++;
            }
        }

        if (_qteActive)
            UpdateMovingNotes();

        // Handle the bite prompt input first, if awaiting
        if (_waitingForBitePress)
        {
            HandleBitePromptInput();
            return;
        }

        // If QTE is active, handle button presses
        if (_qteActive)
        {
            HandleQTEInput();
        }
    }

    private void OnCastPerformed(InputAction.CallbackContext ctx)
    {
        if (_isFishing) return;

        var scheme = GetComponent<PlayerInput>().currentControlScheme;
        if (isPlayerOne ? scheme != "PS4_P1" : scheme != "PS4_P2")
            return;

        float value = ctx.ReadValue<float>();

        // Pull-back setup
        if (!_pulledDown && value < -0.9f)
        {
            _pulledDown = true;
            _animator.SetTrigger("PullBack");
            Debug.Log("Pull-back");
            return;
        }

        // Push-up and casting
        if (_pulledDown && value > 0.9f)
        {
            _pulledDown = false;
            _animator.SetTrigger("Cast");
            Debug.Log("Cast");

            _isFishing = true;
            StartCoroutine(WaitForCastThenBite());
        }
    }

    private IEnumerator WaitForCastThenBite()
    {
        if (_castLength > 0f)
            yield return new WaitForSeconds(_castLength);
        else
            yield return new WaitForSeconds(0.2f);

        // Random delay until the fish bites
        float biteDelay = Random.Range(minBiteTime, maxBiteTime);
        yield return new WaitForSeconds(biteDelay);

        BeginBitePrompt();
    }

    private void BeginBitePrompt()
    {
        _waitingForBitePress = true;
        _bitePromptEndTime = Time.time + bitePromptDuration;

        // Show player?specific “Bite” prompt
        GameConfigurator.Instance.ShowBitePrompt(isPlayerOne);
        // Hide shared “Failed Bite” text
        GameConfigurator.Instance.HideFailedBite();

        Debug.Log("Fish biting");
    }

    private void HandleBitePromptInput()
    {
        Gamepad pad = isPlayerOne ? Gamepad.all[0] : Gamepad.all[1];
        if (pad == null) return;

        if (pad.buttonSouth.wasPressedThisFrame)
        {
            _waitingForBitePress = false;
            // Hide player?specific “Bite” prompt
            GameConfigurator.Instance.HideBitePrompt(isPlayerOne);
            Debug.Log("Bite confirmed");
            StartButtonSequence();
            return;
        }

        if (Time.time >= _bitePromptEndTime)
        {
            _waitingForBitePress = false;
            GameConfigurator.Instance.HideBitePrompt(isPlayerOne);
            StartCoroutine(ShowFailedBiteAfterDelay());
            Debug.Log("Bite missed");
        }
    }

    private IEnumerator ShowFailedBiteAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        GameConfigurator.Instance.ShowFailedBite();

        _animator.Play("FishingIdle", 0);
        _isFishing = false;
        StartCoroutine(HideFailedBiteAfterDelay());
    }

    private IEnumerator HideFailedBiteAfterDelay()
    {
        yield return new WaitForSeconds(failedBiteDuration);
        GameConfigurator.Instance.HideFailedBite();
    }

    private void StartButtonSequence()
    {
        _qteActive = true;
        activeNotes.Clear();
        _currentSequence.Clear();
        _spawnTimes.Clear();
        _spawnIndex = 0;

        // Determine big or small fish, store for scoring
        _isCurrentFishBig = (Random.value < 0.5f);
        int length = _isCurrentFishBig ? bigFishSequenceLength : smallFishSequenceLength;

        float initialDelay = 0.2f;
        float spacing = 0.5f;
        float now = Time.time;
        for (int i = 0; i < length; i++)
        {
            _currentSequence.Add(Random.Range(0, buttonIcons.Length));
            _spawnTimes.Add(now + initialDelay + i * spacing);
        }

        _animator.SetTrigger("PullBack");
        GameConfigurator.Instance.ShowQTEUI(isPlayerOne);
        Debug.Log("QTE started (BigFish=" + _isCurrentFishBig + ")");
    }

    private void SpawnSingleNote(int buttonIndex)
    {
        if (buttonSlotTemplateForThisPlayer == null || qteUIParentForThisPlayer == null) return;

        GameObject go = Instantiate(buttonSlotTemplateForThisPlayer, qteUIParentForThisPlayer);
        go.SetActive(true);

        Image img = go.GetComponent<Image>();
        img.sprite = buttonIcons[buttonIndex];
        img.color = new Color(1f, 1f, 1f, 1f);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.localPosition = new Vector3(500f, 0f, 0f);

        activeNotes.Add(new MovingNote
        {
            rect = rt,
            expectedButton = buttonIndex,
            hasBeenHit = false
        });
    }

    private void UpdateMovingNotes()
    {
        float dx = scrollSpeed * Time.deltaTime;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];
            note.rect.anchoredPosition += Vector2.left * dx;

            float x = note.rect.anchoredPosition.x;
            if (!note.hasBeenHit && x < successXMin)
            {
                // Missed one ? fail immediately
                activeNotes.RemoveAt(i);
                Destroy(note.rect.gameObject);
                OnQTEFailure();
                return;
            }
        }
    }

    private void HandleQTEInput()
    {
        Gamepad pad = isPlayerOne ? Gamepad.all[0] : Gamepad.all[1];
        if (pad == null) return;

        int pressed = -1;
        if (pad.buttonEast.wasPressedThisFrame) pressed = 0;   // Circle
        if (pad.buttonSouth.wasPressedThisFrame) pressed = 1;  // Cross
        if (pad.buttonWest.wasPressedThisFrame) pressed = 2;   // Square
        if (pad.buttonNorth.wasPressedThisFrame) pressed = 3;  // Triangle
        if (pressed < 0) return;

        bool hitRegistered = false;
        for (int i = 0; i < activeNotes.Count; i++)
        {
            var note = activeNotes[i];
            if (note.hasBeenHit) continue;

            float x = note.rect.anchoredPosition.x;
            if (x >= successXMin && x <= successXMax)
            {
                hitRegistered = true;
                if (pressed == note.expectedButton)
                {
                    note.hasBeenHit = true;
                    Destroy(note.rect.gameObject);
                    activeNotes.RemoveAt(i);

                    if (activeNotes.Count == 0 && _spawnIndex >= _currentSequence.Count)
                        OnQTESuccess();
                }
                else
                {
                    OnQTEFailure();
                }
                break;
            }
        }

        if (!hitRegistered)
        {
            OnQTEFailure();
        }
    }

    private void OnQTESuccess()
    {
        _qteActive = false;
        foreach (var note in activeNotes)
            Destroy(note.rect.gameObject);
        activeNotes.Clear();

        GameConfigurator.Instance.HideQTEUI(isPlayerOne);

        // Award points: big fish = 2, small fish = 1
        _fishCount += _isCurrentFishBig ? 2 : 1;
        if (scoreTextForThisPlayer != null)
            scoreTextForThisPlayer.text = _fishCount.ToString();

        Debug.Log($"{(isPlayerOne ? "P1" : "P2")} Fish caught: {_fishCount} (BigFish={_isCurrentFishBig})");

        GameConfigurator.Instance.ShowFishCaught();
        StartCoroutine(HideFishCaughtAfterDelay());

        _animator.SetTrigger("ReelIn");
        StartCoroutine(ResetAfterReel());
    }

    private IEnumerator HideFishCaughtAfterDelay()
    {
        yield return new WaitForSeconds(fishCaughtDuration);
        GameConfigurator.Instance.HideFishCaught();
    }

    private void OnQTEFailure()
    {
        _qteActive = false;
        foreach (var note in activeNotes)
            Destroy(note.rect.gameObject);
        activeNotes.Clear();

        GameConfigurator.Instance.HideQTEUI(isPlayerOne);

        GameConfigurator.Instance.ShowFailedBite();
        StartCoroutine(HideFailedBiteAfterDelay());

        _animator.SetTrigger("FailedReel");
        StartCoroutine(ResetAfterFailedReel());
    }

    private IEnumerator ResetAfterReel()
    {
        if (_reelInLength > 0f)
            yield return new WaitForSeconds(_reelInLength);
        else
            yield return new WaitForSeconds(0.3f);

        _animator.Play("FishingIdle", 0);
        _isFishing = false;
        Debug.Log("Returned to idle");
    }

    private IEnumerator ResetAfterFailedReel()
    {
        if (_failedReelLength > 0f)
            yield return new WaitForSeconds(_failedReelLength);
        else
            yield return new WaitForSeconds(0.3f);

        _animator.Play("FishingIdle", 0);
        _isFishing = false;
        Debug.Log("Returned to idle after failure");
    }
}
