using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class PlaceableObject
{
    [Header("Object Settings")]
    public GameObject prefab;
    public string name;

    [Header("Placement Behavior")]
    public bool alignToTerrain = true;
    public bool randomYRotation = true;
    [Range(-180f, 180f)]
    public float rotationOffset = 0f;

    [Header("Scale Variation")]
    public bool randomScale = false;
    [Range(0.1f, 3f)]
    public float minScale = 0.8f;
    [Range(0.1f, 3f)]
    public float maxScale = 1.2f;

    [Header("Constraints")]
    [Range(0f, 90f)]
    public float maxSlope = 45f;
    public bool showSlopeWarning = true;
}

public class TerrainManualPlacer : MonoBehaviour
{
    [Header("Terrain Reference")]
    [SerializeField] private Terrain targetTerrain;

    [Header("Manual Placement")]
    [SerializeField] private List<PlaceableObject> placeableObjects = new List<PlaceableObject>();
    [SerializeField] private int selectedObjectIndex = 0;

    [Header("Placement Settings")]
    [SerializeField] private LayerMask terrainLayerMask = -1;
    [SerializeField] private bool enableClickPlacement = true;
    [SerializeField] private KeyCode placementModifier = KeyCode.LeftShift;

    [Header("Organization")]
    [SerializeField] private bool createParentContainer = true;
    [SerializeField] private string containerName = "Manually Placed Objects";

    [Header("Visual Feedback")]
    [SerializeField] private bool showPlacementPreview = true;
    [SerializeField] private Color previewColor = Color.green;
    [SerializeField] private Color invalidPreviewColor = Color.red;

    private Transform objectContainer;
    private List<GameObject> placedObjects = new List<GameObject>();
    private Vector3 lastMousePosition;
    private TerrainInfo currentTerrainInfo;
    private bool isValidPlacement;

#if UNITY_EDITOR
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!enableClickPlacement || placeableObjects == null || placeableObjects.Count == 0)
            return;

        Event currentEvent = Event.current;

        UpdateMouseTerrainInfo();

        if (currentEvent.type == EventType.MouseDown &&
            currentEvent.button == 0 &&
            (placementModifier == KeyCode.None || currentEvent.modifiers == EventModifiers.Shift))
        {
            if (isValidPlacement && GetCurrentPlaceableObject() != null)
            {
                PlaceObjectAtCurrentPosition();
                currentEvent.Use();
            }
        }

        if (showPlacementPreview && isValidPlacement)
        {
            DrawPlacementPreview();
        }

        if (currentEvent.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }
    }

    private void UpdateMouseTerrainInfo()
    {
        Vector2 mousePosition = Event.current.mousePosition;

        Camera sceneCamera = SceneView.lastActiveSceneView?.camera;
        if (sceneCamera == null) return;

        mousePosition.y = sceneCamera.pixelHeight - mousePosition.y;
        Ray ray = sceneCamera.ScreenPointToRay(mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, terrainLayerMask))
        {
            if (hit.collider.GetComponent<Terrain>() == targetTerrain)
            {
                currentTerrainInfo = GetTerrainInfoAtPosition(hit.point);
                isValidPlacement = ValidatePlacement(currentTerrainInfo);
                lastMousePosition = hit.point;
            }
            else
            {
                isValidPlacement = false;
            }
        }
        else
        {
            isValidPlacement = false;
        }
    }

    private void DrawPlacementPreview()
    {
        PlaceableObject currentObject = GetCurrentPlaceableObject();
        if (currentObject?.prefab == null) return;

        Color handleColor = isValidPlacement ? previewColor : invalidPreviewColor;
        Handles.color = handleColor;

        Handles.SphereHandleCap(0, currentTerrainInfo.worldPosition, Quaternion.identity, 0.5f, EventType.Repaint);

        Handles.DrawLine(currentTerrainInfo.worldPosition,
                        currentTerrainInfo.worldPosition + currentTerrainInfo.normal * 2f);

        Vector3 labelPosition = currentTerrainInfo.worldPosition + Vector3.up * 1f;
        string infoText = $"Slope: {currentTerrainInfo.slope:F1}°";

        if (!isValidPlacement && currentObject.showSlopeWarning)
        {
            infoText += $"\nMax: {currentObject.maxSlope:F1}°";
        }

        Handles.Label(labelPosition, infoText);

        Handles.color = Color.white;
    }
#endif

    private void OnValidate()
    {
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
        }

        if (placeableObjects != null && placeableObjects.Count > 0)
        {
            selectedObjectIndex = Mathf.Clamp(selectedObjectIndex, 0, placeableObjects.Count - 1);
        }
    }

    private PlaceableObject GetCurrentPlaceableObject()
    {
        if (placeableObjects == null || placeableObjects.Count == 0 ||
            selectedObjectIndex < 0 || selectedObjectIndex >= placeableObjects.Count)
            return null;

        return placeableObjects[selectedObjectIndex];
    }

    private TerrainInfo GetTerrainInfoAtPosition(Vector3 worldPos)
    {
        TerrainInfo info = new TerrainInfo();

        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            info.isValid = false;
            return info;
        }

        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainPosition = targetTerrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        float relativeX = (worldPos.x - terrainPosition.x) / terrainSize.x;
        float relativeZ = (worldPos.z - terrainPosition.z) / terrainSize.z;

        if (relativeX < 0 || relativeX > 1 || relativeZ < 0 || relativeZ > 1)
        {
            info.isValid = false;
            return info;
        }

        info.height = terrainData.GetInterpolatedHeight(relativeX, relativeZ);
        info.worldPosition = new Vector3(worldPos.x, terrainPosition.y + info.height, worldPos.z);

        Vector3 terrainNormal = terrainData.GetInterpolatedNormal(relativeX, relativeZ);
        info.normal = terrainNormal;
        info.slope = Vector3.Angle(Vector3.up, terrainNormal);

        info.isValid = true;
        return info;
    }

    private bool ValidatePlacement(TerrainInfo terrainInfo)
    {
        if (!terrainInfo.isValid) return false;

        PlaceableObject currentObject = GetCurrentPlaceableObject();
        if (currentObject?.prefab == null) return false;

        if (terrainInfo.slope > currentObject.maxSlope) return false;

        return true;
    }

    private void PlaceObjectAtCurrentPosition()
    {
        PlaceableObject currentObject = GetCurrentPlaceableObject();
        if (currentObject?.prefab == null) return;

        SetupObjectContainer();
        PlaceObject(currentObject, currentTerrainInfo);
        SavePlacementChanges();

        Debug.Log($"Placed {currentObject.name} at {currentTerrainInfo.worldPosition}");
    }

    private void PlaceObject(PlaceableObject placeableObject, TerrainInfo terrainInfo)
    {
        GameObject newObject = Instantiate(placeableObject.prefab, terrainInfo.worldPosition, Quaternion.identity);

        if (objectContainer != null)
        {
            newObject.transform.SetParent(objectContainer);
        }

        if (placeableObject.alignToTerrain)
        {
            newObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, terrainInfo.normal);
        }

        if (placeableObject.randomYRotation)
        {
            float randomY = Random.Range(0f, 360f) + placeableObject.rotationOffset;
            newObject.transform.Rotate(0, randomY, 0, Space.Self);
        }
        else if (placeableObject.rotationOffset != 0f)
        {
            newObject.transform.Rotate(0, placeableObject.rotationOffset, 0, Space.Self);
        }

        if (placeableObject.randomScale)
        {
            float scale = Random.Range(placeableObject.minScale, placeableObject.maxScale);
            newObject.transform.localScale *= scale;
        }

        placedObjects.Add(newObject);

#if UNITY_EDITOR
        EditorUtility.SetDirty(newObject);
        if (newObject.transform.parent != null)
        {
            EditorUtility.SetDirty(newObject.transform.parent.gameObject);
        }
#endif
    }

    private void SetupObjectContainer()
    {
        if (createParentContainer)
        {
            GameObject container = GameObject.Find(containerName);
            if (container == null)
            {
                container = new GameObject(containerName);
#if UNITY_EDITOR
                EditorUtility.SetDirty(container);
#endif
            }
            objectContainer = container.transform;
        }
    }

    private void SavePlacementChanges()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);

        if (objectContainer != null)
        {
            EditorUtility.SetDirty(objectContainer.gameObject);
        }

        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
            }
        }

        if (Application.isPlaying)
        {
            SaveObjectsToSceneInPlayMode();
        }
        else
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }
#endif
    }

#if UNITY_EDITOR
    private void SaveObjectsToSceneInPlayMode()
    {
        if (placedObjects.Count > 0)
        {
            GameObject lastPlaced = placedObjects[placedObjects.Count - 1];
            if (lastPlaced != null)
            {
                PlacedObjectData objectData = new PlacedObjectData
                {
                    prefabPath = AssetDatabase.GetAssetPath(GetCurrentPlaceableObject().prefab),
                    position = lastPlaced.transform.position,
                    rotation = lastPlaced.transform.rotation,
                    scale = lastPlaced.transform.localScale,
                    parentName = objectContainer != null ? objectContainer.name : ""
                };

                SaveObjectDataToPrefs(objectData);

                Debug.Log($"Saved object data for: {lastPlaced.name} at {lastPlaced.transform.position}");
            }
        }
    }

    private void SaveObjectDataToPrefs(PlacedObjectData data)
    {
        int count = EditorPrefs.GetInt("TerrainPlacer_ObjectCount", 0);

        string prefix = $"TerrainPlacer_Object_{count}_";
        EditorPrefs.SetString(prefix + "prefabPath", data.prefabPath);
        EditorPrefs.SetFloat(prefix + "posX", data.position.x);
        EditorPrefs.SetFloat(prefix + "posY", data.position.y);
        EditorPrefs.SetFloat(prefix + "posZ", data.position.z);
        EditorPrefs.SetFloat(prefix + "rotX", data.rotation.x);
        EditorPrefs.SetFloat(prefix + "rotY", data.rotation.y);
        EditorPrefs.SetFloat(prefix + "rotZ", data.rotation.z);
        EditorPrefs.SetFloat(prefix + "rotW", data.rotation.w);
        EditorPrefs.SetFloat(prefix + "scaleX", data.scale.x);
        EditorPrefs.SetFloat(prefix + "scaleY", data.scale.y);
        EditorPrefs.SetFloat(prefix + "scaleZ", data.scale.z);
        EditorPrefs.SetString(prefix + "parentName", data.parentName);

        EditorPrefs.SetInt("TerrainPlacer_ObjectCount", count + 1);
    }

    [ContextMenu("Restore Objects from Play Mode")]
    public void RestoreObjectsFromPlayMode()
    {
        int count = EditorPrefs.GetInt("TerrainPlacer_ObjectCount", 0);
        if (count == 0)
        {
            Debug.Log("No objects to restore from play mode.");
            return;
        }

        SetupObjectContainer();
        int restored = 0;

        for (int i = 0; i < count; i++)
        {
            string prefix = $"TerrainPlacer_Object_{i}_";
            string prefabPath = EditorPrefs.GetString(prefix + "prefabPath", "");

            if (!string.IsNullOrEmpty(prefabPath))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    Vector3 position = new Vector3(
                        EditorPrefs.GetFloat(prefix + "posX", 0),
                        EditorPrefs.GetFloat(prefix + "posY", 0),
                        EditorPrefs.GetFloat(prefix + "posZ", 0)
                    );

                    Quaternion rotation = new Quaternion(
                        EditorPrefs.GetFloat(prefix + "rotX", 0),
                        EditorPrefs.GetFloat(prefix + "rotY", 0),
                        EditorPrefs.GetFloat(prefix + "rotZ", 0),
                        EditorPrefs.GetFloat(prefix + "rotW", 1)
                    );

                    Vector3 scale = new Vector3(
                        EditorPrefs.GetFloat(prefix + "scaleX", 1),
                        EditorPrefs.GetFloat(prefix + "scaleY", 1),
                        EditorPrefs.GetFloat(prefix + "scaleZ", 1)
                    );

                    GameObject newObj = Instantiate(prefab, position, rotation);
                    newObj.transform.localScale = scale;

                    if (objectContainer != null)
                    {
                        newObj.transform.SetParent(objectContainer);
                    }

                    EditorUtility.SetDirty(newObj);
                    restored++;
                }
            }
        }

        ClearSavedObjectData();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log($"Restored {restored} objects from play mode and saved to scene!");
    }

    [ContextMenu("Clear Saved Object Data")]
    public void ClearSavedObjectData()
    {
        int count = EditorPrefs.GetInt("TerrainPlacer_ObjectCount", 0);

        for (int i = 0; i < count; i++)
        {
            string prefix = $"TerrainPlacer_Object_{i}_";
            EditorPrefs.DeleteKey(prefix + "prefabPath");
            EditorPrefs.DeleteKey(prefix + "posX");
            EditorPrefs.DeleteKey(prefix + "posY");
            EditorPrefs.DeleteKey(prefix + "posZ");
            EditorPrefs.DeleteKey(prefix + "rotX");
            EditorPrefs.DeleteKey(prefix + "rotY");
            EditorPrefs.DeleteKey(prefix + "rotZ");
            EditorPrefs.DeleteKey(prefix + "rotW");
            EditorPrefs.DeleteKey(prefix + "scaleX");
            EditorPrefs.DeleteKey(prefix + "scaleY");
            EditorPrefs.DeleteKey(prefix + "scaleZ");
            EditorPrefs.DeleteKey(prefix + "parentName");
        }

        EditorPrefs.DeleteKey("TerrainPlacer_ObjectCount");
        Debug.Log("Cleared all saved object data.");
    }
#endif

    [System.Serializable]
    public struct PlacedObjectData
    {
        public string prefabPath;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public string parentName;
    }

    [ContextMenu("Clear All Placed Objects")]
    public void ClearAllPlacedObjects()
    {
        for (int i = placedObjects.Count - 1; i >= 0; i--)
        {
            if (placedObjects[i] != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Destroy(placedObjects[i]);
                }
                else
                {
                    DestroyImmediate(placedObjects[i]);
                }
#else
                Destroy(placedObjects[i]);
#endif
            }
        }
        placedObjects.Clear();

        if (objectContainer != null)
        {
            for (int i = objectContainer.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Destroy(objectContainer.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(objectContainer.GetChild(i).gameObject);
                }
#else
                Destroy(objectContainer.GetChild(i).gameObject);
#endif
            }
        }

        SavePlacementChanges();
        Debug.Log("All placed objects cleared!");
    }

    [ContextMenu("Validate Current Setup")]
    public void ValidateCurrentSetup()
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

        if (placeableObjects == null || placeableObjects.Count == 0)
        {
            Debug.LogWarning("No placeable objects defined!");
            return;
        }

        int validObjects = 0;
        for (int i = 0; i < placeableObjects.Count; i++)
        {
            PlaceableObject obj = placeableObjects[i];
            if (obj?.prefab != null)
            {
                validObjects++;
                Debug.Log($"Object {i}: Name='{obj.name}', Prefab='{obj.prefab.name}', MaxSlope={obj.maxSlope}°");
            }
            else
            {
                Debug.LogWarning($"Placeable object at index {i}: Name='{obj?.name ?? "null"}', Prefab={(obj?.prefab != null ? obj.prefab.name : "NULL")}");
            }
        }

        PlaceableObject currentObj = GetCurrentPlaceableObject();
        Debug.Log($"Manual placer validation complete! {validObjects}/{placeableObjects.Count} objects have valid prefabs.");
        Debug.Log($"Current selection: {selectedObjectIndex} - Name: '{currentObj?.name ?? "NULL"}', Prefab: {(currentObj?.prefab != null ? currentObj.prefab.name : "NULL")}");
        Debug.Log($"Placement: {(enableClickPlacement ? "Enabled" : "Disabled")}");

        if (enableClickPlacement)
        {
            string modifierText = placementModifier == KeyCode.None ? "Click" : $"{placementModifier} + Click";
            Debug.Log($"Usage: {modifierText} on terrain to place objects");
        }
    }

    public void SetSelectedObject(int index)
    {
        if (placeableObjects != null && index >= 0 && index < placeableObjects.Count)
        {
            selectedObjectIndex = index;
            Debug.Log($"Selected object: {placeableObjects[index]?.name ?? "None"}");
        }
    }

    public void NextObject()
    {
        if (placeableObjects != null && placeableObjects.Count > 0)
        {
            selectedObjectIndex = (selectedObjectIndex + 1) % placeableObjects.Count;
            Debug.Log($"Selected object: {GetCurrentPlaceableObject()?.name ?? "None"}");
        }
    }

    public void PreviousObject()
    {
        if (placeableObjects != null && placeableObjects.Count > 0)
        {
            selectedObjectIndex = selectedObjectIndex - 1;
            if (selectedObjectIndex < 0) selectedObjectIndex = placeableObjects.Count - 1;
            Debug.Log($"Selected object: {GetCurrentPlaceableObject()?.name ?? "None"}");
        }
    }
}

public struct TerrainInfo
{
    public Vector3 worldPosition;
    public Vector3 normal;
    public float height;
    public float slope;
    public bool isValid;
}