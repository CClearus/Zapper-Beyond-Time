using UnityEngine;

public class WireModeManager : MonoBehaviour
{
    public static WireModeManager Instance { get; private set; }

    [Tooltip("If true, terminals become interactable (visible).")]
    public bool wiringMode = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ToggleWireMode()
    {
        wiringMode = !wiringMode;
        ApplyTerminalVisibility();
    }

    public void EnableWireMode()
    {
        wiringMode = true;
        ApplyTerminalVisibility();
    }

    public void DisableWireMode()
    {
        wiringMode = false;
        ApplyTerminalVisibility();
    }

    private void ApplyTerminalVisibility()
    {
        int terminalLayer = LayerMask.NameToLayer("Terminal");
        int nodeLayer = LayerMask.NameToLayer("Node");
        
        // Find all WireNode components (works even if disabled)
        WireNode[] allNodes = Resources.FindObjectsOfTypeAll<WireNode>();
        
        foreach (WireNode node in allNodes)
        {
            // Make sure it's a scene object, not a prefab
            if (node.gameObject.hideFlags == HideFlags.None && node.gameObject.scene.isLoaded)
            {
                node.gameObject.SetActive(wiringMode);
                Debug.Log($"Set WireNode {node.gameObject.name} to {wiringMode}");
            }
        }
        
        // Also handle any objects in Terminal and Node layers
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Check if object is in Terminal or Node layer
            if (obj.layer == terminalLayer || obj.layer == nodeLayer)
            {
                // Make sure it's a scene object, not a prefab or asset
                if (obj.hideFlags == HideFlags.None && obj.scene.isLoaded)
                {
                    obj.SetActive(wiringMode);
                    Debug.Log($"Set {obj.name} (Layer: {LayerMask.LayerToName(obj.layer)}) to {wiringMode}");
                }
            }
        }
        
        // ✅ NEW: Notify all FixedPlacementObjects
        FixedPlacementObject[] fixedObjects = FindObjectsByType<FixedPlacementObject>(FindObjectsSortMode.None);
        foreach (FixedPlacementObject fixedObj in fixedObjects)
        {
            fixedObj.OnWireModeChanged(wiringMode);
        }
        
        Debug.Log($"Wire mode set to: {wiringMode}");
    }
}