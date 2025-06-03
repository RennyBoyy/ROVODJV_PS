using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class PlanetController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float playerRotationSpeed = 50f;
    [SerializeField] private float idleRotationSpeed = 10f;
    [SerializeField] private Vector3 idleRotationAxis = Vector3.up;
    [SerializeField] private float rotationalInertia = 0.8f;
    [Header("Original Rotation Correction")]
    [SerializeField] private bool correctXRotation = true;
    [SerializeField] private float xCorrectionSpeed = 2f;
    private float originalXRotation;

    [Header("Bobbing Motion")]
    [SerializeField] private float bobSpeed = 1f;
    [SerializeField] private float bobAmount = 0.1f;
    private Vector3 _startPos;
    private float _bobPhase;

    [Header("Selection Settings")]
    [SerializeField] private float raycastDistance = 7f;
    [SerializeField] private float snapDuration = 0.5f;
    [SerializeField] private float snapCooldownDuration = 2f;
    [SerializeField] private float inactivityTimeout = 3f;

    [Header("Level Configuration")]
    [SerializeField] private List<LevelData> levelData;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private float musicFadeInDuration = 2f;

    [Header("Audio Volume Controls")]
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float ambientVolume = 0.5f;
    [SerializeField] private float audioTransitionDuration = 1f;

    [Header("Input Actions (via PlayerInput) - Optional")]
    public InputActionAsset actions;

    private InputAction moveAction;
    private InputAction confirmAction;
    private bool useNewInputSystem = false;

    [System.Serializable]
    public struct LevelData
    {
        [Header("Identification")]
        public string levelName;

        [Header("Detection")]
        public Transform selectingSpot;

        [Header("Visual Effects (Future)")]
        public Transform islandPrefab;

        [Header("UI & Scene")]
        public GameObject panel;
        public int levelNumber;

        [Header("Audio")]
        public AudioClip ambientSound;
    }

    private enum PlanetState { Idle, Active, Focused }
    private PlanetState currentState = PlanetState.Idle;

    private Transform currentlyFocusedLevel;
    private float lastInputTime;
    private Vector3 screenCenter;
    private bool _isSnapping = false;
    private bool _isFocused = false;
    private float _snapCooldownTimer = 0f;
    private Quaternion _snapStartRot;
    private Quaternion _snapEndRot;
    private float _snapLerp;
    private string _snapTargetName;
    private GameObject _activePanel;
    private int _activeLevelNumber;
    private AudioSource _currentAmbientSource;
    private bool _isTransitioningAudio = false;

    // Smooth rotation variables from old system
    private Vector2 currentVelocity = Vector2.zero;
    private bool wasReceivingInput = false;
    private Transform _cam;
    private float _idleTimer;
    private float _idleWeight = 1f;
    private float _idleWeightVel = 0f;
    private Vector2 _lastInput = Vector2.up;

    void Awake()
    {
        _cam = Camera.main.transform;

        foreach (var levelInfo in levelData)
            if (levelInfo.panel != null)
                levelInfo.panel.SetActive(false);

        SetupNewInputSystem();
    }

    private void SetupNewInputSystem()
    {
        try
        {
            var pi = GetComponent<PlayerInput>();
            if (pi != null)
            {
                var map = pi.currentActionMap;
                if (map != null)
                {
                    moveAction = map.FindAction("Move", true);
                    confirmAction = map.FindAction("Confirm", true);

                    if (moveAction != null && confirmAction != null)
                    {
                        useNewInputSystem = true;
                        Debug.Log("PlanetController: Using New Input System");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PlanetController: PlayerInput setup failed, using traditional input. Error: {e.Message}");
        }

        if (!useNewInputSystem)
        {
            Debug.Log("PlanetController: Using Traditional Input System");
        }
    }

    private void Start()
    {
        screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f);
        lastInputTime = Time.time;
        _startPos = transform.position;
        _bobPhase = 0f;

        originalXRotation = transform.eulerAngles.x;

        if (musicSource != null) musicSource.volume = 0f;
        if (sfxSource != null) sfxSource.volume = sfxVolume;

        StartCoroutine(StartBackgroundMusic());

        ValidateLevelData();
    }

    private void ValidateLevelData()
    {
        for (int i = 0; i < levelData.Count; i++)
        {
            var level = levelData[i];
            if (level.selectingSpot == null)
            {
                Debug.LogWarning($"Level '{level.levelName}' has no selecting spot assigned!");
                continue;
            }

            if (!level.selectingSpot.CompareTag("Continent"))
            {
                level.selectingSpot.tag = "Continent";
                Debug.Log($"Auto-assigned 'Continent' tag to {level.levelName} selecting spot");
            }

            if (!level.selectingSpot.IsChildOf(transform))
            {
                Debug.LogWarning($"Level '{level.levelName}' selecting spot is not a child of the planet!");
            }
        }
    }

    private void OnEnable()
    {
        if (useNewInputSystem)
        {
            moveAction?.Enable();
            confirmAction?.Enable();
        }
    }

    private void OnDisable()
    {
        if (useNewInputSystem)
        {
            moveAction?.Disable();
            confirmAction?.Disable();
        }
    }

    private IEnumerator StartBackgroundMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.Play();

            float elapsed = 0f;
            while (elapsed < musicFadeInDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / musicFadeInDuration);
                yield return null;
            }
            musicSource.volume = musicVolume;
        }
    }

    private void Update()
    {
        UpdateAudioVolumes();

        if (_snapCooldownTimer > 0f)
            _snapCooldownTimer -= Time.deltaTime;

        if (_isSnapping)
        {
            RunSnap();
            return;
        }

        if (_isFocused)
        {
            Vector2 input = GetInputVector();
            if (input.sqrMagnitude > 0.1f)
            {
                ClosePanel();
                _isFocused = false;
                _snapCooldownTimer = snapCooldownDuration;
                HandleMovement(input);
            }
            else if (GetConfirmInput())
            {
                SelectCurrentLevel();
            }
            return;
        }

        Vector2 inputVector = GetInputVector();
        if (inputVector.sqrMagnitude > 0.001f)
            HandleMovement(inputVector);
        else
            ApplyIdleMotion();

        TryAutoSnap();
    }

    private Vector2 GetInputVector()
    {
        if (useNewInputSystem && moveAction != null)
        {
            return moveAction.ReadValue<Vector2>();
        }
        else
        {
            return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }
    }

    private bool GetConfirmInput()
    {
        if (useNewInputSystem && confirmAction != null)
        {
            return confirmAction.triggered;
        }
        else
        {
            return Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Space);
        }
    }

    private void HandleMovement(Vector2 input)
    {
        _lastInput = input.normalized;
        _idleTimer = 0f;
        _idleWeight = Mathf.SmoothDamp(_idleWeight, 0f, ref _idleWeightVel, 1f / 3f);
        lastInputTime = Time.time;
        wasReceivingInput = true;

        if (currentState == PlanetState.Idle)
        {
            EnterActiveState();
        }

        // Use smooth velocity system from old version
        currentVelocity = input * playerRotationSpeed;

        // Apply rotation with velocity
        float rotationX = currentVelocity.x * Time.deltaTime;
        float rotationY = -currentVelocity.y * Time.deltaTime;

        transform.Rotate(Vector3.up, rotationX, Space.World);
        transform.Rotate(_cam.right, rotationY, Space.World);

        transform.position = _startPos;
    }

    private void ApplyIdleMotion()
    {
        if (wasReceivingInput)
        {
            wasReceivingInput = false;
        }

        // Apply inertia when no input
        currentVelocity *= rotationalInertia;

        // If velocity is very small, stop completely
        if (currentVelocity.magnitude < 0.1f)
        {
            currentVelocity = Vector2.zero;
        }

        // Apply remaining velocity from inertia
        if (currentVelocity.magnitude > 0)
        {
            float rotationX = currentVelocity.x * Time.deltaTime;
            float rotationY = -currentVelocity.y * Time.deltaTime;

            transform.Rotate(Vector3.up, rotationX, Space.World);
            transform.Rotate(_cam.right, rotationY, Space.World);
        }

        _idleTimer += Time.deltaTime;
        if (_idleTimer < 0.5f)
        {
            transform.position = _startPos;
            return;
        }

        if (currentState == PlanetState.Active && Time.time - lastInputTime > inactivityTimeout)
        {
            EnterIdleState();
        }

        if (currentState == PlanetState.Idle)
        {
            _idleWeight = Mathf.SmoothDamp(_idleWeight, 1f, ref _idleWeightVel, 1f / 3f);
            _bobPhase += Time.deltaTime * bobSpeed;

            Vector3 axis;
            float sign;
            if (Mathf.Abs(_lastInput.x) >= Mathf.Abs(_lastInput.y))
            {
                axis = Vector3.up;
                sign = Mathf.Sign(_lastInput.x);
            }
            else
            {
                axis = _cam.right;
                sign = -Mathf.Sign(_lastInput.y);
            }

            transform.Rotate(axis, sign * idleRotationSpeed * _idleWeight * Time.deltaTime, Space.World);

            float offsetY = Mathf.Sin(_bobPhase) * bobAmount * _idleWeight;
            transform.position = _startPos + Vector3.up * offsetY;

            if (correctXRotation)
            {
                CorrectXRotation();
            }
        }
    }

    private void EnterActiveState()
    {
        currentState = PlanetState.Active;
    }

    private void EnterIdleState()
    {
        currentState = PlanetState.Idle;
        currentlyFocusedLevel = null;
        currentVelocity = Vector2.zero; // Reset velocity when entering idle
    }

    private void CorrectXRotation()
    {
        float currentXRotation = transform.eulerAngles.x;
        if (currentXRotation > 180f)
        {
            currentXRotation -= 360f;
        }

        float targetXRotation = originalXRotation;
        if (targetXRotation > 180f)
        {
            targetXRotation -= 360f;
        }

        float xDifference = Mathf.DeltaAngle(currentXRotation, targetXRotation);

        if (Mathf.Abs(xDifference) > 0.1f)
        {
            float correctionAmount = Mathf.Sign(xDifference) * xCorrectionSpeed * Time.deltaTime;

            if (Mathf.Abs(correctionAmount) > Mathf.Abs(xDifference))
            {
                correctionAmount = xDifference;
            }

            transform.Rotate(Vector3.right, correctionAmount, Space.World);
        }
    }

    private void TryAutoSnap()
    {
        if (_snapCooldownTimer > 0f || _isSnapping || _isFocused)
            return;

        Ray ray = new Ray(_cam.position, _cam.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance) &&
            hit.transform.CompareTag("Continent") &&
            hit.transform.IsChildOf(transform))
        {
            BeginSnap(hit.transform);
        }
    }

    private void BeginSnap(Transform target)
    {
        _bobPhase = 0f;
        transform.position = _startPos;
        currentVelocity = Vector2.zero; // Stop all velocity when snapping

        LevelData? targetLevel = FindLevelDataBySelectingSpot(target);
        if (targetLevel == null)
        {
            Debug.LogWarning($"No level data found for selecting spot: {target.name}");
            return;
        }

        _snapTargetName = targetLevel.Value.levelName;
        _isSnapping = true;
        _snapLerp = 0f;
        _snapStartRot = transform.rotation;

        Vector3 normal = (target.position - transform.position).normalized;
        Vector3 toCam = (_cam.position - transform.position).normalized;
        Quaternion align = Quaternion.FromToRotation(normal, toCam);
        _snapEndRot = align * transform.rotation;

        if (sfxSource != null && hoverSound != null)
        {
            sfxSource.PlayOneShot(hoverSound);
        }
    }

    private LevelData? FindLevelDataBySelectingSpot(Transform selectingSpot)
    {
        foreach (var level in levelData)
        {
            if (level.selectingSpot == selectingSpot)
            {
                return level;
            }
        }
        return null;
    }

    private void RunSnap()
    {
        _snapLerp += Time.deltaTime / snapDuration;
        float t = Mathf.Clamp01(_snapLerp);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        transform.rotation = Quaternion.Slerp(_snapStartRot, _snapEndRot, smoothT);
        transform.position = _startPos;

        if (t >= 1f)
        {
            _isSnapping = false;
            _isFocused = true;
            currentState = PlanetState.Focused;
            ShowPanelFor(_snapTargetName);
        }
    }

    private void ShowPanelFor(string levelName)
    {
        foreach (var levelInfo in levelData)
        {
            if (levelInfo.levelName.Equals(levelName, System.StringComparison.OrdinalIgnoreCase))
            {
                levelInfo.panel?.SetActive(true);
                _activePanel = levelInfo.panel;
                _activeLevelNumber = levelInfo.levelNumber;

                if (levelInfo.ambientSound != null)
                {
                    StartCoroutine(TransitionToAmbientAudio(levelInfo.ambientSound));
                }
                return;
            }
        }
        Debug.LogWarning($"No panel assigned for level '{levelName}'");
    }

    private void ClosePanel()
    {
        if (_activePanel != null)
        {
            _activePanel.SetActive(false);
            _activePanel = null;
        }

        StartCoroutine(TransitionBackToBackgroundMusic());

        _idleTimer = 0f;
        _idleWeightVel = 0f;
        _idleWeight = 1f;
        _bobPhase = 0f;
        transform.position = _startPos;
        currentVelocity = Vector2.zero; // Reset velocity when closing panel

        currentState = PlanetState.Active;
        lastInputTime = Time.time;
    }

    private void SelectCurrentLevel()
    {
        if (sfxSource != null && selectSound != null)
        {
            sfxSource.PlayOneShot(selectSound);
        }

        PersistentSceneManager sceneManager = PersistentSceneManager.Instance;
        if (sceneManager != null)
        {
            sceneManager.LoadLevelWithTransition(_activeLevelNumber, "");
        }
        else
        {
            Debug.LogError("PersistentSceneManager not found! Cannot load level.");
        }
    }

    private void UpdateAudioVolumes()
    {
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

        if (musicSource != null && !_isTransitioningAudio && musicSource.isPlaying)
        {
            musicSource.volume = musicVolume;
        }

        if (_currentAmbientSource != null)
        {
            _currentAmbientSource.volume = ambientVolume;
        }
    }

    private IEnumerator TransitionToAmbientAudio(AudioClip ambientClip)
    {
        _isTransitioningAudio = true;

        if (musicSource != null && musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < audioTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / audioTransitionDuration;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            musicSource.volume = 0f;
            musicSource.Pause();
        }

        if (_currentAmbientSource != null)
        {
            _currentAmbientSource.Stop();
            Destroy(_currentAmbientSource.gameObject);
        }

        GameObject ambientGO = new GameObject("LevelAmbient");
        _currentAmbientSource = ambientGO.AddComponent<AudioSource>();
        _currentAmbientSource.clip = ambientClip;
        _currentAmbientSource.loop = true;
        _currentAmbientSource.volume = 0f;
        _currentAmbientSource.Play();

        float ambientElapsed = 0f;
        while (ambientElapsed < audioTransitionDuration)
        {
            ambientElapsed += Time.deltaTime;
            float t = ambientElapsed / audioTransitionDuration;
            _currentAmbientSource.volume = Mathf.Lerp(0f, ambientVolume, t);
            yield return null;
        }

        _currentAmbientSource.volume = ambientVolume;
        _isTransitioningAudio = false;
    }

    private IEnumerator TransitionBackToBackgroundMusic()
    {
        _isTransitioningAudio = true;

        if (_currentAmbientSource != null)
        {
            float startVolume = _currentAmbientSource.volume;
            float elapsed = 0f;

            while (elapsed < audioTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / audioTransitionDuration;
                _currentAmbientSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            _currentAmbientSource.Stop();
            Destroy(_currentAmbientSource.gameObject);
            _currentAmbientSource = null;
        }

        if (musicSource != null)
        {
            if (!musicSource.isPlaying)
            {
                musicSource.UnPause();
            }

            float elapsed = 0f;
            while (elapsed < audioTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / audioTransitionDuration;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, t);
                yield return null;
            }

            musicSource.volume = musicVolume;
        }

        _isTransitioningAudio = false;
    }
}