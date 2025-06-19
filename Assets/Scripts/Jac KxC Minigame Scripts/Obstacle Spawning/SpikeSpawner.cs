using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeSpawner : MonoBehaviour
{
    private SpecialObstacle config;
    private Vector3 basePosition;
    private float[] laneOffsets;
    private float slopeAngle;
    private bool hasTriggered = false;
    private List<GameObject> spawnedSpikes = new List<GameObject>();

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    public void SetupSpawner(SpecialObstacle specialObstacle, Vector3 basePos, float[] lanes, float slope)
    {
        config = specialObstacle;
        basePosition = basePos;
        laneOffsets = lanes;
        slopeAngle = slope;

        SetupTrigger();

        if (showDebugInfo)
        {
            Debug.Log($"SpikeSpawner setup complete for {specialObstacle.name}");
        }
    }

    private void SetupTrigger()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(8f, 5f, 3f);     
        }
        else
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered && config != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Player {other.name} triggered spike spawner!");
            }

            hasTriggered = true;
            StartCoroutine(SpawnSpikes());
        }
    }

    private IEnumerator SpawnSpikes()
    {
        if (config == null || config.spikePrefab == null)
        {
            Debug.LogError("SpikeSpawner: No spike prefab configured!");
            yield break;
        }

        List<int> selectedLanes = SelectRandomLanes();

        if (showDebugInfo)
        {
            Debug.Log($"Targeting lanes: [{string.Join(", ", selectedLanes)}]");
        }

        for (int i = 0; i < config.numberOfSpikes; i++)
        {
            foreach (int laneIndex in selectedLanes)
            {
                SpawnSingleSpike(laneIndex);
            }

            yield return new WaitForSeconds(config.spikeSpawnInterval);
        }

        if (showDebugInfo)
        {
            Debug.Log($"Finished spawning {config.numberOfSpikes * selectedLanes.Count} spikes");
        }
    }

    private List<int> SelectRandomLanes()
    {
        List<int> availableLanes = new List<int>(config.targetLanes);
        List<int> selectedLanes = new List<int>();

        int lanesToSelect = Mathf.Min(config.lanesPerActivation, availableLanes.Count);

        for (int i = 0; i < lanesToSelect; i++)
        {
            int randomIndex = Random.Range(0, availableLanes.Count);
            selectedLanes.Add(availableLanes[randomIndex]);
            availableLanes.RemoveAt(randomIndex);
        }

        return selectedLanes;
    }

    private void SpawnSingleSpike(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= laneOffsets.Length)
        {
            Debug.LogWarning($"Invalid lane index: {laneIndex}");
            return;
        }

        Vector3 lanePosition = basePosition + new Vector3(laneOffsets[laneIndex], 0f, 0f);
        Vector3 spawnPosition = lanePosition + Vector3.up * config.spikeSpawnHeight;

        spawnPosition += new Vector3(
            Random.Range(-0.5f, 0.5f),    
            Random.Range(-0.5f, 0.5f),    
            Random.Range(-1f, 1f)         
        );

        Quaternion spikeRotation = Quaternion.Euler(slopeAngle, 0f, Random.Range(0f, 360f));
        GameObject spike = Instantiate(config.spikePrefab, spawnPosition, spikeRotation);
        spike.name = $"FallingSpike_Lane{laneIndex}_{spawnedSpikes.Count}";

        FallingSpike fallingComponent = spike.GetComponent<FallingSpike>();
        if (fallingComponent == null)
        {
            fallingComponent = spike.AddComponent<FallingSpike>();
        }

        fallingComponent.Initialize(config.spikeFallSpeed, lanePosition.y);

        spawnedSpikes.Add(spike);

        if (showDebugInfo)
        {
            Debug.Log($"Spawned spike at lane {laneIndex}, position: {spawnPosition}");
        }
    }

    public void CleanupSpikes()
    {
        foreach (GameObject spike in spawnedSpikes)
        {
            if (spike != null)
            {
                Destroy(spike);
            }
        }
        spawnedSpikes.Clear();
    }

    private void OnDestroy()
    {
        CleanupSpikes();
    }
}