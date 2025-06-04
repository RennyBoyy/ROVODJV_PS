using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private float moveInput;

    [SerializeField] private float jumpForce = 30f;
    private bool isGrounded = true;

    [SerializeField] private Rigidbody m_Rigidbody;
    public float m_Thrust = 1.0f;
    public float maxSpeed = 100f;
    private bool jumpInput;
    private bool moving;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        moving = true;
    }
    private void Update()
    {
        HandleMovement();
        if (moving)
        {
            m_Rigidbody.AddForce(new Vector3(0, -1f, 0.8f), ForceMode.Impulse);
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
        //Debug.Log(ctx.ReadValue<Vector2>());
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
        if (collision.gameObject.CompareTag("Respawn"))
        {
            SceneManager.LoadScene("RenTest");
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
