using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private float moveInput;

    [SerializeField] private Rigidbody m_Rigidbody;
    public float m_Thrust = 1.0f;
    public float maxSpeed = 100f;
    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
       
    }
    private void Update()
    {
        HandleMovement();
        m_Rigidbody.AddForce(new Vector3(0, -1f, 0.8f), ForceMode.Impulse);
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
