using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public enum SlopeDirection
{
    NorthToSouth,
    SouthToNorth,
    EastToWest,
    WestToEast
}

public class TerrainSlopeGenerator : MonoBehaviour
{
    [Header("Slope Settings")]
    [SerializeField] private float minHeight = 0f;
    [SerializeField] private float maxHeight = 50f;
    [Range(0f, 1f)]
    [SerializeField] private float slopeStartPosition = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float slopeEndPosition = 1f;
    [SerializeField] private SlopeDirection direction = SlopeDirection.NorthToSouth;

    [Header("Terrain Reference")]
    [SerializeField] private Terrain targetTerrain;

    [Header("Height Settings")]
    [SerializeField] private AnimationCurve slopeCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Generation")]
    [SerializeField] private bool generateOnStart = false;
    [SerializeField] private bool autoBackup = true;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateSlope();
        }
    }

    private void OnValidate()
    {
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
        }
    }

    [ContextMenu("Generate Slope")]
    public void GenerateSlope()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("No terrain assigned! Please assign a terrain to modify.");
            return;
        }

        TerrainData terrainData = targetTerrain.terrainData;

        if (terrainData == null)
        {
            Debug.LogError("Terrain has no TerrainData!");
            return;
        }

        if (!ValidateTerrainData(terrainData))
        {
            Debug.LogError("Terrain data validation failed!");
            return;
        }

        if (autoBackup)
        {
            BackupTerrain();
        }

        GenerateSlopeHeightmap(terrainData);

        SaveTerrainChanges(terrainData);

        Debug.Log($"Slope generated successfully! Min Height: {minHeight}, Max Height: {maxHeight}, Direction: {direction}");
    }

    private bool ValidateTerrainData(TerrainData terrainData)
    {
        if (terrainData.heightmapResolution <= 0)
        {
            Debug.LogError("Invalid heightmap resolution!");
            return false;
        }

        if (terrainData.size.x <= 0 || terrainData.size.y <= 0 || terrainData.size.z <= 0)
        {
            Debug.LogError("Invalid terrain size!");
            return false;
        }

        if (minHeight >= maxHeight)
        {
            Debug.LogError("Min height must be less than max height!");
            return false;
        }

        if (slopeStartPosition >= slopeEndPosition)
        {
            Debug.LogError("Slope start position must be less than end position!");
            return false;
        }

        return true;
    }

    private void GenerateSlopeHeightmap(TerrainData terrainData)
    {
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];

        Vector3 terrainSize = terrainData.size;
        float normalizedMinHeight = minHeight / terrainSize.y;
        float normalizedMaxHeight = maxHeight / terrainSize.y;

        normalizedMinHeight = Mathf.Clamp01(normalizedMinHeight);
        normalizedMaxHeight = Mathf.Clamp01(normalizedMaxHeight);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float normalizedX = (float)x / (width - 1);
                float normalizedZ = (float)z / (height - 1);

                float slopeProgress = CalculateSlopeProgress(normalizedX, normalizedZ);

                float curveValue = slopeCurve.Evaluate(slopeProgress);

                float finalHeight = Mathf.Lerp(normalizedMinHeight, normalizedMaxHeight, curveValue);

                heights[x, z] = Mathf.Clamp01(finalHeight);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    private void SaveTerrainChanges(TerrainData terrainData)
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(targetTerrain);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Terrain changes saved to disk.");
#endif
    }

    private float CalculateSlopeDistance(Vector3 terrainSize)
    {
        switch (direction)
        {
            case SlopeDirection.NorthToSouth:
            case SlopeDirection.SouthToNorth:
                return terrainSize.z * Mathf.Abs(slopeEndPosition - slopeStartPosition);
            case SlopeDirection.EastToWest:
            case SlopeDirection.WestToEast:
                return terrainSize.x * Mathf.Abs(slopeEndPosition - slopeStartPosition);
            default:
                return terrainSize.z;
        }
    }

    private float CalculateSlopeProgress(float normalizedX, float normalizedZ)
    {
        float progress = 0f;

        switch (direction)
        {
            case SlopeDirection.NorthToSouth:
                progress = normalizedZ;
                break;
            case SlopeDirection.SouthToNorth:
                progress = 1f - normalizedZ;
                break;
            case SlopeDirection.EastToWest:
                progress = normalizedX;
                break;
            case SlopeDirection.WestToEast:
                progress = 1f - normalizedX;
                break;
        }

        progress = Mathf.InverseLerp(slopeStartPosition, slopeEndPosition, progress);
        return Mathf.Clamp01(progress);
    }

    [ContextMenu("Flatten Terrain")]
    public void FlattenTerrain()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("No terrain assigned!");
            return;
        }

        TerrainData terrainData = targetTerrain.terrainData;

        if (!ValidateTerrainData(terrainData))
        {
            Debug.LogError("Terrain data validation failed!");
            return;
        }

        if (autoBackup)
        {
            BackupTerrain();
        }

        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];

        float normalizedHeight = Mathf.Clamp01(minHeight / terrainData.size.y);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                heights[x, z] = normalizedHeight;
            }
        }

        terrainData.SetHeights(0, 0, heights);

        SaveTerrainChanges(terrainData);

        Debug.Log("Terrain flattened!");
    }

    [ContextMenu("Backup Terrain")]
    public void BackupTerrain()
    {
#if UNITY_EDITOR
        if (targetTerrain != null && targetTerrain.terrainData != null)
        {
            string path = AssetDatabase.GetAssetPath(targetTerrain.terrainData);
            if (!string.IsNullOrEmpty(path))
            {
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = path.Replace(".asset", $"_backup_{timestamp}.asset");

                if (AssetDatabase.CopyAsset(path, backupPath))
                {
                    AssetDatabase.Refresh();
                    Debug.Log($"Terrain backed up to: {backupPath}");
                }
                else
                {
                    Debug.LogError("Failed to create terrain backup!");
                }
            }
            else
            {
                Debug.LogWarning("Terrain data is not saved as an asset file. Cannot create backup.");
            }
        }
        else
        {
            Debug.LogError("No terrain or terrain data available for backup!");
        }
#endif
    }

    [ContextMenu("Restore from Backup")]
    public void RestoreFromBackup()
    {
#if UNITY_EDITOR
        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogError("No terrain assigned!");
            return;
        }

        string path = AssetDatabase.GetAssetPath(targetTerrain.terrainData);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Cannot find terrain data asset path!");
            return;
        }

        string directory = System.IO.Path.GetDirectoryName(path);
        string filename = System.IO.Path.GetFileNameWithoutExtension(path);

        string[] backupFiles = System.IO.Directory.GetFiles(directory, filename + "_backup_*.asset");
        if (backupFiles.Length == 0)
        {
            Debug.LogWarning("No backup files found!");
            return;
        }

        System.Array.Sort(backupFiles);
        string mostRecentBackup = backupFiles[backupFiles.Length - 1];

        TerrainData backupData = AssetDatabase.LoadAssetAtPath<TerrainData>(mostRecentBackup);
        if (backupData != null)
        {
            RestoreTerrainData(backupData);
            Debug.Log($"Restored terrain from backup: {mostRecentBackup}");
        }
        else
        {
            Debug.LogError("Failed to load backup terrain data!");
        }
#endif
    }

    [ContextMenu("Restore from Custom TerrainData")]
    public void RestoreFromCustomTerrainData()
    {
#if UNITY_EDITOR
        if (targetTerrain == null)
        {
            Debug.LogError("No terrain assigned!");
            return;
        }

        string path = EditorUtility.OpenFilePanel("Select TerrainData to restore", "Assets", "asset");
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Restoration cancelled.");
            return;
        }

        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }

        TerrainData customData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if (customData != null)
        {
            RestoreTerrainData(customData);
            Debug.Log($"Restored terrain from custom data: {path}");
        }
        else
        {
            Debug.LogError("Failed to load selected TerrainData!");
        }
#endif
    }

    private void RestoreTerrainData(TerrainData newTerrainData)
    {
#if UNITY_EDITOR
        if (targetTerrain == null || newTerrainData == null)
        {
            Debug.LogError("Invalid terrain or terrain data!");
            return;
        }

        TerrainData oldTerrainData = targetTerrain.terrainData;

        targetTerrain.terrainData = newTerrainData;

        targetTerrain.Flush();

        EditorUtility.SetDirty(targetTerrain);
        EditorUtility.SetDirty(newTerrainData);

        SceneView.RepaintAll();

        AssetDatabase.Refresh();

        Debug.Log($"Terrain data restored successfully. Visual mesh should now be updated.");
        Debug.Log($"Old data: {(oldTerrainData != null ? AssetDatabase.GetAssetPath(oldTerrainData) : "None")}");
        Debug.Log($"New data: {AssetDatabase.GetAssetPath(newTerrainData)}");
#endif
    }

    public float GetActualSlopeAngle()
    {
        if (targetTerrain == null) return 0f;

        Vector3 terrainSize = targetTerrain.terrainData.size;
        float slopeDistance = CalculateSlopeDistance(terrainSize);
        float heightDifference = Mathf.Abs(maxHeight - minHeight);

        if (slopeDistance <= 0f) return 0f;

        return Mathf.Atan(heightDifference / slopeDistance) * Mathf.Rad2Deg;
    }

    public void AlignWithTrack(Transform trackStart, Transform trackEnd)
    {
        if (trackStart == null || trackEnd == null)
        {
            Debug.LogError("Track start and end points must be assigned!");
            return;
        }

        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogError("No terrain assigned!");
            return;
        }

        Vector3 trackDirection = (trackEnd.position - trackStart.position).normalized;

        if (Mathf.Abs(trackDirection.z) > Mathf.Abs(trackDirection.x))
        {
            direction = trackDirection.z > 0 ? SlopeDirection.NorthToSouth : SlopeDirection.SouthToNorth;
        }
        else
        {
            direction = trackDirection.x > 0 ? SlopeDirection.EastToWest : SlopeDirection.WestToEast;
        }

        minHeight = Mathf.Min(trackStart.position.y, trackEnd.position.y);
        maxHeight = Mathf.Max(trackStart.position.y, trackEnd.position.y);

        float trackDistance = Vector3.Distance(trackStart.position, trackEnd.position);
        float heightDifference = Mathf.Abs(trackEnd.position.y - trackStart.position.y);

        if (trackDistance > 0f)
        {
            float calculatedAngle = Mathf.Atan(heightDifference / trackDistance) * Mathf.Rad2Deg;
            Debug.Log($"Terrain aligned with track. Direction: {direction}, Min Height: {minHeight}, Max Height: {maxHeight}, Calculated Angle: {calculatedAngle:F1}°");
        }

        GenerateSlope();
    }

    [ContextMenu("Validate Terrain")]
    public void ValidateCurrentTerrain()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("No terrain assigned!");
            return;
        }

        if (targetTerrain.terrainData == null)
        {
            Debug.LogError("Terrain has no TerrainData!");
            return;
        }

        bool isValid = ValidateTerrainData(targetTerrain.terrainData);
        if (isValid)
        {
            Debug.Log("Terrain validation passed!");
            Debug.Log($"Heightmap Resolution: {targetTerrain.terrainData.heightmapResolution}");
            Debug.Log($"Terrain Size: {targetTerrain.terrainData.size}");
            Debug.Log($"Current Slope Angle: {GetActualSlopeAngle():F1}°");
        }
        else
        {
            Debug.LogError("Terrain validation failed!");
        }
    }
}