using UnityEngine;

/// <summary>
/// Makes a light bulb's brightness respond dynamically to electrical current.
/// Attach this to your light bulb prefab alongside the PlacedObjectComponent.
/// </summary>
public class DynamicLightBulb : MonoBehaviour
{
    [Header("Light References")]
    [Tooltip("The Light component on this object (auto-assigned if null)")]
    public Light lightSource;

    [Header("Brightness Settings")]
    [Tooltip("Minimum light intensity when current is zero")]
    [Range(0f, 10f)]
    public float minIntensity = 0f;

    [Tooltip("Maximum light intensity at rated current")]
    [Range(0f, 50f)]
    public float maxIntensity = 8f;

    [Tooltip("Current (in Amps) at which the bulb reaches maximum brightness")]
    public float ratedCurrent = 1f;

    [Header("Response Curve")]
    [Tooltip("How the brightness responds to current (Linear = straight, Quadratic = more realistic)")]
    public BrightnessResponse responseType = BrightnessResponse.Quadratic;

    [Tooltip("Smooth transition speed (higher = faster response)")]
    [Range(0.1f, 20f)]
    public float smoothSpeed = 5f;

    [Header("Color Temperature")]
    [Tooltip("Enable dynamic color based on current (dim = red, bright = white)")]
    public bool useDynamicColor = true;

    [Tooltip("Color when barely lit (low current)")]
    public Color lowCurrentColor = new Color(1f, 0.3f, 0f); // Orange-red

    [Tooltip("Color at full brightness (rated current)")]
    public Color fullCurrentColor = Color.white;

    [Header("Flicker Effect")]
    [Tooltip("Add subtle flicker when current is low or unstable")]
    public bool enableFlicker = true;

    [Tooltip("Intensity of flicker effect")]
    [Range(0f, 0.5f)]
    public float flickerAmount = 0.1f;

    [Tooltip("Speed of flicker")]
    [Range(0.1f, 10f)]
    public float flickerSpeed = 3f;

    [Header("Burnout Effect")]
    [Tooltip("Show visual effect when component is damaged")]
    public bool showBurnoutEffect = true;

    [Tooltip("Color when burned out")]
    public Color burnoutColor = new Color(0.5f, 0.5f, 0.5f); // Dark gray

    // Internal state
    private PlacedObjectComponent placedComponent;
    private ElectricalSimulationSystem electricalSystem;
    private float targetIntensity;
    private float currentIntensity;
    private Color targetColor;
    private float flickerOffset;
    private bool isBurnedOut = false;

    void Start()
    {
        // Auto-assign light component
        if (lightSource == null)
        {
            lightSource = GetComponent<Light>();
            if (lightSource == null)
            {
                Debug.LogError($"[DynamicLightBulb] No Light component found on {gameObject.name}!");
                enabled = false;
                return;
            }
        }

        // Find the PlacedObjectComponent
        placedComponent = GetComponent<PlacedObjectComponent>();
        if (placedComponent == null)
        {
            Debug.LogError($"[DynamicLightBulb] No PlacedObjectComponent found on {gameObject.name}!");
            enabled = false;
            return;
        }

        // Find the electrical system
        electricalSystem = FindFirstObjectByType<ElectricalSimulationSystem>();
        if (electricalSystem == null)
        {
            Debug.LogWarning($"[DynamicLightBulb] No ElectricalSimulationSystem found in scene!");
        }

        // Initialize
        flickerOffset = Random.Range(0f, 100f);
        lightSource.intensity = minIntensity;
        currentIntensity = minIntensity;

        Debug.Log($"[DynamicLightBulb] Initialized on {gameObject.name}");
    }

    void Update()
    {
        if (electricalSystem == null || !electricalSystem.IsSimulationRunning())
        {
            // Turn off light when simulation is stopped
            targetIntensity = minIntensity;
            targetColor = lowCurrentColor;
        }
        else
        {
            UpdateLightFromElectricalData();
        }

        // Smooth transitions
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothSpeed);
        lightSource.intensity = currentIntensity;

        if (useDynamicColor && !isBurnedOut)
        {
            lightSource.color = Color.Lerp(lightSource.color, targetColor, Time.deltaTime * smoothSpeed);
        }
    }

    void UpdateLightFromElectricalData()
    {
        if (placedComponent == null || placedComponent.placedObjectData == null)
            return;

        // Access the electrical node data through the simulation system
        float current = GetCurrentThroughBulb();
        bool isFunctional = IsComponentFunctional();

        // Check if burned out
        if (!isFunctional && !isBurnedOut)
        {
            TriggerBurnout();
            return;
        }

        if (isBurnedOut)
        {
            targetIntensity = 0f;
            return;
        }

        // Calculate brightness based on current
        float currentRatio = Mathf.Clamp01(current / ratedCurrent);
        
        // Apply response curve
        float brightnessRatio = CalculateBrightnessRatio(currentRatio);

        // Calculate target intensity
        targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, brightnessRatio);

        // Add flicker effect at low currents
        if (enableFlicker && currentRatio < 0.3f && currentRatio > 0.01f)
        {
            float flicker = Mathf.PerlinNoise(Time.time * flickerSpeed + flickerOffset, 0f);
            flicker = (flicker - 0.5f) * 2f * flickerAmount; // Center around 0
            targetIntensity += targetIntensity * flicker;
        }

        // Update color based on current
        if (useDynamicColor)
        {
            targetColor = Color.Lerp(lowCurrentColor, fullCurrentColor, brightnessRatio);
        }
    }

    float CalculateBrightnessRatio(float currentRatio)
    {
        switch (responseType)
        {
            case BrightnessResponse.Linear:
                return currentRatio;

            case BrightnessResponse.Quadratic:
                // More realistic - brightness increases faster at higher currents
                return currentRatio * currentRatio;

            case BrightnessResponse.SquareRoot:
                // Dimmer at low currents, brighter faster
                return Mathf.Sqrt(currentRatio);

            case BrightnessResponse.Exponential:
                // Very dim until near rated current้
                return Mathf.Pow(currentRatio, 3f);

            default:
                return currentRatio;
        }
    }

    float GetCurrentThroughBulb()
    {
        // This requires accessing the electrical simulation's node data
        // We'll use reflection to access the private dictionary
        if (electricalSystem == null) return 0f;

        var nodeField = typeof(ElectricalSimulationSystem).GetField("electricalNodes", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (nodeField != null)
        {
            var nodesDict = nodeField.GetValue(electricalSystem) as System.Collections.IDictionary;
            if (nodesDict != null)
            {
                foreach (System.Collections.DictionaryEntry entry in nodesDict)
                {
                    var placedObj = entry.Key as PlacedGridObject;
                    if (placedObj == placedComponent.placedObjectData)
                    {
                        // Access the ElectricalNode
                        var node = entry.Value;
                        var currentProperty = node.GetType().GetField("current");
                        if (currentProperty != null)
                        {
                            return (float)currentProperty.GetValue(node);
                        }
                    }
                }
            }
        }

        return 0f;
    }

    bool IsComponentFunctional()
    {
        if (electricalSystem == null) return true;

        var nodeField = typeof(ElectricalSimulationSystem).GetField("electricalNodes", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (nodeField != null)
        {
            var nodesDict = nodeField.GetValue(electricalSystem) as System.Collections.IDictionary;
            if (nodesDict != null)
            {
                foreach (System.Collections.DictionaryEntry entry in nodesDict)
                {
                    var placedObj = entry.Key as PlacedGridObject;
                    if (placedObj == placedComponent.placedObjectData)
                    {
                        var node = entry.Value;
                        var functionalProperty = node.GetType().GetField("isFunctional");
                        if (functionalProperty != null)
                        {
                            return (bool)functionalProperty.GetValue(node);
                        }
                    }
                }
            }
        }

        return true;
    }

    void TriggerBurnout()
    {
        isBurnedOut = true;
        targetIntensity = 0f;

        if (showBurnoutEffect)
        {
            // Change the bulb's material/color to show it's burned out
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    if (mat.HasProperty("_Color"))
                        mat.color = burnoutColor;
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", burnoutColor);
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", Color.black);
                }
            }

            lightSource.color = burnoutColor;
        }

        Debug.Log($"[DynamicLightBulb] {gameObject.name} burned out!");
    }

    // Public method to reset the bulb (if you want to add a "replace bulb" feature)
    public void ResetBulb()
    {
        isBurnedOut = false;
        currentIntensity = minIntensity;
        lightSource.intensity = minIntensity;
        
        if (useDynamicColor)
            lightSource.color = lowCurrentColor;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the light range in the editor
        if (lightSource != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, lightSource.range);
        }
    }
}

public enum BrightnessResponse
{
    Linear,      // Direct 1:1 relationship
    Quadratic,   // More realistic for incandescent bulbs
    SquareRoot,  // Brighter at lower currents
    Exponential  // Very dim until near rated current
}