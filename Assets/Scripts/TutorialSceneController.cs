using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class TutorialSceneController : MonoBehaviour
{
    [Header("Tutorial UI Panels")]
    [SerializeField] private GameObject[] tutorialPanels;

    [Header("Ready System - Image Based")]
    [SerializeField] private GameObject[] playerReadyImages = new GameObject[2];
    [SerializeField] private Sprite waitingSprite;
    [SerializeField] private Sprite readySprite;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Debug Options")]
    [SerializeField] private bool singleControllerDebug = false;

    private InputAction[] confirmActions;
    private bool[] playerReady = new bool[2];
    private int currentTutorialIndex = 0;
    private bool allPlayersReady = false;
    private Image[] readyImages = new Image[2];

    void Awake()
    {
        confirmActions = new InputAction[2];
        SetupInputActions();
        CacheReadyImages();
    }

    void Start()
    {
        PersistentSceneManager sceneManager = PersistentSceneManager.Instance;
        if (sceneManager != null)
        {
            currentTutorialIndex = sceneManager.SelectedLevel;
            Debug.Log($"Tutorial Scene: Showing tutorial for level {currentTutorialIndex}");
        }

        //int playerIndex = -1;
        //for (int i = 0; i < allPlayers.Length; i++)
        //{
        //    if (allPlayers[i] == this)
        //    {
        //        playerIndex = i;
        //        break;
        //    }
        //}

        //var gamepads = Gamepad.all;
        //if (playerIndex == 0 && gamepads.Count > 0) // Fruity (P1)
        //{
        //    if (player1Input != null)
        //    {
        //        player1Input.SwitchCurrentControlScheme("Gamepad", gamepads[0]);
        //        player1Input.ActivateInput();
        //    }
        //}
        //else if (playerIndex == 1 && gamepads.Count > 1) // Potato (P2)
        //{
        //    if (player2Input != null)
        //    {
        //        player2Input.SwitchCurrentControlScheme("Gamepad", gamepads[1]);
        //        player2Input.ActivateInput();
        //    }
        //}



        ShowTutorial();
        StartCoroutine(FadeInFromBlack());
    }

    void CacheReadyImages()
    {
        for (int i = 0; i < playerReadyImages.Length; i++)
        {
            if (playerReadyImages[i] != null)
            {
                readyImages[i] = playerReadyImages[i].GetComponent<Image>();
                if (readyImages[i] == null)
                {
                    Debug.LogError($"Player {i + 1} ready image doesn't have an Image component!");
                }
            }
            else
            {
                Debug.LogError($"Player {i + 1} ready image GameObject is not assigned!");
            }
        }
    }

    void SetupInputActions()
    {
        try
        {
            if (inputActions != null)
            {
                var playerMap1 = inputActions.FindActionMap("Player");
                var playerMap2 = inputActions.FindActionMap("Player2");

                if (playerMap1 != null)
                    confirmActions[0] = playerMap1.FindAction("Confirm");
                if (playerMap2 != null)
                    confirmActions[1] = playerMap2.FindAction("Confirm");

                Debug.Log($"Input setup complete. Player1 action: {(confirmActions[0] != null ? "Found" : "Not found")}, Player2 action: {(confirmActions[1] != null ? "Found" : "Not found")}");
            }
            else
            {
                Debug.LogWarning("InputActions asset not assigned! Tutorial will use fallback input.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Input setup failed: {e.Message}. Using fallback input system.");
        }
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        int index = playerInput.playerIndex;
        PlayerIdentity identity = (PlayerIdentity)index;
        Gamepad pad = playerInput.devices.Count > 0 ? playerInput.devices[0] as Gamepad : null;
        PlayerManager.Instance.RegisterPlayer(index, pad, identity);

        // Mark the player ready and run the logic inside OnPlayerConfirm
        OnPlayerConfirm(index);
    }

    void Update()
    {
        if (singleControllerDebug)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                for (int i = 0; i < playerReady.Length; i++)
                {
                    if (!playerReady[i])
                    {
                        OnPlayerConfirm(i);
                        return;
                    }
                }
            }
        }
        else if (confirmActions[0] == null || confirmActions[1] == null)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Space))
            {
                OnPlayerConfirm(0);
            }

            if (Input.GetKeyDown(KeyCode.Joystick2Button1) || Input.GetKeyDown(KeyCode.Return))
            {
                OnPlayerConfirm(1);
            }
        }
    }

    void ShowTutorial()
    {
        foreach (var panel in tutorialPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        int panelIndex = Mathf.Clamp(currentTutorialIndex - 1, 0, tutorialPanels.Length - 1);
        if (panelIndex < tutorialPanels.Length && tutorialPanels[panelIndex] != null)
        {
            tutorialPanels[panelIndex].SetActive(true);
            Debug.Log($"Showing tutorial panel {panelIndex} for level {currentTutorialIndex}");
        }
        else
        {
            Debug.LogWarning($"No tutorial panel found for level {currentTutorialIndex} (panel index {panelIndex})");
        }

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
        Debug.Log($"Player {playerIndex + 1} is ready!");
        UpdateReadyUI();

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
            Debug.Log("All players ready! Starting game...");
            StartCoroutine(StartGameAfterDelay());
        }
    }

    void UpdateReadyUI()
    {
        for (int i = 0; i < readyImages.Length && i < playerReady.Length; i++)
        {
            if (readyImages[i] != null)
            {
                readyImages[i].sprite = playerReady[i] ? readySprite : waitingSprite;
                Debug.Log($"Updated Player {i + 1} image to {(playerReady[i] ? "ready" : "waiting")} sprite");
            }
        }
    }

    IEnumerator StartGameAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

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
        yield return new WaitForSeconds(0.1f);
        Debug.Log("Tutorial scene loaded and faded in via PersistentSceneManager");
    }

    public void OnReadyButtonPressed(int playerIndex)
    {
        OnPlayerConfirm(playerIndex);
    }
}