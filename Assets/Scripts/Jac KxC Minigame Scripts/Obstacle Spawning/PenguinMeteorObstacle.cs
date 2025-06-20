using UnityEngine;

public class PenguinMeteorObstacle : MonoBehaviour
{
    [SerializeField] private Vector3 startPosition = new Vector3(0f, 20f, 0f);
    [SerializeField] private Vector3 endPosition = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 finalRotation = new Vector3(0f, 0f, 45f);
    [SerializeField] private float fallDuration = 3f;
    [SerializeField] private GameObject snowParticles;

    private int myRowIndex;
    private Vector3 myBasePosition;
    private float[] laneOffsets;
    private SkiSlopeScript slopeScript;
    private float triggerDistance;
    private int triggerRow;

    private bool hasTriggered = false;
    private bool isFalling = false;
    private Vector3 startPos;
    private Vector3 targetPosition;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private float fallTimer = 0f;

    public void Initialize(int rowIndex, Vector3 basePosition, float[] lanes, float slope, SkiSlopeScript script, float triggerDist)
    {
        myRowIndex = rowIndex;
        myBasePosition = basePosition;
        laneOffsets = lanes;
        slopeScript = script;
        triggerDistance = triggerDist;

        triggerRow = Mathf.Max(1, myRowIndex - Mathf.RoundToInt(triggerDistance));

        startPos = basePosition + startPosition;
        targetPosition = basePosition + endPosition;
        startRotation = transform.rotation;
        targetRotation = Quaternion.Euler(finalRotation);

        transform.position = startPos;
    }

    public void OnRowTriggered(int triggeredRowIndex, GameObject triggeringPlayer)
    {
        if (triggeredRowIndex == triggerRow && !hasTriggered)
        {
            TriggerFall();
        }
    }

    public void TriggerFall()
    {
        if (hasTriggered) return;

        hasTriggered = true;
        isFalling = true;
        fallTimer = 0f;
        gameObject.tag = "Obstacle";
    }

    private void Update()
    {
        if (isFalling)
        {
            fallTimer += Time.deltaTime;
            float progress = fallTimer / fallDuration;

            if (progress >= 1f)
            {
                progress = 1f;
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                isFalling = false;
                OnImpact();
            }
            else
            {
                transform.position = Vector3.Lerp(startPos, targetPosition, progress);
                transform.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);
            }
        }
    }

    private void OnImpact()
    {
        if (snowParticles != null)
        {
            GameObject particles = Instantiate(snowParticles, transform.position, Quaternion.identity);
            Destroy(particles, 3f);
        }
    }

    public bool HasTriggered => hasTriggered;
}