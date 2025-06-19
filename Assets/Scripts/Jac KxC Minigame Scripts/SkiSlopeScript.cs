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

public class SkiSlopeScript : MonoBehaviour
{
    [Header("Slope Points")]
    [SerializeField] private Transform slopeStart;
    [SerializeField] private Transform slopeEnd;

    [Header("Lane Configuration")]
    [SerializeField] private float[] laneOffsets = new float[] { -2f, 0f, 2f };

    [Header("Obstacle Prefabs & Shit")]
    [Tooltip("please keep index 0 for empty space only")]
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Positioning")]
    [SerializeField] private bool autoAdjustHeight = true;
    [Tooltip("Additional Y offset from calculated ground position")]
    [SerializeField] private float groundOffset = 0f;
    [Tooltip("Use raycast to find ground (more accurate)")]
    [SerializeField] private bool useRaycastGrounding = false;
    [SerializeField] private LayerMask groundLayerMask = -1;
    [Tooltip("Auto-adjust for pivot point in center of object")]
    [SerializeField] private bool compensateForCenterPivot = true;

    [Header("Slope Rotation")]
    [SerializeField] private bool autoDetectSlopeAngle = true;
    [Tooltip("Manual slope angle in degrees (X rotation)")]
    [SerializeField] private float manualSlopeAngle = 0f;
    [Tooltip("GameObject with the slope surface (for auto-detection)")]
    [SerializeField] private GameObject slopeObject;

    [Header("Premade Row Patterns")]
    [SerializeField] private ObstacleRow[] obstacleRows;

    [Header("Spawn Configuration")]
    [SerializeField] private int numberOfRows = 15;
    [SerializeField] private float rowSpacing = 8f;
    [SerializeField] private bool useFixedSpacing = false;

    [Header("Dynamic Cleanup + Debug")]
    [SerializeField] private bool enableDynamicCleanup = true;
    [SerializeField] private int rowsToKeepBehindLastPlayer = 2;
    [SerializeField] private float cleanupCheckInterval = 1f;
    [SerializeField] private bool showDebugInfo = false;

    private List<Transform> spawnedObstacles = new List<Transform>();
    private List<float> rowZPositions = new List<float>();
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
        if (autoDetectSlopeAngle)
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
            else if (slopeStart != null && slopeEnd != null)
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
        else
        {
            cachedSlopeAngle = manualSlopeAngle;

            if (showDebugInfo)
            {
                Debug.Log($"Using manual slope angle: {cachedSlopeAngle}°");
            }
        }
    }

    private void SpawnAllRows()
    {
        Vector3 direction = (slopeEnd.position - slopeStart.position).normalized;
        float totalDistance = Vector3.Distance(slopeStart.position, slopeEnd.position);

        float spacing;
        if (useFixedSpacing)
        {
            spacing = rowSpacing;
        }
        else
        {
            spacing = totalDistance / (numberOfRows + 1);
        }

        for (int row = 1; row <= numberOfRows; row++)
        {
            Vector3 basePosition = slopeStart.position + direction * (spacing * row);
            SpawnRow(basePosition, row);
            rowZPositions.Add(basePosition.z);
        }
    }

    private void SpawnRow(Vector3 basePosition, int rowIndex)
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
            GameObject spawnedObstacle = Instantiate(prefab, spawnPosition, slopeRotation);
            spawnedObstacle.name = $"{prefab.name}_Row{rowIndex}_Lane{lane}";

            AdjustObstacleHeight(spawnedObstacle, basePosition.y);

            spawnedObstacles.Add(spawnedObstacle.transform);
        }
    }

    private void AdjustObstacleHeight(GameObject obstacle, float baseY)
    {
        if (!autoAdjustHeight)
        {
            obstacle.transform.position += Vector3.up * groundOffset;
            return;
        }

        float finalGroundY = baseY;

        if (useRaycastGrounding)
        {
            Vector3 rayStart = obstacle.transform.position + Vector3.up * 10f;     
            Ray ray = new Ray(rayStart, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, 20f, groundLayerMask))
            {
                finalGroundY = hit.point.y;

                if (showDebugInfo)
                {
                    Debug.Log($"Raycast found ground for {obstacle.name} at Y: {hit.point.y}");
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Raycast failed for {obstacle.name}, using base position");
                }
            }
        }

        float pivotCompensation = 0f;
        if (compensateForCenterPivot)
        {
            Renderer renderer = obstacle.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = obstacle.GetComponentInChildren<Renderer>();
            }

            if (renderer != null)
            {
                Vector3 currentPos = obstacle.transform.position;
                Bounds bounds = renderer.bounds;

                float distanceToBottom = currentPos.y - bounds.min.y;
                pivotCompensation = distanceToBottom;

                if (showDebugInfo)
                {
                    Debug.Log($"Pivot compensation for {obstacle.name}: {pivotCompensation} (bounds bottom: {bounds.min.y}, pivot: {currentPos.y})");
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"No renderer found for {obstacle.name}, cannot compensate for pivot");
                }
            }
        }

        obstacle.transform.position = new Vector3(
            obstacle.transform.position.x,
            finalGroundY + pivotCompensation + groundOffset,
            obstacle.transform.position.z
        );

        if (showDebugInfo)
        {
            Debug.Log($"Final position for {obstacle.name}: Y = {finalGroundY} + {pivotCompensation} + {groundOffset} = {finalGroundY + pivotCompensation + groundOffset}");
        }
    }

    private void PerformDynamicCleanup()
    {
        if (progressTracker == null || spawnedObstacles.Count == 0) return;

        float player1Progress = progressTracker.GetPlayerProgress(1);
        float player2Progress = progressTracker.GetPlayerProgress(2);
        float trailingProgress = Mathf.Min(player1Progress, player2Progress);

        float startZ = slopeStart.position.z;
        float endZ = slopeEnd.position.z;
        float trailingPlayerZ = Mathf.Lerp(startZ, endZ, trailingProgress);

        Vector3 direction = (slopeEnd.position - slopeStart.position).normalized;
        float rowSpacingActual = useFixedSpacing ? rowSpacing : Vector3.Distance(slopeStart.position, slopeEnd.position) / (numberOfRows + 1);
        float cleanupThreshold = trailingPlayerZ - (rowsToKeepBehindLastPlayer * rowSpacingActual * Mathf.Sign(direction.z));

        List<Transform> obstaclesToRemove = new List<Transform>();
        foreach (Transform obstacle in spawnedObstacles)
        {
            if (obstacle == null) continue;

            bool shouldRemove = false;
            if (direction.z > 0)
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
                spawnedObstacles.Remove(obstacle);
                DestroyImmediate(obstacle.gameObject);
            }
        }

        if (showDebugInfo && obstaclesToRemove.Count > 0)
        {
            Debug.Log($"SkiSlopeSpawner: Cleaned up {obstaclesToRemove.Count} obstacles. {spawnedObstacles.Count} remaining.");
        }
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
        rowZPositions.Clear();
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
            Gizmos.color = Color.green;
            foreach (float zPos in rowZPositions)
            {
                Vector3 rowCenter = new Vector3(slopeStart.position.x, slopeStart.position.y, zPos);
                Gizmos.DrawWireSphere(rowCenter, 0.5f);
            }
        }
    }
}