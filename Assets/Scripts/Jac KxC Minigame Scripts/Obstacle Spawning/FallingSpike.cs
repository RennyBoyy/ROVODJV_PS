using UnityEngine;

public class FallingSpike : MonoBehaviour
{
    private float fallSpeed = 15f;
    private float groundLevel = 0f;
    private bool hasHitGround = false;
    private float destroyDelay = 3f;         

    [Header("Settings")]
    [SerializeField] private bool usePhysics = false;      
    [SerializeField] private bool rotateWhileFalling = true;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 0, 180f);

    private Rigidbody rb;

    public void Initialize(float speed, float groundY)
    {
        fallSpeed = speed;
        groundLevel = groundY;

        if (usePhysics)
        {
            SetupPhysicsSpike();
        }

        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    private void SetupPhysicsSpike()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;         
        rb.freezeRotation = !rotateWhileFalling;
    }

    private void Update()
    {
        if (hasHitGround) return;

        if (usePhysics && rb != null)
        {
            rb.linearVelocity = Vector3.down * fallSpeed;
        }
        else
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
        }

        if (rotateWhileFalling)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }

        if (transform.position.y <= groundLevel)
        {
            HitGround();
        }

        if (transform.position.y < groundLevel - 10f)
        {
            Destroy(gameObject);
        }
    }

    private void HitGround()
    {
        if (hasHitGround) return;

        hasHitGround = true;

        Vector3 groundPosition = transform.position;
        groundPosition.y = groundLevel;
        transform.position = groundPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (rotateWhileFalling)
        {
            rotateWhileFalling = false;
        }

        Destroy(gameObject, destroyDelay);

        OnSpikeImpact();
    }

    private void OnSpikeImpact()
    {
        Debug.Log($"Spike {gameObject.name} hit the ground!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasHitGround)
        {
            HandlePlayerHit(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !hasHitGround)
        {
            HandlePlayerHit(collision.gameObject);
        }

        if (collision.contacts[0].normal.y > 0.5f && !hasHitGround)
        {
            HitGround();
        }
    }

    private void HandlePlayerHit(GameObject player)
    {
        Debug.Log($"Spike hit player: {player.name}");

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            GameManager_Slope gameManager = FindFirstObjectByType<GameManager_Slope>();
            if (gameManager != null)
            {
                gameManager.TriggerGameEndFromPlayer(playerController);
            }
        }

        Destroy(gameObject);
    }

    public void SetDestroyDelay(float delay)
    {
        destroyDelay = delay;
    }

    public void SetImpactEffect(GameObject effectPrefab)
    {
    }
}