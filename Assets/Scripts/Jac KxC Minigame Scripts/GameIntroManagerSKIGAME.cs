using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]


public class GameIntroManagerSKIGAME : MonoBehaviour
{
    public static GameIntroManagerSKIGAME Instance { get; private set; }

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
    [SerializeField] private GameObject gameCameraobject;

    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private FlythroughPoint[] flythroughPoints;
    [SerializeField] private float flySpeed = 1f;
    [Space(30)]
    [SerializeField] private GameObject frutyNameUI;
    [SerializeField] private GameObject potatoNameUI;
    [SerializeField] private GameObject vsSplashUI;
    [SerializeField] private Transform vsSplashPoint;
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
    [SerializeField] private Animator[] bystanderAnimators;
    [SerializeField] private float cheerStartDelayMin = 0f;
    [SerializeField] private float cheerStartDelayMax = 2f;

    [Header("UI Canvases")]
    [SerializeField] private GameObject gameplayUIPanel;
    [SerializeField] private GameObject player1WinUI;
    [SerializeField] private GameObject player2WinUI;
    // Add other global UI canvases here as needed

    [Header("ENDING")]
    [SerializeField] private Transform podium1stPlace;
    [SerializeField] private Transform podium2ndPlace;
    [SerializeField] private Transform outroCameraTransform;
    [SerializeField] private Transform PodeumPositionend;
    [SerializeField] private float endgameDelay = 3f;
    [SerializeField] private float outroCameraTransitionSpeed = 2f;
    [SerializeField] private AnimationCurve outroCameraCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    // Add other ending/outro fields here as needed

    [Header("Skip Intro")]
    [SerializeField] private KeyCode skipIntroKey = KeyCode.Space;
    [SerializeField] private bool allowSkipIntro = true;

    [Header("Skip Intro Tooltip")]
    [SerializeField] private GameObject skipIntroTooltipPanel;
    [SerializeField] private Animator[] kittyAnimators;



    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private RectTransform countdownRectTransform;
    private Vector3 originalCountdownScale;
    private bool introComplete = false;
    private PlayerController[] playerScripts;
    private bool gameEnded = false;
    private bool introSkipped = false;


    [SerializeField] private GameObject retryButtonHighlight;
    [SerializeField] private GameObject planetButtonHighlight;
    [SerializeField] private string currentSceneName = "GameScene";
    [SerializeField] private string planetSceneName = "PlanetScene";
    [SerializeField] private KeyCode selectKey = KeyCode.Space;
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;



    private int currentEndgameSelection = 0; // 0 = Retry, 1 = Planet
    private bool endgameUIActive = false;
    private bool endgameInputEnabled = false;

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


        playerScripts = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        DisablePlayerInput();

        SetupCountdownUI();

        Debug.Log("GameIntroManager starting intro sequence");

        if (skipIntroTooltipPanel != null)
            skipIntroTooltipPanel.SetActive(true);

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

        if (endgameUIActive && endgameInputEnabled)
        {
            HandleEndgameUIInput();
        }

    }

    // === ENDGAME BULLSHIT UI===

    private void InitializeEndgameUI()
    {
        if (retryButtonHighlight != null)
            retryButtonHighlight.SetActive(false);
        if (planetButtonHighlight != null)
            planetButtonHighlight.SetActive(false);
    }

    private void HandleEndgameUIInput()
    {
        bool upPressed = Input.GetKeyDown(upKey) ||
                        (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.leftStick.ReadValue().y > 0.5f);

        bool downPressed = Input.GetKeyDown(downKey) ||
                          (Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame) ||
                          (Gamepad.current != null && Gamepad.current.leftStick.ReadValue().y < -0.5f);

        if (upPressed)
        {
            NavigateEndgameUp();
        }
        else if (downPressed)
        {
            NavigateEndgameDown();
        }

        if (Input.GetKeyDown(selectKey) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
        {
            SelectCurrentEndgameOption();
        }
    }

    private void NavigateEndgameUp()
    {
        currentEndgameSelection = (currentEndgameSelection - 1 + 2) % 2;
        UpdateEndgameSelectionDisplay();
    }

    private void NavigateEndgameDown()
    {
        currentEndgameSelection = (currentEndgameSelection + 1) % 2;
        UpdateEndgameSelectionDisplay();
    }

    private void UpdateEndgameSelectionDisplay()
    {
        if (retryButtonHighlight != null)
            retryButtonHighlight.SetActive(currentEndgameSelection == 0);

        if (planetButtonHighlight != null)
            planetButtonHighlight.SetActive(currentEndgameSelection == 1);
    }

    private void SelectCurrentEndgameOption()
    {

        switch (currentEndgameSelection)
        {
            case 0:
                RetryGame();
                break;
            case 1:
                GoToPlanet();
                break;
        }
    }

    private void RetryGame()
    {
        endgameInputEnabled = false;

        if (!string.IsNullOrEmpty(currentSceneName))
        {
            SceneManager.LoadScene(currentSceneName);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void GoToPlanet()
    {
        endgameInputEnabled = false;

        if (!string.IsNullOrEmpty(planetSceneName))
        {
            SceneManager.LoadScene(planetSceneName);
        }
    }

    private void ShowEndgameUI()
    {
        endgameUIActive = true;
        currentEndgameSelection = 0;
        UpdateEndgameSelectionDisplay();
        endgameInputEnabled = true;
    }

    public void SetCurrentSceneName(string sceneName)
    {
        currentSceneName = sceneName;
    }

    public void SetPlanetSceneName(string sceneName)
    {
        planetSceneName = sceneName;
    }


    void StartCheeringSequence()
    {
        foreach (var animator in bystanderAnimators)
        {
            float delay = Random.Range(cheerStartDelayMin, cheerStartDelayMax);
            StartCoroutine(StartCheerWithDelay(animator, delay));
        }
    }

    private IEnumerator StartCheerWithDelay(Animator animator, float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool("isCheering", true);
        yield return new WaitForSeconds(20f);
        animator.SetBool("isCheering", false);
    }

    IEnumerator FlyThroughCameraPath()
    {
        if (flythroughPoints == null || flythroughPoints.Length < 4) yield break;

        gameCamera.transform.position = flythroughPoints[0].point.position;
        gameCamera.transform.rotation = flythroughPoints[0].point.rotation;

        for (int i = 1; i < flythroughPoints.Length - 2; i++)
        {
            StartCheeringSequence();
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
                // Only trigger animation if this is a character intro target
                if (next.isCharacter)
                {
                    foreach (var player in playerScripts)
                    {
                        if (player.PlayerType == next.targetPlayer)
                        {
                            player.PlayIntroTargetAnimation(next.introAnimTrigger);
                        }
                    }
                }

                if (next.overlayUI != null) next.overlayUI.SetActive(true);

                float pauseElapsed = 0f;
                while (pauseElapsed < next.pauseDuration)
                {
                    pauseElapsed += Time.deltaTime;
                    gameCamera.transform.position += next.driftDirection * next.driftSpeed * Time.deltaTime;
                    yield return null;
                }

                // Only reset animation if this is a character intro target
                if (next.isCharacter)
                {
                    foreach (var player in playerScripts)
                    {
                        if (player.PlayerType == next.targetPlayer)
                        {
                            player.ResetIntroTargetAnimation(next.introAnimTrigger);
                        }
                    }
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

        if (skipIntroTooltipPanel != null)
            skipIntroTooltipPanel.SetActive(false);
        EnableGameplay();


        yield return StartCoroutine(PlayCountdown());

       
        introComplete = true;
    }


    // Smoothly transitions the camera back to the gameplay position
    IEnumerator ReturnToGameplayView()
    {
        Debug.Log("Returning camera to gameplay view");
        if (gameCamera == null) yield break;

        Vector3 startPosition = gameCamera.transform.position;
        Quaternion startRotation = gameCamera.transform.rotation;


        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            float t = zoomCurve.Evaluate(elapsed);
            yield return null;
        }

        gameCameraobject.SetActive(false);
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
                player.moving = false;
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
                foreach (var animator in kittyAnimators)
                {
                    animator?.SetTrigger("Start");  
                }
            }
        }
    }

    // Marks gameplay as active and lets players/thieves start moving
    void EnableGameplay()
    {
        GameManager_Slope gameManager = FindFirstObjectByType<GameManager_Slope>();

        EnablePlayerInput();

        

        if (gameplayUIPanel != null)
        {
            gameplayUIPanel.SetActive(true);
            Debug.Log("Gameplay UI panel activated at EnableGameplay");
        }

        Debug.Log("Gameplay enabled!");
    }



    

    public bool IsIntroComplete()
    {
        return introComplete;
    }

    public bool IsGameEnded()
    {
        return gameEnded;
    }

    // --- ENDGAME HANDLING ---
    public void OnGameEnd(int winningPlayer)
    {
        gameEnded = true;

        // Disable player input during outro
        DisablePlayerInput();

        // Disable gameplay UI panel
        if (gameplayUIPanel != null)
        {
            gameplayUIPanel.SetActive(false);
            Debug.Log("Gameplay UI panel deactivated at game end");
        }

       

        // Start the outro sequence with a small delay
        StartCoroutine(StartOutroSequence(winningPlayer));

    }

    private void SetupPodiumCharacters(int winningPlayer)
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            PlayerController player = players[i];
            if (player == null) continue;

            // Determine if this player won or lost
            // 0 = P1 (Fruity) lost, so P2 (Potato) won
            // 1 = P2 (Potato) lost, so P1 (Fruity) won
            bool isWinner = (player.playerID == winningPlayer);
            if (isWinner)
            {
                player.transform.position = podium1stPlace.position;
            }
            else
            {
                player.transform.position = podium2ndPlace.position;
            }

            // Make characters face the camera (screen)
            FaceCharacterToCamera(player);

            // Play victory or defeat animation
            string animationTrigger = isWinner ? "Victory" : "Defeat";
            player.PlayIntroTargetAnimation(animationTrigger);
        }
    }

    private void FaceCharacterToCamera(PlayerController player)
    {
        if (player == null || gameCamera == null) return;

        // Get the direction from character to camera
        Vector3 directionToCamera = (gameCamera.transform.position - player.transform.position).normalized;
        directionToCamera.y = 0; // Keep rotation only on horizontal plane

        // Handle different character facing directions
        if (player.PlayerType == PlayerIdentity.Fruity)
        {
            // P1 Fruity's model faces -Z, so we need to rotate 180 degrees
            // to make it face the camera properly
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            player.transform.rotation = targetRotation;
        }
        else if (player.PlayerType == PlayerIdentity.Potato)
        {
            // P2 Potato's model faces +Z, so normal facing works
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            player.transform.rotation = targetRotation;
        }
    }

    private IEnumerator StartOutroSequence(int winningPlayer)
    {
        if (PodeumPositionend != null)
        {
            gameCameraobject.transform.position = PodeumPositionend.position;
            gameCameraobject.transform.rotation = PodeumPositionend.rotation;
        }

        gameCameraobject.SetActive(true);

        // ⏳ Delay BEFORE we teleport players or animate
        yield return new WaitForSeconds(endgameDelay);

        // Move camera to outro position
        if (outroCameraTransform != null && gameCamera != null)
        {
            yield return StartCoroutine(SmoothLerpToOutro());
        }

        // NOW set up the podium after the delay + camera transition
        SetupPodiumCharacters(winningPlayer);
        StartCheeringSequence();
        // Show the appropriate Win UI
        if (winningPlayer == 0 && player1WinUI != null)
            player1WinUI.SetActive(true);
        else if (winningPlayer == 1 && player2WinUI != null)
            player2WinUI.SetActive(true);

        StartCoroutine(HandleEndgameOutro());
    }


    private IEnumerator SmoothLerpToOutro()
    {
        Vector3 startPosition = gameCamera.transform.position;
        Quaternion startRotation = gameCamera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * outroCameraTransitionSpeed;
            float t = outroCameraCurve.Evaluate(elapsed);

            gameCamera.transform.position = Vector3.Lerp(startPosition, outroCameraTransform.position, t);
            gameCamera.transform.rotation = Quaternion.Lerp(startRotation, outroCameraTransform.rotation, t);

            yield return null;
        }

        gameCamera.transform.position = outroCameraTransform.position;
        gameCamera.transform.rotation = outroCameraTransform.rotation;
    }

    private IEnumerator HandleEndgameOutro()
    {
        yield return new WaitForSeconds(endgameDelay);
        // Endgame UI is now visible, input is handled in Update()

        ShowEndgameUI();
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
        if (gameCamera != null)
        {
            gameCameraobject.SetActive(false);
        }

        // Enable player input and start countdown sequence
        EnablePlayerInput();
        StartCoroutine(SkipIntroCountdownSequence());
        if (skipIntroTooltipPanel != null)
            skipIntroTooltipPanel.SetActive(false);
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