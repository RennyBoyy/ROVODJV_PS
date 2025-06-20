using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObstacleRow
{
    [Tooltip("pattern for the row e.g. 1,0,2, where 0 is an empty field. always")]
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
    [Header("Basic Info")]
    public string name = "Complex Obstacle";
    public GameObject prefab;
    [Tooltip("Which row this obstacle should spawn on (1-based)")]
    public int targetRow = 15;
    [Tooltip("How many rows before the obstacle should the trigger activate")]
    public float triggerDistance = 2f;
}

public class SkiSlopeScript : MonoBehaviour
{
    [Header("Slope Points")]
    [SerializeField] private Transform slopeStart;
    [SerializeField] private Transform slopeEnd;

    [Header("Lane Configuration")]
    [SerializeField] private float[] laneOffsets = new float[] { -2f, 0f, 2f };

    [Header("Slope Rotation")]
    [Tooltip("GameObject with the slope surface (for auto-detection)")]
    [SerializeField] private GameObject slopeObject;

    [Header("Normal Obstacle Prefabs")]
    [Tooltip("please keep index 0 for empty space only")]
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Normal Row Patterns")]
    [SerializeField] private ObstacleRow[] obstacleRows;

    [Header("Complex Obstacles")]
    [SerializeField] private ComplexObstacle[] complexObstacles;

    [Header("Spawn Configuration")]
    [SerializeField] private int numberOfRows = 30;

    [Header("Dynamic Cleanup + Debug")]
    [SerializeField] private bool enableDynamicCleanup = true;
    [SerializeField] private int rowsToKeepBehindLastPlayer = 2;
    [SerializeField] private float cleanupCheckInterval = 1f;
    [SerializeField] private bool showDebugInfo = false;

    private List<Transform> spawnedObstacles = new List<Transform>();
    private List<Transform> spawnedComplexObstacles = new List<Transform>();
    private List<float> rowZPositions = new List<float>();
    private List<int> complexObstacleRowIndices = new List<int>();
    private SkiProgressTracker progressTracker;
    private float lastCleanupCheck = 0f;
    private float cachedSlopeAngle = 0f;

    private void Start()
    {
        ValidateSetup();
        CalculateSlopeAngle();
        SpawnAllRows();

        if (enableDynamicCleanup)
        {
            progressTracker = FindFirstObjectByType<SkiProgressTracker>();
            if (progressTracker == null)
            {
                Debug.LogWarning("No tracker found -- dynamic cleanup disabled.");
                enableDynamicCleanup = false;
            }
        }
    }

    private void Update()
    {
        if (enableDynamicCleanup && Time.time - lastCleanupCheck >= cleanupCheckInterval)
        {
            PerformDynamicCleanup();
            lastCleanupCheck = Time.time;
        }
    }

    private void ValidateSetup()
    {
        if (slopeStart == null || slopeEnd == null)
        {
            return;
        }

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            return;
        }

        if (laneOffsets == null || laneOffsets.Length == 0)
        {
            return;
        }
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

            if (showDebugInfo)
            {
                Debug.Log($"Auto-detected slope angle: {cachedSlopeAngle}° from slope object '{slopeObject.name}'");
            }
        }
        else
        {
            if (slopeStart != null && slopeEnd != null)
            {
                Vector3 slopeDirection = (slopeEnd.position - slopeStart.position).normalized;
                cachedSlopeAngle = Mathf.Asin(slopeDirection.y) * Mathf.Rad2Deg;

                if (showDebugInfo)
                {
                    Debug.Log($"Auto-calculated slope angle: {cachedSlopeAngle}° from start/end points");
                }
            }
            else
            {
                cachedSlopeAngle = 0f;
                Debug.LogWarning("Cannot auto-detect slope angle: no slope object or start/end points assigned");
            }
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
            Debug.LogWarning($"No complex obstacle found for row {rowIndex}, spawning normal row instead");
            SpawnNormalRow(basePosition, rowIndex, rowParent);
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Row {rowIndex}: Spawning complex obstacle '{targetObstacle.name}'");
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

        spawnedComplexObstacles.Add(complexObject.transform);
    }

    private void SpawnNormalRow(Vector3 basePosition, int rowIndex, Transform rowParent)
    {
        if (obstacleRows == null || obstacleRows.Length == 0)
        {
            Debug.LogError("no obstacle rows created");
            return;
        }

        ObstacleRow selectedRow = obstacleRows[Random.Range(0, obstacleRows.Length)];
        int[] lanePattern = selectedRow.GetLanePattern();

        if (showDebugInfo)
        {
            Debug.Log($"Row {rowIndex}: Using pattern '{selectedRow.rowName}' - {selectedRow.pattern} -> [{string.Join(", ", lanePattern)}]");
        }

        for (int lane = 0; lane < Mathf.Min(laneOffsets.Length, lanePattern.Length); lane++)
        {
            int obstacleIndex = lanePattern[lane];

            if (obstacleIndex == 0) continue;

            if (obstacleIndex < 0 || obstacleIndex >= obstaclePrefabs.Length)
            {
                Debug.LogWarning($"invalid obstacle index {obstacleIndex} in pattern '{selectedRow.pattern}'");
                continue;
            }

            GameObject prefab = obstaclePrefabs[obstacleIndex];
            if (prefab == null)
            {
                Debug.LogWarning($"Obstacle prefab at index {obstacleIndex} is null. Skipping.");
                continue;
            }

            Vector3 spawnPosition = basePosition + new Vector3(laneOffsets[lane], 0f, 0f);
            Quaternion slopeRotation = Quaternion.Euler(cachedSlopeAngle, 0f, 0f);
            GameObject spawnedObstacle = Instantiate(prefab, spawnPosition, slopeRotation, rowParent);
            spawnedObstacle.name = $"{prefab.name}_Row{rowIndex}_Lane{lane}";

            spawnedObstacles.Add(spawnedObstacle.transform);
        }
    }

    private void PerformDynamicCleanup()
    {
        if (progressTracker == null) return;

        float player1Progress = progressTracker.GetPlayerProgress(1);
        float player2Progress = progressTracker.GetPlayerProgress(2);
        float trailingProgress = Mathf.Min(player1Progress, player2Progress);

        float startZ = slopeStart.position.z;
        float endZ = slopeEnd.position.z;
        float trailingPlayerZ = Mathf.Lerp(startZ, endZ, trailingProgress);

        Vector3 direction = (slopeEnd.position - slopeStart.position).normalized;
        float rowSpacingActual = Vector3.Distance(slopeStart.position, slopeEnd.position) / (numberOfRows + 1);
        float cleanupThreshold = trailingPlayerZ - (rowsToKeepBehindLastPlayer * rowSpacingActual * Mathf.Sign(direction.z));

        CleanupObstacleList(spawnedObstacles, cleanupThreshold, direction.z, "normal");
        CleanupObstacleList(spawnedComplexObstacles, cleanupThreshold, direction.z, "complex");
    }

    private void CleanupObstacleList(List<Transform> obstacleList, float cleanupThreshold, float directionZ, string type)
    {
        List<Transform> obstaclesToRemove = new List<Transform>();

        foreach (Transform obstacle in obstacleList)
        {
            if (obstacle == null) continue;

            bool shouldRemove = false;
            if (directionZ > 0)
            {
                shouldRemove = obstacle.position.z < cleanupThreshold;
            }
            else
            {
                shouldRemove = obstacle.position.z > cleanupThreshold;
            }

            if (shouldRemove)
            {
                obstaclesToRemove.Add(obstacle);
            }
        }

        foreach (Transform obstacle in obstaclesToRemove)
        {
            if (obstacle != null)
            {
                obstacleList.Remove(obstacle);
                DestroyImmediate(obstacle.gameObject);
            }
        }

        if (showDebugInfo && obstaclesToRemove.Count > 0)
        {
            Debug.Log($"Cleaned up {obstaclesToRemove.Count} {type} obstacles. {obstacleList.Count} remaining.");
        }
    }

    public Vector3 GetRowPosition(int rowIndex)
    {
        Vector3 direction = (slopeEnd.position - slopeStart.position).normalized;
        float totalDistance = Vector3.Distance(slopeStart.position, slopeEnd.position);
        float spacing = totalDistance / (numberOfRows + 1);
        return slopeStart.position + direction * (spacing * rowIndex);
    }

    public void AddCustomRow(string pattern, string name = "Custom")
    {
        ObstacleRow newRow = new ObstacleRow
        {
            pattern = pattern,
            rowName = name
        };

        System.Array.Resize(ref obstacleRows, obstacleRows.Length + 1);
        obstacleRows[obstacleRows.Length - 1] = newRow;
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

    public void RespawnAllRows()
    {
        ClearAllObstacles();
        SpawnAllRows();
    }

    [ContextMenu("Respawn Obstacles")]
    public void DebugRespawn()
    {
        RespawnAllRows();
    }

    private void OnDrawGizmosSelected()
    {
        if (slopeStart == null || slopeEnd == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(slopeStart.position, slopeEnd.position);

        Vector3 direction = (slopeEnd.position - slopeStart.position).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < laneOffsets.Length; i++)
        {
            Vector3 laneStart = slopeStart.position + perpendicular * laneOffsets[i];
            Vector3 laneEnd = slopeEnd.position + perpendicular * laneOffsets[i];
            Gizmos.DrawLine(laneStart, laneEnd);
        }

        if (Application.isPlaying && rowZPositions.Count > 0)
        {
            for (int i = 0; i < rowZPositions.Count; i++)
            {
                bool isComplexRow = complexObstacleRowIndices.Contains(i + 1);
                Gizmos.color = isComplexRow ? Color.red : Color.green;

                Vector3 rowCenter = new Vector3(slopeStart.position.x, slopeStart.position.y, rowZPositions[i]);
                Gizmos.DrawWireSphere(rowCenter, isComplexRow ? 1f : 0.5f);
            }
        }
    }
}