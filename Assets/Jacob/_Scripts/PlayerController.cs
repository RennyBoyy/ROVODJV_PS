using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private float moveInput;

    [SerializeField] private float jumpForce = 5f;
    private bool isGrounded = true;

    [SerializeField] private Rigidbody m_Rigidbody;
    public float m_Thrust = 1.0f;
    public float maxSpeed = 100f;
    private bool jumpInput;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
       
    }
    private void Update()
    {
        HandleMovement();

        m_Rigidbody.AddForce(new Vector3(0, -1f, 0.8f), ForceMode.Impulse);

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
        Debug.Log(ctx.ReadValue<Vector2>());
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
