using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class PlayerFishingController : MonoBehaviour
{
    public enum State { Idle, Casting, WaitingForBite, Biting, Sequencing, Reeling }
    public State CurrentState { get; private set; } = State.Idle;

    [Tooltip("0 = first Gamepad, 1 = second Gamepad")]
    public int playerIndex = 0;

    [Header("Sequence Settings")]
    [Tooltip("Min inputs for small fish")]
    public int smallFishSteps = 3;
    [Tooltip("Min inputs for big fish")]
    public int bigFishSteps = 5;
    [Tooltip("Chance (0–1) to get a 'big' fish")]
    public float bigFishChance = 0.3f;
    [Tooltip("Pixels per second icons scroll across bar")]
    public float sequenceScrollSpeed = 200f;

    [Header("Reel Settings")]
    [Tooltip("How much spin input required to finish reeling")]
    public float reelThreshold = 2f;

    [Header("References")]
    public Animator lineAnimator;              // triggers bob animation on bite
    public RectTransform sequenceContainer;    // parent for sequence icons
    public GameObject sequenceIconPrefab;      // prefab with SequenceIcon script + Image
    public GameObject spinPrompt;              // UI prompt telling user to spin
    public Slider reelProgressBar;             // 0?1 slider for reel-in progress

    [Header("Events")]
    public UnityEvent<int> OnFishCaught;       // passes playerIndex

    private FishingControls controls;
    private Coroutine castCoroutine;
    private Coroutine biteCoroutine;

    // sequence state
    private Queue<string> sequenceQueue;
    private List<RectTransform> iconObjects = new List<RectTransform>();

    // reel-in accumulator
    private float reelAccumulator;

    private void Awake()
    {

        // instantiate input wrapper
        controls = new FishingControls();

        // restrict to this controller
        if (Gamepad.all.Count > playerIndex)
            controls.devices = new[] { Gamepad.all[playerIndex] };

        // wire callbacks
        controls.Fishing.Cast.performed += OnCastInput;
        controls.Fishing.ReelStart.performed += OnReelStart;
        controls.Fishing.Sequence.performed += OnSequenceInput;
        controls.Fishing.ReelSpin.performed += OnReelSpin;

        controls.Fishing.Enable();

        // hide UI elements initially
        spinPrompt.SetActive(false);
        reelProgressBar.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        controls.Fishing.Disable();
        controls.Dispose();
    }

    private void Update()
    {
        if (CurrentState == State.Sequencing)
            ScrollSequenceIcons();
    }

    // CASTING 

    private void OnCastInput(InputAction.CallbackContext ctx)
    {
        if (CurrentState != State.Idle) return;
        Vector2 stick = ctx.ReadValue<Vector2>();
        if (stick.y < -0.8f && castCoroutine == null)
            castCoroutine = StartCoroutine(CastDetection());
    }

    private IEnumerator CastDetection()
    {
        CurrentState = State.Casting;

        // wait until stick near neutral
        yield return new WaitUntil(() =>
            Mathf.Abs(controls.Fishing.Cast.ReadValue<Vector2>().y) < 0.2f
        );

        // then wait for push-forward
        yield return new WaitUntil(() =>
            controls.Fishing.Cast.ReadValue<Vector2>().y > 0.8f
        );

        castCoroutine = null;
        CurrentState = State.WaitingForBite;
        StartBiteCoroutine();
    }

    //BITE DETECTION 

    private void StartBiteCoroutine()
    {
        biteCoroutine = StartCoroutine(WaitForBite());
    }

    private IEnumerator WaitForBite()
    {
        float delay = UnityEngine.Random.Range(2f, 5f);
        yield return new WaitForSeconds(delay);

        CurrentState = State.Biting;
        if (lineAnimator) lineAnimator.SetTrigger("Bob");

        biteCoroutine = null;
    }

    // SEQUENCE MINIGAME 

    private void OnReelStart(InputAction.CallbackContext ctx)
    {
        if (CurrentState != State.Biting || !ctx.performed) return;
        CurrentState = State.Sequencing;
        StartSequence();
    }

    private void StartSequence()
    {
        // clear old icons
        foreach (var rt in iconObjects) Destroy(rt.gameObject);
        iconObjects.Clear();

        // choose fish size
        bool big = UnityEngine.Random.value < bigFishChance;
        int steps = big ? bigFishSteps : smallFishSteps;

        // build queue
        sequenceQueue = new Queue<string>(steps);
        var choices = new[] { "buttonSouth", "buttonEast", "buttonWest", "buttonNorth" };
        for (int i = 0; i < steps; i++)
            sequenceQueue.Enqueue(choices[UnityEngine.Random.Range(0, choices.Length)]);

        // spawn icons
        float startX = sequenceContainer.rect.width;
        float spacing = 80f;
        int idx = 0;
        foreach (var btn in sequenceQueue)
        {
            var go = Instantiate(sequenceIconPrefab, sequenceContainer);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + idx * spacing, 0f);
            //go.GetComponent<SequenceIcon>()?.Initialize(btn);
            iconObjects.Add(rt);
            idx++;
        }
    }

    private void ScrollSequenceIcons()
    {
        float delta = sequenceScrollSpeed * Time.deltaTime;
        for (int i = iconObjects.Count - 1; i >= 0; i--)
        {
            var rt = iconObjects[i];
            rt.anchoredPosition -= new Vector2(delta, 0f);

            if (rt.anchoredPosition.x < -sequenceContainer.rect.width * 0.5f)
            {
                Destroy(rt.gameObject);
                iconObjects.RemoveAt(i);
                if (sequenceQueue.Count > 0) sequenceQueue.Dequeue();
            }
        }
    }

    private void OnSequenceInput(InputAction.CallbackContext ctx)
    {
        if (CurrentState != State.Sequencing || !ctx.performed) return;

        if (sequenceQueue.Count == 0) return;
        string pressed = ctx.control.name;

        if (pressed == sequenceQueue.Peek())
        {
            sequenceQueue.Dequeue();
            var firstRt = iconObjects[0];
            Destroy(firstRt.gameObject);
            iconObjects.RemoveAt(0);

            if (sequenceQueue.Count == 0)
            {
                CurrentState = State.Reeling;
                spinPrompt.SetActive(true);
                reelProgressBar.gameObject.SetActive(true);
                reelAccumulator = 0f;
            }
        }
        else
        {
            // restart on wrong
            CurrentState = State.Sequencing;
            StartSequence();
        }
    }

    // REEL-IN SPIN

    private void OnReelSpin(InputAction.CallbackContext ctx)
    {
        if (CurrentState != State.Reeling) return;

        Vector2 spin = ctx.ReadValue<Vector2>();
        reelAccumulator += spin.magnitude * Time.deltaTime;

        if (reelProgressBar)
            reelProgressBar.value = Mathf.Clamp01(reelAccumulator / reelThreshold);

        if (reelAccumulator >= reelThreshold)
            FinishReelIn();
    }

    private void FinishReelIn()
    {
        CurrentState = State.Idle;
        spinPrompt.SetActive(false);
        reelProgressBar.gameObject.SetActive(false);
        OnFishCaught?.Invoke(playerIndex);
    }

    public void ResetFishing()
    {
        if (castCoroutine != null) StopCoroutine(castCoroutine);
        if (biteCoroutine != null) StopCoroutine(biteCoroutine);

        CurrentState = State.Idle;
        spinPrompt.SetActive(false);
        reelProgressBar.gameObject.SetActive(false);

        foreach (var rt in iconObjects) Destroy(rt.gameObject);
        iconObjects.Clear();
    }
}
