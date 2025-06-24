using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObstacleRow
{
    public string pattern = "0,0,0";
    public string rowName = "";

    public int[] GetLanePattern()
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return new int[] { 0, 0, 0 };
        }

        string[] parts = pattern.Split(',');
        int[] result = new int[3];

        for (int i = 0; i < 3; i++)
        {
            if (i < parts.Length && int.TryParse(parts[i].Trim(), out int value))
            {
                result[i] = value;
            }
            else
            {
                result[i] = 0;
            }
        }
        return result;
    }
}

[System.Serializable]
public class ComplexObstacle
{
    public string name = "Complex Obstacle";
    public GameObject prefab;
    public int targetRow = 15;
    public float triggerDistance = 2f;
}

public class SkiSlopeScript : MonoBehaviour
{
    [SerializeField] private Transform slopeStart;
    [SerializeField] private Transform slopeEnd;
    [SerializeField] private float[] laneOffsets = new float[] { -2f, 0f, 2f };
    [SerializeField] private GameObject slopeObject;
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private ObstacleRow[] obstacleRows;
    [SerializeField] private ComplexObstacle[] complexObstacles;
    [SerializeField] private int numberOfRows = 30;

    private List<Transform> spawnedObstacles = new List<Transform>();
    private List<Transform> spawnedComplexObstacles = new List<Transform>();
    private List<float> rowZPositions = new List<float>();
    private List<int> complexObstacleRowIndices = new List<int>();
    private float cachedSlopeAngle = 0f;

    private void Start()
    {
        CalculateSlopeAngle();
        SpawnAllRows();
    }

    private void CalculateSlopeAngle()
    {
        if (slopeObject != null)
        {
            cachedSlopeAngle = slopeObject.transform.eulerAngles.x;
            if (cachedSlopeAngle > 180f)
            {
                cachedSlopeAngle -= 360f;
            }
        }
        else
        {
            cachedSlopeAngle = 0f;
        }
    }

    private void SpawnAllRows()
    {
        Vector3 direction = (slopeEnd.position - slopeStart.position).normalized;
        float totalDistance = Vector3.Distance(slopeStart.position, slopeEnd.position);
        float spacing = totalDistance / (numberOfRows + 1);

        for (int row = 1; row <= numberOfRows; row++)
        {
            Vector3 basePosition = slopeStart.position + direction * (spacing * row);

            GameObject rowParent = new GameObject($"Row_{row}");
            rowParent.transform.position = basePosition;
            rowParent.transform.rotation = Quaternion.Euler(cachedSlopeAngle, 0f, 0f);

            BoxCollider rowTrigger = rowParent.AddComponent<BoxCollider>();
            rowTrigger.isTrigger = true;
            rowTrigger.size = new Vector3(100f, 100f, 0.5f);

            RowTrigger rowComponent = rowParent.AddComponent<RowTrigger>();
            rowComponent.Initialize(row);

            bool shouldSpawnComplex = ShouldSpawnComplexObstacle(row);

            if (shouldSpawnComplex)
            {
                SpawnComplexObstacle(basePosition, row, rowParent.transform);
                complexObstacleRowIndices.Add(row);
            }
            else
            {
                SpawnNormalRow(basePosition, row, rowParent.transform);
            }

            rowZPositions.Add(basePosition.z);
        }
    }

    private bool ShouldSpawnComplexObstacle(int rowIndex)
    {
        if (complexObstacles == null || complexObstacles.Length == 0)
            return false;

        foreach (ComplexObstacle complexObstacle in complexObstacles)
        {
            if (complexObstacle.targetRow == rowIndex)
            {
                return true;
            }
        }
        return false;
    }

    private void SpawnComplexObstacle(Vector3 basePosition, int rowIndex, Transform rowParent)
    {
        ComplexObstacle targetObstacle = null;

        foreach (ComplexObstacle complexObstacle in complexObstacles)
        {
            if (complexObstacle.targetRow == rowIndex)
            {
                targetObstacle = complexObstacle;
                break;
            }
        }

        if (targetObstacle == null || targetObstacle.prefab == null)
        {
            SpawnNormalRow(basePosition, rowIndex, rowParent);
            return;
        }

        Vector3 spawnPosition = basePosition;
        Quaternion slopeRotation = Quaternion.Euler(cachedSlopeAngle, 0f, 0f);

        GameObject complexObject = Instantiate(targetObstacle.prefab, spawnPosition, slopeRotation, rowParent);
        complexObject.name = $"{targetObstacle.name}_Row{rowIndex}";

        FallingTreeObstacle treeComponent = complexObject.GetComponent<FallingTreeObstacle>();
        if (treeComponent != null)
        {
            treeComponent.Initialize(rowIndex, basePosition, laneOffsets, cachedSlopeAngle, this, targetObstacle.triggerDistance);
        }

        IceArchObstacle iceArchComponent = complexObject.GetComponent<IceArchObstacle>();
        if (iceArchComponent != null)
        {
            iceArchComponent.Initialize(rowIndex, basePosition, laneOffsets, cachedSlopeAngle, this, targetObstacle.triggerDistance);
        }

        PenguinMeteorObstacle penguinComponent = complexObject.GetComponent<PenguinMeteorObstacle>();
        if (penguinComponent != null)
        {
            penguinComponent.Initialize(rowIndex, basePosition, laneOffsets, cachedSlopeAngle, this, targetObstacle.triggerDistance);
        }

        spawnedComplexObstacles.Add(complexObject.transform);
    }

    private void SpawnNormalRow(Vector3 basePosition, int rowIndex, Transform rowParent)
    {
        if (obstacleRows == null || obstacleRows.Length == 0)
        {
            return;
        }

        ObstacleRow selectedRow = obstacleRows[Random.Range(0, obstacleRows.Length)];
        int[] lanePattern = selectedRow.GetLanePattern();

        for (int lane = 0; lane < Mathf.Min(laneOffsets.Length, lanePattern.Length); lane++)
        {
            int obstacleIndex = lanePattern[lane];

            if (obstacleIndex == 0) continue;

            if (obstacleIndex < 0 || obstacleIndex >= obstaclePrefabs.Length)
            {
                continue;
            }

            GameObject prefab = obstaclePrefabs[obstacleIndex];
            if (prefab == null)
            {
                continue;
            }

            Vector3 spawnPosition = basePosition + new Vector3(laneOffsets[lane], 0f, 0f);
            Quaternion slopeRotation = Quaternion.Euler(cachedSlopeAngle, 0f, 0f);
            GameObject spawnedObstacle = Instantiate(prefab, spawnPosition, slopeRotation, rowParent);
            spawnedObstacle.name = $"{prefab.name}_Row{rowIndex}_Lane{lane}";

            spawnedObstacles.Add(spawnedObstacle.transform);
        }
    }

    public Vector3 GetRowPosition(int rowIndex)
    {
        Vector3 direction = (slopeEnd.position - slopeStart.position).normalized;
        float totalDistance = Vector3.Distance(slopeStart.position, slopeEnd.position);
        float spacing = totalDistance / (numberOfRows + 1);
        return slopeStart.position + direction * (spacing * rowIndex);
    }

    public void ClearAllObstacles()
    {
        foreach (Transform obstacle in spawnedObstacles)
        {
            if (obstacle != null)
            {
                DestroyImmediate(obstacle.gameObject);
            }
        }
        spawnedObstacles.Clear();

        foreach (Transform obstacle in spawnedComplexObstacles)
        {
            if (obstacle != null)
            {
                DestroyImmediate(obstacle.gameObject);
            }
        }
        spawnedComplexObstacles.Clear();

        rowZPositions.Clear();
        complexObstacleRowIndices.Clear();
    }
}