using System.Collections.Generic;
using UnityEngine;

public class SkiSlopeSpawner : MonoBehaviour
{
    [Header("Slope Points")]
    [SerializeField] private Transform slopeStart;
    [SerializeField] private Transform slopeEnd;

    [Header("Obstacle Settings")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private float[] laneOffsets = new float[] { -2f, 0f, 2f };

    [SerializeField] private GameObject snowmanPrefab;

    [Header("Spawn Configuration")]
    [SerializeField] private int numberOfRows = 10;

    private void Start()
    {
        if (slopeStart == null || slopeEnd == null)
        {
            Debug.LogError("SkiSlopeSpawner: Assign both slopeStart and slopeEnd Transforms in the inspector.");
            return;
        }
        SpawnAllRows();
    }

    private void SpawnAllRows()
    {
        Vector3 dir = (slopeEnd.position - slopeStart.position).normalized;
        float totalDist = Vector3.Distance(slopeStart.position, slopeEnd.position);
        float spacing = totalDist / (numberOfRows + 1);

        for (int row = 1; row <= numberOfRows; row++)
        {
            Vector3 basePos = slopeStart.position + dir * (spacing * row);

            // Pick a random obstacle prefab for this row
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

            // If it's the snowman, only spawn it in the center lane and skip the rest
            if (prefab == snowmanPrefab)
            {
                Vector3 spawnPos = basePos + new Vector3(laneOffsets[1], 0f, 0f); // center lane
                Instantiate(prefab, spawnPos, Quaternion.identity);
                continue; // Skip to next row
            }

            // Otherwise, pick two lanes out of three, leave one lane empty
            List<int> lanes = new List<int> { 0, 1, 2 };
            for (int pick = 0; pick < 2; pick++)
            {
                int randIndex = Random.Range(0, lanes.Count);
                int laneID = lanes[randIndex];
                lanes.RemoveAt(randIndex);

                Vector3 spawnPos = basePos + new Vector3(laneOffsets[laneID], 0f, 0f);
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
        }
    }
}