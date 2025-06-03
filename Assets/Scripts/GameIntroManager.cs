using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameIntroManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Transform gameplayPosition;

    [Header("Character Introduction")]
    [SerializeField] private Transform[] introTargets;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private float zoomDistance = 5f;
    [SerializeField] private float zoomHeight = 1f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float animationWaitTime = 2f;

    [Header("Countdown")]
    [SerializeField] private GameObject countdownCanvas;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private int countdownFrom = 3;
    [SerializeField] private float countdownDuration = 1f;
    [SerializeField] private float goDuration = 0.8f;
    [SerializeField] private string goText = "GO!";

    [Header("Countdown Animation")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1.5f, 1, 0.8f);
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Countdown Colors")]
    [SerializeField] private Color[] countdownColors = { Color.red, Color.yellow, Color.green };
    [SerializeField] private Color goColor = Color.white;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip countdownSound;
    [SerializeField] private AudioClip goSound;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private RectTransform countdownRectTransform;
    private Vector3 originalCountdownScale;
    private bool introComplete = false;
    private PlayerScript[] playerScripts;

    void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gameCamera != null)
        {
            originalCameraPosition = gameCamera.transform.position;
            originalCameraRotation = gameCamera.transform.rotation;
        }

        playerScripts = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        DisablePlayerInput();

        SetupCountdownUI();

        Debug.Log("GameIntroManager starting intro sequence");

        StartCoroutine(PlayFullIntroSequence());
    }

    void SetupCountdownUI()
    {
        if (countdownCanvas == null)
        {
            CreateCountdownUI();
        }

        if (countdownText != null)
        {
            countdownRectTransform = countdownText.GetComponent<RectTransform>();
            originalCountdownScale = countdownRectTransform.localScale;
        }

        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);
    }

    void CreateCountdownUI()
    {
        GameObject canvasGO = new GameObject("CountdownCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject textGO = new GameObject("CountdownText");
        textGO.transform.SetParent(canvasGO.transform, false);

        countdownText = textGO.AddComponent<TextMeshProUGUI>();
        countdownText.text = "3";
        countdownText.fontSize = 120;
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.color = Color.white;
        countdownText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        countdownCanvas = canvasGO;
        countdownRectTransform = textRect;
        originalCountdownScale = textRect.localScale;
    }

    public IEnumerator PlayFullIntroSequence()
    {
        if (introComplete) yield break;

        // Don't try to control the PersistentSceneManager fade here
        // Let it handle the initial fade-in, then proceed with intro
        Debug.Log("GameIntroManager: Starting intro sequence");

        yield return new WaitForSeconds(0.8f); // Wait for PersistentSceneManager fade

        if (introTargets != null && introTargets.Length > 0)
        {
            for (int i = 0; i < introTargets.Length; i++)
            {
                if (introTargets[i] != null)
                {
                    yield return StartCoroutine(IntroduceTarget(introTargets[i]));
                    yield return StartCoroutine(ReturnToGameplayView());
                }
            }
        }

        Debug.Log("Camera animation complete - enabling player input for countdown");
        EnablePlayerInput();

        yield return StartCoroutine(PlayCountdown());

        EnableGameplay();

        introComplete = true;
    }

    IEnumerator IntroduceTarget(Transform target)
    {
        if (target == null || gameCamera == null) yield break;

        Vector3 targetPosition = target.position;

        Vector3 characterForward = target.forward;
        Vector3 cameraOffset = (-characterForward * zoomDistance) + (Vector3.up * zoomHeight);
        Vector3 cameraTargetPos = targetPosition + cameraOffset;

        Vector3 directionToTarget = (targetPosition - cameraTargetPos).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        Vector3 startPosition = gameCamera.transform.position;
        Quaternion startRotation = gameCamera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            float t = zoomCurve.Evaluate(elapsed);

            gameCamera.transform.position = Vector3.Lerp(startPosition, cameraTargetPos, t);
            gameCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);

            yield return null;
        }

        gameCamera.transform.position = cameraTargetPos;
        gameCamera.transform.rotation = targetRotation;

        yield return new WaitForSeconds(0.5f);

        Animator characterAnimator = target.GetComponent<Animator>();
        if (characterAnimator != null)
        {
            characterAnimator.Play("Lose Animation", 0, 0f);
            Debug.Log($"Force playing Lose Animation on {target.name}");
        }
        else
        {
            Debug.LogWarning($"No Animator component found on {target.name}");
        }

        yield return new WaitForSeconds(animationWaitTime + holdDuration);

        if (characterAnimator != null)
        {
            characterAnimator.Play("Idle stance", 0, 0f);
            Debug.Log($"Force playing Idle stance on {target.name}");
        }
    }

    IEnumerator ReturnToGameplayView()
    {
        if (gameCamera == null) yield break;

        Vector3 startPosition = gameCamera.transform.position;
        Quaternion startRotation = gameCamera.transform.rotation;

        Vector3 targetPosition = gameplayPosition != null ? gameplayPosition.position : originalCameraPosition;
        Quaternion targetRotation = gameplayPosition != null ? gameplayPosition.rotation : originalCameraRotation;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            float t = zoomCurve.Evaluate(elapsed);

            gameCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            gameCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);

            yield return null;
        }

        gameCamera.transform.position = targetPosition;
        gameCamera.transform.rotation = targetRotation;

        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator PlayCountdown()
    {
        if (countdownCanvas != null)
            countdownCanvas.SetActive(true);

        for (int i = countdownFrom; i > 0; i--)
        {
            yield return StartCoroutine(DisplayCountdownNumber(i));
        }

        yield return StartCoroutine(DisplayGo());

        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);
    }

    IEnumerator DisplayCountdownNumber(int number)
    {
        if (countdownText == null) yield break;

        countdownText.text = number.ToString();

        Color targetColor = countdownColors.Length > 0 ?
                           countdownColors[Mathf.Min(number - 1, countdownColors.Length - 1)] :
                           Color.white;
        countdownText.color = targetColor;

        PlayCountdownSound();

        yield return StartCoroutine(AnimateCountdownElement(countdownDuration));
    }

    IEnumerator DisplayGo()
    {
        if (countdownText == null) yield break;

        countdownText.text = goText;
        countdownText.color = goColor;

        PlayGoSound();

        yield return StartCoroutine(AnimateCountdownElement(goDuration));
    }

    IEnumerator AnimateCountdownElement(float duration)
    {
        if (countdownRectTransform == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scaleValue = scaleCurve.Evaluate(t);
            countdownRectTransform.localScale = originalCountdownScale * scaleValue;

            if (countdownText != null)
            {
                Color color = countdownText.color;
                color.a = fadeCurve.Evaluate(t);
                countdownText.color = color;
            }

            yield return null;
        }

        countdownRectTransform.localScale = originalCountdownScale;
    }

    void PlayCountdownSound()
    {
        if (audioSource != null && countdownSound != null)
        {
            audioSource.PlayOneShot(countdownSound);
        }
    }

    void PlayGoSound()
    {
        if (audioSource != null && goSound != null)
        {
            audioSource.PlayOneShot(goSound);
        }
        else if (audioSource != null && countdownSound != null)
        {
            audioSource.pitch = 1.2f;
            audioSource.PlayOneShot(countdownSound);
            audioSource.pitch = 1f;
        }
    }

    void DisablePlayerInput()
    {
        Debug.Log("Disabling player input during camera animation");
        foreach (var player in playerScripts)
        {
            if (player != null)
            {
                player.enabled = false;
            }
        }
    }

    void EnablePlayerInput()
    {
        Debug.Log("Enabling player input for countdown phase");
        foreach (var player in playerScripts)
        {
            if (player != null)
            {
                player.enabled = true;
            }
        }
    }

    void EnableGameplay()
    {
        GameManager_Fruity gameManager = FindFirstObjectByType<GameManager_Fruity>();
        if (gameManager != null)
        {
            gameManager.gameActive = true;
        }

        EnablePlayerInput();

        TheifScript[] thieves = FindObjectsByType<TheifScript>(FindObjectsSortMode.None);
        foreach (var thief in thieves)
        {
            thief.canWave = true;
        }

        Debug.Log("Gameplay enabled!");
    }

    public void SetIntroTargets(Transform[] targets)
    {
        introTargets = targets;
    }

    public void SetGameplayPosition(Transform position)
    {
        gameplayPosition = position;
    }

    public bool IsIntroComplete()
    {
        return introComplete;
    }
}