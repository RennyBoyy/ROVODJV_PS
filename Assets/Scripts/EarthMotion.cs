using UnityEngine;

public class EarthMotion : MonoBehaviour
{
    [Header("Speeds")]
    [Tooltip("Degrees/sec when using keyboard or joystick")]
    public float controlRotationSpeed = 30f;
    [Tooltip("Degrees/sec when idle")]
    public float idleSpinSpeed = 10f;

    [Header("Bobbing")]
    [Tooltip("Cycles per second")]
    public float bobSpeed = 1f;
    [Tooltip("Vertical bob amplitude")]
    public float bobAmount = 0.1f;

    [Header("Idle Resume")]
    [Tooltip("Secs after last input before idle returns")]
    public float idleDelay = 0.5f;
    [Tooltip("How fast idle fades back in")]
    public float idleFadeSpeed = 3f;

    [Header("Joystick")]
    [Tooltip("Name of the horizontal stick axis (set up in Input Manager)")]
    public string joystickXAxis = "JoystickX";
    [Tooltip("Name of the vertical stick axis")]
    public string joystickYAxis = "JoystickY";
    [Tooltip("Ignore stick values smaller than this")]
    [Range(0f, 1f)]
    public float joystickDeadzone = 0.2f;

    // Internals
    private Vector3 startPos;
    private float idleTimer = 0f, idleWeight = 1f;
    private Vector2 lastInput = Vector2.up; // default spin up around Y

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        Vector2 input = GatherInput();
        HandleMovement(input);
        ApplyIdleMotion();
    }

    private Vector2 GatherInput()
    {
        Vector2 input = Vector2.zero;

        // Keyboard A/D
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x = 1f;
        // Keyboard W/S
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y = 1f; // tilt forward
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y = -1f; // tilt back

        // Joystick stick
        float jx = Input.GetAxis(joystickXAxis);
        float jyRaw = Input.GetAxis(joystickYAxis);
        float jy = -jyRaw; // **invert** vertical

        // Deadzone
        if (Mathf.Abs(jx) > joystickDeadzone) input.x += jx;
        if (Mathf.Abs(jy) > joystickDeadzone) input.y += jy;

        return Vector2.ClampMagnitude(input, 1f);
    }

    private void HandleMovement(Vector2 input)
    {
        bool hasInput = input.sqrMagnitude > 0.001f;
        if (hasInput)
        {
            // Remember for idle direction
            lastInput = input.normalized;

            // Suppress idle
            idleTimer = 0f;
            idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * idleFadeSpeed);

            // Horizontal spin (world Y)
            transform.Rotate(Vector3.up, input.x * controlRotationSpeed * Time.deltaTime, Space.World);

            // Vertical tilt (camera right axis)
            Vector3 camRight = Camera.main.transform.right;
            transform.Rotate(camRight, -input.y * controlRotationSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            // Fade back to idle
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleDelay)
                idleWeight = Mathf.Lerp(idleWeight, 1f, Time.deltaTime * idleFadeSpeed);
        }
    }

    private void ApplyIdleMotion()
    {
        if (idleWeight <= 0.001f) return;

        // Determine spin axis & sign from lastInput
        Vector3 axis;
        float sign;
        if (Mathf.Abs(lastInput.x) >= Mathf.Abs(lastInput.y))
        {
            axis = Vector3.up;
            sign = Mathf.Sign(lastInput.x);
        }
        else
        {
            axis = Camera.main.transform.right;
            sign = -Mathf.Sign(lastInput.y);
        }

        // Idle spin
        transform.Rotate(axis, sign * idleSpinSpeed * idleWeight * Time.deltaTime, Space.World);

        // Idle bob
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount * idleWeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
