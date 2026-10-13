using UnityEngine;
using System.Linq;

public class FixedPlacementObject : MonoBehaviour
{
    [Header("Object Data")]
    [Tooltip("Enter the Object ID from GridObjectDatabaseSO (e.g., 'battery_9v', 'resistor_100')")]
    public string objectID;
    
    [Header("Grid Position (Optional)")]
    [Tooltip("If you want this object to occupy grid cells, set its grid position")]
    public bool occupiesGridCells = false;
    public Vector2Int gridPosition;
    public Vector2Int gridSize = Vector2Int.one;
    
    [Header("Auto-Register")]
    [Tooltip("Automatically register with placement system on Start")]
    public bool autoRegister = true;
    
    [Header("Debug Info (Read Only)")]
    [SerializeField] private string loadedObjectName;
    
    private PlacedGridObject virtualPlacedObject;
    private ObjectPlacementSystem placementSystem;
    private GridObjectData objectData;
    
    void Start()
    {
        if (autoRegister)
        {
            RegisterWithPlacementSystem();
        }
        
        // Apply current wire mode state to terminals
        ApplyWireModeToTerminals();
    }
    
    public void RegisterWithPlacementSystem()
    {
        // Validate object ID
        if (string.IsNullOrEmpty(objectID))
        {
            Debug.LogError($"[FixedPlacement] {gameObject.name} has no Object ID assigned!", this);
            return;
        }
        
        // Find placement system
        placementSystem = FindFirstObjectByType<ObjectPlacementSystem>();
        if (placementSystem == null)
        {
            Debug.LogError($"[FixedPlacement] Could not find ObjectPlacementSystem in scene!", this);
            return;
        }
        
        // Load object data from database using ID
        objectData = GetObjectDataByID(objectID);
        if (objectData == null)
        {
            Debug.LogError($"[FixedPlacement] Could not find GridObjectData with ID '{objectID}' in database!", this);
            return;
        }
        
        // Update debug info
        loadedObjectName = objectData.displayName;
        
        // Create virtual PlacedGridObject data
        virtualPlacedObject = new PlacedGridObject
        {
            gridPosition = gridPosition,
            worldPosition = transform.position,
            gameObject = gameObject,
            objectData = objectData,
            size = gridSize,
            rotation = transform.rotation,
            rotationDegrees = transform.eulerAngles.y,
            objectTypeName = objectData.objectTypeName,
            canPlaceOnWire = objectData.canPlaceOnWire
        };
        
        // Set layer (but NOT for terminals/nodes - they manage their own visibility)
        SetLayerRecursive(transform, true);
        
        // Add PlacedObjectComponent
        PlacedObjectComponent component = GetComponent<PlacedObjectComponent>();
        if (component == null)
        {
            component = gameObject.AddComponent<PlacedObjectComponent>();
        }
        component.Initialize(virtualPlacedObject, placementSystem);
        
        // Ensure colliders exist
        EnsureCollidersExist();
        
        // Register with placement system
        placementSystem.RegisterFixedPlacementObject(virtualPlacedObject, occupiesGridCells);
        
        Debug.Log($"[FixedPlacement] Registered '{objectData.displayName}' (ID: {objectID}) at {transform.position}");
    }
    
    GridObjectData GetObjectDataByID(string id)
    {
        // Try to find the database
        GridObjectDatabaseSO database = placementSystem?.objectDatabase;
        
        if (database == null)
        {
            // Fallback: try to find it in resources or assets
            database = Resources.FindObjectsOfTypeAll<GridObjectDatabaseSO>().FirstOrDefault();
            
            if (database == null)
            {
                Debug.LogError("[FixedPlacement] Could not find GridObjectDatabaseSO!");
                return null;
            }
        }
        
        // Use the database's method to get object by ID
        return database.GetObjectByID(id);
    }
    
    void SetLayerRecursive(Transform parent, bool isRoot = false)
    {
        int terminalLayer = LayerMask.NameToLayer("Terminal");
        int nodeLayer = LayerMask.NameToLayer("Node");
        
        // Set layer for current object (if not root or not a terminal/node)
        if (!isRoot || (parent.gameObject.layer != terminalLayer && parent.gameObject.layer != nodeLayer))
        {
            // Don't change layer if it's already Terminal or Node
            if (parent.gameObject.layer != terminalLayer && parent.gameObject.layer != nodeLayer)
            {
                parent.gameObject.layer = LayerMask.NameToLayer("PlacedObject");
            }
        }
        
        // Recursively set for children
        foreach (Transform child in parent)
        {
            // Skip terminals and nodes - they manage their own layers
            if (child.gameObject.layer != terminalLayer && child.gameObject.layer != nodeLayer)
            {
                child.gameObject.layer = LayerMask.NameToLayer("PlacedObject");
            }
            
            SetLayerRecursive(child);
        }
    }
    
    void ApplyWireModeToTerminals()
    {
        if (WireModeManager.Instance == null) return;
        
        bool wiringMode = WireModeManager.Instance.wiringMode;
        int terminalLayer = LayerMask.NameToLayer("Terminal");
        int nodeLayer = LayerMask.NameToLayer("Node");
        
        // Find all child terminals and nodes
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        
        foreach (Transform child in allChildren)
        {
            if (child.gameObject.layer == terminalLayer || child.gameObject.layer == nodeLayer)
            {
                child.gameObject.SetActive(wiringMode);
                Debug.Log($"[FixedPlacement] Set terminal/node {child.name} to {wiringMode}");
            }
            
            // Also check for WireNode components
            WireNode wireNode = child.GetComponent<WireNode>();
            if (wireNode != null)
            {
                child.gameObject.SetActive(wiringMode);
                Debug.Log($"[FixedPlacement] Set WireNode {child.name} to {wiringMode}");
            }
        }
    }
    
    void EnsureCollidersExist()
    {
        Collider[] existingColliders = GetComponentsInChildren<Collider>();
        
        if (existingColliders.Length > 0)
        {
            foreach (Collider col in existingColliders)
            {
                col.enabled = true;
            }
            return;
        }
        
        // Add box collider if none exist
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.center = transform.InverseTransformPoint(combinedBounds.center);
            boxCol.size = combinedBounds.size;
        }
        else
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.size = Vector3.one;
        }
    }
    
    void OnDestroy()
    {
        if (placementSystem != null && virtualPlacedObject != null)
        {
            placementSystem.UnregisterFixedPlacementObject(virtualPlacedObject);
        }
    }
    
    // Editor helper to validate the ID
    void OnValidate()
    {
        if (!string.IsNullOrEmpty(objectID) && Application.isPlaying)
        {
            // Try to load and show the name
            ObjectPlacementSystem tempSystem = FindFirstObjectByType<ObjectPlacementSystem>();
            if (tempSystem != null && tempSystem.objectDatabase != null)
            {
                GridObjectData data = tempSystem.objectDatabase.GetObjectByID(objectID);
                loadedObjectName = data != null ? data.displayName : "NOT FOUND";
            }
        }
    }
    
    // Called when wire mode changes (can be called by WireModeManager)
    public void OnWireModeChanged(bool wiringMode)
    {
        ApplyWireModeToTerminals();
    }
}