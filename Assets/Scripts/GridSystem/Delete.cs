using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ObjectDeleteSystem : MonoBehaviour
{
    [Header("References")]
    public ObjectPlacementSystem placementSystem;
    
    [Header("Delete Settings")]
    public KeyCode cancelDeleteKey = KeyCode.Escape;
    public Color highlightColor = new Color(1f, 0f, 0f, 0.7f); // Red with transparency
    [Range(0.1f, 1.0f)]
    public float highlightAlpha = 0.7f;
    
    // Delete mode state
    private bool isDeleteMode = false;
    private Camera playerCamera;
    private PlacedGridObject hoveredObject = null;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Material highlightMaterial;
    
    // Events
    public System.Action<bool> OnDeleteModeChanged;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();
            
        if (placementSystem == null)
            placementSystem = FindFirstObjectByType<ObjectPlacementSystem>();
            
        CreateHighlightMaterial();
    }

    void Update()
    {
        if (isDeleteMode)
        {
            HandleDeleteInput();
            UpdateHover();
        }
    }

    void CreateHighlightMaterial()
    {
        // Try to use URP Lit shader with transparency
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader != null)
        {
            highlightMaterial = new Material(urpShader);
            highlightMaterial.SetFloat("_Surface", 1); // Transparent
            highlightMaterial.SetFloat("_Blend", 0); // Alpha blending
            highlightMaterial.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            highlightMaterial.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            highlightMaterial.SetFloat("_ZWrite", 0);
            highlightMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            highlightMaterial.EnableKeyword("_ALPHABLEND_ON");
            highlightMaterial.renderQueue = 3000;
            highlightMaterial.SetFloat("_Metallic", 0f);
            highlightMaterial.SetFloat("_Smoothness", 0.1f);
        }
        else
        {
            // Fallback to Standard shader
            highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.SetFloat("_Mode", 3); // Transparent mode
            highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            highlightMaterial.SetInt("_ZWrite", 0);
            highlightMaterial.DisableKeyword("_ALPHATEST_ON");
            highlightMaterial.EnableKeyword("_ALPHABLEND_ON");
            highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            highlightMaterial.renderQueue = 3000;
            highlightMaterial.SetFloat("_Metallic", 0f);
            highlightMaterial.SetFloat("_Glossiness", 0.1f);
        }
        
        Color finalColor = highlightColor;
        finalColor.a = highlightAlpha;
        highlightMaterial.color = finalColor;
        
        if (highlightMaterial.HasProperty("_BaseColor"))
            highlightMaterial.SetColor("_BaseColor", finalColor);
    }

    void HandleDeleteInput()
    {
        // Cancel delete mode
        if (Input.GetKeyDown(cancelDeleteKey))
        {
            CancelDeleteMode();
            return;
        }

        // Don't delete if over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Delete object on left click
        if (Input.GetMouseButtonDown(0))
        {
            TryDeleteObject();
        }
    }

    void UpdateHover()
    {
        PlacedGridObject newHoveredObject = GetObjectUnderMouse();
        
        if (newHoveredObject != hoveredObject)
        {
            // Remove highlight from previous object
            if (hoveredObject != null)
            {
                RemoveHighlight(hoveredObject);
            }
            
            // Highlight new object
            hoveredObject = newHoveredObject;
            if (hoveredObject != null)
            {
                ApplyHighlight(hoveredObject);
            }
        }
    }

    PlacedGridObject GetObjectUnderMouse()
    {
        if (playerCamera == null) return null;
        
        // Raycast against PlacedObject layer
        int placedObjectLayer = LayerMask.NameToLayer("PlacedObject");
        LayerMask placedObjectMask = 1 << placedObjectLayer;
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, placedObjectMask);
        
        // Find the closest hit with a PlacedObjectComponent
        float closestDistance = Mathf.Infinity;
        PlacedGridObject closestObject = null;
        
        foreach (RaycastHit hit in hits)
        {
            PlacedObjectComponent placedComponent = hit.collider.GetComponentInParent<PlacedObjectComponent>();
            if (placedComponent != null && placedComponent.placedObjectData != null)
            {
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestObject = placedComponent.placedObjectData;
                }
            }
        }
        
        return closestObject;
    }

    void ApplyHighlight(PlacedGridObject placedObject)
    {
        if (placedObject?.gameObject == null) return;

        Renderer[] renderers = placedObject.gameObject.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            
            // Store original materials if not already stored
            if (!originalMaterials.ContainsKey(renderer))
            {
                originalMaterials[renderer] = renderer.sharedMaterials;
            }

            // Create highlight materials array
            Material[] highlightMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < highlightMaterials.Length; i++)
            {
                highlightMaterials[i] = highlightMaterial;
            }
            
            renderer.materials = highlightMaterials;
        }
        
        Debug.Log($"Highlighted object: {placedObject.objectData.displayName} at {placedObject.gridPosition}");
    }

    void RemoveHighlight(PlacedGridObject placedObject)
    {
        if (placedObject?.gameObject == null) return;

        Renderer[] renderers = placedObject.gameObject.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            
            // Restore original materials
            if (originalMaterials.ContainsKey(renderer))
            {
                renderer.materials = originalMaterials[renderer];
                originalMaterials.Remove(renderer);
            }
        }
    }

    void TryDeleteObject()
    {
        if (hoveredObject != null)
        {
            Debug.Log($"Attempting to delete: {hoveredObject.objectData.displayName}");
            
            // Store reference before removing highlight
            PlacedGridObject objToDelete = hoveredObject;
            
            // Remove highlight before deleting
            RemoveHighlight(hoveredObject);
            hoveredObject = null;
            
            // Delete the object
            if (placementSystem != null)
            {
                placementSystem.RemoveObject(objToDelete.gridPosition);
                Debug.Log("Object deleted successfully");
            }
            else
            {
                Debug.LogError("PlacementSystem reference is null!");
            }
        }
        else
        {
            Debug.Log("No object under mouse to delete");
        }
    }

    // PUBLIC METHODS FOR UI INTEGRATION
    public void StartDeleteMode()
    {
        Debug.Log("StartDeleteMode called");
        
        // Cancel placement mode if active
        if (placementSystem != null && placementSystem.IsInPlacementMode())
        {
            placementSystem.CancelPlacement();
        }
        
        isDeleteMode = true;
        OnDeleteModeChanged?.Invoke(true);
        Debug.Log("Delete mode ACTIVE");
    }

    public void CancelDeleteMode()
    {
        Debug.Log("CancelDeleteMode called");
        
        isDeleteMode = false;
        
        // Remove any current highlight
        if (hoveredObject != null)
        {
            RemoveHighlight(hoveredObject);
            hoveredObject = null;
        }
        
        // Clean up any remaining stored materials
        originalMaterials.Clear();
        
        OnDeleteModeChanged?.Invoke(false);
        Debug.Log("Delete mode CANCELLED");
    }

    public void ToggleDeleteMode()
    {
        Debug.Log($"ToggleDeleteMode called. Current state: {isDeleteMode}");
        
        if (isDeleteMode)
        {
            CancelDeleteMode();
        }
        else
        {
            StartDeleteMode();
        }
    }

    // Utility methods
    public bool IsInDeleteMode() => isDeleteMode;

    void OnDisable()
    {
        // Clean up highlights when component is disabled
        if (hoveredObject != null)
        {
            RemoveHighlight(hoveredObject);
        }
        originalMaterials.Clear();
    }
}