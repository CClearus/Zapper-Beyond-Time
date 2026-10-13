using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlacementInfoUI : MonoBehaviour
{
    [Header("References")]
    public ObjectPlacementSystem placementSystem;
    public GridObjectDatabaseSO objectDatabase;
    
    [Header("UI Panel")]
    public GameObject infoPanel;
    
    [Header("UI Elements - Assign in Inspector")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public RawImage objectImage;
    public TextMeshProUGUI descriptionText;
    
    [Header("Editable Input Fields")]
    public TMP_InputField voltageInput;
    public TMP_InputField maxVoltageInput;
    public TMP_InputField resistanceInput;
    public TMP_InputField maxCurrentInput;
    
    private GridObjectData currentObjectData;
    
    void Start()
    {
        // Find placement system if not assigned
        if (placementSystem == null)
            placementSystem = FindFirstObjectByType<ObjectPlacementSystem>();
        
        // Subscribe to placement mode changes
        if (placementSystem != null)
        {
            placementSystem.OnPlacementModeChanged += OnPlacementModeChanged;
        }
        
        // Setup input field listeners
        SetupInputFieldListeners();
        
        // Hide panel initially
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (placementSystem != null)
        {
            placementSystem.OnPlacementModeChanged -= OnPlacementModeChanged;
        }
    }
    
    void SetupInputFieldListeners()
    {
        if (voltageInput != null)
        {
            voltageInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            voltageInput.onEndEdit.AddListener(OnVoltageChanged);
        }
        
        if (maxVoltageInput != null)
        {
            maxVoltageInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            maxVoltageInput.onEndEdit.AddListener(OnMaxVoltageChanged);
        }
        
        if (resistanceInput != null)
        {
            resistanceInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            resistanceInput.onEndEdit.AddListener(OnResistanceChanged);
        }
        
        if (maxCurrentInput != null)
        {
            maxCurrentInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            maxCurrentInput.onEndEdit.AddListener(OnMaxCurrentChanged);
        }
    }
    
    void OnPlacementModeChanged(bool isActive)
    {
        if (isActive)
        {
            // Get current object data
            string currentObjectID = placementSystem.GetCurrentObjectID();
            currentObjectData = objectDatabase.GetObjectByID(currentObjectID);
            
            if (currentObjectData != null)
            {
                ShowObjectInfo(currentObjectData);
            }
        }
        else
        {
            // Hide panel when placement mode is off
            if (infoPanel != null)
                infoPanel.SetActive(false);
            
            currentObjectData = null;
        }
    }
    
    void ShowObjectInfo(GridObjectData objectData)
    {
        if (infoPanel != null)
            infoPanel.SetActive(true);
        
        // Set title
        if (titleText != null)
            titleText.text = objectData.displayName;
        
        // Set subtitle (object type)
        if (subtitleText != null)
            subtitleText.text = objectData.objectTypeName;
        
        // Set object image
        if (objectImage != null && objectData.objectImage != null)
        {
            objectImage.texture = objectData.objectImage;
            objectImage.gameObject.SetActive(true);
        }
        else if (objectImage != null)
        {
            objectImage.gameObject.SetActive(false);
        }
        
        // Set description
        if (descriptionText != null)
            descriptionText.text = objectData.description;
        
        // Set voltage input field
        if (voltageInput != null)
        {
            if (objectData.isVoltageSource)
            {
                voltageInput.text = objectData.voltage.ToString("F1");
                voltageInput.interactable = true;
            }
            else
            {
                voltageInput.text = "-";
                voltageInput.interactable = false;
            }
        }
        
        // Set max voltage input field
        if (maxVoltageInput != null)
        {
            if (objectData.maxVoltage > 0)
            {
                maxVoltageInput.text = objectData.maxVoltage.ToString("F1");
                maxVoltageInput.interactable = true;
            }
            else
            {
                maxVoltageInput.text = "No Limit";
                maxVoltageInput.interactable = false;
            }
        }
        
        // Set resistance input field
        if (resistanceInput != null)
        {
            if (objectData.resistance > 0)
            {
                resistanceInput.text = objectData.resistance.ToString("F1");
                resistanceInput.interactable = true;
            }
            else
            {
                resistanceInput.text = "-";
                resistanceInput.interactable = false;
            }
        }
        
        // Set max current input field
        if (maxCurrentInput != null)
        {
            if (objectData.maxAmperage > 0)
            {
                maxCurrentInput.text = objectData.maxAmperage.ToString("F2");
                maxCurrentInput.interactable = true;
            }
            else
            {
                maxCurrentInput.text = "No Limit";
                maxCurrentInput.interactable = false;
            }
        }
    }
    
    // Input field change handlers
    void OnVoltageChanged(string value)
    {
        if (currentObjectData != null && float.TryParse(value, out float newVoltage))
        {
            currentObjectData.voltage = newVoltage;
            voltageInput.text = newVoltage.ToString("F1");
            Debug.Log($"Voltage updated to: {newVoltage}V");
        }
        else if (currentObjectData != null)
        {
            // Reset to original value if parsing fails
            voltageInput.text = currentObjectData.voltage.ToString("F1");
        }
    }
    
    void OnMaxVoltageChanged(string value)
    {
        if (currentObjectData != null && float.TryParse(value, out float newMaxVoltage))
        {
            currentObjectData.maxVoltage = newMaxVoltage;
            maxVoltageInput.text = newMaxVoltage.ToString("F1");
            Debug.Log($"Max Voltage updated to: {newMaxVoltage}V");
        }
        else if (currentObjectData != null)
        {
            // Reset to original value if parsing fails
            if (currentObjectData.maxVoltage > 0)
                maxVoltageInput.text = currentObjectData.maxVoltage.ToString("F1");
        }
    }
    
    void OnResistanceChanged(string value)
    {
        if (currentObjectData != null && float.TryParse(value, out float newResistance))
        {
            currentObjectData.resistance = newResistance;
            resistanceInput.text = newResistance.ToString("F1");
            Debug.Log($"Resistance updated to: {FormatResistance(newResistance)}");
        }
        else if (currentObjectData != null)
        {
            // Reset to original value if parsing fails
            resistanceInput.text = currentObjectData.resistance.ToString("F1");
        }
    }
    
    void OnMaxCurrentChanged(string value)
    {
        if (currentObjectData != null && float.TryParse(value, out float newMaxCurrent))
        {
            currentObjectData.maxAmperage = newMaxCurrent;
            maxCurrentInput.text = newMaxCurrent.ToString("F2");
            Debug.Log($"Max Current updated to: {newMaxCurrent}A");
        }
        else if (currentObjectData != null)
        {
            // Reset to original value if parsing fails
            if (currentObjectData.maxAmperage > 0)
                maxCurrentInput.text = currentObjectData.maxAmperage.ToString("F2");
        }
    }
    
    string FormatResistance(float resistance)
    {
        if (resistance >= 1000000)
            return (resistance / 1000000f).ToString("F1") + "MΩ";
        else if (resistance >= 1000)
            return (resistance / 1000f).ToString("F1") + "kΩ";
        else
            return resistance.ToString("F1") + "Ω";
    }
}