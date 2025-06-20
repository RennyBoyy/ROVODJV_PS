using System.Collections.Generic;
using UnityEngine;

public class IceArchObstacle : MonoBehaviour
{
    [Header("Ice Spike Settings")]
    [SerializeField] private GameObject icSpikePrefab;
    [SerializeField] private int spikesToDrop = 2;         

    [Header("Spike Spawn Points")]
    [SerializeField] private Transform leftSpikePoint;
    [SerializeField] private Transform centerSpikePoint;
    [SerializeField] private Transform rightSpikePoint;

    [Header("Arch Positioning")]
    [SerializeField] private Vector3 archOffset = new Vector3(0f, 0f, 0f);      

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private int myRowIndex;
    private Vector3 myBasePosition;
    private float[] laneOffsets;
    private float slopeAngle;
    private SkiSlopeScript slopeScript;
    private float triggerDistance;
    private int triggerRow;       

    private bool hasTriggered = false;
    private List<GameObject> spawnedSpikes = new List<GameObject>();
    private List<Transform> spikeSpawnPoints = new List<Transform>();

    private void Awake()
    {
        if (leftSpikePoint != null) spikeSpawnPoints.Add(leftSpikePoint);
        if (centerSpikePoint != null) spikeSpawnPoints.Add(centerSpikePoint);
        if (rightSpikePoint != null) spikeSpawnPoints.Add(rightSpikePoint);

        if (spikeSpawnPoints.Count == 0)
        {
            Debug.LogError("IceArchObstacle: No spike spawn points assigned!");
        }
    }

    private void Start()
    {
        SpawnIceSpikes();
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

        transform.position = basePosition + archOffset;

        if (showDebugInfo)
        {
            Debug.Log($"IceArch initialized at row {rowIndex}. Will be triggered by row {triggerRow}.");
        }
    }

    private void SpawnIceSpikes()
    {
        if (icSpikePrefab == null)
        {
            Debug.LogError("IceArchObstacle: No ice spike prefab assigned!");
            return;
        }

        foreach (Transform spawnPoint in spikeSpawnPoints)
        {
            if (spawnPoint == null) continue;

            GameObject spike = Instantiate(icSpikePrefab, spawnPoint.position, spawnPoint.rotation);
            spike.name = $"IceSpike_{spawnPoint.name}";
            spike.transform.SetParent(transform);        

            Rigidbody spikeRb = spike.GetComponent<Rigidbody>();
            if (spikeRb == null)
            {
                spikeRb = spike.AddComponent<Rigidbody>();
            }
            spikeRb.isKinematic = true;    
            spikeRb.mass = 5f;      

            if (spike.GetComponent<Collider>() == null)
            {
                spike.AddComponent<BoxCollider>();
            }

            spike.tag = "Obstacle";

            spawnedSpikes.Add(spike);

            if (showDebugInfo)
            {
                Debug.Log($"Spawned ice spike at {spawnPoint.name}");
            }
        }
    }

    public void OnRowTriggered(int triggeredRowIndex, GameObject triggeringPlayer)
    {
        if (triggeredRowIndex == triggerRow && !hasTriggered)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Ice Arch at row {myRowIndex} received trigger signal from row {triggeredRowIndex}!");
            }
            TriggerSpikeDrop();
        }
    }

    public void TriggerSpikeDrop()
    {
        if (hasTriggered || spawnedSpikes.Count == 0) return;

        hasTriggered = true;

        if (showDebugInfo)
        {
            Debug.Log($"Ice Arch spike drop triggered for row {myRowIndex}!");
        }

        List<GameObject> spikesToRelease = SelectRandomSpikes();

        foreach (GameObject spike in spikesToRelease)
        {
            if (spike != null)
            {
                Rigidbody spikeRb = spike.GetComponent<Rigidbody>();
                if (spikeRb != null)
                {
                    spikeRb.isKinematic = false;   

                    if (showDebugInfo)
                    {
                        Debug.Log($"Released ice spike: {spike.name}");
                    }
                }
            }
        }
    }

    private List<GameObject> SelectRandomSpikes()
    {
        List<GameObject> availableSpikes = new List<GameObject>(spawnedSpikes);
        List<GameObject> selectedSpikes = new List<GameObject>();

        availableSpikes.RemoveAll(spike => spike == null);

        int spikesToSelect = Mathf.Min(spikesToDrop, availableSpikes.Count);

        for (int i = 0; i < spikesToSelect; i++)
        {
            if (availableSpikes.Count == 0) break;

            int randomIndex = Random.Range(0, availableSpikes.Count);
            selectedSpikes.Add(availableSpikes[randomIndex]);
            availableSpikes.RemoveAt(randomIndex);
        }

        return selectedSpikes;
    }

    public bool HasTriggered => hasTriggered;
    public int GetTriggerRow() => triggerRow;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (Transform spawnPoint in spikeSpawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
            }
        }

        if (slopeScript != null && triggerRow > 0)
        {
            Gizmos.color = Color.yellow;
            Vector3 triggerRowPos = slopeScript.GetRowPosition(triggerRow);
            Gizmos.DrawWireCube(triggerRowPos, new Vector3(12f, 8f, 0.5f));
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(8f, 6f, 2f));
    }
}