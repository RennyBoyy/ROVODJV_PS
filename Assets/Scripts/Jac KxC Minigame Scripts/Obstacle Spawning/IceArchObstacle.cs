using System.Collections.Generic;
using UnityEngine;

public class IceArchObstacle : MonoBehaviour
{
    [SerializeField] private GameObject icSpikePrefab;
    [SerializeField] private int spikesToDrop = 2;
    [SerializeField] private Transform leftSpikePoint;
    [SerializeField] private Transform centerSpikePoint;
    [SerializeField] private Transform rightSpikePoint;
    [SerializeField] private Vector3 archOffset = new Vector3(0f, 0f, 0f);

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
    }

    private void SpawnIceSpikes()
    {
        if (icSpikePrefab == null) return;

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
            spikeRb.mass = 10f;

            if (spike.GetComponent<Collider>() == null)
            {
                spike.AddComponent<BoxCollider>();
            }

            // Add the spike stopper component
            IceSpikeStopper stopper = spike.AddComponent<IceSpikeStopper>();

            spike.tag = "Obstacle";

            spawnedSpikes.Add(spike);
        }
    }

    public void OnRowTriggered(int triggeredRowIndex, GameObject triggeringPlayer)
    {
        if (triggeredRowIndex == triggerRow && !hasTriggered)
        {
            TriggerSpikeDrop();
        }
    }

    public void TriggerSpikeDrop()
    {
        if (hasTriggered || spawnedSpikes.Count == 0) return;

        hasTriggered = true;

        List<GameObject> spikesToRelease = SelectRandomSpikes();

        foreach (GameObject spike in spikesToRelease)
        {
            if (spike != null)
            {
                Rigidbody spikeRb = spike.GetComponent<Rigidbody>();
                if (spikeRb != null)
                {
                    spikeRb.isKinematic = false;
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
}

public class IceSpikeStopper : MonoBehaviour
{
    private bool hasStopped = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasStopped) return;

        if (collision.gameObject.CompareTag("Slope"))
        {
            hasStopped = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }
}