using System.Collections.Generic;
using UnityEngine;

public class SkiSlopeSpawner : MonoBehaviour
{
    [Header("Slope Endpoints")]
    [Tooltip("Top of the slope / first spawn row.")]
    [SerializeField] private Transform slopeStart;
    [Tooltip("Bottom of the slope / last spawn row.")]
    [SerializeField] private Transform slopeEnd;

    [Header("Obstacle Settings")]
    [Tooltip("All obstacle prefabs you want to spawn.")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [Tooltip("Local X offsets for each lane (left, center, right).")]
    [SerializeField] private float[] laneOffsets = new float[] { -2f, 0f, 2f };

    [Header("Spawn Configuration")]
    [Tooltip("Total number of rows of obstacles along the slope.")]
    [SerializeField] private int numberOfRows = 10;
    //[SerializeField] private float spawnInterval = 2f; // if you want to do this over time

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
        // 1) Compute direction and total distance between the two endpoints
        Vector3 dir = (slopeEnd.position - slopeStart.position).normalized;
        float totalDist = Vector3.Distance(slopeStart.position, slopeEnd.position);

        // 2) Compute the spacing between each row (we'll leave a little margin at top/bottom)
        //    If you want rows exactly at start and end, use (numberOfRows - 1). 
        //    Here we offset by +1 so there's a small gap from the very start.
        float spacing = totalDist / (numberOfRows + 1);

        for (int row = 1; row <= numberOfRows; row++)
        {
            // 3) Calculate the world‐space position of this row along the slope
            Vector3 basePos = slopeStart.position + dir * (spacing * row);

            // 4) Pick two lanes out of three, leave one lane empty
            List<int> lanes = new List<int> { 0, 1, 2 };
            for (int pick = 0; pick < 2; pick++)
            {
                int randIndex = Random.Range(0, lanes.Count);
                int laneID = lanes[randIndex];
                lanes.RemoveAt(randIndex);

                Vector3 spawnPos = basePos + new Vector3(laneOffsets[laneID], 0f, 0f);

                // 5) Pick a random obstacle prefab and instantiate
                GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
        }
    }
}