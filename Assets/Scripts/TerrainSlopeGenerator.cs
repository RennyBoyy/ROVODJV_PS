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

        GenerateSlopeHeightmap(terrainData);

        Debug.Log($"Slope generated successfully! Min Height: {minHeight}, Max Height: {maxHeight}, Direction: {direction}");

#if UNITY_EDITOR
        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(targetTerrain);
#endif
    }

    private void GenerateSlopeHeightmap(TerrainData terrainData)
    {
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];

        Vector3 terrainSize = terrainData.size;
        float normalizedMinHeight = minHeight / terrainSize.y;
        float normalizedMaxHeight = maxHeight / terrainSize.y;

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
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];

        float normalizedHeight = minHeight / terrainData.size.y;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                heights[x, z] = normalizedHeight;
            }
        }

        terrainData.SetHeights(0, 0, heights);

#if UNITY_EDITOR
        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(targetTerrain);
#endif

        Debug.Log("Terrain flattened!");
    }

    public float GetActualSlopeAngle()
    {
        if (targetTerrain == null) return 0f;

        Vector3 terrainSize = targetTerrain.terrainData.size;
        float slopeDistance = CalculateSlopeDistance(terrainSize);
        float heightDifference = Mathf.Abs(maxHeight - minHeight);

        return Mathf.Atan(heightDifference / slopeDistance) * Mathf.Rad2Deg;
    }

    public void AlignWithTrack(Transform trackStart, Transform trackEnd)
    {
        if (trackStart == null || trackEnd == null)
        {
            Debug.LogError("Track start and end points must be assigned!");
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
        float calculatedAngle = Mathf.Atan(heightDifference / trackDistance) * Mathf.Rad2Deg;

        Debug.Log($"Terrain aligned with track. Direction: {direction}, Min Height: {minHeight}, Max Height: {maxHeight}, Calculated Angle: {calculatedAngle:F1}°");

        GenerateSlope();
    }
}