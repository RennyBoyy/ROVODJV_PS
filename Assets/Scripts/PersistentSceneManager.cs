using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class PersistentSceneManager : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private GameObject transitionCanvas;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scene Names")]
    [SerializeField] private string tutorialSceneName = "TutorialScene";

    private static PersistentSceneManager instance;
    private int selectedLevel = -1;
    private string targetGameScene = "";
    private bool isTransitioning = false;

    public static PersistentSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PersistentSceneManager>();
            }
            return instance;
        }
    }

    // Public property to get selected level info
    public int SelectedLevel => selectedLevel;
    public string TargetGameScene => targetGameScene;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTransitionCanvas();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeTransitionCanvas()
    {
        if (transitionCanvas == null)
        {
            // Create transition canvas if not assigned
            GameObject canvasGO = new GameObject("TransitionCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Create fade image
            GameObject imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(canvasGO.transform, false);

            fadeImage = imageGO.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;

            RectTransform rectTransform = imageGO.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            transitionCanvas = canvasGO;
            DontDestroyOnLoad(canvasGO);
        }

        // Start with transparent fade image
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    // Called from EarthMotion when player selects a level
    public void LoadLevelWithTransition(int levelNumber, string gameSceneName)
    {
        if (isTransitioning) return;

        selectedLevel = levelNumber;
        targetGameScene = gameSceneName;

        StartCoroutine(TransitionToTutorial());
    }

    private IEnumerator TransitionToTutorial()
    {
        isTransitioning = true;

        // Fade to black
        yield return StartCoroutine(FadeOut());

        // Load tutorial scene
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(tutorialSceneName);

        // Wait for scene to load
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // Tutorial scene will handle its own fade in and UI
        isTransitioning = false;
    }

    // Called from tutorial scene when players are ready
    public void LoadGameSceneWithTransition()
    {
        if (isTransitioning || string.IsNullOrEmpty(targetGameScene)) return;

        StartCoroutine(TransitionToGameScene());
    }

    private IEnumerator TransitionToGameScene()
    {
        isTransitioning = true;

        // Fade to black
        yield return StartCoroutine(FadeOut());

        // Load the actual game scene
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetGameScene);

        // Wait for scene to load
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // Fade in to reveal the game
        yield return StartCoroutine(FadeIn());

        // Game scene intro manager will take over from here
        isTransitioning = false;
    }

    // Method to fade to black and return to hub/main menu
    public void ReturnToHub(string hubSceneName = "MainMenu")
    {
        if (isTransitioning) return;

        StartCoroutine(TransitionToHub(hubSceneName));
    }

    private IEnumerator TransitionToHub(string hubSceneName)
    {
        isTransitioning = true;

        // Fade to black
        yield return StartCoroutine(FadeOut());

        // Reset level selection
        selectedLevel = -1;
        targetGameScene = "";

        // Load hub scene
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(hubSceneName);

        // Wait for scene to load
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // Fade in to reveal hub
        yield return StartCoroutine(FadeIn());

        isTransitioning = false;
    }

    private IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color startColor = fadeImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color startColor = fadeImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
    }

    // Public method for other scripts to check transition state
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
}