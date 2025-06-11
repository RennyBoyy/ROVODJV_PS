using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameOverManager : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomDistance = 5f;
    [SerializeField] private float zoomHeight = 1f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float loseAnimationDuration = 2.5f;

    [Header("Player Targets")]
    [SerializeField] private Transform player1Target;
    [SerializeField] private Transform player2Target;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private Image gameOverImage;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float decisionTimer = 20f;

    [Header("Player 1 Buttons")]
    [SerializeField] private Image player1ReadyButton;
    [SerializeField] private Image player1CancelButton;
    [SerializeField] private Sprite player1ReadyPressed;
    [SerializeField] private Sprite player1CancelPressed;

    [Header("Player 2 Buttons")]
    [SerializeField] private Image player2ReadyButton;
    [SerializeField] private Image player2CancelButton;
    [SerializeField] private Sprite player2ReadyPressed;
    [SerializeField] private Sprite player2CancelPressed;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Scene Settings")]
    [SerializeField] private string planetSceneName = "BugabooPlanet";
    [SerializeField] private float choiceExecutionDelay = 3f;

    [Header("Podium Settings")]
    [SerializeField] private Transform podiumWinnerSpot;
    [SerializeField] private Transform podiumLoserSpot;
    [SerializeField] private Transform podiumScarecrowSpot;
    [SerializeField] private GameObject scarecrowPrefab;
    private GameObject spawnedScarecrow;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private InputAction[] confirmActions;
    private InputAction[] cancelActions;
    private bool[] playerReady = new bool[2];
    private bool[] playerCanceled = new bool[2];
    private Sprite[] originalReadySprites = new Sprite[2];
    private Sprite[] originalCancelSprites = new Sprite[2];
    private bool gameOverActive = false;
    private float currentTimer;
    private bool choiceMade = false;

    private static GameOverManager instance;
    [SerializeField] private float cameraXOffset;
    [SerializeField] private float cameraYOffset;

    public static GameOverManager Instance => instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gameCamera != null)
        {
            originalCameraPosition = gameCamera.transform.position;
            originalCameraRotation = gameCamera.transform.rotation;
        }

        SetupInputActions();
        StoreOriginalSprites();

        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }

    void SetupInputActions()
    {
        confirmActions = new InputAction[2];
        cancelActions = new InputAction[2];

        try
        {
            if (inputActions != null)
            {
                var playerMap1 = inputActions.FindActionMap("Player");
                var playerMap2 = inputActions.FindActionMap("Player2");

                if (playerMap1 != null)
                {
                    confirmActions[0] = playerMap1.FindAction("Confirm");
                    cancelActions[0] = playerMap1.FindAction("Cancel");
                }
                if (playerMap2 != null)
                {
                    confirmActions[1] = playerMap2.FindAction("Confirm");
                    cancelActions[1] = playerMap2.FindAction("Cancel");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Input setup failed: {e.Message}");
        }
    }

    void StoreOriginalSprites()
    {
        if (player1ReadyButton != null)
            originalReadySprites[0] = player1ReadyButton.sprite;
        if (player1CancelButton != null)
            originalCancelSprites[0] = player1CancelButton.sprite;
        if (player2ReadyButton != null)
            originalReadySprites[1] = player2ReadyButton.sprite;
        if (player2CancelButton != null)
            originalCancelSprites[1] = player2CancelButton.sprite;
    }

    void OnEnable()
    {
        for (int i = 0; i < confirmActions.Length; i++)
        {
            if (confirmActions[i] != null)
            {
                confirmActions[i].Enable();
                int playerIndex = i;
                confirmActions[i].performed += (ctx) => OnPlayerReady(playerIndex);
            }
            if (cancelActions[i] != null)
            {
                cancelActions[i].Enable();
                int playerIndex = i;
                cancelActions[i].performed += (ctx) => OnPlayerCancel(playerIndex);
            }
        }
    }

    void OnDisable()
    {
        for (int i = 0; i < confirmActions.Length; i++)
        {
            if (confirmActions[i] != null)
            {
                confirmActions[i].performed -= (ctx) => OnPlayerReady(i);
                confirmActions[i].Disable();
            }
            if (cancelActions[i] != null)
            {
                cancelActions[i].performed -= (ctx) => OnPlayerCancel(i);
                cancelActions[i].Disable();
            }
        }
    }

    void Update()
    {
        if (gameOverActive && !choiceMade)
        {
            currentTimer -= Time.deltaTime;
            UpdateTimerDisplay();

            if (currentTimer <= 0f)
            {
                choiceMade = true;
                StartCoroutine(ExecuteChoice(false));
            }

            if (confirmActions[0] == null || confirmActions[1] == null)
            {
                if (Input.GetKeyDown(KeyCode.JoystickButton1))
                    OnPlayerReady(0);
                if (Input.GetKeyDown(KeyCode.Joystick2Button1))
                    OnPlayerReady(1);
                if (Input.GetKeyDown(KeyCode.JoystickButton0))
                    OnPlayerCancel(0);
                if (Input.GetKeyDown(KeyCode.Joystick2Button0))
                    OnPlayerCancel(1);
            }
        }
    }

    public void TriggerGameOver(bool isPlayer1Loser)
    {
        if (gameOverActive) return;

        StartCoroutine(GameOverSequence(isPlayer1Loser));
    }

    IEnumerator GameOverSequence(bool isPlayer1Loser)
    {
        gameOverActive = true;

        

        // Assign winner and loser
        Transform winner = isPlayer1Loser ? player2Target : player1Target;
        Transform loser = isPlayer1Loser ? player1Target : player2Target;

        if (winner != null && podiumLoserSpot != null)
            winner.position = podiumLoserSpot.position;
        if (loser != null && podiumWinnerSpot != null)
            loser.position = podiumWinnerSpot.position;
        // flat target so bear doesn’t tilt up/down
        Vector3 camFlat = gameCamera.transform.position;
        camFlat.y = winner.position.y;

        // point +Z at camera…
        winner.LookAt(camFlat, Vector3.up);
        // …but your mesh’s “face” is actually on –Z, so flip it
        winner.Rotate(0f, 200f, 0f, Space.Self);

        Vector3 camFlatL = gameCamera.transform.position;
        camFlatL.y = loser.position.y;
        loser.LookAt(camFlatL, Vector3.up);
        loser.Rotate(0f, 200f, 0f, Space.Self);


        // Place scarecrow on third spot
        if (scarecrowPrefab != null && podiumScarecrowSpot != null)
        {
            if (spawnedScarecrow != null)
                Destroy(spawnedScarecrow);
            spawnedScarecrow = Instantiate(scarecrowPrefab, podiumScarecrowSpot.position, podiumScarecrowSpot.rotation);
        }

        // Pan camera to frame the podium
        yield return StartCoroutine(PanToPodium());

        // Play animations if needed
        if (winner != null)
        {
            Animator winnerAnimator = winner.GetComponent<Animator>();
            if (winnerAnimator != null)
                winnerAnimator.Play("Idle stance", 0, 0f);
        }
        if (loser != null)
        {
            Animator loserAnimator = loser.GetComponent<Animator>();
            if (loserAnimator != null)
                loserAnimator.Play("Lose Animation", 0, 0f);
        }

        yield return new WaitForSeconds(loseAnimationDuration);

        ShowGameOverUI();
    }

    /*IEnumerator PanToPlayer(Transform target)
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
    }*/

    IEnumerator PanToPodium()
    {
        if (gameCamera == null || podiumWinnerSpot == null || podiumLoserSpot == null || podiumScarecrowSpot == null)
            yield break;

        // Calculate center point of the podium
        Vector3 center = (podiumWinnerSpot.position + podiumLoserSpot.position + podiumScarecrowSpot.position) / 3f;

        // Offset for camera position
        Vector3 offset = Quaternion.Euler(cameraXOffset, cameraYOffset, 0f) * new Vector3(0, 0, -zoomDistance * 1.5f) + Vector3.up * zoomHeight;
        Vector3 cameraTargetPos = center + offset;

        // Look at the center from the offset
        Quaternion cameraTargetRot = Quaternion.LookRotation(center - cameraTargetPos);

        Vector3 startPosition = gameCamera.transform.position;
        Quaternion startRotation = gameCamera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            float t = zoomCurve.Evaluate(elapsed);

            gameCamera.transform.position = Vector3.Lerp(startPosition, cameraTargetPos, t);
            gameCamera.transform.rotation = Quaternion.Lerp(startRotation, cameraTargetRot, t);

            yield return null;
        }

        gameCamera.transform.position = cameraTargetPos;
        gameCamera.transform.rotation = cameraTargetRot;
    }

    void ShowGameOverUI()
    {
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        for (int i = 0; i < 2; i++)
        {
            playerReady[i] = false;
            playerCanceled[i] = false;
        }

        ResetButtonSprites();

        currentTimer = decisionTimer;
        choiceMade = false;
        UpdateTimerDisplay();
    }

    void ResetButtonSprites()
    {
        if (player1ReadyButton != null && originalReadySprites[0] != null)
            player1ReadyButton.sprite = originalReadySprites[0];
        if (player1CancelButton != null && originalCancelSprites[0] != null)
            player1CancelButton.sprite = originalCancelSprites[0];
        if (player2ReadyButton != null && originalReadySprites[1] != null)
            player2ReadyButton.sprite = originalReadySprites[1];
        if (player2CancelButton != null && originalCancelSprites[1] != null)
            player2CancelButton.sprite = originalCancelSprites[1];
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(currentTimer);
            timerText.text = seconds.ToString();
        }
    }

    void OnPlayerReady(int playerIndex)
    {
        if (!gameOverActive || choiceMade || playerIndex >= 2) return;

        playerReady[playerIndex] = true;
        playerCanceled[playerIndex] = false;

        Debug.Log($"Player {playerIndex + 1} ready for replay");

        if (playerIndex == 0 && player1ReadyButton != null && player1ReadyPressed != null)
        {
            player1ReadyButton.sprite = player1ReadyPressed;
            if (player1CancelButton != null && originalCancelSprites[0] != null)
                player1CancelButton.sprite = originalCancelSprites[0];
        }
        else if (playerIndex == 1 && player2ReadyButton != null && player2ReadyPressed != null)
        {
            player2ReadyButton.sprite = player2ReadyPressed;
            if (player2CancelButton != null && originalCancelSprites[1] != null)
                player2CancelButton.sprite = originalCancelSprites[1];
        }

        CheckForDecision();
    }

    void OnPlayerCancel(int playerIndex)
    {
        if (!gameOverActive || choiceMade || playerIndex >= 2) return;

        playerCanceled[playerIndex] = true;
        playerReady[playerIndex] = false;

        Debug.Log($"Player {playerIndex + 1} wants to return to planet");

        if (playerIndex == 0 && player1CancelButton != null && player1CancelPressed != null)
        {
            player1CancelButton.sprite = player1CancelPressed;
            if (player1ReadyButton != null && originalReadySprites[0] != null)
                player1ReadyButton.sprite = originalReadySprites[0];
        }
        else if (playerIndex == 1 && player2CancelButton != null && player2CancelPressed != null)
        {
            player2CancelButton.sprite = player2CancelPressed;
            if (player2ReadyButton != null && originalReadySprites[1] != null)
                player2ReadyButton.sprite = originalReadySprites[1];
        }

        CheckForDecision();
    }

    void CheckForDecision()
    {
        if (playerCanceled[0] || playerCanceled[1])
        {
            choiceMade = true;
            StartCoroutine(ExecuteChoice(false));      
            return;
        }

        if (playerReady[0] && playerReady[1])
        {
            choiceMade = true;
            StartCoroutine(ExecuteChoice(true));    
            return;
        }
    }

    IEnumerator ExecuteChoice(bool replay)
    {
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        Debug.Log("Game Over - waiting for Main Menu button.");

        // No automatic scene change!
        // Wait here until the player presses the Main Menu button.
        yield break;
    }

    // Add this method to be called by your Main Menu button:
    public void OnMainMenuButtonPressed()
    {
        PersistentSceneManager sceneManager = PersistentSceneManager.Instance;
        if (sceneManager != null)
        {
            sceneManager.ReturnToHub(planetSceneName);
        }
        else
        {
            SceneManager.LoadScene(planetSceneName);
        }
    }

    public void OnReplayButtonPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void HandleGameEnd(bool player1Lost)
    {
        TriggerGameOver(player1Lost);

    }

    public void SetPlayerTargets(Transform player1, Transform player2)
    {
        player1Target = player1;
        player2Target = player2;
    }

    public bool IsGameOverActive()
    {
        return gameOverActive;
    }
}