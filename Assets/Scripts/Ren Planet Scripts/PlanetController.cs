using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    [SerializeField] private float inputLockDuration = 0.5f;

    [Header("Snap Detection")]
    [SerializeField] private float snapDetectionRadius = 2f;
    [SerializeField] private float snapAngleThreshold = 30f;

    [Header("Snap Smoothing")]
    [SerializeField] private AnimationCurve snapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float snapStrength = 1f;
    [SerializeField] private bool useSphericalSnapping = true;

    [Header("Visual Effects")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private string outlineShaderName = "Outline";
    [SerializeField] private bool useOutlineEffect = true;

    [Header("Level Configuration")]
    [SerializeField] private List<LevelData> levelData;

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

        [Header("Visual Effects")]
        public GameObject islandGameObject;

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
    private bool _inputLocked = false;
    private float _snapCooldownTimer = 0f;
    private float _inputLockTimer = 0f;
    private Quaternion _snapStartRot;
    private Quaternion _snapEndRot;
    private float _snapLerp;
    private string _snapTargetName;
    private GameObject _activePanel;
    private int _activeLevelNumber;
    private GameObject _currentOutlinedIsland;
    private Material[] _originalMaterials;

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

    private void Update()
    {
        if (_snapCooldownTimer > 0f)
            _snapCooldownTimer -= Time.deltaTime;

        if (_inputLockTimer > 0f)
            _inputLockTimer -= Time.deltaTime;
        else
            _inputLocked = false;

        if (_isSnapping)
        {
            RunSnap();
            return;
        }

        if (_isFocused)
        {
            if (!_inputLocked)
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
            }
            return;
        }

        if (currentState != PlanetState.Idle)
        {
            TryAutoSnap();
        }

        if (!_inputLocked)
        {
            Vector2 inputVector = GetInputVector();
            if (inputVector.sqrMagnitude > 0.001f)
                HandleMovement(inputVector);
            else
                ApplyIdleMotion();
        }
        else
        {
            ApplyIdleMotion();
        }
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

        currentVelocity = input * playerRotationSpeed;

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

        currentVelocity *= rotationalInertia;

        if (currentVelocity.magnitude < 0.1f)
        {
            currentVelocity = Vector2.zero;
        }

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
        currentVelocity = Vector2.zero;
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

        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (var level in levelData)
        {
            if (level.selectingSpot == null) continue;

            Vector3 toSpot = (level.selectingSpot.position - _cam.position).normalized;
            float angle = Vector3.Angle(_cam.forward, toSpot);

            if (angle <= snapAngleThreshold)
            {
                float distance = Vector3.Distance(_cam.position, level.selectingSpot.position);
                float score = angle + (distance * 0.1f);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = level.selectingSpot;
                }
            }
        }

        if (bestTarget != null)
        {
            BeginSnap(bestTarget);
        }
    }

    private void BeginSnap(Transform target)
    {
        _bobPhase = 0f;
        transform.position = _startPos;
        currentVelocity = Vector2.zero;

        LevelData? targetLevel = FindLevelDataBySelectingSpot(target);
        if (targetLevel == null)
        {
            Debug.LogWarning($"No level data found for selecting spot: {target.name}");
            return;
        }

        _snapTargetName = targetLevel.Value.levelName;
        _isSnapping = true;
        _inputLocked = true;
        _snapLerp = 0f;
        _snapStartRot = transform.rotation;

        if (useSphericalSnapping)
        {
            Vector3 targetWorldPos = target.position;
            Vector3 planetCenter = transform.position;
            Vector3 cameraPos = _cam.position;

            Vector3 toCameraFromPlanet = (cameraPos - planetCenter).normalized;
            Vector3 toTargetFromPlanet = (targetWorldPos - planetCenter).normalized;

            Quaternion targetAlignment = Quaternion.FromToRotation(toTargetFromPlanet, toCameraFromPlanet);
            _snapEndRot = targetAlignment * transform.rotation;
        }
        else
        {
            Vector3 normal = (target.position - transform.position).normalized;
            Vector3 toCam = (_cam.position - transform.position).normalized;
            Quaternion align = Quaternion.FromToRotation(normal, toCam);
            _snapEndRot = align * transform.rotation;
        }

        _snapEndRot = Quaternion.Slerp(_snapStartRot, _snapEndRot, snapStrength);

        if (useOutlineEffect && targetLevel.Value.islandGameObject != null)
        {
            ApplyOutlineToIsland(targetLevel.Value.islandGameObject);
        }

        if (PlanetGameConfigurator.Instance != null)
        {
            PlanetGameConfigurator.Instance.PlayHoverSound();
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

        float smoothT = snapCurve.Evaluate(t);

        transform.rotation = Quaternion.Slerp(_snapStartRot, _snapEndRot, smoothT);
        transform.position = _startPos;

        if (t >= 1f)
        {
            _isSnapping = false;
            _isFocused = true;
            _inputLockTimer = inputLockDuration;
            _inputLocked = true;
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

                if (levelInfo.ambientSound != null && PlanetGameConfigurator.Instance != null)
                {
                    PlanetGameConfigurator.Instance.TransitionToAmbientAudio(levelInfo.ambientSound);
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

        RemoveOutlineFromIsland();

        if (PlanetGameConfigurator.Instance != null)
        {
            PlanetGameConfigurator.Instance.TransitionBackToBackgroundMusic();
        }

        _idleTimer = 0f;
        _idleWeightVel = 0f;
        _idleWeight = 1f;
        _bobPhase = 0f;
        transform.position = _startPos;
        currentVelocity = Vector2.zero;

        currentState = PlanetState.Active;
        lastInputTime = Time.time;
    }

    private void SelectCurrentLevel()
    {
        if (PlanetGameConfigurator.Instance != null)
        {
            PlanetGameConfigurator.Instance.PlaySelectSound();
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

    private void ApplyOutlineToIsland(GameObject island)
    {
        if (island == null || outlineMaterial == null) return;

        RemoveOutlineFromIsland();

        _currentOutlinedIsland = island;

        Renderer[] renderers = island.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        _originalMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                _originalMaterials[i] = renderers[i].material;

                Material[] materials = new Material[renderers[i].materials.Length + 1];

                for (int j = 0; j < renderers[i].materials.Length; j++)
                {
                    materials[j] = renderers[i].materials[j];
                }

                materials[materials.Length - 1] = outlineMaterial;

                renderers[i].materials = materials;
            }
        }
    }

    private void RemoveOutlineFromIsland()
    {
        if (_currentOutlinedIsland == null || _originalMaterials == null) return;

        Renderer[] renderers = _currentOutlinedIsland.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length && i < _originalMaterials.Length; i++)
        {
            if (renderers[i] != null && _originalMaterials[i] != null)
            {
                renderers[i].material = _originalMaterials[i];
            }
        }

        _currentOutlinedIsland = null;
        _originalMaterials = null;
    }
}