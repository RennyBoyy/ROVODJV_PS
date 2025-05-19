using UnityEngine;
using System.Collections.Generic;

public class EarthMotion : MonoBehaviour
{
    [Header("Speeds")]
    [SerializeField] private float controlRotationSpeed = 30f;   // user drag
    [SerializeField] private float idleSpinSpeed = 10f;   // auto-spin when idle

    [Header("Bobbing")]
    [SerializeField] private float bobSpeed = 1f;
    [SerializeField] private float bobAmount = 0.1f;

    [Header("Idle Resume")]
    [SerializeField] private float idleDelay = 0.5f;
    [SerializeField] private float idleFadeSpeed = 3f;

    [Header("Joystick Axes & Deadzone")]
    [SerializeField] private string joystickXAxis = "JoystickX";
    [SerializeField] private string joystickYAxis = "JoystickY";
    [Range(0f, 1f)]
    [SerializeField] private float joystickDeadzone = 0.2f;

    [Header("Focus / Snap Settings")]
    [SerializeField] private float raycastDistance = 7f;
    [SerializeField] private float snapDuration = 0.5f;
    [SerializeField] private KeyCode exitKeyKeyboard = KeyCode.B;
    [SerializeField] private KeyCode exitKeyJoystick = KeyCode.JoystickButton1;

    [Header("Snap Cooldown")]
    [SerializeField] private float snapCooldownDuration = 2f;

    // Make this struct serializable so it shows up in the Inspector
    [System.Serializable]
    public struct ContinentPanel
    {
        public string continentName;  // e.g. "Africa"
        public GameObject panel;        // assign the matching UI panel here
    }

    [Header("Continent Panels")]
    [SerializeField] private List<ContinentPanel> continentPanels;

    // Internals
    private Transform _cam;
    private Vector3 _startPos;
    private float _idleTimer;
    private float _idleWeight = 1f;
    private Vector2 _lastInput = Vector2.up;

    private bool _isSnapping;
    private bool _isFocused;
    private Quaternion _snapStartRot;
    private Quaternion _snapEndRot;
    private float _snapLerp;

    private float _snapCooldownTimer = 0f;
    private string _snapTargetName;
    private GameObject _activePanel;

    void Awake()
    {
        _cam = Camera.main.transform;

        // Hide all continent panels at start
        foreach (var cp in continentPanels)
            if (cp.panel != null)
                cp.panel.SetActive(false);
    }

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        // Cooldown tick
        if (_snapCooldownTimer > 0f)
            _snapCooldownTimer -= Time.deltaTime;

        // If mid?snap, advance it
        if (_isSnapping)
        {
            RunSnap();
            return;
        }

        // If a panel is up, only watch for the exit key
        if (_isFocused)
        {
            if (Input.GetKeyDown(exitKeyKeyboard) || Input.GetKeyDown(exitKeyJoystick))
            {
                ClosePanel();
                _isFocused = false;
                _snapCooldownTimer = snapCooldownDuration;
            }
            return;
        }

        // Normal rotate vs. idle
        Vector2 inV = GatherInput();
        if (inV.sqrMagnitude > 0.001f)
            HandleMovement(inV);
        else
            ApplyIdleMotion();

        // Try auto?snap
        TryAutoSnap();
    }

    Vector2 GatherInput()
    {
        Vector2 v = Vector2.zero;
        if (Input.GetKey(KeyCode.A)) v.x = -1f;
        if (Input.GetKey(KeyCode.D)) v.x = 1f;
        if (Input.GetKey(KeyCode.W)) v.y = 1f;
        if (Input.GetKey(KeyCode.S)) v.y = -1f;

        float jx = Input.GetAxis(joystickXAxis);
        float jy = -Input.GetAxis(joystickYAxis);
        if (Mathf.Abs(jx) > joystickDeadzone) v.x += jx;
        if (Mathf.Abs(jy) > joystickDeadzone) v.y += jy;

        return Vector2.ClampMagnitude(v, 1f);
    }

    void HandleMovement(Vector2 inV)
    {
        _lastInput = inV.normalized;
        _idleTimer = 0f;
        _idleWeight = Mathf.Lerp(_idleWeight, 0f, Time.deltaTime * idleFadeSpeed);

        transform.Rotate(Vector3.up,
                         inV.x * controlRotationSpeed * Time.deltaTime,
                         Space.World);

        transform.Rotate(_cam.right,
                         -inV.y * controlRotationSpeed * Time.deltaTime,
                         Space.World);
    }

    void ApplyIdleMotion()
    {
        _idleTimer += Time.deltaTime;
        if (_idleTimer < idleDelay) return;

        _idleWeight = Mathf.Lerp(_idleWeight, 1f, Time.deltaTime * idleFadeSpeed);
        if (_idleWeight <= 0.001f) return;

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

        float newY = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount * _idleWeight;
        transform.position = new Vector3(_startPos.x, newY, _startPos.z);
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
        _snapTargetName = target.name;
        _isSnapping = true;
        _snapLerp = 0f;
        _snapStartRot = transform.rotation;

        Vector3 normal = (target.position - transform.position).normalized;
        Vector3 toCamera = (_cam.position - transform.position).normalized;
        Quaternion align = Quaternion.FromToRotation(normal, toCamera);
        _snapEndRot = align * transform.rotation;
    }

    void RunSnap()
    {
        _snapLerp += Time.deltaTime / snapDuration;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_snapLerp));
        transform.rotation = Quaternion.Slerp(_snapStartRot, _snapEndRot, t);

        if (_snapLerp >= 1f)
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
                return;
            }
        }
        Debug.LogWarning($"No UI panel assigned for continent '{continentName}'");
    }

    void ClosePanel()
    {
        if (_activePanel != null)
        {
            _activePanel.SetActive(false);
            _activePanel = null;
        }
        _idleTimer = 0f;
        _idleWeight = 1f;
    }
}
