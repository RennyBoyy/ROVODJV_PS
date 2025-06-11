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
    [SerializeField]
    private string[] gameSceneNames = {
        "",
        "First Level",
        "MiniGameFish",    };

    private static PersistentSceneManager instance;
    private int selectedLevel = -1;
    private string targetGameScene = "";
    private bool isTransitioning = false;
    private bool comingFromTutorial = false;

    public static PersistentSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PersistentSceneManager>();
            }
            return instance;
        }
    }

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

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (IsGameScene(currentScene) && comingFromTutorial)
        {
            StartCoroutine(FadeInOnGameStart());
        }
    }

    private bool IsGameScene(string sceneName)
    {
        foreach (string gameScene in gameSceneNames)
        {
            if (!string.IsNullOrEmpty(gameScene) && gameScene == sceneName)
                return true;
        }
        return false;
    }

    private IEnumerator FadeInOnGameStart()
    {
        SetBlackImmediate();

        yield return null;

        GameIntroManager introManager = FindFirstObjectByType<GameIntroManager>();
        if (introManager != null)
        {
            Debug.Log("GameIntroManager found - letting it handle the fade-in");
        }
        else
        {
            yield return StartCoroutine(FadeIn());
        }

        comingFromTutorial = false;
    }

    private void InitializeTransitionCanvas()
    {
        if (transitionCanvas == null)
        {
            GameObject canvasGO = new GameObject("TransitionCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

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

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    public void LoadLevelWithTransition(int levelNumber, string gameSceneName)
    {
        if (isTransitioning) return;

        selectedLevel = levelNumber;

        if (string.IsNullOrEmpty(gameSceneName))
        {
            if (levelNumber > 0 && levelNumber < gameSceneNames.Length)
            {
                targetGameScene = gameSceneNames[levelNumber];
            }
            else
            {
                Debug.LogError($"Invalid level number: {levelNumber}. Check gameSceneNames array.");
                return;
            }
        }
        else
        {
            targetGameScene = gameSceneName;
        }

        StartCoroutine(TransitionToTutorial());
    }

    private IEnumerator TransitionToTutorial()
    {
        isTransitioning = true;

        yield return StartCoroutine(FadeOut());

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(tutorialSceneName);

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeIn());

        isTransitioning = false;
    }

    public void LoadGameSceneWithTransition()
    {
        if (isTransitioning || string.IsNullOrEmpty(targetGameScene)) return;

        StartCoroutine(TransitionToGameScene());
    }

    private IEnumerator TransitionToGameScene()
    {
        isTransitioning = true;
        comingFromTutorial = true;

        yield return StartCoroutine(FadeOut());

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetGameScene);

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        SetBlackImmediate();

        GameIntroManager introManager = FindFirstObjectByType<GameIntroManager>();
        if (introManager != null)
        {
            Debug.Log("GameIntroManager found - waiting for it to be ready, then fading from black");
            yield return new WaitForSeconds(0.3f);

            yield return StartCoroutine(FadeIn());
        }
        else
        {
            Debug.Log("No GameIntroManager found - fading in normally");
            yield return StartCoroutine(FadeIn());
        }

        isTransitioning = false;
    }

    public void ReturnToHub(string hubSceneName = "MainMenu")
    {
        if (isTransitioning) return;

        StartCoroutine(TransitionToHub(hubSceneName));
    }

    private IEnumerator TransitionToHub(string hubSceneName)
    {
        isTransitioning = true;

        yield return StartCoroutine(FadeOut());

        selectedLevel = -1;
        targetGameScene = "";
        comingFromTutorial = false;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(hubSceneName);

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeIn());

        isTransitioning = false;
    }

    public void FadeToBlack()
    {
        if (!isTransitioning)
        {
            StartCoroutine(FadeOut());
        }
    }

    public void FadeFromBlack()
    {
        if (!isTransitioning)
        {
            StartCoroutine(FadeIn());
        }
    }

    public void SetBlackImmediate()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
            Debug.Log("Screen set to black immediately");
        }
    }

    public void SetTransparentImmediate()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            Debug.Log("Screen set to transparent immediately");
        }
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    private IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        Debug.Log("Starting fade to black");
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
        Debug.Log("Fade to black complete");
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        Debug.Log("Starting fade from black");
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
        Debug.Log("Fade from black complete");
    }
}