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

    private InputAction _castAction;
    private Animator _animator;
    private bool _pulledDown = false;
    private bool _isFishing = false;

    private float _castLength = 0f;
    private float _reelInLength = 0f;
    private float _failedReelLength = 0f;

    // QTE UI & icons
    private Sprite[] buttonIcons;
    private TMP_Text scoreText;
    private GameObject buttonSlotTemplate;
    private RectTransform qteUIParent;

    // Bite prompt state
    private bool _waitingForBite = false;
    private float _biteEndTime = 0f;

    // QTE sequence state
    private bool _qteActive = false;
    private List<MovingNote> activeNotes = new List<MovingNote>();
    private List<int> _sequence = new List<int>();
    private List<float> _spawnTimes = new List<float>();
    private int _spawnIndex = 0;

    // Difficulty scaling
    private int _sequenceCount = 0;

    // Score & fish type
    private int _fishCount = 0;
    private bool _currentBig = false;

    // Configured timings
    private float minBiteTime;
    private float maxBiteTime;
    private float bitePromptDuration;
    private int smallSeqLen;
    private int bigSeqLen;
    private float fishCaughtDuration;
    private float failedBiteDuration;

    // QTE hit window
    private const float successMin = 200f;
    private const float successMax = 300f;

    private class MovingNote
    {
        public RectTransform rect;
        public int expected;
        public bool hit;
    }

    private void Awake()
    {
        var pi = GetComponent<PlayerInput>();
        if (isPlayerOne)
        {
            if (Gamepad.all.Count < 1) { enabled = false; return; }
            pi.SwitchCurrentControlScheme("PS4_P1", Gamepad.all[0]);
        }
        else
        {
            if (Gamepad.all.Count < 2) { enabled = false; return; }
            pi.SwitchCurrentControlScheme("PS4_P2", Gamepad.all[1]);
        }

        _castAction = pi.actions["Cast"];
        _animator = GetComponent<Animator>();
        if (_animator == null) { Debug.LogError("Animator missing"); enabled = false; return; }
        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == "Cast") _castLength = clip.length;
            if (clip.name == "Reel In") _reelInLength = clip.length;
            if (clip.name == "FailedReel") _failedReelLength = clip.length;
        }
    }

    private void Start()
    {
        if (GameConfigurator.Instance == null)
            StartCoroutine(InitializeDelayed());
        else
            InitializeFromConfigurator();
    }

    private IEnumerator InitializeDelayed()
    {
        yield return null;
        yield return null;
        if (GameConfigurator.Instance == null) { enabled = false; yield break; }
        InitializeFromConfigurator();
    }

    private void InitializeFromConfigurator()
    {
        var cfg = GameConfigurator.Instance;
        buttonIcons = cfg.buttonIcons;
        scoreText = isPlayerOne ? cfg.scoreP1Text : cfg.scoreP2Text;
        buttonSlotTemplate = isPlayerOne ? cfg.buttonSlotTemplate_P1 : cfg.buttonSlotTemplate_P2;
        qteUIParent = isPlayerOne ? cfg.qteUIParent_P1 : cfg.qteUIParent_P2;

        minBiteTime = cfg.minBiteTime;
        maxBiteTime = cfg.maxBiteTime;
        bitePromptDuration = cfg.bitePromptDuration;
        smallSeqLen = cfg.smallFishSequenceLength;
        bigSeqLen = cfg.bigFishSequenceLength;
        fishCaughtDuration = cfg.fishCaughtDuration;
        failedBiteDuration = cfg.failedBiteDuration;

        scoreText.text = _fishCount.ToString();
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
        // Spawn notes at scheduled times
        if (_qteActive && _spawnIndex < _sequence.Count && Time.time >= _spawnTimes[_spawnIndex])
        {
            SpawnNote(_sequence[_spawnIndex]);
            _spawnIndex++;
        }

        // Move notes each frame
        if (_qteActive)
            MoveNotes();

        // Handle bite prompt first
        if (_waitingForBite)
        {
            HandleBitePromptInput();
            return;
        }

        // Then handle QTE button input
        if (_qteActive)
            HandleQTEInput();
    }

    private void OnCastPerformed(InputAction.CallbackContext ctx)
    {
        if (_isFishing) return;
        var scheme = GetComponent<PlayerInput>().currentControlScheme;
        if (isPlayerOne ? scheme != "PS4_P1" : scheme != "PS4_P2") return;

        float value = ctx.ReadValue<float>();
        if (!_pulledDown && value < -0.9f)
        {
            _pulledDown = true;
            _animator.SetTrigger("PullBack");
            return;
        }
        if (_pulledDown && value > 0.9f)
        {
            _pulledDown = false;
            _animator.SetTrigger("Cast");
            _isFishing = true;
            StartCoroutine(WaitForCastAndBite());
        }
    }

    private IEnumerator WaitForCastAndBite()
    {
        yield return new WaitForSeconds(_castLength > 0f ? _castLength : 0.2f);
        yield return new WaitForSeconds(Random.Range(minBiteTime, maxBiteTime));
        BeginBitePrompt();
    }

    private void BeginBitePrompt()
    {
        _waitingForBite = true;
        _biteEndTime = Time.time + bitePromptDuration;
        GameConfigurator.Instance.ShowBitePrompt(isPlayerOne);
        GameConfigurator.Instance.HideFailedBite(isPlayerOne);
    }

    private void HandleBitePromptInput()
    {
        var pad = isPlayerOne ? Gamepad.all[0] : Gamepad.all[1];
        if (pad == null) return;

        if (pad.buttonSouth.wasPressedThisFrame)
        {
            _waitingForBite = false;
            GameConfigurator.Instance.HideBitePrompt(isPlayerOne);
            StartSequence();
            return;
        }

        if (Time.time >= _biteEndTime)
        {
            _waitingForBite = false;
            GameConfigurator.Instance.HideBitePrompt(isPlayerOne);
            StartCoroutine(ShowFailedBite());
        }
    }

    private IEnumerator ShowFailedBite()
    {
        yield return new WaitForSeconds(0.5f);
        GameConfigurator.Instance.ShowFailedBite(isPlayerOne);
        _animator.Play("FishingIdle", 0);
        _isFishing = false;
        yield return new WaitForSeconds(failedBiteDuration);
        GameConfigurator.Instance.HideFailedBite(isPlayerOne);
    }

    private void StartSequence()
    {
        _sequenceCount++;
        _qteActive = true;
        activeNotes.Clear();
        _sequence.Clear();
        _spawnTimes.Clear();
        _spawnIndex = 0;

        _currentBig = Random.value < 0.5f;
        int length = _currentBig ? bigSeqLen : smallSeqLen;
        float now = Time.time, delay = 0.2f, spacing = 0.5f;
        for (int i = 0; i < length; i++)
        {
            _sequence.Add(Random.Range(0, buttonIcons.Length));
            _spawnTimes.Add(now + delay + i * spacing);
        }

        _animator.SetTrigger("PullBack");
        GameConfigurator.Instance.ShowQTEUI(isPlayerOne);
    }

    private void SpawnNote(int buttonIndex)
    {
        if (buttonSlotTemplate == null || qteUIParent == null) return;
        var go = Instantiate(buttonSlotTemplate, qteUIParent);
        go.SetActive(true);
        var img = go.GetComponent<Image>();
        img.sprite = buttonIcons[buttonIndex];
        img.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.localPosition = new Vector3(500f, 0f, 0f);
        activeNotes.Add(new MovingNote { rect = rt, expected = buttonIndex, hit = false });
    }

    private void MoveNotes()
    {
        float dx = GameConfigurator.Instance.GetScrollSpeed(_sequenceCount) * Time.deltaTime;
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];
            note.rect.anchoredPosition += Vector2.left * dx;
            if (!note.hit && note.rect.anchoredPosition.x < successMin)
            {
                Destroy(note.rect.gameObject);
                activeNotes.RemoveAt(i);
                OnQTEFailure();
                return;
            }
        }
    }

    private void HandleQTEInput()
    {
        var pad = isPlayerOne ? Gamepad.all[0] : Gamepad.all[1];
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
            if (note.hit) continue;
            float x = note.rect.anchoredPosition.x;
            if (x >= successMin && x <= successMax)
            {
                hitRegistered = true;
                if (pressed == note.expected)
                {
                    note.hit = true;
                    Destroy(note.rect.gameObject);
                    activeNotes.RemoveAt(i);
                    if (activeNotes.Count == 0 && _spawnIndex >= _sequence.Count)
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
            OnQTEFailure();
    }

    private void OnQTESuccess()
    {
        _qteActive = false;
        foreach (var note in activeNotes) Destroy(note.rect.gameObject);
        activeNotes.Clear();
        GameConfigurator.Instance.HideQTEUI(isPlayerOne);

        int points = _currentBig
            ? GameConfigurator.Instance.bigFishPoints
            : GameConfigurator.Instance.smallFishPoints;
        _fishCount += points;
        scoreText.text = _fishCount.ToString();

        GameConfigurator.Instance.ShowFishCaught(isPlayerOne);
        StartCoroutine(HideFishCaught());

        // **spawn and animate fish immediately**
        GameConfigurator.Instance.ShowFishMove(_currentBig, isPlayerOne);

        _animator.SetTrigger("ReelIn");
        StartCoroutine(ResetAfterReel());
    }

    private IEnumerator HideFishCaught()
    {
        yield return new WaitForSeconds(fishCaughtDuration);
        GameConfigurator.Instance.HideFishCaught(isPlayerOne);
    }

    private void OnQTEFailure()
    {
        _qteActive = false;
        foreach (var note in activeNotes) Destroy(note.rect.gameObject);
        activeNotes.Clear();

        GameConfigurator.Instance.HideQTEUI(isPlayerOne);
        GameConfigurator.Instance.ShowFailedBite(isPlayerOne);
        StartCoroutine(HideFishCaught());

        _animator.SetTrigger("FailedReel");
        StartCoroutine(ResetAfterFail());
    }

    private IEnumerator ResetAfterReel()
    {
        yield return new WaitForSeconds(_reelInLength > 0f ? _reelInLength : 0.3f);
        _animator.Play("FishingIdle", 0);
        _isFishing = false;
    }

    private IEnumerator ResetAfterFail()
    {
        yield return new WaitForSeconds(_failedReelLength > 0f ? _failedReelLength : 0.3f);
        _animator.Play("FishingIdle", 0);
        _isFishing = false;
    }

    private IEnumerator PostReelRoutine() { yield break; }
}
