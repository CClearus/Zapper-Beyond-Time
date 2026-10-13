using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;

public class ObjectPlacementSystem : MonoBehaviour
{
    [Header("References")]
    public GridSystem gridSystem;
    public GridObjectDatabaseSO objectDatabase;
    public ObjectVariantSystem variantSystem;

    [Header("Preview Settings")]
    public Material previewMaterial;
    public Material footprintMaterial;
    public LayerMask groundLayerMask = 1;

    [Header("Preview Colors")]
    public Color validPlacementColor = Color.green;
    public Color invalidPlacementColor = Color.red;
    [Range(0.1f, 1.0f)]
    public float previewAlpha = 0.7f;

    [Header("Input Settings")]
    public KeyCode cancelKey = KeyCode.Escape;

    // Current placement state
    private bool isPlacementMode = false;
    private string currentObjectID = "";
    private GridObjectData currentObjectData;
    private GameObject previewObject;
    private GameObject footprintPlane;
    private GameObject customPlanePreview;
    private Camera playerCamera;

    // Placed objects tracking
    private Dictionary<Vector2Int, PlacedGridObject> placedObjects = new Dictionary<Vector2Int, PlacedGridObject>();
    // Add this after: private Dictionary<Vector2Int, PlacedGridObject> placedObjects = new Dictionary<Vector2Int, PlacedGridObject>();

    // Track fixed placement objects separately
    private List<PlacedGridObject> fixedPlacementObjects = new List<PlacedGridObject>();

    // Events
    public System.Action<PlacedGridObject> OnObjectPlaced;
    public System.Action<PlacedGridObject> OnObjectRemoved;
    public System.Action<bool> OnPlacementModeChanged;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();

        if (gridSystem == null)
            gridSystem = FindFirstObjectByType<GridSystem>();

        if (variantSystem == null)
            variantSystem = GetComponent<ObjectVariantSystem>();

        CreateDefaultPreviewMaterial();
    }

    void CreateDefaultPreviewMaterial()
    {
        if (previewMaterial == null)
        {
            // Try the new custom bloom-resistant shader first
            Shader customShader = Shader.Find("Custom/PreviewObjectNoBloom");
            if (customShader != null && customShader.name != "Hidden/InternalErrorShader")
            {
                previewMaterial = new Material(customShader);
                previewMaterial.SetFloat("_Metallic", 0f);
                previewMaterial.SetFloat("_Smoothness", 0.1f);
            }
            else
            {
                // Try original custom shader
                customShader = Shader.Find("Custom/PreviewMaterial");
                if (customShader != null && customShader.name != "Hidden/InternalErrorShader")
                {
                    previewMaterial = new Material(customShader);
                }
                else
                {
                    // Fallback: Use URP Lit shader with transparency
                    Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader != null)
                    {
                        previewMaterial = new Material(urpShader);
                        // Set to transparent mode
                        previewMaterial.SetFloat("_Surface", 1); // Transparent
                        previewMaterial.SetFloat("_Blend", 0); // Alpha blending
                        previewMaterial.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        previewMaterial.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        previewMaterial.SetFloat("_ZWrite", 0);
                        previewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        previewMaterial.EnableKeyword("_ALPHABLEND_ON");
                        previewMaterial.renderQueue = 3000;
                    }
                    else
                    {
                        // Last resort: Legacy transparent shader
                        previewMaterial = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
                    }
                }
            }
            previewMaterial.color = new Color(1f, 1f, 1f, previewAlpha);
        }
    }

    void Update()
    {
        if (isPlacementMode)
        {
            HandlePlacementInput();
            UpdatePreview();
        }
    }

    void HandlePlacementInput()
    {
        // Cancel placement
        if (Input.GetKeyDown(cancelKey))
        {
            CancelPlacement();
            return;
        }

        // Don't place if over UI
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        // Place object on left click
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceObject();
        }
    }

    void UpdatePreview()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        if (mouseWorldPos == Vector3.zero)
        {
            if (previewObject != null)
                previewObject.SetActive(false);
            if (footprintPlane != null)
                footprintPlane.SetActive(false);
            return;
        }

        // Snap to grid
        Vector3 snappedPos = gridSystem.SnapToGrid(mouseWorldPos);
        Vector2Int gridCoords = gridSystem.GetGridCoordinates(snappedPos);

        // Get rotated size
        Vector2Int effectiveSize = GetEffectiveSize();

        // Check if position is valid
        bool canPlace = CanPlaceObjectAt(gridCoords, currentObjectData, effectiveSize);

        // Update preview object
        UpdatePreviewObject(snappedPos, canPlace, effectiveSize);
    }

    Vector2Int GetEffectiveSize()
    {
        if (currentObjectData == null)
            return Vector2Int.zero;

        // Return the size directly from current variant data
        // Each rotation variant already has its own sizeX/sizeY defined
        return new Vector2Int(currentObjectData.sizeX, currentObjectData.sizeY);
    }


    void UpdatePreviewObject(Vector3 position, bool canPlace, Vector2Int effectiveSize)
    {
        if (previewObject == null && currentObjectData?.prefab != null)
        {
            CreatePreviewObject();
        }

        if (footprintPlane == null && currentObjectData != null)
        {
            CreateFootprintPlane(effectiveSize);
        }

        if (previewObject != null)
        {
            previewObject.SetActive(true);
            previewObject.transform.position = position + currentObjectData.visualOffset;

            // Apply prefab rotation from data
            previewObject.transform.rotation = Quaternion.Euler(currentObjectData.prefabRotation);
        }

        if (footprintPlane != null)
        {
            footprintPlane.SetActive(true);
            // Position footprint at grid position with custom offset
            footprintPlane.transform.position = position + currentObjectData.planeOffset;
            footprintPlane.transform.rotation = Quaternion.identity;

            // Update footprint size if rotation changed
            UpdateFootprintSize(effectiveSize);

            // Update footprint color based on placement validity
            UpdateFootprintColor(canPlace);
        }

        // Update preview object color
        UpdatePreviewObjectColor(canPlace);
    }

    void CreatePreviewObject()
    {
        if (currentObjectData?.prefab != null)
        {
            previewObject = Instantiate(currentObjectData.prefab);

            // Remove any colliders to prevent interference
            Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // Disable any scripts that might interfere
            MonoBehaviour[] scripts = previewObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script != this)
                    script.enabled = false;
            }
        }
    }

    void CreateFootprintPlane(Vector2Int effectiveSize)
    {
        if (currentObjectData == null) return;

        // Check if there's a custom plane prefab
        if (currentObjectData.planePrefab != null)
        {
            // Use custom plane prefab
            customPlanePreview = Instantiate(currentObjectData.planePrefab);
            customPlanePreview.name = "CustomPlanePreview";

            // Remove colliders
            Collider[] colliders = customPlanePreview.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            footprintPlane = customPlanePreview;
        }
        else
        {
            // Create a plane GameObject (default behavior)
            footprintPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            footprintPlane.name = "FootprintPlane";

            // Remove the collider as we don't need it
            Collider planeCollider = footprintPlane.GetComponent<Collider>();
            if (planeCollider != null)
                DestroyImmediate(planeCollider);

            // Scale the plane based on effective size (considering rotation)
            UpdateFootprintSize(effectiveSize);

            // Assign material
            Renderer planeRenderer = footprintPlane.GetComponent<Renderer>();
            if (footprintMaterial != null)
            {
                planeRenderer.material = new Material(footprintMaterial);
            }
            else
            {
                // Create a default material if none assigned
                Material defaultMat = new Material(Shader.Find("Standard"));
                defaultMat.color = Color.white;
                defaultMat.SetFloat("_Metallic", 0f);
                defaultMat.SetFloat("_Glossiness", 0.5f);
                planeRenderer.material = defaultMat;
            }
        }
    }

    void UpdateFootprintSize(Vector2Int effectiveSize)
    {
        if (footprintPlane == null) return;

        // Scale the plane based on effective size (plane is 10x10 units by default)
        float scaleX = effectiveSize.x * 0.1f; // 0.1 because plane is 10 units wide
        float scaleZ = effectiveSize.y * 0.1f; // 0.1 because plane is 10 units long
        footprintPlane.transform.localScale = new Vector3(scaleX, 1, scaleZ);
    }

    void UpdateFootprintColor(bool canPlace)
    {
        if (footprintPlane == null) return;

        // Get all renderers (works for both custom prefabs and default plane)
        Renderer[] renderers = footprintPlane.GetComponentsInChildren<Renderer>();

        Color footprintColor = canPlace ? validPlacementColor : invalidPlacementColor;
        footprintColor.a = 0.5f; // Semi-transparent

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = footprintColor;
            }
        }
    }

    void UpdatePreviewObjectColor(bool canPlace)
    {
        if (previewObject == null) return;

        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();

        // Use green for valid placement, red for invalid
        Color previewColor = canPlace ? validPlacementColor : invalidPlacementColor;
        previewColor.a = previewAlpha;

        foreach (Renderer renderer in renderers)
        {
            if (previewMaterial != null)
            {
                // Create new materials based on preview material
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = new Material(previewMaterial);

                    // Set color properties for different shader types
                    materials[i].color = previewColor;
                    if (materials[i].HasProperty("_Color"))
                        materials[i].SetColor("_Color", previewColor);
                    if (materials[i].HasProperty("_BaseColor"))
                        materials[i].SetColor("_BaseColor", previewColor);
                    if (materials[i].HasProperty("_MainColor"))
                        materials[i].SetColor("_MainColor", previewColor);

                    // Ensure low metallic and smoothness to avoid bloom-like effects
                    if (materials[i].HasProperty("_Metallic"))
                        materials[i].SetFloat("_Metallic", 0f);
                    if (materials[i].HasProperty("_Smoothness"))
                        materials[i].SetFloat("_Smoothness", 0.1f);

                    // Disable emission to prevent bloom
                    if (materials[i].HasProperty("_EmissionColor"))
                        materials[i].SetColor("_EmissionColor", Color.black);
                    if (materials[i].HasProperty("_EmissiveColor"))
                        materials[i].SetColor("_EmissiveColor", Color.black);
                }
                renderer.materials = materials;
            }
            else
            {
                // Fallback: modify existing materials
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    materials[i] = new Material(renderer.materials[i]);
                    materials[i].color = previewColor;

                    // Disable emission to prevent bloom
                    if (materials[i].HasProperty("_EmissionColor"))
                        materials[i].SetColor("_EmissionColor", Color.black);
                    if (materials[i].HasProperty("_EmissiveColor"))
                        materials[i].SetColor("_EmissiveColor", Color.black);

                    // Reduce metallic and smoothness
                    if (materials[i].HasProperty("_Metallic"))
                        materials[i].SetFloat("_Metallic", 0f);
                    if (materials[i].HasProperty("_Smoothness"))
                        materials[i].SetFloat("_Smoothness", 0.1f);

                    // Try to enable transparency on URP materials
                    if (materials[i].HasProperty("_Surface"))
                    {
                        materials[i].SetFloat("_Surface", 1); // Transparent
                        materials[i].SetFloat("_Blend", 0); // Alpha blending
                        materials[i].SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        materials[i].SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        materials[i].SetFloat("_ZWrite", 0);
                        materials[i].EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        materials[i].EnableKeyword("_ALPHABLEND_ON");
                        materials[i].renderQueue = 3000;
                    }
                }
                renderer.materials = materials;
            }
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    bool CanPlaceObjectAt(Vector2Int gridPosition, GridObjectData objectData, Vector2Int effectiveSize)
    {
        if (objectData == null) return false;

        // Check if all required cells are free and within bounds
        for (int x = 0; x < effectiveSize.x; x++)
        {
            for (int y = 0; y < effectiveSize.y; y++)
            {
                Vector2Int checkPos = gridPosition + new Vector2Int(x, y);

                // Check bounds
                if (!gridSystem.AreCoordinatesValid(checkPos.x, checkPos.y))
                    return false;

                // Check if cell is occupied
                if (placedObjects.ContainsKey(checkPos))
                    return false;
            }
        }
        return true;
    }

    void TryPlaceObject()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        if (mouseWorldPos == Vector3.zero) return;

        Vector3 snappedPos = gridSystem.SnapToGrid(mouseWorldPos);
        Vector2Int gridCoords = gridSystem.GetGridCoordinates(snappedPos);
        Vector2Int effectiveSize = GetEffectiveSize();

        if (CanPlaceObjectAt(gridCoords, currentObjectData, effectiveSize))
        {
            PlaceObject(gridCoords, snappedPos, effectiveSize);
        }
    }

    void PlaceObject(Vector2Int gridPosition, Vector3 worldPosition, Vector2Int effectiveSize)
    {
        if (currentObjectData?.prefab == null) return;

        // Get current rotation
        // Get rotation from prefabRotation field
        Quaternion rotation = Quaternion.Euler(currentObjectData.prefabRotation);

        // Instantiate the actual object with rotation and visual offset
        GameObject newObject = Instantiate(currentObjectData.prefab, worldPosition + currentObjectData.visualOffset, rotation);
        newObject.layer = LayerMask.NameToLayer("PlacedObject");

        // Set all children to PlacedObject layer too
        Transform[] allChildren = newObject.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            child.gameObject.layer = LayerMask.NameToLayer("PlacedObject");
        }
        EnsureCollidersExist(newObject);

        // Create placed object data
        // Create placed object data
        // Create placed object data
        PlacedGridObject placedObject = new PlacedGridObject
        {
            gridPosition = gridPosition,
            worldPosition = worldPosition,
            gameObject = newObject,
            objectData = currentObjectData,
            size = effectiveSize,
            rotation = rotation,
            rotationDegrees = currentObjectData.rotationDegrees,
            objectTypeName = currentObjectData.objectTypeName,
            canPlaceOnWire = currentObjectData.canPlaceOnWire // NEW: Store the wire placement flag
        };

        // Mark all occupied cells
        for (int x = 0; x < effectiveSize.x; x++)
        {
            for (int y = 0; y < effectiveSize.y; y++)
            {
                Vector2Int occupiedCell = gridPosition + new Vector2Int(x, y);
                placedObjects[occupiedCell] = placedObject;
            }
        }

        // Add PlacedObjectComponent for easy reference
        PlacedObjectComponent placedComponent = newObject.GetComponent<PlacedObjectComponent>();
        if (placedComponent == null)
        {
            placedComponent = newObject.AddComponent<PlacedObjectComponent>();
        }
        placedComponent.Initialize(placedObject, this);

        OnObjectPlaced?.Invoke(placedObject);

        Debug.Log($"Placed {currentObjectData.displayName} at grid position {gridPosition} with rotation {placedObject.rotationDegrees}°");
    }

    public void RemoveObject(Vector2Int gridPosition)
    {
        if (!placedObjects.ContainsKey(gridPosition)) return;

        PlacedGridObject placedObject = placedObjects[gridPosition];

        // NEW: Find and remove all wires connected to this object
        WirePlacementSystem wireSystem = FindFirstObjectByType<WirePlacementSystem>();
        if (wireSystem != null)
        {
            wireSystem.RemoveWiresConnectedToObject(placedObject.gameObject);
        }

        // Remove from all occupied cells
        for (int x = 0; x < placedObject.size.x; x++)
        {
            for (int y = 0; y < placedObject.size.y; y++)
            {
                Vector2Int occupiedCell = placedObject.gridPosition + new Vector2Int(x, y);
                placedObjects.Remove(occupiedCell);
            }
        }

        // Destroy the game object
        if (placedObject.gameObject != null)
        {
            DestroyImmediate(placedObject.gameObject);
        }

        OnObjectRemoved?.Invoke(placedObject);

        Debug.Log($"Removed {placedObject.objectData.displayName} from grid position {gridPosition}");
    }

    // PUBLIC METHODS FOR UI BUTTON onClick EVENTS
    public void StartPlacementWithID(string objectID)
    {
        GridObjectData objectData = GetObjectDataByID(objectID);
        if (objectData != null)
        {
            currentObjectID = objectID;
            currentObjectData = objectData;
            isPlacementMode = true;

            // Initialize variant group
            if (variantSystem != null)
            {
                variantSystem.InitializeVariantGroup(objectID, objectDatabase);
            }

            OnPlacementModeChanged?.Invoke(true);
            Debug.Log($"Started placement mode for: {objectData.displayName}");
        }
    }


    public void CancelPlacement()
    {
        isPlacementMode = false;
        currentObjectID = "";
        currentObjectData = null;

        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }

        if (footprintPlane != null)
        {
            DestroyImmediate(footprintPlane);
            footprintPlane = null;
            customPlanePreview = null;
        }
        customPlanePreview = null;
        // Reset rotation
        if (variantSystem != null)
        {
            variantSystem.ResetVariant();
        }

        OnPlacementModeChanged?.Invoke(false);
        Debug.Log("Placement mode cancelled");
    }

    public void TogglePlacementWithID(string objectID)
    {
        if (isPlacementMode && currentObjectID == objectID)
        {
            CancelPlacement();
        }
        else
        {
            StartPlacementWithID(objectID);
        }
    }


    void EnsureCollidersExist(GameObject obj)
    {
        // Check if object already has colliders
        Collider[] existingColliders = obj.GetComponentsInChildren<Collider>();

        if (existingColliders.Length > 0)
        {
            // Enable all colliders
            foreach (Collider col in existingColliders)
            {
                col.enabled = true;
            }
            Debug.Log($"Enabled {existingColliders.Length} existing collider(s)");
            return;
        }

        // No colliders found, add a box collider to the root
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();

        if (renderers.Length > 0)
        {
            // Calculate bounds from all renderers
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            // Add box collider to root object
            BoxCollider boxCol = obj.AddComponent<BoxCollider>();
            boxCol.center = obj.transform.InverseTransformPoint(combinedBounds.center);
            boxCol.size = combinedBounds.size;

            Debug.Log($"Added BoxCollider to {obj.name} with size {boxCol.size}");
        }
        else
        {
            // Fallback: add a small box collider
            BoxCollider boxCol = obj.AddComponent<BoxCollider>();
            boxCol.size = Vector3.one;
            Debug.LogWarning($"No renderers found, added default BoxCollider to {obj.name}");
        }
    }
    GridObjectData GetObjectDataByID(string objectID)
    {
        if (objectDatabase == null) return null;

        // Use the database's GetObjectByID method which handles rotation variants
        return objectDatabase.GetObjectByID(objectID);
    }

    // Utility methods
    public bool IsInPlacementMode() => isPlacementMode;
    public string GetCurrentObjectID() => currentObjectID;
    public PlacedGridObject GetPlacedObjectAt(Vector2Int gridPosition)
    {
        return placedObjects.ContainsKey(gridPosition) ? placedObjects[gridPosition] : null;
    }

    public List<PlacedGridObject> GetAllPlacedObjects()
{
    List<PlacedGridObject> uniqueObjects = new List<PlacedGridObject>();
    HashSet<PlacedGridObject> addedObjects = new HashSet<PlacedGridObject>();

    // Add normally placed objects
    foreach (PlacedGridObject obj in placedObjects.Values)
    {
        if (!addedObjects.Contains(obj))
        {
            uniqueObjects.Add(obj);
            addedObjects.Add(obj);
        }
    }
    
    // Add fixed placement objects
    foreach (PlacedGridObject obj in fixedPlacementObjects)
    {
        if (!addedObjects.Contains(obj))
        {
            uniqueObjects.Add(obj);
            addedObjects.Add(obj);
        }
    }

    return uniqueObjects;
}

    public void SwitchToVariant(GridObjectData newVariantData)
    {
        if (newVariantData == null) return;

        currentObjectData = newVariantData;
        currentObjectID = newVariantData.objectID;

        // Destroy old preview and footprint to force recreation with new prefab
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }
        if (footprintPlane != null)
        {
            DestroyImmediate(footprintPlane);
            footprintPlane = null;
        }

        Debug.Log($"Switched to variant: {newVariantData.displayName} (Rotation: {newVariantData.rotationDegrees}°)");
    }

    public void ClearAllObjects()
    {
        List<PlacedGridObject> allObjects = GetAllPlacedObjects();
        foreach (PlacedGridObject obj in allObjects)
        {
            RemoveObject(obj.gridPosition);
        }
    }
    
    // Add before the closing brace of ObjectPlacementSystem class

/// <summary>
/// Register a fixed placement object (placed manually in scene, not via placement system)
/// </summary>
public void RegisterFixedPlacementObject(PlacedGridObject placedObject, bool occupyGridCells)
{
    if (placedObject == null)
    {
        Debug.LogError("[ObjectPlacement] Cannot register null fixed placement object");
        return;
    }
    
    // Add to fixed placement list
    if (!fixedPlacementObjects.Contains(placedObject))
    {
        fixedPlacementObjects.Add(placedObject);
        Debug.Log($"[ObjectPlacement] Registered fixed placement object: {placedObject.objectData.displayName}");
    }
    
    // Optionally occupy grid cells
    if (occupyGridCells)
    {
        for (int x = 0; x < placedObject.size.x; x++)
        {
            for (int y = 0; y < placedObject.size.y; y++)
            {
                Vector2Int occupiedCell = placedObject.gridPosition + new Vector2Int(x, y);
                
                if (gridSystem.AreCoordinatesValid(occupiedCell.x, occupiedCell.y))
                {
                    placedObjects[occupiedCell] = placedObject;
                }
            }
        }
        Debug.Log($"[ObjectPlacement] Fixed object occupies grid cells at {placedObject.gridPosition}");
    }
}

/// <summary>
/// Unregister a fixed placement object
/// </summary>
public void UnregisterFixedPlacementObject(PlacedGridObject placedObject)
{
    if (placedObject == null) return;
    
    fixedPlacementObjects.Remove(placedObject);
    
    // Remove from grid cells if it was occupying them
    foreach (var kvp in placedObjects.ToList())
    {
        if (kvp.Value == placedObject)
        {
            placedObjects.Remove(kvp.Key);
        }
    }
    
    Debug.Log($"[ObjectPlacement] Unregistered fixed placement object: {placedObject.objectData.displayName}");
}
}

// Data structure for placed objects
[System.Serializable]
public class PlacedGridObject
{
    public Vector2Int gridPosition;
    public Vector3 worldPosition;
    public GameObject gameObject;
    public GridObjectData objectData;
    public Vector2Int size;
    public Quaternion rotation;
    public float rotationDegrees;
    public string objectTypeName; // Stores the custom type name
    public bool canPlaceOnWire; // NEW: Can this object be placed on wires?
}


// Component to attach to placed objects for easy reference
public class PlacedObjectComponent : MonoBehaviour
{
    public PlacedGridObject placedObjectData;
    private ObjectPlacementSystem placementSystem;

    public void Initialize(PlacedGridObject data, ObjectPlacementSystem system)
    {
        placedObjectData = data;
        placementSystem = system;
    }

    public void RemoveSelf()
    {
        if (placementSystem != null)
        {
            placementSystem.RemoveObject(placedObjectData.gridPosition);
        }
    }
}