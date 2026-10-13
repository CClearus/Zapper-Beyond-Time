using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GridObjectDatabase", menuName = "Grid/Grid Object Database", order = 0)]
public class GridObjectDatabaseSO : ScriptableObject
{
    [Header("All Grid Objects")]
    public List<GridObjectData> objects = new List<GridObjectData>();
    
    /// <summary>
    /// Get all rotation variants for a base object (flattens the hierarchy)
    /// </summary>
    public List<GridObjectData> GetRotationVariants(string baseObjectID)
    {
        List<GridObjectData> variants = new List<GridObjectData>();
        
        foreach (GridObjectData obj in objects)
        {
            if (obj.objectID == baseObjectID)
            {
                // Add the base object first
                variants.Add(obj);
                
                // Add all its rotation variants
                if (obj.rotationVariants != null && obj.rotationVariants.Count > 0)
                {
                    foreach (RotationVariant variant in obj.rotationVariants)
                    {
                        // Create a temporary GridObjectData for this rotation
                        GridObjectData rotatedData = new GridObjectData
                        {
                            objectID = obj.objectID + "." + variant.subID,
                            displayName = obj.displayName + " (" + variant.rotationDegrees + "°)",
                            prefab = variant.prefab,
                            planePrefab = variant.planePrefab,
                            planeOffset = variant.planeOffset,
                            sizeX = variant.sizeX,
                            sizeY = variant.sizeY,
                            rotationDegrees = variant.rotationDegrees,
                            prefabRotation = variant.prefabRotation,
                            visualOffset = variant.visualOffset,
                            collisionOffset = variant.collisionOffset,
                            objectImage = obj.objectImage,
                            objectTypeName = obj.objectTypeName,
                            canPlaceOnWire = obj.canPlaceOnWire,
                            value = obj.value,
                            description = obj.description,
                            resistance = obj.resistance,
                            voltage = obj.voltage,
                            maxVoltage = obj.maxVoltage,
                            maxAmperage = obj.maxAmperage,
                            isVoltageSource = obj.isVoltageSource,
                            voltageNoiseAmplitude = obj.voltageNoiseAmplitude,
                            voltageNoiseRate = obj.voltageNoiseRate
                        };
                        variants.Add(rotatedData);
                    }
                }
                
                break;
            }
        }
        
        return variants;
    }
    
    /// <summary>
    /// Get a specific object by full ID (including rotation variants)
    /// </summary>
    public GridObjectData GetObjectByID(string fullObjectID)
    {
        // Check if it's a rotation variant (contains a dot)
        if (fullObjectID.Contains("."))
        {
            string[] parts = fullObjectID.Split('.');
            string baseID = parts[0];
            string subID = parts[1];
            
            // Find the base object
            foreach (GridObjectData obj in objects)
            {
                if (obj.objectID == baseID)
                {
                    // Look for matching rotation variant
                    if (obj.rotationVariants != null)
                    {
                        foreach (RotationVariant variant in obj.rotationVariants)
                        {
                            if (variant.subID == subID)
                            {
                                // Return constructed GridObjectData
                                return new GridObjectData
                                {
                                    objectID = fullObjectID,
                                    displayName = obj.displayName + " (" + variant.rotationDegrees + "°)",
                                    prefab = variant.prefab,
                                    planePrefab = variant.planePrefab,
                                    planeOffset = variant.planeOffset,
                                    sizeX = variant.sizeX,
                                    sizeY = variant.sizeY,
                                    rotationDegrees = variant.rotationDegrees,
                                    prefabRotation = variant.prefabRotation,
                                    visualOffset = variant.visualOffset,
                                    collisionOffset = variant.collisionOffset,
                                    objectImage = obj.objectImage,
                                    objectTypeName = obj.objectTypeName,
                                    canPlaceOnWire = obj.canPlaceOnWire,
                                    value = obj.value,
                                    description = obj.description,
                                    resistance = obj.resistance,
                                    voltage = obj.voltage,
                                    maxVoltage = obj.maxVoltage,
                                    maxAmperage = obj.maxAmperage,
                                    isVoltageSource = obj.isVoltageSource,
                                    voltageNoiseAmplitude = obj.voltageNoiseAmplitude,
                                    voltageNoiseRate = obj.voltageNoiseRate
                                };
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // Return base object
            foreach (GridObjectData obj in objects)
            {
                if (obj.objectID == fullObjectID)
                    return obj;
            }
        }
        
        return null;
    }
}

[System.Serializable]
public class GridObjectData
{
    [Header("Identification")]
    [Tooltip("Unique ID for this grid object (e.g., '1', '2', '3')")]
    public string objectID;

    [Tooltip("Human-readable name for this object")]
    public string displayName;

    [Header("Base Placement Settings")]
    [Tooltip("Base prefab to spawn (default rotation)")]
    public GameObject prefab;

    [Tooltip("How many grid cells in X this object occupies")]
    public int sizeX = 1;

    [Tooltip("How many grid cells in Y this object occupies")]
    public int sizeY = 1;
    
    [Header("Rotation Variants")]
    [Tooltip("Add rotation variants here. Final ID will be: objectID.subID (e.g., '1.1', '1.2')")]
    public List<RotationVariant> rotationVariants = new List<RotationVariant>();

    [Header("Rotation Info (Optional)")]
    [Tooltip("Rotation in degrees for base prefab (usually 0)")]
    public float rotationDegrees = 0f;

    [Tooltip("Prefab rotation offset (X, Y, Z in degrees) - applied to the instantiated object")]
    public Vector3 prefabRotation = Vector3.zero;

    [Header("Visual Plane Settings")]
    [Tooltip("Optional plane prefab to show under this object")]
    public GameObject planePrefab;

    [Tooltip("Position offset for the plane (X, Y, Z in world units)")]
    public Vector3 planeOffset = Vector3.zero;

    [Header("Offset Settings")]
    [Tooltip("Position offset for the visual object (X, Y, Z in world units)")]
    public Vector3 visualOffset = Vector3.zero;

    [Tooltip("Position offset for the collision/hitbox (X, Y, Z in world units)")]
    public Vector3 collisionOffset = Vector3.zero;

    [Header("UI Display")]
    [Tooltip("Image/texture to display in UI (optional)")]
    public Texture2D objectImage;

    [Header("Type & Properties")]
    [Tooltip("Custom object type name (e.g., 'Resistor', 'LED', 'Custom Component')")]
    public string objectTypeName = "None";

    [Tooltip("Can this object be placed on top of wires?")]
    public bool canPlaceOnWire = false;

    [Tooltip("Optional value (resistance, voltage, etc.)")]
    public float value;

    [Header("Electrical Properties")]
    [Tooltip("Resistance in Ohms")]
    public float resistance = 1f;

    [Tooltip("Voltage for sources, or rated voltage for components")]
    public float voltage = 0f;

    [Tooltip("Maximum voltage this component can handle (0 = no limit)")]
    public float maxVoltage = 0f;

    [Tooltip("Maximum current/amperage this component can handle (0 = no limit)")]
    public float maxAmperage = 0f;

    [Tooltip("Is this a voltage source?")]
    public bool isVoltageSource = false;

    [Header("Visual Effects")]
    [Tooltip("Amplitude of random voltage noise (flicker effect)")]
    [Range(0f, 1f)]
    public float voltageNoiseAmplitude = 0f;

    [Tooltip("Rate of voltage noise changes per second")]
    [Range(0.1f, 20f)]
    public float voltageNoiseRate = 5f;

    [TextArea]
    public string description;
}

[System.Serializable]
public class RotationVariant
{
    [Tooltip("Sub-ID number (e.g., '1' creates '1.1' if parent is '1')")]
    public string subID = "1";

    [Tooltip("Rotated prefab for this variant")]
    public GameObject prefab;

    [Tooltip("Grid size X for this rotation")]
    public int sizeX = 1;

    [Tooltip("Grid size Y for this rotation")]
    public int sizeY = 1;

    [Tooltip("Rotation in degrees (e.g., 90, 180, 270)")]
    public float rotationDegrees = 90f;
    
    [Tooltip("Prefab rotation offset (X, Y, Z in degrees) for this variant")]
    public Vector3 prefabRotation = Vector3.zero;
    
    [Tooltip("Optional plane prefab for this rotation variant")]
    public GameObject planePrefab;

    [Tooltip("Position offset for the plane (X, Y, Z in world units)")]
    public Vector3 planeOffset = Vector3.zero;

    [Tooltip("Visual offset for this rotation")]
    public Vector3 visualOffset = Vector3.zero;

    [Tooltip("Collision offset for this rotation")]
    public Vector3 collisionOffset = Vector3.zero;
}

[System.Serializable]
public class ObjectType
{
    public string typeName = "None";
    
    public ObjectType(string name = "None")
    {
        typeName = name;
    }
    
    public override string ToString()
    {
        return typeName;
    }
}