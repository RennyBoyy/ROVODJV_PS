using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Physics Controllers")]
    [SerializeField] private float jumpForce = 30f;
    [SerializeField] private bool isGrounded = true;
    [SerializeField] private bool moving;
    [SerializeField] private Rigidbody m_Rigidbody;
    private float moveInput;
    public float m_Thrust = 1.0f;
    public float z_Thrust = 0.9f;
    public float maxSpeed = 100f;
    private bool jumpInput;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        moving = false;
    }
    private void FixedUpdate()
    {
        m_Rigidbody.useGravity = true; // Always enable gravity

        HandleMovement();
        if (moving)
        {
            // Apply a continuous forward force
            m_Rigidbody.AddForce(new Vector3(0, 0, z_Thrust), ForceMode.Force);

            // Clamp minimum forward speed
            Vector3 velocity = m_Rigidbody.linearVelocity;
            if (velocity.z < 2f)
            {
                velocity.z = 2f;
                m_Rigidbody.linearVelocity = velocity;
            }
        }

        if (jumpInput && isGrounded)
        {
            m_Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpInput = false;
            isGrounded = false;
        }

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
            jumpInput = true;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Destroy(collision.gameObject);
            StartCoroutine(StopMoving());
        }
        if (collision.gameObject.CompareTag("RoughTerrain"))
        {
            z_Thrust = 10f;
        }
        else if (collision.gameObject.CompareTag("Slope")) z_Thrust = 20f;

        if (collision.gameObject.CompareTag("Wall"))
        {
            Vector3 velocity = m_Rigidbody.linearVelocity;
            velocity.x = 0; 
            velocity.z *= 0.8f; 
            m_Rigidbody.linearVelocity = velocity;
        }
    }
    private IEnumerator StopMoving()
    {
        yield return new WaitForSeconds(2f);
    }

    private void HandleMovement()
    {
       
        if (moveInput > 0.1f)
        {
            m_Rigidbody.AddForce(new Vector3(m_Thrust, 0, 0), ForceMode.Impulse);
        }
        else if (moveInput < -0.1f)
        {
            m_Rigidbody.AddForce(new Vector3(-m_Thrust, 0, 0), ForceMode.Impulse);
        }
    }
   
}
