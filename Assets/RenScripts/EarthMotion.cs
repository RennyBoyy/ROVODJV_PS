using UnityEngine;
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

    [Header("Joystick Axes & Deadzone")]
    [SerializeField] private string joystickXAxis = "JoystickX";
    [SerializeField] private string joystickYAxis = "JoystickY";
    [Range(0f, 1f)]
    [SerializeField] private float joystickDeadzone = 0.2f;

    [Header("Focus / Snap Settings")]
    [SerializeField] private float raycastDistance = 7f;
    [SerializeField] private float snapDuration = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float snapSmoothStep = 0.8f;  // 0=linear, 1=full SmoothStep
    [SerializeField] private KeyCode exitKeyKeyboard = KeyCode.B;
    [SerializeField] private KeyCode exitKeyJoystick = KeyCode.JoystickButton1;

    [Header("Snap Cooldown")]
    [SerializeField] private float snapCooldownDuration = 2f;

    [System.Serializable]
    public struct ContinentPanel
    {
        public string continentName;  // matches continent Transform.name
        public GameObject panel;        // assign UI panels here 
    }

    [Header("Continent Panels")]
    [SerializeField] private List<ContinentPanel> continentPanels;

    // Internals
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

    void Awake()
    {
        _cam = Camera.main.transform;
        // hide all panels at startup
        foreach (var cp in continentPanels)
            if (cp.panel != null)
                cp.panel.SetActive(false);
    }

    void Start()
    {
        _startPos = transform.position;
        _bobPhase = 0f;
    }

    void Update()
    {
        // cooldown tick
        if (_snapCooldownTimer > 0f)
            _snapCooldownTimer -= Time.deltaTime;

        // advancing a snap
        if (_isSnapping)
        {
            RunSnap();
            return;
        }

        // if panel is open, only watch for exit
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

        // user input vs idle
        Vector2 inV = GatherInput();
        if (inV.sqrMagnitude > 0.001f)
            HandleMovement(inV);
        else
            ApplyIdleMotion();

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
        // smoothly fade idle weight down
        _idleWeight = Mathf.SmoothDamp(_idleWeight, 0f, ref _idleWeightVel, 1f / idleFadeSpeed);

        transform.Rotate(Vector3.up,
                         inV.x * controlRotationSpeed * Time.deltaTime,
                         Space.World);

        transform.Rotate(_cam.right,
                         -inV.y * controlRotationSpeed * Time.deltaTime,
                         Space.World);
        // always stay at base position while rotating
        transform.position = _startPos;
    }

    void ApplyIdleMotion()
    {
        _idleTimer += Time.deltaTime;
        if (_idleTimer < idleDelay)
        {
            // before bobbing starts, stay at base
            transform.position = _startPos;
            return;
        }

        // smoothly fade idle weight up
        _idleWeight = Mathf.SmoothDamp(_idleWeight, 1f, ref _idleWeightVel, 1f / idleFadeSpeed);

        // custom phase in/out
        _bobPhase += Time.deltaTime * bobSpeed;

        // rotate slowly around the last input axis
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

        // bob using our phase
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
        // reset bob & pos
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
        // reset to start fresh next idle
        _idleTimer = 0f;
        _idleWeightVel = 0f;
        _idleWeight = 1f;
        _bobPhase = 0f;
        transform.position = _startPos;
    }
}
