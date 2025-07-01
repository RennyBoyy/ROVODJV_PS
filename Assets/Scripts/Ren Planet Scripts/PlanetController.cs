using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlanetController : MonoBehaviour
{
    [Header("Speeds")]
    [SerializeField] private float controlRotationSpeed = 30f;
    [SerializeField] private float idleSpinSpeed = 10f;

    [Header("Bobbing")]
    [SerializeField] private float bobSpeed = 1f;
    [SerializeField] private float bobAmount = 0.1f;

    [Header("Idle Resume")]
    [SerializeField] private float idleDelay = 0.5f;
    [SerializeField] private float idleFadeSpeed = 3f;

    [Header("Focus / Snap Settings")]
    [SerializeField] private float raycastDistance = 7f;
    [SerializeField] private float snapDuration = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float snapSmoothStep = 0.8f;

    [Header("Snap Cooldown")]
    [SerializeField] private float snapCooldownDuration = 2f;

    [Header("Visual Effects")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private bool useOutlineEffect = true;

    [Header("Level Configuration")]
    [SerializeField] private List<LevelData> levelData;

    [Header("Input Actions (via PlayerInput)")]
    public InputActionAsset actions;

    private InputAction moveAction;
    private InputAction backAction;
    private InputAction confirmAction;

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

    private Transform _cam;
    private Vector3 _startPos;
    private float _bobPhase;
    private float _idleTimer;
    private float _idleWeight = 1f;
    private float _idleWeightVel = 0f;
    private Vector2 _lastInput = Vector2.up;

    private bool _isSnapping;
    private bool _isFocused;
    private Quaternion _snapStartRot;
    private Quaternion _snapEndRot;
    private float _snapLerp;

    private float _snapCooldownTimer = 0f;
    private string _snapTargetName;
    private GameObject _activePanel;
    private int _activeLevelNumber;
    private GameObject _currentOutlinedIsland;
    private Material[] _originalMaterials;

    void Awake()
    {
        _cam = Camera.main.transform;

        foreach (var levelInfo in levelData)
            if (levelInfo.panel != null)
                levelInfo.panel.SetActive(false);

        var pi = GetComponent<PlayerInput>();
        var map = pi.currentActionMap;

        moveAction = map.FindAction("Move", true);
        backAction = map.FindAction("Back", true);
        confirmAction = map.FindAction("Confirm", true);

        ValidateLevelData();
    }

    void Start()
    {
        _startPos = transform.position;
        _bobPhase = 0f;
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

    void OnEnable()
    {
        moveAction?.Enable();
        backAction?.Enable();
        confirmAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        backAction?.Disable();
        confirmAction?.Disable();
    }

    void Update()
    {
        if (_snapCooldownTimer > 0f)
            _snapCooldownTimer -= Time.deltaTime;

        if (_isSnapping)
        {
            RunSnap();
            return;
        }

        if (_isFocused)
        {
            if (backAction.triggered)
            {
                ClosePanel();
                _isFocused = false;
                _snapCooldownTimer = snapCooldownDuration;
            }
            else if (confirmAction.triggered)
            {
                SelectCurrentLevel();
            }
            return;
        }

        Vector2 inV = moveAction.ReadValue<Vector2>();
        if (inV.sqrMagnitude > 0.001f)
            HandleMovement(inV);
        else
            ApplyIdleMotion();

        TryAutoSnap();
    }

    void HandleMovement(Vector2 inV)
    {
        _lastInput = inV.normalized;
        _idleTimer = 0f;
        _idleWeight = Mathf.SmoothDamp(_idleWeight, 0f, ref _idleWeightVel, 1f / idleFadeSpeed);

        transform.Rotate(Vector3.up,
                         inV.x * controlRotationSpeed * Time.deltaTime,
                         Space.World);

        transform.Rotate(_cam.right,
                         -inV.y * controlRotationSpeed * Time.deltaTime,
                         Space.World);

        transform.position = _startPos;
    }

    void ApplyIdleMotion()
    {
        _idleTimer += Time.deltaTime;
        if (_idleTimer < idleDelay)
        {
            transform.position = _startPos;
            return;
        }

        _idleWeight = Mathf.SmoothDamp(_idleWeight, 1f, ref _idleWeightVel, 1f / idleFadeSpeed);
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

        transform.Rotate(axis,
                         sign * idleSpinSpeed * _idleWeight * Time.deltaTime,
                         Space.World);

        float offsetY = Mathf.Sin(_bobPhase) * bobAmount * _idleWeight;
        transform.position = _startPos + Vector3.up * offsetY;
    }

    void TryAutoSnap()
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

    void BeginSnap(Transform target)
    {
        _bobPhase = 0f;
        transform.position = _startPos;

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

    void RunSnap()
    {
        _snapLerp += Time.deltaTime / snapDuration;
        float rawT = Mathf.Clamp01(_snapLerp);
        float smoothT = Mathf.SmoothStep(0f, 1f, rawT);
        float t = Mathf.Lerp(rawT, smoothT, snapSmoothStep);

        transform.rotation = Quaternion.Slerp(_snapStartRot, _snapEndRot, t);
        transform.position = _startPos;

        if (rawT >= 1f)
        {
            _isSnapping = false;
            _isFocused = true;
            ShowPanelFor(_snapTargetName);
        }
    }

    void ShowPanelFor(string levelName)
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

    void ClosePanel()
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

    private void OnDrawGizmosSelected()
    {
        if (_cam == null) return;

        Gizmos.color = Color.cyan;
        Vector3 rayStart = _cam.position;
        Vector3 rayDirection = _cam.forward;
        Vector3 rayEnd = rayStart + rayDirection * raycastDistance;

        Gizmos.DrawLine(rayStart, rayEnd);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rayEnd, 0.5f);

        Ray ray = new Ray(rayStart, rayDirection);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hit.point, 0.3f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(rayStart, hit.point);

            if (hit.transform.CompareTag("Continent") && hit.transform.IsChildOf(transform))
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(hit.transform.position, Vector3.one * 2f);
            }
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(_cam.position, _cam.forward * 3f);
    }
}