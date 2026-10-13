using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class ElectricalSwitch : MonoBehaviour
{
    [Header("Switch Settings")]
    [Tooltip("Is the switch currently ON (closed circuit)?")]
    public bool isOn = false;
    
    [Tooltip("Switch display name")]
    public string switchName = "Switch";
    
    [Header("Switch as Electrical Component")]
    [Tooltip("The PlacedObjectComponent of this switch (auto-assigned)")]
    public PlacedObjectComponent switchComponent;
    
    [Header("Switch Behavior")]
    [Tooltip("Resistance when switch is CLOSED (ON) in Ohms - very low")]
    public float closedResistance = 0.01f;
    
    [Tooltip("Resistance when switch is OPEN (OFF) in Ohms - very high")]
    public float openResistance = 10000000f; // 10MΩ = effectively open circuit
    
    [Header("3D Model Interaction")]
    [Tooltip("The 3D model/collider that can be clicked to toggle")]
    public GameObject switchModel;
    
    [Tooltip("Layer mask for raycast (Default should work)")]
    public LayerMask clickableLayer = -1;
    
    [Header("UI Status Display - Match ElectricalSim Style")]
    [Tooltip("TextMeshProUGUI for switch name")]
    public TextMeshProUGUI switchNameText;
    
    [Tooltip("TextMeshProUGUI for switch status")]
    public TextMeshProUGUI switchStatusText;
    
    [Tooltip("TextMeshProUGUI for resistance display")]
    public TextMeshProUGUI resistanceText;
    
    
    [Tooltip("Value text for resistance (with formatting)")]
    public TextMeshProUGUI resistanceValueText;
    
    [Tooltip("Circuit info text")]
    public TextMeshProUGUI circuitInfoText;
    
    [Header("UI Dynamic Positioning (Match ElectricalSim)")]
    [Tooltip("Color for numeric values")]
    public Color valueColor = Color.white;
    
    [Tooltip("Color for unit text (V, A, Ω, etc.)")]
    public Color unitColor = new Color(0.67f, 0.67f, 0.67f, 1f);
    
    [Tooltip("Unit text size as percentage (100 = same size as value)")]
    [Range(50, 100)]
    public int unitSizePercent = 75;
    
    [Header("Interaction")]
    [Tooltip("Key to toggle switch")]
    public KeyCode toggleKey = KeyCode.None;
    
    [Tooltip("UI Button to toggle switch (optional)")]
    public Button toggleButton;
    
    [Tooltip("Interaction range for 3D model click (0 = infinite)")]
    public float interactionRange = 10f;
    
    // Internal state
    private ElectricalSimulationSystem simulationSystem;
    private Camera playerCamera;
    private PlacedGridObject switchPlacedObject;
    
    void Start()
    {
        // Find simulation system
        simulationSystem = FindFirstObjectByType<ElectricalSimulationSystem>();
        if (simulationSystem == null)
        {
            Debug.LogWarning($"[SWITCH] {switchName}: ElectricalSimulationSystem not found!");
        }
        
        playerCamera = Camera.main;
        
        // Auto-assign switch model if not set
        // Auto-assign switch model if not set
    if (switchModel == null)
    {
        // Try to find a child with a collider, or use this gameObject
        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            switchModel = colliders[0].gameObject;
            Debug.Log($"[SWITCH] Auto-assigned switch model: {switchModel.name}");
        }
        else
        {
            switchModel = gameObject;
            Debug.LogWarning($"[SWITCH] No collider found! Add a Box Collider to make switch clickable.");
        }
    }
        
        // Auto-assign switch component if not set
        if (switchComponent == null)
        {
            switchComponent = GetComponent<PlacedObjectComponent>();
        }
        
        // Get the PlacedGridObject reference
        if (switchComponent != null && switchComponent.placedObjectData != null)
        {
            switchPlacedObject = switchComponent.placedObjectData;
            Debug.Log($"[SWITCH] {switchName}: Found PlacedGridObject: {switchPlacedObject.objectData.displayName}");
        }
        else
        {
            Debug.LogError($"[SWITCH] {switchName}: No PlacedObjectComponent or PlacedGridObject found! Switch won't work in simulation.");
        }
        
        // Setup button listener
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(Toggle);
        }
        
        // Apply initial state
        ApplySwitchState();
        UpdateStatusDisplay();
    }
    
    void Update()
    {
        // Toggle with key
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
        
        // Handle 3D model clicking
        if (Input.GetMouseButtonDown(0))
        {
            HandleModelClick();
        }
        
        // Update display continuously
        UpdateStatusDisplay();
    }
    
    void HandleModelClick()
    {
        if (playerCamera == null || switchModel == null) return;
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Raycast with layer mask
        if (Physics.Raycast(ray, out hit, interactionRange > 0 ? interactionRange : Mathf.Infinity, clickableLayer))
        {
            // Check if we hit the switch model or any of its children
            Transform hitTransform = hit.collider.transform;
            
            if (hitTransform == switchModel.transform || hitTransform.IsChildOf(switchModel.transform))
            {
                // Check distance if range is set
                if (interactionRange > 0)
                {
                    float distance = Vector3.Distance(playerCamera.transform.position, hit.point);
                    if (distance > interactionRange)
                        return;
                }
                
                Toggle();
            }
        }
    }
    
    public void Toggle()
    {
        isOn = !isOn;
        ApplySwitchState();
        UpdateStatusDisplay();
        
        Debug.Log($"[SWITCH] {switchName}: Toggled to {(isOn ? "CLOSED (ON)" : "OPEN (OFF)")}");
        
        // Force simulation to re-initialize circuits if running
        if (simulationSystem != null && simulationSystem.IsSimulationRunning())
        {
            // The simulation will pick up the resistance change automatically
            // No need to restart - the UpdateElectricalSimulation will handle it
            Debug.Log($"[SWITCH] {switchName}: Resistance changed to {(isOn ? closedResistance : openResistance)}Ω");
        }
    }
    
    public void SetState(bool state)
    {
        if (isOn != state)
        {
            isOn = state;
            ApplySwitchState();
            UpdateStatusDisplay();
        }
    }
    
    void ApplySwitchState()
{
    if (switchPlacedObject == null || switchPlacedObject.objectData == null)
    {
        Debug.LogWarning($"[SWITCH] {switchName}: Cannot apply state - no PlacedGridObject!");
        return;
    }
    
    // Change the resistance based on switch state
    float newResistance = isOn ? closedResistance : openResistance;
    switchPlacedObject.objectData.resistance = newResistance;
    
    // CRITICAL: Also update the ElectricalNode if simulation is running
    if (simulationSystem != null && simulationSystem.IsSimulationRunning())
    {
        // Force the simulation to see the new resistance
        var electricalNodes = typeof(ElectricalSimulationSystem)
            .GetField("electricalNodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(simulationSystem) as Dictionary<PlacedGridObject, ElectricalNode>;
        
        if (electricalNodes != null && electricalNodes.ContainsKey(switchPlacedObject))
        {
            electricalNodes[switchPlacedObject].resistance = newResistance;
            Debug.Log($"[SWITCH] {switchName}: Updated ElectricalNode resistance to {newResistance}Ω");
        }
    }
    
    Debug.Log($"[SWITCH] {switchName}: Set resistance to {newResistance}Ω (Switch {(isOn ? "CLOSED" : "OPEN")})");
}
    
    void UpdateStatusDisplay()
    {
        // Update switch name
        if (switchNameText != null)
        {
            switchNameText.text = switchName;
        }
        
        // Update status (CLOSED/OPEN or ON/OFF)
        if (switchStatusText != null)
        {
            switchStatusText.text = isOn ? "CLOSED (ON)" : "OPEN (OFF)";
        }
        
        // Update resistance display
        float currentResistance = isOn ? closedResistance : openResistance;
        
        if (resistanceText != null)
        {
            resistanceText.text = FormatResistance(currentResistance);
        }
        
        // Update with formatted value/unit (ElectricalSim style)
        if (resistanceValueText != null)
        {
            SetFormattedValue(resistanceValueText, currentResistance, "Ω");
        }
        
        // Update circuit info
        if (circuitInfoText != null)
        {
            string circuitStatus = isOn ? "Circuit CLOSED - Current flows" : "Circuit OPEN - No current";
            circuitInfoText.text = circuitStatus;
        }
    }
    
    string FormatResistance(float resistance)
    {
        // Format large resistances nicely
        if (resistance >= 1000000f)
        {
            return $"{(resistance / 1000000f):F2}MΩ";
        }
        else if (resistance >= 1000f)
        {
            return $"{(resistance / 1000f):F2}kΩ";
        }
        else
        {
            return $"{resistance:F2}Ω";
        }
    }
    
    void SetFormattedValue(TextMeshProUGUI textField, float value, string unit, int decimalPlaces = 2)
    {
        if (textField == null) return;
        
        textField.richText = true;
        
        // Format based on magnitude
        string valueStr;
        string actualUnit = unit;
        
        if (value >= 1000000f)
        {
            valueStr = (value / 1000000f).ToString($"F{decimalPlaces}");
            actualUnit = "M" + unit; // MΩ
        }
        else if (value >= 1000f)
        {
            valueStr = (value / 1000f).ToString($"F{decimalPlaces}");
            actualUnit = "k" + unit; // kΩ
        }
        else
        {
            valueStr = value.ToString($"F{decimalPlaces}");
        }
        
        string unitColorHex = ColorUtility.ToHtmlStringRGBA(unitColor);
        textField.text = $"{valueStr} <size={unitSizePercent}%><color=#{unitColorHex}>{actualUnit}</color></size>";
    }
    
    // Public methods for external control
    public void TurnOn()
    {
        SetState(true);
    }
    
    public void TurnOff()
    {
        SetState(false);
    }
    
    public bool IsOn()
    {
        return isOn;
    }
    
    public float GetCurrentResistance()
    {
        return isOn ? closedResistance : openResistance;
    }
    
    void OnDrawGizmos()
    {
        // Draw switch state indicator
        Gizmos.color = isOn ? Color.green : Color.red;
        
        if (switchModel != null)
        {
            Gizmos.DrawWireSphere(switchModel.transform.position, 0.3f);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
        
        // Draw interaction range if set
        if (interactionRange > 0 && switchModel != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(switchModel.transform.position, interactionRange);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Highlight switch model
        if (switchModel != null)
        {
            Gizmos.color = isOn ? Color.green : Color.red;
            Gizmos.DrawWireCube(switchModel.transform.position, Vector3.one * 0.5f);
        }
        
        // Draw label
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f, 
            $"{switchName}\n{(isOn ? "CLOSED" : "OPEN")}\n{FormatResistance(isOn ? closedResistance : openResistance)}"
        );
        #endif
    }
    
    void OnDestroy()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(Toggle);
        }
    }
    
    #if UNITY_EDITOR
    void OnValidate()
    {
        // Update display in editor when values change
        if (Application.isPlaying)
        {
            ApplySwitchState();
            UpdateStatusDisplay();
        }
    }
    #endif
}