using Unity.VisualScripting;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float deathTime = 3f;
    public float projectileSpeed = 10f;
    public float spinSpeed = 360f;

    private Vector3 moveDirection;
    private Transform visual;
    private bool isGravityOn = false;

    void Awake()
    {
        moveDirection = transform.forward;

        if (transform.childCount > 0)
        {
            visual = transform.GetChild(0);
        }
        else
        {
            visual = transform;
        }
    }

    void Start()
    {
        Destroy(this.gameObject, deathTime);
    }

    void Update()
    {
        if (!isGravityOn)
        {
            transform.position += -moveDirection * projectileSpeed * Time.deltaTime;
        }

        if (visual != null)
        {
            if (!isGravityOn)
            {
                visual.Rotate(Vector3.one, spinSpeed * Time.deltaTime, Space.Self);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Monster"))
        {
            Rigidbody rb;
            if (TryGetComponent<Rigidbody>(out rb))
            {
                rb.useGravity = true;
                isGravityOn = true;
                
            }
        }
        if (other.CompareTag("AppleBarrier"))
        {
            Destroy(this.gameObject);
        }
    }
}
