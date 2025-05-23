using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class EarthMotion : MonoBehaviour
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

    [Header("Continent Panels & Scenes")]
    [Tooltip("For each continent, assign the UI panel GameObject and the scene name to load on Confirm")]
    [SerializeField] private List<ContinentPanel> continentPanels;

    [Header("Input Actions (via PlayerInput)")]
    public InputActionAsset actions;

    // hooked up in Awake()
    private InputAction moveAction;
    private InputAction backAction;
    private InputAction confirmAction;

    // internals
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
    private string _activeSceneName;  // ? track which scene to load

    [System.Serializable]
    public struct ContinentPanel
    {
        public string continentName;  // must match the Transform.name of your continent
        public GameObject panel;         // the UI panel to show
        public string sceneName;      // the scene to load when Confirm is pressed
    }

    void Awake()
    {
        _cam = Camera.main.transform;

        // hide all continent panels at startup
        foreach (var cp in continentPanels)
            if (cp.panel != null)
                cp.panel.SetActive(false);

        // grab our PlayerInput component and current action map
        var pi = GetComponent<PlayerInput>();
        var map = pi.currentActionMap;

        moveAction = map.FindAction("Move", true);
        backAction = map.FindAction("Back", true);
        confirmAction = map.FindAction("Confirm", true);
    }

    void Start()
    {
        _startPos = transform.position;
        _bobPhase = 0f;
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
        // countdown for snap cooldown
        if (_snapCooldownTimer > 0f)
            _snapCooldownTimer -= Time.deltaTime;

        // if we're in the middle of snapping, just do that
        if (_isSnapping)
        {
            RunSnap();
            return;
        }

        // if a panel is open, listen only for Back or Confirm
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
                if (!string.IsNullOrEmpty(_activeSceneName))
                    SceneManager.LoadScene(_activeSceneName);
                else
                    Debug.LogWarning($"No scene assigned for continent '{_snapTargetName}'");
            }
            return;
        }

        // normal rotation input
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

        // horizontal rotation
        transform.Rotate(Vector3.up,
                         inV.x * controlRotationSpeed * Time.deltaTime,
                         Space.World);

        // vertical rotation
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

        _snapTargetName = target.name;
        _isSnapping = true;
        _snapLerp = 0f;
        _snapStartRot = transform.rotation;

        Vector3 normal = (target.position - transform.position).normalized;
        Vector3 toCam = (_cam.position - transform.position).normalized;
        Quaternion align = Quaternion.FromToRotation(normal, toCam);
        _snapEndRot = align * transform.rotation;
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

    void ShowPanelFor(string continentName)
    {
        foreach (var cp in continentPanels)
        {
            if (cp.continentName.Equals(continentName, System.StringComparison.OrdinalIgnoreCase))
            {
                cp.panel?.SetActive(true);
                _activePanel = cp.panel;
                _activeSceneName = cp.sceneName;
                return;
            }
        }
        Debug.LogWarning($"No panel assigned for continent '{continentName}'");
    }

    void ClosePanel()
    {
        if (_activePanel != null)
        {
            _activePanel.SetActive(false);
            _activePanel = null;
        }

        _idleTimer = 0f;
        _idleWeightVel = 0f;
        _idleWeight = 1f;
        _bobPhase = 0f;
        transform.position = _startPos;
    }
}
