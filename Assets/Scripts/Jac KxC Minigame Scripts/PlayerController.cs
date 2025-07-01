using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Physics Controllers")]
    [SerializeField] private Rigidbody m_Rigidbody;
    [SerializeField] private Animator kittyAnimator;
    [SerializeField] private GameManager_Slope gameManagerSlope;

    [Header("Gravity Scale")]
    [Range(0.5f, 10f)]
    [Tooltip("Multiplier on Unity’s default 9.81 m/s² gravity")]
    [SerializeField] private float gravityScale = 2f;

    private float maxLeanAngle = 20f;
    private float leanSpeed = 5f;

    [Header("Obstacle Slam")]
    [SerializeField] private float obstacleSlamForce = 20f;

    [Header("Movement Settings")]
    [Range(0f, 10f)]
    [Tooltip("How hard the player is pushed left/right")]
    [SerializeField] private float lateralThrust = 1f;
    [Range(0f, 200f)]
    [Tooltip("Max horizontal speed")]
    [SerializeField] private float maxSpeed = 10f;

    [Header("Slide Settings")]
    [Range(0f, 10f)]
    [Tooltip("g·sinθ multiplier for downhill slide")]
    [SerializeField] private float slopeAcceleration = 6f;

    [Header("Jump Settings")]
    [Range(0f, 20f)]
    [Tooltip("Peak jump height in meters")]
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Balloon Reference")]
    [SerializeField] private BalloonPhysics balloonController;

    [Header("Camera Shake Settings")]
    [SerializeField] private CinemachineBasicMultiChannelPerlin cameraShake;
    [SerializeField] private float cameraShakeAmplitude;

    [Header("Player Identity")]
    [SerializeField] private PlayerIdentity playerIdentity;
    public PlayerIdentity PlayerType => playerIdentity;

    public PlayerInput player1Input;
    public PlayerInput player2Input;

    // runtime state
    private bool isGrounded;
    public bool moving;
    private float moveInput;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool didplayer1win;
    public int playerID;

    void Start()
    {
        
            m_Rigidbody = GetComponent<Rigidbody>();
            gameManagerSlope = FindFirstObjectByType<GameManager_Slope>();

            var data = PlayerManager.Instance.GetPlayer(playerID);
            playerIdentity = data.identity;
            Gamepad pad = data.gamepad;

            if (playerID == 0 && pad != null && player1Input != null)
            {
                player1Input.SwitchCurrentActionMap("Player");
                player1Input.ActivateInput();
            }
            else if (playerID == 1 && pad != null && player2Input != null)
            {
                player2Input.SwitchCurrentActionMap("Player2");
                player2Input.ActivateInput();
            }

            // (If you want to use the Gamepad for custom input, you can use 'pad' here)

            // strengthen Unity gravity uniformly:
            Physics.gravity = new Vector3(0f, Physics.gravity.y * gravityScale, 0f);

            // lock all physics-driven rotation
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            cameraShake.AmplitudeGain = 0f;
            isGrounded = true;
        
    }

    void Update()
    {
        // coyote & jump-buffer timers
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;
        jumpBufferCounter -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        HandleJump();
        HandleMovement();
        ApplySlopeSlideAndLean();
        ClampHorizontalSpeed();
        /*if (moving)
        {   
            Vector3 customGravity = gravityScale * Physics.gravity;
            m_Rigidbody.AddForce(customGravity, ForceMode.Acceleration);
        }*/
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            // consume the jump
            kittyAnimator?.SetTrigger("Jump");
            isGrounded = false;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;

            // true Newtonian jump: v0 = √(2 g_scaled h)
            float g = Mathf.Abs(Physics.gravity.y);
            float vY = Mathf.Sqrt(2f * g * jumpHeight);
            Vector3 v = m_Rigidbody.linearVelocity;
            m_Rigidbody.linearVelocity = new Vector3(v.x, vY, v.z);

            SkiGameConfigurator.Instance?.PlayJumpSound(playerID == 1);
        }
    }

    private void HandleMovement()
    {
        int dir = 0;
        if (moveInput > 0.1f)
        {
            m_Rigidbody.AddForce(Vector3.right * lateralThrust, ForceMode.Impulse);
            dir = 1;
        }
        else if (moveInput < -0.1f)
        {
            m_Rigidbody.AddForce(Vector3.left * lateralThrust, ForceMode.Impulse);
            dir = -1;
        }
        kittyAnimator?.SetInteger("MoveDirection", dir);

    }

    private void ApplySlopeSlideAndLean()
    {
        if (isGrounded && Physics.Raycast(transform.position, Vector3.down, out var hit, 1.2f))
        {
            // slide: project (scaled) gravity onto the slope
            Vector3 downSlope = Vector3.ProjectOnPlane(Physics.gravity, hit.normal)
                                * slopeAcceleration;
            m_Rigidbody.AddForce(downSlope, ForceMode.Acceleration);

            // optional lean around X-axis
            float signedAngle = Vector3.SignedAngle(
                Vector3.up, hit.normal, Vector3.right
            );
            float pitch = Mathf.Clamp(signedAngle, -maxLeanAngle, maxLeanAngle);
            Quaternion target = Quaternion.Euler(pitch, m_Rigidbody.rotation.eulerAngles.y, 0f);
            m_Rigidbody.MoveRotation(
                Quaternion.Slerp(m_Rigidbody.rotation, target, Time.fixedDeltaTime * leanSpeed)
            );
        }
    }

    private void ClampHorizontalSpeed()
    {
        var v = m_Rigidbody.linearVelocity;
        var xz = new Vector2(v.x, v.z);
        if (xz.magnitude > maxSpeed)
        {
            xz = xz.normalized * maxSpeed;
            m_Rigidbody.linearVelocity = new Vector3(xz.x, v.y, xz.y);
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!moving) return;
        moveInput = ctx.ReadValue<Vector2>().x;
        Debug.Log($"Player {playerID} move input: {moveInput}");
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
            if (ctx.performed && isGrounded && moving)
                jumpBufferCounter = jumpBufferTime;
    }

    public void startMoving()
    {

        WaitForSeconds wait = new WaitForSeconds(5.5f);
        m_Rigidbody.useGravity = true;
        moving = true;

        // Compute the exact vertical velocity to reach jumpHeight:
        float g = Mathf.Abs(Physics.gravity.y);
        float vY = Mathf.Sqrt(2f * g * jumpHeight);

        // Build a flat forward direction (ignore any residual Y)
        Vector3 forwardDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        m_Rigidbody.linearVelocity = forwardDir * 10f
                             + Vector3.up * vY;

    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.contacts[0].normal.y > 0.5f)
            isGrounded = true;

        if (col.gameObject.CompareTag("Obstacle"))
        {
            m_Rigidbody.angularVelocity = Vector3.zero;
            kittyAnimator?.SetTrigger("Hit");
            cameraShake.AmplitudeGain = cameraShakeAmplitude;
            m_Rigidbody.linearVelocity = Vector3.zero;
            m_Rigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
            m_Rigidbody.AddForce(Vector3.down * obstacleSlamForce, ForceMode.VelocityChange);
            SkiGameConfigurator.Instance?.PlayObstacleHitSound(playerID == 1);
            Destroy(col.gameObject);
            StartCoroutine(CollisionRecovery());
         

        }

        if (col.gameObject.CompareTag("RoughTerrain"))

        {
            maxSpeed = 60;
        }


    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("RoughTerrain")) maxSpeed = 100;
    }

    private IEnumerator CollisionRecovery()
    {
        moving = false;
        SkiGameConfigurator.Instance?.StopSkiingSound(playerID == 1);
        yield return new WaitForSeconds(1.2f);
        moving = true;
        cameraShake.AmplitudeGain = 0f;
        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        SkiGameConfigurator.Instance?.StartSkiingSound(playerID == 1);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("LoseCon")) return;

            if (gameManagerSlope != null)
            {
            if (other.CompareTag("Player")) 
            {
                gameManagerSlope.EndGame(0);
            }
            if (other.CompareTag("Player2"))
            {
                gameManagerSlope.EndGame(1);
            }
        }
        moving = false;
        m_Rigidbody.useGravity = false;
        m_Rigidbody.linearVelocity = Vector3.zero;
        kittyAnimator?.SetTrigger(didplayer1win ? "Victory" : "Defeat");
    }
    public void PlayIntroTargetAnimation(string triggerName)
    {
        if (kittyAnimator != null && !string.IsNullOrEmpty(triggerName))
            kittyAnimator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Resets an intro/target animation trigger (call at end of intro to return to default state).
    /// </summary>
    public void ResetIntroTargetAnimation(string triggerName)
    {
        if (kittyAnimator != null && !string.IsNullOrEmpty(triggerName))
            kittyAnimator.ResetTrigger(triggerName);
    }
}
