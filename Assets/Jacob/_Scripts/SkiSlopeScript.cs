using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkiSlopeScript : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private float[] laneOffsets = new float[] { -2f, 0f, 2f };
    [SerializeField] private float spawnInterval = 2f;

    private void Start()
    {
        StartCoroutine(SpawnObstaclesRoutine());
    }

    private IEnumerator SpawnObstaclesRoutine()
    {
        while (true)
        {
            SpawnObstacles();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnObstacles()
    {
        foreach (var floor in spawnPoints)
        {
            // Pick 1 or 2 unique lanes
            List<int> laneIndices = new List<int> { 0, 1, 2 };
            int obstaclesToSpawn = Random.Range(1, 3);
            for (int i = 0; i < obstaclesToSpawn; i++)
            {
                int idx = Random.Range(0, laneIndices.Count);
                int laneIdx = laneIndices[idx];
                laneIndices.RemoveAt(idx);

                Vector3 spawnPos = floor.position + new Vector3(laneOffsets[laneIdx], 0, 0);
                GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
        }
    }
}