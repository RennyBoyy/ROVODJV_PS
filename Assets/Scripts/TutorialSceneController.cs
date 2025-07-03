using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

[System.Serializable]
public class MinigameUISet
{
    [Header("Main UI")]
    public GameObject tutorialPanel;

    [Header("Player 1 Ready System")]
    public GameObject player1ReadyImage;
    public Sprite player1WaitingSprite;
    public Sprite player1ReadySprite;

    [Header("Player 2 Ready System")]
    public GameObject player2ReadyImage;
    public Sprite player2WaitingSprite;
    public Sprite player2ReadySprite;
}

public class TutorialSceneController : MonoBehaviour
{
    [Header("Minigame UI Sets")]
    [SerializeField] private MinigameUISet[] minigameUISets;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    private InputAction[] confirmActions;
    private bool[] playerReady = new bool[2];
    private int currentTutorialIndex = 0;
    private bool allPlayersReady = false;
    private Image[] currentReadyImages = new Image[2];
    private MinigameUISet currentUISet;

    [SerializeField] private AudioSource sfxSource;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [SerializeField] private AudioClip readySound;

    void Awake()
    {
        confirmActions = new InputAction[2];
        SetupInputActions();
        ConfigureAudioSource(sfxSource, sfxVolume, false);
    }

    void Start()
    {
        PersistentSceneManager sceneManager = PersistentSceneManager.Instance;
        if (sceneManager != null)
        {
            currentTutorialIndex = sceneManager.SelectedLevel;
        }
        ShowTutorial();
        StartCoroutine(FadeInFromBlack());
    }

    private void ConfigureAudioSource(AudioSource source, float volume, bool loop)
    {
        if (source != null)
        {
            source.loop = loop;
            source.playOnAwake = false;
            source.volume = volume;
        }
    }

    public void PlayReadySound()
    {
        if (sfxSource != null && readySound != null)
        {
            sfxSource.PlayOneShot(readySound);
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

                if (confirmActions[0] != null)
                {
                    Debug.Log("Player 1 confirmed");

                    confirmActions[0].performed += ctx => OnPlayerConfirm(0);
                    confirmActions[0].Enable();
                }
                if (confirmActions[1] != null)
                {
                    Debug.Log("Player 2 confirmed");

                    confirmActions[1].performed += ctx => OnPlayerConfirm(1);
                    confirmActions[1].Enable();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Input setup failed: {e.Message}. Using fallback input system.");
        }
    }

   

    void Update()
    {
       
        if (confirmActions[0] == null || confirmActions[1] == null)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {

                OnPlayerConfirm(0);
                PlayReadySound();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                OnPlayerConfirm(1);
                PlayReadySound();
            }
        }
    }

    void ShowTutorial()
    {
        HideAllMinigameUIs();

        int uiSetIndex = Mathf.Clamp(currentTutorialIndex - 1, 0, minigameUISets.Length - 1);

        if (uiSetIndex < minigameUISets.Length && minigameUISets[uiSetIndex] != null)
        {
            currentUISet = minigameUISets[uiSetIndex];
            ShowMinigameUI(currentUISet);
            CacheCurrentReadyImages();
        }

        for (int i = 0; i < playerReady.Length; i++)
        {
            playerReady[i] = false;
        }

        UpdateReadyUI();
    }

    void HideAllMinigameUIs()
    {
        foreach (var uiSet in minigameUISets)
        {
            if (uiSet != null)
            {
                if (uiSet.tutorialPanel != null)
                    uiSet.tutorialPanel.SetActive(false);
                if (uiSet.player1ReadyImage != null)
                    uiSet.player1ReadyImage.SetActive(false);
                if (uiSet.player2ReadyImage != null)
                    uiSet.player2ReadyImage.SetActive(false);
            }
        }
    }

    void ShowMinigameUI(MinigameUISet uiSet)
    {
        if (uiSet.tutorialPanel != null)
            uiSet.tutorialPanel.SetActive(true);
        if (uiSet.player1ReadyImage != null)
            uiSet.player1ReadyImage.SetActive(true);
        if (uiSet.player2ReadyImage != null)
            uiSet.player2ReadyImage.SetActive(true);
    }

    void CacheCurrentReadyImages()
    {
        if (currentUISet == null) return;

        if (currentUISet.player1ReadyImage != null)
        {
            currentReadyImages[0] = currentUISet.player1ReadyImage.GetComponent<Image>();
        }
        else
        {
            currentReadyImages[0] = null;
        }

        if (currentUISet.player2ReadyImage != null)
        {
            currentReadyImages[1] = currentUISet.player2ReadyImage.GetComponent<Image>();
        }
        else
        {
            currentReadyImages[1] = null;
        }
    }

    void OnPlayerConfirm(int playerIndex)
    {
        if (allPlayersReady || playerIndex >= playerReady.Length) return;
        playerReady[playerIndex] = true;

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
            StartCoroutine(StartGameAfterDelay());
        }
    }

    void UpdateReadyUI()
    {
        if (currentUISet == null) return;

        if (currentReadyImages[0] != null)
        {
            Sprite spriteToUse = playerReady[0] ? currentUISet.player1ReadySprite : currentUISet.player1WaitingSprite;
            currentReadyImages[0].sprite = spriteToUse;
        }

        if (currentReadyImages[1] != null)
        {
            Sprite spriteToUse = playerReady[1] ? currentUISet.player2ReadySprite : currentUISet.player2WaitingSprite;
            currentReadyImages[1].sprite = spriteToUse;
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
    }

    IEnumerator FadeInFromBlack()
    {
        yield return new WaitForSeconds(0.1f);
    }

    public void OnReadyButtonPressed(int playerIndex)
    {
        OnPlayerConfirm(playerIndex);
    }
}