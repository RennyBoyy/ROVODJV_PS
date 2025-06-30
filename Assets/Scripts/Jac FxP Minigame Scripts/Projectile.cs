using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Lifetime & Speed Settings")]
    public float deathTime = 3f;
    public float projectileSpeed = 10f;
    public float initialSpeedMultiplier = 1.5f;
    public float timeToNormalize = 1f;

    [Header("Spin")]
    public float spinSpeed = 360f;

    private Vector3 moveDirection;  
    private Transform visual;    
    private bool isGravityOn = false; 

    private float baseSpeed;        
    private float initialSpeed;     
    private float currentSpeed;         
    private float elapsedDecel = 0f;   

    void Awake()
    {
        if (transform.childCount > 0)
        {
            visual = transform.GetChild(0);
        }
        else
        {
            visual = transform;
        }

        baseSpeed = projectileSpeed;
        initialSpeed = baseSpeed * initialSpeedMultiplier;
        currentSpeed = initialSpeed;
    }

    void Start()
    {
        Destroy(gameObject, deathTime);
    }

    void Update()
    {
        // If gravity isn't on yet, move forward and handle deceleration:
        if (!isGravityOn)
        {
            // 1) Move forward by currentSpeed
            transform.position += -moveDirection * currentSpeed * Time.deltaTime;

            // 2) Spin the visual child (only if gravity is off)
            if (visual != null)
                visual.Rotate(Vector3.one, spinSpeed * Time.deltaTime, Space.Self);

            // 3) Slow down gradually until we reach baseSpeed
            if (elapsedDecel < timeToNormalize)
            {
                elapsedDecel += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedDecel / timeToNormalize);
                currentSpeed = Mathf.Lerp(initialSpeed, baseSpeed, t);
            }
            else
            {
                // Once we've finished decelerating, lock to baseSpeed
                currentSpeed = baseSpeed;
            }
        }
       
    }

    private void OnTriggerEnter(Collider other)
    {
        // When hitting a Monster, turn on physics gravity so the projectile drops out of flight:
        if (other.CompareTag("Monster"))
        {
            Rigidbody rb;
            if (TryGetComponent<Rigidbody>(out rb))
            {
                rb.useGravity = true;
                isGravityOn = true;
            }
        }
        // If we hit an AppleBarrier, destroy immediately:
        else if (other.CompareTag("AppleBarrier"))
        {
            Destroy(gameObject);
        }
    }

    public void SetMoveDirection(Vector3 dir)
    {
        moveDirection = dir;
    }
}
