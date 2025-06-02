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

    // Our core Inputs & Animator
    private InputAction _castAction;
    private Animator _animator;
    private bool _pulledDown = false;

    // The cached lengths of "Cast", "ReelIn", and "FailedReel" clips/anim
    private float _castLength = 0f;
    private float _reelInLength = 0f;
    private float _failedReelLength = 0f;

    // I use this to prevent recasting while fishing/QTE is in progress
    private bool _isFishing = false;

    // Our fish-Bite prompt settings
    [Header("Fish-Bite Settings")]
    [Tooltip("Min seconds before fish bites.")]
    [SerializeField] private float minBiteTime = 3f;
    [Tooltip("Max seconds before fish bites.")]
    [SerializeField] private float maxBiteTime = 5f;
    [Tooltip("How long (seconds) the bite prompt stays up awaiting buttonSouth.")]
    [SerializeField] private float bitePromptDuration = 0.5f;
    [Tooltip("UI GameObject: displays \"Bite! Press X now\"")]
    [SerializeField] private GameObject bitePromptUI = null;
    [Tooltip("TextMeshProUGUI for showing \"Failed Bite\"")]
    [SerializeField] private TMP_Text failedBiteText = null;

    private bool _waitingForBitePress = false;
    private float _bitePromptEndTime = 0f;

    // QTE(Quick Timed Event) scrolling-note settings
    [Header("QTE Scrolling-Note Settings")]
    [Tooltip("The masked panel that holds scrolling notes.")]
    [SerializeField] private RectTransform qteUIParent = null;
    [Tooltip("Disabled template for each note/icon.")]
    [SerializeField] private GameObject buttonSlotTemplate = null;
    [Tooltip("Sprites for face-buttons: 0=Circle,1=Cross,2=Square,3=Triangle")]
    [SerializeField] private Sprite[] buttonIcons = new Sprite[4];
    [Tooltip("Pixels per second that notes scroll left.")]
    [SerializeField] private float scrollSpeed = 300f;
    [Tooltip("Sequence length for a small fish.")]
    [SerializeField] private int smallFishSequenceLength = 3;
    [Tooltip("Sequence length for a big fish.")]
    [SerializeField] private int bigFishSequenceLength = 5;

    // The ssuccess window between x = 200 and x = 300 for players to correctly press btns in time
    private const float successXMin = 200f;
    private const float successXMax = 300f;

    //track whether QTE is active
    private bool _qteActive = false;

    //for tracking each moving note
    private class MovingNote
    {
        public RectTransform rect;
        public int expectedButton;
        public bool hasBeenHit;
    }
    private List<MovingNote> activeNotes = new List<MovingNote>();

    // generated sequence of buttons and the times at which theyll spawn
    private List<int> _currentSequence = new List<int>();
    private List<float> _spawnTimes = new List<float>();
    private int _spawnIndex = 0;

    // score display
    [Header("Score Settings")]
    [Tooltip("TextMeshProUGUI showing fish caught.")]
    [SerializeField] private TMP_Text scoreText = null;
    private int _fishCount = 0;

    // message durations and times
    [Header("Message Durations")]
    [Tooltip("How long (seconds) \"Fish Caught\" displays.")]
    [SerializeField] private float fishCaughtDuration = 2f;
    [Tooltip("How long (seconds) \"Failed Bite\" displays.")]
    [SerializeField] private float failedBiteDuration = 1f;
    [Tooltip("TextMeshProUGUI for showing \"Fish Caught\"")]
    [SerializeField] private TMP_Text fishCaughtText = null;

    private void Awake()
    {
        // gamepad setups
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

        // animator nd clip lengths
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("Animator not found");
            enabled = false;
            return;
        }

        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == "Cast") _castLength = clip.length;
            if (clip.name == "Reel In") _reelInLength = clip.length;
            if (clip.name == "FailedReel") _failedReelLength = clip.length;
        }

        // UI setup
        if (bitePromptUI != null) bitePromptUI.SetActive(false);
        if (failedBiteText != null) failedBiteText.gameObject.SetActive(false);
        if (fishCaughtText != null) fishCaughtText.gameObject.SetActive(false);
        if (qteUIParent != null) qteUIParent.gameObject.SetActive(false);
        if (buttonSlotTemplate != null) buttonSlotTemplate.SetActive(false);

        if (scoreText != null) scoreText.text = _fishCount.ToString();
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

    private void OnCastPerformed(InputAction.CallbackContext ctx)
    {
        if (_isFishing) return;

        var scheme = GetComponent<PlayerInput>().currentControlScheme;
        if (isPlayerOne ? scheme != "PS4_P1" : scheme != "PS4_P2")
            return;

        float value = ctx.ReadValue<float>();

        // pullback settip
        if (!_pulledDown && value < -0.9f)
        {
            _pulledDown = true;
            _animator.SetTrigger("PullBack");
            Debug.Log("Pull-back");
            return;
        }

        // pushup and casting settup
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

        float biteDelay = Random.Range(minBiteTime, maxBiteTime);
        yield return new WaitForSeconds(biteDelay);

        BeginBitePrompt();
    }

    private void BeginBitePrompt()
    {
        _waitingForBitePress = true;
        _bitePromptEndTime = Time.time + bitePromptDuration;

        if (bitePromptUI != null) bitePromptUI.SetActive(true);
        if (failedBiteText != null) failedBiteText.gameObject.SetActive(false);

        Debug.Log("Fish biting");
    }

    private void Update()
    {
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

        if (_waitingForBitePress)
        {
            HandleBitePromptInput();
            return;
        }

        if (_qteActive)
        {
            HandleQTEInput();
        }
    }

    private void HandleBitePromptInput()
    {
        Gamepad pad = isPlayerOne ? Gamepad.all[0] : Gamepad.all[1];
        if (pad == null) return;

        if (pad.buttonSouth.wasPressedThisFrame)
        {
            _waitingForBitePress = false;
            if (bitePromptUI != null) bitePromptUI.SetActive(false);
            Debug.Log("Bite confirmed");
            StartButtonSequence();
            return;
        }

        if (Time.time >= _bitePromptEndTime)
        {
            _waitingForBitePress = false;
            if (bitePromptUI != null) bitePromptUI.SetActive(false);
            StartCoroutine(ShowFailedBiteAfterDelay());
            Debug.Log("Bite missed");
        }
    }

    private IEnumerator ShowFailedBiteAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        if (failedBiteText != null) failedBiteText.gameObject.SetActive(true);
        _animator.Play("FishingIdle", 0);
        _isFishing = false;
        StartCoroutine(HideFailedBiteAfterDelay());
    }

    private IEnumerator HideFailedBiteAfterDelay()
    {
        yield return new WaitForSeconds(failedBiteDuration);
        if (failedBiteText != null) failedBiteText.gameObject.SetActive(false);
    }

    private void StartButtonSequence()
    {
        _qteActive = true;
        activeNotes.Clear();
        _currentSequence.Clear();
        _spawnTimes.Clear();
        _spawnIndex = 0;

        bool isBigFish = Random.value < 0.5f;
        int length = isBigFish ? bigFishSequenceLength : smallFishSequenceLength;

        float initialDelay = 0.2f;
        float spacing = 0.4f;
        float now = Time.time;
        for (int i = 0; i < length; i++)
        {
            _currentSequence.Add(Random.Range(0, buttonIcons.Length));
            _spawnTimes.Add(now + initialDelay + i * spacing);
        }

        _animator.SetTrigger("PullBack");
        if (qteUIParent != null) qteUIParent.gameObject.SetActive(true);
        Debug.Log("QTE started");
    }

    private void SpawnSingleNote(int buttonIndex)
    {
        GameObject go = Instantiate(buttonSlotTemplate, qteUIParent);
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
        if (pad.buttonEast.wasPressedThisFrame) pressed = 0;
        if (pad.buttonSouth.wasPressedThisFrame) pressed = 1;
        if (pad.buttonWest.wasPressedThisFrame) pressed = 2;
        if (pad.buttonNorth.wasPressedThisFrame) pressed = 3;
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
        foreach (var note in activeNotes) Destroy(note.rect.gameObject);
        activeNotes.Clear();
        if (qteUIParent != null) qteUIParent.gameObject.SetActive(false);

        _fishCount++;
        if (scoreText != null) scoreText.text = _fishCount.ToString();
        Debug.Log("Fish caught: " + _fishCount);

        if (fishCaughtText != null)
        {
            fishCaughtText.gameObject.SetActive(true);
            StartCoroutine(HideFishCaughtAfterDelay());
        }

        _animator.SetTrigger("ReelIn");
        StartCoroutine(ResetAfterReel());
    }

    private IEnumerator HideFishCaughtAfterDelay()
    {
        yield return new WaitForSeconds(fishCaughtDuration);
        if (fishCaughtText != null) fishCaughtText.gameObject.SetActive(false);
    }

    private void OnQTEFailure()
    {
        _qteActive = false;
        foreach (var note in activeNotes) Destroy(note.rect.gameObject);
        activeNotes.Clear();
        if (qteUIParent != null) qteUIParent.gameObject.SetActive(false);

        if (failedBiteText != null) failedBiteText.gameObject.SetActive(true);
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
