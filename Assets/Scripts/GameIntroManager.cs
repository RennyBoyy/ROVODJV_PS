using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameIntroManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Transform gameplayPosition; // Empty GameObject positioned where camera should be during gameplay

    [Header("Character Introduction")]
    [SerializeField] private Transform[] introTargets; // GameObjects to zoom to (characters, objects, etc.)
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private float zoomDistance = 3f;
    [SerializeField] private float zoomHeight = 1f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

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

    void Start()
    {
        // Auto-setup if components aren't assigned
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gameCamera != null)
        {
            originalCameraPosition = gameCamera.transform.position;
            originalCameraRotation = gameCamera.transform.rotation;
        }

        SetupCountdownUI();

        // Start the intro sequence automatically
        StartCoroutine(PlayFullIntroSequence());
    }

    void SetupCountdownUI()
    {
        // Create countdown UI if not assigned
        if (countdownCanvas == null)
        {
            CreateCountdownUI();
        }

        if (countdownText != null)
        {
            countdownRectTransform = countdownText.GetComponent<RectTransform>();
            originalCountdownScale = countdownRectTransform.localScale;
        }

        // Hide countdown initially
        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);
    }

    void CreateCountdownUI()
    {
        // Create countdown canvas
        GameObject canvasGO = new GameObject("CountdownCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Create countdown text
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

        // Wait a moment for scene to settle
        yield return new WaitForSeconds(0.5f);

        // Character introductions
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

        // Countdown
        yield return StartCoroutine(PlayCountdown());

        // Enable gameplay
        EnableGameplay();

        introComplete = true;
    }

    IEnumerator IntroduceTarget(Transform target)
    {
        if (target == null || gameCamera == null) yield break;

        // Calculate target position for close-up
        Vector3 targetPosition = target.position;
        Vector3 cameraTargetPos = targetPosition +
                                 (Vector3.back * zoomDistance) +
                                 (Vector3.up * zoomHeight);

        // Calculate rotation to look at target
        Vector3 directionToTarget = (targetPosition - cameraTargetPos).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        // Store current camera transform
        Vector3 startPosition = gameCamera.transform.position;
        Quaternion startRotation = gameCamera.transform.rotation;

        // Zoom in to target
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            float t = zoomCurve.Evaluate(elapsed);

            gameCamera.transform.position = Vector3.Lerp(startPosition, cameraTargetPos, t);
            gameCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);

            yield return null;
        }

        // Ensure final position is exact
        gameCamera.transform.position = cameraTargetPos;
        gameCamera.transform.rotation = targetRotation;

        // Hold on target
        yield return new WaitForSeconds(holdDuration);
    }

    IEnumerator ReturnToGameplayView()
    {
        if (gameCamera == null) yield break;

        Vector3 startPosition = gameCamera.transform.position;
        Quaternion startRotation = gameCamera.transform.rotation;

        // Use gameplay position if assigned, otherwise use original position
        Vector3 targetPosition = gameplayPosition != null ? gameplayPosition.position : originalCameraPosition;
        Quaternion targetRotation = gameplayPosition != null ? gameplayPosition.rotation : originalCameraRotation;

        // Zoom out to gameplay view
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            float t = zoomCurve.Evaluate(elapsed);

            gameCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            gameCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);

            yield return null;
        }

        // Ensure final position is exact
        gameCamera.transform.position = targetPosition;
        gameCamera.transform.rotation = targetRotation;

        // Brief pause before next action
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator PlayCountdown()
    {
        // Show countdown canvas
        if (countdownCanvas != null)
            countdownCanvas.SetActive(true);

        // Countdown numbers
        for (int i = countdownFrom; i > 0; i--)
        {
            yield return StartCoroutine(DisplayCountdownNumber(i));
        }

        // Display "GO!"
        yield return StartCoroutine(DisplayGo());

        // Hide countdown
        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);
    }

    IEnumerator DisplayCountdownNumber(int number)
    {
        if (countdownText == null) yield break;

        // Set text and color
        countdownText.text = number.ToString();

        // Get color for this number
        Color targetColor = countdownColors.Length > 0 ?
                           countdownColors[Mathf.Min(number - 1, countdownColors.Length - 1)] :
                           Color.white;
        countdownText.color = targetColor;

        // Play sound
        PlayCountdownSound();

        // Animate
        yield return StartCoroutine(AnimateCountdownElement(countdownDuration));
    }

    IEnumerator DisplayGo()
    {
        if (countdownText == null) yield break;

        // Set text and color
        countdownText.text = goText;
        countdownText.color = goColor;

        // Play go sound
        PlayGoSound();

        // Animate
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

            // Scale animation
            float scaleValue = scaleCurve.Evaluate(t);
            countdownRectTransform.localScale = originalCountdownScale * scaleValue;

            // Fade animation
            if (countdownText != null)
            {
                Color color = countdownText.color;
                color.a = fadeCurve.Evaluate(t);
                countdownText.color = color;
            }

            yield return null;
        }

        // Reset scale
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
            // Use countdown sound with higher pitch for "GO!"
            audioSource.pitch = 1.2f;
            audioSource.PlayOneShot(countdownSound);
            audioSource.pitch = 1f;
        }
    }

    void EnableGameplay()
    {
        // Enable game manager
        GameManager_Fruity gameManager = FindFirstObjectByType<GameManager_Fruity>();
        if (gameManager != null)
        {
            gameManager.gameActive = true;
        }

        // Enable player controls
        PlayerScript[] players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.enabled = true;
        }

        // Enable enemy spawning - but set canWave to true so it can start spawning
        TheifScript thief = FindFirstObjectByType<TheifScript>();
        if (thief != null)
        {
            thief.enabled = true;
            // Use reflection to set the private canWave field to true
            var canWaveField = typeof(TheifScript).GetField("canWave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (canWaveField != null)
            {
                canWaveField.SetValue(thief, true);
            }
        }

        Debug.Log("Gameplay enabled!");
    }

    // Public methods for manual control if needed
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