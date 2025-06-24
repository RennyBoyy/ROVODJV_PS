using UnityEngine;

public class FallingTreeObstacle : MonoBehaviour
{
    [SerializeField] private float pushForce = 500f;
    [SerializeField] private Vector3 forceDirection = new Vector3(-1f, 0f, 0f);
    [SerializeField] private Vector3 forcePosition = new Vector3(0f, 5f, 0f);
    [SerializeField] private Vector3 standingOffset = new Vector3(3f, 0f, 0f);

    private int myRowIndex;
    private Vector3 myBasePosition;
    private float[] laneOffsets;
    private float slopeAngle;
    private SkiSlopeScript slopeScript;
    private float triggerDistance;
    private int triggerRow;

    private bool hasTriggered = false;
    private Rigidbody treeRigidbody;

    private void Awake()
    {
        treeRigidbody = GetComponent<Rigidbody>();
        if (treeRigidbody == null)
        {
            treeRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        treeRigidbody.isKinematic = true;
        treeRigidbody.mass = 10f;
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

        transform.position = basePosition + standingOffset;
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

        treeRigidbody.isKinematic = false;

        Vector3 worldForcePosition = transform.position + transform.TransformDirection(forcePosition);
        Vector3 worldForceDirection = transform.TransformDirection(forceDirection.normalized);

        treeRigidbody.AddForceAtPosition(worldForceDirection * pushForce, worldForcePosition, ForceMode.Impulse);

        gameObject.tag = "Obstacle";
    }

    public bool HasTriggered => hasTriggered;
}