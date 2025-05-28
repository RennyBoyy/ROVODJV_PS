using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class TutorialSceneController : MonoBehaviour
{
    [Header("Tutorial UI Panels")]
    [SerializeField] private GameObject[] tutorialPanels; // Different panels for different levels

    [Header("Ready System")]
    [SerializeField] private TextMeshProUGUI[] playerReadyTexts; // "Ready?" / "Ready!" texts for each player
    [SerializeField] private string waitingText = "Press X to Ready Up";
    [SerializeField] private string readyText = "Ready!";
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color waitingColor = Color.white;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    private InputAction[] confirmActions; // Confirm actions for each player
    private bool[] playerReady = new bool[2];
    private int currentTutorialIndex = 0;
    private bool allPlayersReady = false;

    void Start()
    {
        // Get the level info from PersistentSceneManager
        PersistentSceneManager sceneManager = PersistentSceneManager.Instance;
        if (sceneManager != null)
        {
            currentTutorialIndex = sceneManager.SelectedLevel;
            Debug.Log($"Tutorial Scene: Showing tutorial for level {currentTutorialIndex}");
        }

        SetupInputActions();
        ShowTutorial();
        StartCoroutine(FadeInFromBlack());
    }

    void SetupInputActions()
    {
        // Initialize input actions for both players
        confirmActions = new InputAction[2];

        // You might need to adjust this based on your input system setup
        // Option 1: Separate action maps for each player
        var playerMap1 = inputActions?.FindActionMap("Player");
        var playerMap2 = inputActions?.FindActionMap("Player2");

        if (playerMap1 != null)
            confirmActions[0] = playerMap1.FindAction("Confirm");
        if (playerMap2 != null)
            confirmActions[1] = playerMap2.FindAction("Confirm");

        // Option 2: If using PlayerInput with multiple players, you might need to find them differently
        // PlayerInput[] playerInputs = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        // for (int i = 0; i < playerInputs.Length && i < confirmActions.Length; i++)
        // {
        //     confirmActions[i] = playerInputs[i].currentActionMap.FindAction("Confirm");
        // }
    }

    void OnEnable()
    {
        for (int i = 0; i < confirmActions.Length; i++)
        {
            if (confirmActions[i] != null)
            {
                confirmActions[i].Enable();
                int playerIndex = i; // Capture for closure
                confirmActions[i].performed += (ctx) => OnPlayerConfirm(playerIndex);
            }
        }
    }

    void OnDisable()
    {
        for (int i = 0; i < confirmActions.Length; i++)
        {
            if (confirmActions[i] != null)
            {
                confirmActions[i].performed -= (ctx) => OnPlayerConfirm(i);
                confirmActions[i].Disable();
            }
        }
    }

    void ShowTutorial()
    {
        // Hide all tutorial panels first
        foreach (var panel in tutorialPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        // Show the appropriate tutorial panel based on selected level
        int panelIndex = Mathf.Clamp(currentTutorialIndex, 0, tutorialPanels.Length - 1);
        if (panelIndex < tutorialPanels.Length && tutorialPanels[panelIndex] != null)
        {
            tutorialPanels[panelIndex].SetActive(true);
        }

        // Reset ready states
        for (int i = 0; i < playerReady.Length; i++)
        {
            playerReady[i] = false;
        }

        UpdateReadyUI();
    }

    void OnPlayerConfirm(int playerIndex)
    {
        if (allPlayersReady || playerIndex >= playerReady.Length) return;

        playerReady[playerIndex] = true;
        UpdateReadyUI();

        // Check if all players are ready
        bool allReady = true;
        for (int i = 0; i < playerReady.Length; i++)
        {
            if (!playerReady[i])
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            allPlayersReady = true;
            StartCoroutine(StartGameAfterDelay());
        }
    }

    void UpdateReadyUI()
    {
        for (int i = 0; i < playerReadyTexts.Length && i < playerReady.Length; i++)
        {
            if (playerReadyTexts[i] != null)
            {
                playerReadyTexts[i].text = playerReady[i] ? readyText : waitingText;
                playerReadyTexts[i].color = playerReady[i] ? readyColor : waitingColor;
            }
        }
    }

    IEnumerator StartGameAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        // Tell the PersistentSceneManager to load the actual game scene
        PersistentSceneManager sceneManager = PersistentSceneManager.Instance;
        if (sceneManager != null)
        {
            sceneManager.LoadGameSceneWithTransition();
        }
        else
        {
            Debug.LogError("PersistentSceneManager not found!");
        }
    }

    IEnumerator FadeInFromBlack()
    {
        // The scene manager will handle the fade in, but we might want to do additional setup here
        yield return new WaitForSeconds(0.1f);
        // Any additional setup can go here
    }

    // Public method for UI buttons if you prefer button-based ready system instead of input actions
    public void OnReadyButtonPressed(int playerIndex)
    {
        OnPlayerConfirm(playerIndex);
    }
}