using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Physics Controllers")]
    [SerializeField] private float jumpForce = 30f;       // will be recalculated
    [SerializeField] private bool isGrounded = true;
    [SerializeField] private bool moving;
    [SerializeField] private Rigidbody m_Rigidbody;

    [Header("Movement Settings")]
    [SerializeField] private float lateralThrust = 1.0f;
    [SerializeField] private float baseForwardThrust = 0.9f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float gravityMultiplier = 2f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Obstacle Slam")]
    [SerializeField] private float obstacleSlamForce = 20f;

    [Header("Rough Terrain")]
    [SerializeField] private float roughDrag = 1.5f;

    public bool didplayer1win;
    public int playerID;

    private float moveInput;
    private bool jumpInput;
    private bool onRoughGround;
    private float coyoteCounter;
    private float jumpBufferCounter;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        moving = true;
        float g = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
        jumpForce = Mathf.Sqrt(2f * g * jumpHeight);
    }

    void Update()
    {
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        if (jumpInput) jumpBufferCounter = jumpBufferTime;
        jumpBufferCounter -= Time.deltaTime;

        jumpInput = false;
    }

    void FixedUpdate()
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            Vector3 v = m_Rigidbody.linearVelocity;
            v.y = jumpForce;
            m_Rigidbody.linearVelocity = v;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            isGrounded = false;
        }

        HandleMovement();
        m_Rigidbody.useGravity = true;

        if (!isGrounded)
            m_Rigidbody.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);

        if (moving)
            m_Rigidbody.AddForce(new Vector3(0, -0.8f, baseForwardThrust), ForceMode.Impulse);

        m_Rigidbody.linearDamping = onRoughGround ? roughDrag : 0f;

        if (m_Rigidbody.linearVelocity.magnitude > maxSpeed)
            m_Rigidbody.linearVelocity = m_Rigidbody.linearVelocity.normalized * maxSpeed;

        m_Rigidbody.angularVelocity *= 0.2f;
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>().x;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            jumpInput = true;
    }

    private void HandleMovement()
    {
        if (moveInput > 0.1f) m_Rigidbody.AddForce(new Vector3(lateralThrust, 0, 0), ForceMode.Impulse);
        if (moveInput < -0.1f) m_Rigidbody.AddForce(new Vector3(-lateralThrust, 0, 0), ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
            isGrounded = true;

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            // one‐off slam down
            m_Rigidbody.angularVelocity = Vector3.zero;
            var v = m_Rigidbody.linearVelocity;
            v.y = 0f;
            m_Rigidbody.linearVelocity = v;
            m_Rigidbody.AddForce(Vector3.down * obstacleSlamForce, ForceMode.Impulse);

            Destroy(collision.gameObject);
            StartCoroutine(StopMoving());
        }

        if (collision.gameObject.CompareTag("RoughTerrain"))
            onRoughGround = true;
        else if (collision.gameObject.CompareTag("Slope"))
            onRoughGround = false;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("RoughTerrain"))
            onRoughGround = false;
    }

    private IEnumerator StopMoving()
    {
        moving = false;
        yield return new WaitForSeconds(0.5f);
        moving = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("LoseCon")) return;
        didplayer1win = (playerID == 1);
    }
}
