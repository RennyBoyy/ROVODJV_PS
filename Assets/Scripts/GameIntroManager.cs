using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

[System.Serializable]
public class FlythroughPoint
{
    public Transform point;
    public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool isIntroTarget = false;
    public GameObject overlayUI;
    public float pauseDuration = 1.5f;
    public float driftSpeed = 0.5f;
    public Vector3 driftDirection = Vector3.right;
}

public class GameIntroManager : MonoBehaviour
{
    public static GameIntroManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        // No DontDestroyOnLoad, so each scene gets its own instance
    }

    [Header("INTRO")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Transform gameplayPosition;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private FlythroughPoint[] flythroughPoints;
    [SerializeField] private float flySpeed = 1f;
    [Space(30)]
    [SerializeField] private GameObject frutyNameUI;
    [SerializeField] private GameObject potatoNameUI;
    [SerializeField] private GameObject vsSplashUI;
    [SerializeField] private Transform vsSplashPoint;
    [SerializeField] private float splashDisplayDuration = 1.5f;
    [Space(30)]
    [SerializeField] private GameObject countdownCanvas;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private int countdownFrom = 3;
    [SerializeField] private float countdownDuration = 1f;
    [SerializeField] private float goDuration = 0.8f;
    [SerializeField] private string goText = "GO!";
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1.5f, 1, 0.8f);
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private Color[] countdownColors = { Color.red, Color.yellow, Color.green };
    [SerializeField] private Color goColor = Color.white;

    [Header("UI Canvases")]
    [SerializeField] private GameObject gameplayUIPanel;
    [SerializeField] private GameObject player1WinUI;
    [SerializeField] private GameObject player2WinUI;
    // Add other global UI canvases here as needed

    [Header("ENDING")]
    [SerializeField] private Transform podium1stPlace;
    [SerializeField] private Transform podium2ndPlace;
    [SerializeField] private Transform outroCameraTransform;
    [SerializeField] private float endgameDelay = 3f;
    // Add other ending/outro fields here as needed

    [Header("Skip Intro")]
    [SerializeField] private KeyCode skipIntroKey = KeyCode.Space;
    [SerializeField] private bool allowSkipIntro = true;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private RectTransform countdownRectTransform;
    private Vector3 originalCountdownScale;
    private bool introComplete = false;
    private PlayerScript[] playerScripts;
    private bool gameEnded = false;
    private bool introSkipped = false;

    void Start()
    {
        if (gameplayUIPanel != null)
        {
            gameplayUIPanel.SetActive(false);
            Debug.Log("Gameplay UI panel deactivated at Start");
        }

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

    void Update()
    {
        // Check for skip intro input (keyboard or gamepad)
        if (allowSkipIntro && !introComplete && !introSkipped)
        {
            // Keyboard input
            if (Input.GetKeyDown(skipIntroKey))
            {
                SkipIntro();
            }
            // Gamepad input (Circle/B button)
            else if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                SkipIntro();
            }
        }
    }

    IEnumerator FlyThroughCameraPath()
    {
        if (flythroughPoints == null || flythroughPoints.Length < 4) yield break;

        gameCamera.transform.position = flythroughPoints[0].point.position;
        gameCamera.transform.rotation = flythroughPoints[0].point.rotation;

        for (int i = 1; i < flythroughPoints.Length - 2; i++)
        {
            Transform p0 = flythroughPoints[i - 1].point;
            Transform p1 = flythroughPoints[i].point;
            Transform p2 = flythroughPoints[i + 1].point;
            Transform p3 = flythroughPoints[i + 2].point;

            FlythroughPoint next = flythroughPoints[i + 1];
            float segmentDistance = Vector3.Distance(p1.position, p2.position);
            float duration = segmentDistance / flySpeed;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = next.easing.Evaluate(t);

                Vector3 curvedPos = CatmullRom(p0.position, p1.position, p2.position, p3.position, easedT);
                Quaternion rot = Quaternion.Lerp(p1.rotation, p2.rotation, easedT);

                gameCamera.transform.position = curvedPos;
                gameCamera.transform.rotation = rot;

                yield return null;
            }

            if (next.isIntroTarget)
            {
                if (next.overlayUI != null) next.overlayUI.SetActive(true);

                float pauseElapsed = 0f;
                while (pauseElapsed < next.pauseDuration)
                {
                    pauseElapsed += Time.deltaTime;
                    gameCamera.transform.position += next.driftDirection * next.driftSpeed * Time.deltaTime;
                    yield return null;
                }

                if (next.overlayUI != null) next.overlayUI.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(next.pauseDuration);
            }
        }

        // Final segment (straight lerp)
        FlythroughPoint lastPoint = flythroughPoints[flythroughPoints.Length - 1];
        FlythroughPoint penultimatePoint = flythroughPoints[flythroughPoints.Length - 2];

        float lastSegmentDistance = Vector3.Distance(penultimatePoint.point.position, lastPoint.point.position);
        float lastSegmentDuration = lastSegmentDistance / flySpeed;
        float finalElapsed = 0f;
        while (finalElapsed < lastSegmentDuration)
        {
            finalElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(finalElapsed / lastSegmentDuration);
            float easedT = lastPoint.easing.Evaluate(t);

            gameCamera.transform.position = Vector3.Lerp(penultimatePoint.point.position, lastPoint.point.position, easedT);
            gameCamera.transform.rotation = Quaternion.Lerp(penultimatePoint.point.rotation, lastPoint.point.rotation, easedT);

            yield return null;
        }

        if (lastPoint.isIntroTarget)
        {
            if (lastPoint.overlayUI != null) lastPoint.overlayUI.SetActive(true);

            float pauseElapsed = 0f;
            while (pauseElapsed < lastPoint.pauseDuration)
            {
                pauseElapsed += Time.deltaTime;
                gameCamera.transform.position += lastPoint.driftDirection * lastPoint.driftSpeed * Time.deltaTime;
                yield return null;
            }

            if (lastPoint.overlayUI != null) lastPoint.overlayUI.SetActive(false);
        }
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }

    public IEnumerator PlayFullIntroSequence()
    {
        if (introComplete) yield break;

        DisablePlayerInput();
        yield return new WaitForSeconds(0.8f);

        yield return StartCoroutine(FlyThroughCameraPath());
        yield return StartCoroutine(ReturnToGameplayView());

        EnablePlayerInput();
        yield return StartCoroutine(PlayCountdown());

        EnableGameplay();
        introComplete = true;
    }


    // Smoothly transitions the camera back to the gameplay position
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

    // Prepares countdown UI or generates one if missing
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

    // Dynamically creates a simple TMP countdown canvas overlay
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

    // Displays countdown numbers and "GO!" with animations and sound
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

    // Handles one countdown number (3, 2, 1)
    IEnumerator DisplayCountdownNumber(int number)
    {
        if (countdownText == null) yield break;

        countdownText.text = number.ToString();

        Color targetColor = countdownColors.Length > 0 ?
                           countdownColors[Mathf.Min(number - 1, countdownColors.Length - 1)] :
                           Color.white;
        countdownText.color = targetColor;

        FruityGameConfigurator.Instance?.PlayCountdownNumberSound();

        yield return StartCoroutine(AnimateCountdownElement(countdownDuration));
    }

    // Handles the final "GO!" splash
    IEnumerator DisplayGo()
    {
        if (countdownText == null) yield break;

        countdownText.text = goText;
        countdownText.color = goColor;

        FruityGameConfigurator.Instance?.PlayCountdownGoSound();

        yield return StartCoroutine(AnimateCountdownElement(goDuration));
    }

    // Applies scaling/fading animation to countdown text
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

    // Blocks player movement during intro
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

    // Re-enables movement before countdown
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

    // Marks gameplay as active and lets players/thieves start moving
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

        if (gameplayUIPanel != null)
        {
            gameplayUIPanel.SetActive(true);
            Debug.Log("Gameplay UI panel activated at EnableGameplay");
        }

        Debug.Log("Gameplay enabled!");
    }

   

    public void SetGameplayPosition(Transform position)
    {
        gameplayPosition = position;
    }

    public bool IsIntroComplete()
    {
        return introComplete;
    }

    // --- ENDGAME HANDLING ---
    public void OnGameEnd(int winningPlayer)
    {
        // Move camera to outro position
        if (outroCameraTransform != null && gameCamera != null)
        {
            gameCamera.transform.position = outroCameraTransform.position;
            gameCamera.transform.rotation = outroCameraTransform.rotation;
        }

        // Teleport players to podiums
        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            // Index 0 = Fruity (P1, left), Index 1 = Potato (P2, right)
            if (winningPlayer == 0 && i == 0)
                players[i].transform.position = podium1stPlace.position;
            else if (winningPlayer == 1 && i == 1)
                players[i].transform.position = podium1stPlace.position;
            else
                players[i].transform.position = podium2ndPlace.position;
        }

        // Show Win UI
        if (winningPlayer == 0 && player1WinUI != null)
            player1WinUI.SetActive(true);
        else if (winningPlayer == 1 && player2WinUI != null)
            player2WinUI.SetActive(true);

        // Optionally, start outro or reload scene after delay
        StartCoroutine(HandleEndgameOutro());
    }

    private IEnumerator HandleEndgameOutro()
    {
        yield return new WaitForSeconds(endgameDelay);
        // TODO: Transition to outro, podium, or reload scene as needed
        // Example: SceneManager.LoadScene("BugabooPlanet");
    }

    public void SkipIntro()
    {
        if (introComplete || introSkipped) return;
        
        introSkipped = true;
        Debug.Log("Intro skipped - going directly to countdown");
        
        // Stop any ongoing intro coroutines
        StopAllCoroutines();
        
        // Disable any active splash/overlay UI
        DisableAllSplashUI();
        
        // Set camera to gameplay position immediately
        if (gameCamera != null && gameplayPosition != null)
        {
            gameCamera.transform.position = gameplayPosition.position;
            gameCamera.transform.rotation = gameplayPosition.rotation;
        }
        
        // Enable player input and start countdown sequence
        EnablePlayerInput();
        StartCoroutine(SkipIntroCountdownSequence());
    }

    // Special countdown sequence for skipped intro that calls EnableGameplay at the end
    private IEnumerator SkipIntroCountdownSequence()
    {
        yield return StartCoroutine(PlayCountdown());
        
        // After countdown completes, enable gameplay (same as normal sequence)
        EnableGameplay();
        introComplete = true;
    }

    private void DisableAllSplashUI()
    {
        // Disable all splash/overlay UI elements
        if (frutyNameUI != null) frutyNameUI.SetActive(false);
        if (potatoNameUI != null) potatoNameUI.SetActive(false);
        if (vsSplashUI != null) vsSplashUI.SetActive(false);
        
        // Disable any overlay UI from flythrough points
        if (flythroughPoints != null)
        {
            foreach (var point in flythroughPoints)
            {
                if (point.overlayUI != null)
                    point.overlayUI.SetActive(false);
            }
        }
    }
}