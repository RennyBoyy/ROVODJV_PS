using UnityEngine;
using Unity.Cinemachine;

public class FallingTreeObstacle : MonoBehaviour
{
    [Header("Force Settings")]
    [SerializeField] private float pushForce = 500f;
    [SerializeField] private float pushDirection = -1f;       
    [SerializeField] private Vector3 forcePosition = new Vector3(0f, 5f, 0f);

    [Header("Position Settings")]
    [SerializeField] private Vector3 standingOffset = new Vector3(3f, 0f, 0f);

    [Header("Camera Shake Settings")]
    [SerializeField] private float cameraShakeAmplitude = 2f;
    [SerializeField] private float cameraShakeDuration = 0.5f;
    [SerializeField] private float shakeDistanceRows = 3f;          

    private int myRowIndex;
    private Vector3 myBasePosition;
    private float[] laneOffsets;
    private float slopeAngle;
    private SkiSlopeScript slopeScript;
    private float triggerDistance;
    private int triggerRow;

    private bool hasTriggered = false;
    private bool hasHitGround = false;
    private Rigidbody treeRigidbody;
    private AudioSource audioSource;

    private void Awake()
    {
        treeRigidbody = GetComponent<Rigidbody>();
        if (treeRigidbody == null)
        {
            treeRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        treeRigidbody.isKinematic = true;
        treeRigidbody.mass = 10f;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;

        SetupSlopeIgnoring();
    }

    private void SetupSlopeIgnoring()
    {
        GameObject[] slopeObjects = GameObject.FindGameObjectsWithTag("Slope");
        GameObject[] wallObjects = GameObject.FindGameObjectsWithTag("Wall");
        GameObject[] roughObjects = GameObject.FindGameObjectsWithTag("RoughTerrain");
        Collider treeCollider = GetComponent<Collider>();

        if (treeCollider == null)
        {
            treeCollider = gameObject.AddComponent<BoxCollider>();
        }

        foreach (GameObject slopeObject in slopeObjects)
        {
            Collider slopeCollider = slopeObject.GetComponent<Collider>();
            if (slopeCollider != null)
            {
                Physics.IgnoreCollision(treeCollider, slopeCollider);
            }
        }

        foreach (GameObject wallObject in wallObjects)
        {
            Collider wallCollider = wallObject.GetComponent<Collider>();
            if (wallCollider != null)
            {
                Physics.IgnoreCollision(treeCollider, wallCollider);
            }
        }

        foreach (GameObject roughObject in roughObjects)
        {
            Collider roughCollider = roughObject.GetComponent<Collider>();
            if (roughCollider != null)
            {
                Physics.IgnoreCollision(treeCollider, roughCollider);
            }
        }

    }

    public void Initialize(int rowIndex, Vector3 basePosition, float[] lanes, float slope, SkiSlopeScript script, float triggerDist)
    {
        myRowIndex = rowIndex;
        myBasePosition = basePosition;
        laneOffsets = lanes;
        slopeAngle = slope;
        slopeScript = script;
        triggerDistance = triggerDist;

        triggerRow = Mathf.Max(1, myRowIndex - Mathf.RoundToInt(triggerDistance));

        pushDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
        Vector3 finalStandingOffset = new Vector3(standingOffset.x * pushDirection, standingOffset.y, standingOffset.z);

        transform.position = basePosition + finalStandingOffset;
    }

    public void OnRowTriggered(int triggeredRowIndex, GameObject triggeringPlayer)
    {
        if (triggeredRowIndex == triggerRow && !hasTriggered)
        {
            TriggerTreeFall();
        }
    }

    public void TriggerTreeFall()
    {
        if (hasTriggered || treeRigidbody == null) return;

        hasTriggered = true;

        SkiGameConfigurator.Instance?.PlayTreeStartFallingSound(audioSource);

        treeRigidbody.isKinematic = false;

        Vector3 worldForcePosition = transform.position + transform.TransformDirection(forcePosition);
        Vector3 forceDirection = new Vector3(-pushDirection, 0f, 0f);
        Vector3 worldForceDirection = transform.TransformDirection(forceDirection.normalized);

        treeRigidbody.AddForceAtPosition(worldForceDirection * pushForce, worldForcePosition, ForceMode.Impulse);

        gameObject.tag = "Obstacle";
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHitGround || !hasTriggered) return;

        if (collision.gameObject.CompareTag("Slope") || collision.gameObject.name.ToLower().Contains("ground"))
        {
            hasHitGround = true;

            SkiGameConfigurator.Instance?.PlayTreeHitGroundSound(audioSource);

            TriggerCameraShakeForNearbyPlayers();
        }
    }

    private void TriggerCameraShakeForNearbyPlayers()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            if (player == null) continue;

            float distance = Vector3.Distance(player.transform.position, transform.position);

            if (slopeScript != null)
            {
                Vector3 row1Pos = slopeScript.GetRowPosition(1);
                Vector3 row2Pos = slopeScript.GetRowPosition(2);
                float rowSpacing = Vector3.Distance(row1Pos, row2Pos);
                float maxShakeDistance = rowSpacing * shakeDistanceRows;

                if (distance <= maxShakeDistance)
                {
                    StartCoroutine(ShakePlayerCamera(player));
                }
            }
        }
    }

    private System.Collections.IEnumerator ShakePlayerCamera(PlayerController player)
    {
        CinemachineBasicMultiChannelPerlin[] shakeComponents = FindObjectsByType<CinemachineBasicMultiChannelPerlin>(FindObjectsSortMode.None);

        foreach (var shakeComponent in shakeComponents)
        {
            if (shakeComponent != null)
            {
                shakeComponent.AmplitudeGain = cameraShakeAmplitude;
            }
        }

        yield return new WaitForSeconds(cameraShakeDuration);

        foreach (var shakeComponent in shakeComponents)
        {
            if (shakeComponent != null)
            {
                shakeComponent.AmplitudeGain = 0f;
            }
        }
    }

    public bool HasTriggered => hasTriggered;
}