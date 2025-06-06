using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Physics Controllers")]
    [SerializeField] private float jumpForce = 30f;
    [SerializeField] private bool isGrounded = true;
    [SerializeField] private bool moving;
    [SerializeField] private Rigidbody m_Rigidbody;
    [SerializeField] private Vector3 startPosition;


    [Header("Movement Settings")]
    [SerializeField] private float lateralThrust = 1.0f;    
    [SerializeField] private float baseForwardThrust = 0.9f;  
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float jumpDuration = 0.7f;

    private float moveInput;
    private bool jumpInput;
    private bool onRoughGround = false;
    public bool didplayer1win;
    public int playerID;
    private bool isJumping;
    private float moveElapsed = 0f;
    private Vector3 targetJumpPosition;



    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        moving = true; 
    }

    private void FixedUpdate()
    {
        HandleMovement();

        if (moving)
        {
            m_Rigidbody.useGravity = true;
            m_Rigidbody.AddForce(new Vector3(0, -0.8f, baseForwardThrust), ForceMode.Impulse);
        }
        else
        {
            m_Rigidbody.useGravity = false;
        }

        // If on rough terrain, apply extra slowdown or rely on higher drag
        if (onRoughGround)
        {
            m_Rigidbody.AddForce(Vector3.back * 2f, ForceMode.Acceleration);
        }

        // Jump logic
        if (jumpInput && isGrounded)
        {
            m_Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpInput = false;
            isGrounded = false;
        }

        // Clamp overall speed
        if (m_Rigidbody.linearVelocity.magnitude > maxSpeed)
        {
            m_Rigidbody.linearVelocity = m_Rigidbody.linearVelocity.normalized * maxSpeed;
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>().x;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && isGrounded)
        {
            isJumping = true;
            HandleLerpMovement();
        }
    }
    private void HandleLerpMovement()
    {
        if (!isJumping && !isGrounded) return;

        moveElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(moveElapsed / jumpDuration);
        transform.position = Vector3.Lerp(startPosition, targetJumpPosition, t);

        if (t >= 1f)
        {
            isJumping = false;
            transform.position = targetJumpPosition;
        }
    }
    private void startLerpJump()
    {
        startPosition = transform.position;
        moveElapsed = 0f;
        isJumping = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ground check: only set isGrounded when hitting a mostly flat surface
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }

        // Obstacle: destroy it and stop movement temporarily
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Destroy(collision.gameObject);
            StartCoroutine(StopMoving());
        }

        // Rough terrain: enable the slowdown flag and increase drag
        if (collision.gameObject.CompareTag("RoughTerrain"))
        {
            onRoughGround = true;
            m_Rigidbody.linearDamping = 1.5f;
        }
        // Slope: reset drag when back on normal slope
        else if (collision.gameObject.CompareTag("Slope"))
        {
            onRoughGround = false;
            m_Rigidbody.linearDamping = 0f;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("RoughTerrain"))
        {
            onRoughGround = false;
            m_Rigidbody.linearDamping = 0f;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("LoseCon"))
            return;

        if (playerID == 1)
        {
            Debug.Log("Player 1 hit the trigger.");
            didplayer1win = true;
        }
        else if (playerID == 2)
        {
            Debug.Log("Player 2 hit the trigger.");
            didplayer1win = false;
        }
    }

    private void HandleMovement()
    {
        if (moveInput > 0.1f)
        {
            m_Rigidbody.AddForce(new Vector3(lateralThrust, 0, 0), ForceMode.Impulse);
        }
        else if (moveInput < -0.1f)
        {
            m_Rigidbody.AddForce(new Vector3(-lateralThrust, 0, 0), ForceMode.Impulse);
        }
    }

    private IEnumerator StopMoving()
    {
        moving = false;
        yield return new WaitForSeconds(2f);
        moving = true;
    }
}
