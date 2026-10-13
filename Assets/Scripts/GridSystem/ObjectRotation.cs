using UnityEngine;
using System.Collections.Generic;

public class ObjectVariantSystem : MonoBehaviour
{
    [Header("References")]
    public ObjectPlacementSystem placementSystem;
    
    [Header("Variant Cycling Settings")]
    public KeyCode cycleKey = KeyCode.R;
    
    // Current variant state
    private int currentVariantIndex = 0;
    private List<GridObjectData> currentVariantGroup = new List<GridObjectData>();
    private string baseObjectID = "";
    
    void Update()
    {
        // Only allow cycling when in placement mode
        if (placementSystem != null && placementSystem.IsInPlacementMode())
        {
            HandleVariantInput();
        }
        else
        {
            // Reset variant when not in placement mode
            ResetVariant();
        }
    }
    
    void HandleVariantInput()
    {
        if (Input.GetKeyDown(cycleKey))
        {
            CycleToNextVariant();
        }
    }
    
    /// <summary>
    /// Initialize variant group when placement mode starts
    /// This now uses the database's GetRotationVariants method to get flattened hierarchy
    /// </summary>
    public void InitializeVariantGroup(string objectID, GridObjectDatabaseSO database)
    {
        if (database == null)
        {
            Debug.LogWarning("Cannot initialize variant group: database is null");
            return;
        }
        
        baseObjectID = objectID;
        currentVariantGroup.Clear();
        currentVariantIndex = 0;
        
        // Use the database method to get all variants (including nested ones)
        currentVariantGroup = database.GetRotationVariants(objectID);
        
        if (currentVariantGroup.Count > 0)
        {
            Debug.Log($"Initialized variant group for '{objectID}': Found {currentVariantGroup.Count} variant(s)");
            
            // Debug log all variants found
            for (int i = 0; i < currentVariantGroup.Count; i++)
            {
                GridObjectData variant = currentVariantGroup[i];
                Debug.Log($"  Variant {i}: ID='{variant.objectID}', Name='{variant.displayName}', Rotation={variant.rotationDegrees}°");
            }
        }
        else
        {
            Debug.LogWarning($"No variants found for object ID '{objectID}'");
        }
    }
    
    /// <summary>
    /// Cycle to the next variant in the group
    /// </summary>
    void CycleToNextVariant()
    {
        if (currentVariantGroup.Count <= 1)
        {
            Debug.Log("No additional variants available for cycling");
            return;
        }
        
        // Move to next variant (loop back to 0 after last)
        currentVariantIndex = (currentVariantIndex + 1) % currentVariantGroup.Count;
        
        GridObjectData newVariant = currentVariantGroup[currentVariantIndex];
        
        // Update the placement system with new variant
        if (placementSystem != null)
        {
            placementSystem.SwitchToVariant(newVariant);
            Debug.Log($"Cycled to variant {currentVariantIndex + 1}/{currentVariantGroup.Count}: {newVariant.objectID} ({newVariant.displayName})");
        }
    }
    
    /// <summary>
    /// Reset variant state
    /// </summary>
    public void ResetVariant()
    {
        currentVariantIndex = 0;
        currentVariantGroup.Clear();
        baseObjectID = "";
    }
    
    /// <summary>
    /// Get the current variant data
    /// </summary>
    public GridObjectData GetCurrentVariant()
    {
        if (currentVariantGroup.Count > 0 && currentVariantIndex < currentVariantGroup.Count)
        {
            return currentVariantGroup[currentVariantIndex];
        }
        return null;
    }
    
    /// <summary>
    /// Get the number of variants in the current group
    /// </summary>
    public int GetVariantCount()
    {
        return currentVariantGroup.Count;
    }
    
    /// <summary>
    /// Get the current variant index (0-based)
    /// </summary>
    public int GetCurrentVariantIndex()
    {
        return currentVariantIndex;
    }
    
    /// <summary>
    /// Check if there are multiple variants available
    /// </summary>
    public bool HasMultipleVariants()
    {
        return currentVariantGroup.Count > 1;
    }
    
    /// <summary>
    /// Set a specific variant by index
    /// </summary>
    public void SetVariantIndex(int index)
    {
        if (index >= 0 && index < currentVariantGroup.Count)
        {
            currentVariantIndex = index;
            GridObjectData newVariant = currentVariantGroup[currentVariantIndex];
            
            if (placementSystem != null)
            {
                placementSystem.SwitchToVariant(newVariant);
            }
        }
        else
        {
            Debug.LogWarning($"Invalid variant index: {index}. Valid range is 0-{currentVariantGroup.Count - 1}");
        }
    }
    
    /// <summary>
    /// Get all variant IDs in the current group
    /// </summary>
    public List<string> GetVariantIDs()
    {
        List<string> ids = new List<string>();
        foreach (GridObjectData variant in currentVariantGroup)
        {
            ids.Add(variant.objectID);
        }
        return ids;
    }
    
    /// <summary>
    /// Get all variant display names in the current group
    /// </summary>
    public List<string> GetVariantNames()
    {
        List<string> names = new List<string>();
        foreach (GridObjectData variant in currentVariantGroup)
        {
            names.Add(variant.displayName);
        }
        return names;
    }
}