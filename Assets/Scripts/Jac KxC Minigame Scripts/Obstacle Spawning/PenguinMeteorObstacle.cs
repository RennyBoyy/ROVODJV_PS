using UnityEngine;
using Unity.Cinemachine;

public class PenguinMeteorObstacle : MonoBehaviour
{
    [Header("Fall Settings")]
    [SerializeField] private Vector3 startPosition = new Vector3(10f, 20f, 0f);
    [SerializeField] private float fallDirection = -1f;       
    [SerializeField] private Vector3 leftEndPosition = new Vector3(-1.5f, 0f, 0f);
    [SerializeField] private Vector3 rightEndPosition = new Vector3(1.5f, 0f, 0f);
    [SerializeField] private Vector3 finalRotation = new Vector3(0f, 0f, 45f);
    [SerializeField] private float fallDuration = 3f;
    [SerializeField] private GameObject snowParticles;

    [Header("Camera Shake Settings")]
    [SerializeField] private float cameraShakeAmplitude = 3f;
    [SerializeField] private float cameraShakeDuration = 0.8f;
    [SerializeField] private float shakeDistanceRows = 4f;          

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
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }

    public void Initialize(int rowIndex, Vector3 basePosition, float[] lanes, float slope, SkiSlopeScript script, float triggerDist)
    {
        myRowIndex = rowIndex;
        myBasePosition = basePosition;
        laneOffsets = lanes;
        slopeScript = script;
        triggerDistance = triggerDist;

        triggerRow = Mathf.Max(1, myRowIndex - Mathf.RoundToInt(triggerDistance));

        fallDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
        Vector3 finalStartPosition = new Vector3(startPosition.x * fallDirection, startPosition.y, startPosition.z);
        Vector3 finalEndPosition = fallDirection < 0 ? leftEndPosition : rightEndPosition;

        startPos = basePosition + finalStartPosition;
        targetPosition = basePosition + finalEndPosition;
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

        SkiGameConfigurator.Instance?.StartMeteorFallingSound(audioSource);
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
        SkiGameConfigurator.Instance?.StopMeteorFallingSoundAndPlayImpact(audioSource);

        TriggerCameraShakeForNearbyPlayers();

        if (snowParticles != null)
        {
            GameObject particles = Instantiate(snowParticles, transform.position, Quaternion.identity);
            Destroy(particles, 3f);
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