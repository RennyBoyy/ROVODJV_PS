using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class TutorialSceneController : MonoBehaviour
{
    [Header("Tutorial UI Panels")]
    [SerializeField] private GameObject[] tutorialPanels;      

    [Header("Ready System")]
    [SerializeField] private TextMeshProUGUI[] playerReadyTexts;        
    [SerializeField] private string waitingText = "Press X to Ready Up";
    [SerializeField] private string readyText = "Ready!";
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color waitingColor = Color.white;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Debug Options")]
    [SerializeField] private bool singleControllerDebug = false;      
    [SerializeField] private KeyCode debugReadyKey = KeyCode.JoystickButton2;     

    private InputAction[] confirmActions;      
    private bool[] playerReady = new bool[2];
    private int currentTutorialIndex = 0;
    private bool allPlayersReady = false;

    void Awake()
    {
        confirmActions = new InputAction[2];

        SetupInputActions();
    }

    void Start()
    {
        PersistentSceneManager sceneManager = PersistentSceneManager.Instance;
        if (sceneManager != null)
        {
            currentTutorialIndex = sceneManager.SelectedLevel;
            Debug.Log($"Tutorial Scene: Showing tutorial for level {currentTutorialIndex}");
        }

        ShowTutorial();

        StartCoroutine(FadeInFromBlack());
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

    void OnEnable()
    {
        for (int i = 0; i < confirmActions.Length; i++)
        {
            if (confirmActions[i] != null)
            {
                confirmActions[i].Enable();
                int playerIndex = i;    
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