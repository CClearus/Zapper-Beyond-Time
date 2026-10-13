using UnityEngine;

/// <summary>
/// Realistic resistor component implementing Ohm's Law with thermal effects
/// V = I × R
/// P = I² × R = V² / R
/// </summary>
public class ElectricalResistor : MonoBehaviour
{
    [Header("Resistor Specifications")]
    [Tooltip("Nominal resistance in Ohms at 25°C")]
    public float nominalResistance = 1000f; // 1kΩ default
    
    [Tooltip("Power rating in Watts (e.g., 0.25W, 0.5W, 1W, 2W)")]
    public float powerRating = 0.25f;
    
    [Tooltip("Tolerance as percentage (e.g., 1% = 0.01, 5% = 0.05, 10% = 0.10)")]
    [Range(0.001f, 0.20f)]
    public float tolerance = 0.05f; // 5% default
    
    [Tooltip("Temperature coefficient in ppm/°C (parts per million per degree Celsius)")]
    public float temperatureCoefficient = 100f; // Typical for carbon film
    
    [Header("Thermal Properties")]
    [Tooltip("Thermal resistance from junction to ambient (°C/W)")]
    public float thermalResistance = 200f; // Typical for 1/4W resistor
    
    [Tooltip("Ambient temperature in °C")]
    public float ambientTemperature = 25f;
    
    [Tooltip("Maximum junction temperature before failure (°C)")]
    public float maxTemperature = 155f; // Typical for carbon film
    
    [Header("Visual Feedback")]
    [Tooltip("Material to apply color changes (optional)")]
    public Renderer resistorRenderer;
    
    [Tooltip("Normal operating color")]
    public Color normalColor = new Color(0.82f, 0.71f, 0.55f); // Tan/beige
    
    [Tooltip("Color when approaching max power")]
    public Color warmColor = new Color(1f, 0.6f, 0.2f); // Orange
    
    [Tooltip("Color when overheated")]
    public Color overheatedColor = new Color(1f, 0.1f, 0.1f); // Red
    
    [Tooltip("Color when burned out")]
    public Color burnedColor = new Color(0.2f, 0.2f, 0.2f); // Dark gray/black
    
    [Header("Runtime Status (Read-Only)")]
    [SerializeField] private float currentVoltage = 0f;
    [SerializeField] private float currentCurrent = 0f;
    [SerializeField] private float actualResistance = 0f;
    [SerializeField] private float powerDissipation = 0f;
    [SerializeField] private float junctionTemperature = 25f;
    [SerializeField] private float healthPercentage = 100f;
    [SerializeField] private bool isBurnedOut = false;
    
    // Internal state
    private float actualToleranceVariation;
    private float degradationFactor = 1f;
    private float thermalTimeConstant = 5f; // Seconds to reach thermal equilibrium
    private Material resistorMaterial;
    private PlacedObjectComponent placedComponent;
    
    void Start()
    {
        // Apply manufacturing tolerance (randomly varies within tolerance range)
        actualToleranceVariation = Random.Range(-tolerance, tolerance);
        actualResistance = nominalResistance * (1f + actualToleranceVariation);
        
        junctionTemperature = ambientTemperature;
        
        // Get material for visual feedback
        if (resistorRenderer != null)
        {
            resistorMaterial = resistorRenderer.material;
            UpdateVisualFeedback();
        }
        
        // Get reference to placed object data
        placedComponent = GetComponent<PlacedObjectComponent>();
        
        Debug.Log($"[RESISTOR] Initialized: {nominalResistance}Ω (±{tolerance * 100}%), Actual: {actualResistance:F2}Ω, Rated: {powerRating}W");
    }
    
    void Update()
    {
        if (isBurnedOut) return;
        
        // Update thermal model
        UpdateThermalModel();
        
        // Update visual feedback
        UpdateVisualFeedback();
        
        // Check for catastrophic failure
        CheckForFailure();
    }
    
    /// <summary>
    /// Calculate actual resistance considering temperature effects
    /// R(T) = R₀ × [1 + α(T - T₀)]
    /// where α = temperature coefficient in ppm/°C
    /// </summary>
    public float GetCurrentResistance()
    {
        if (isBurnedOut)
            return float.MaxValue; // Open circuit when burned
        
        // Temperature coefficient effect (α in ppm/°C converted to 1/°C)
        float alpha = temperatureCoefficient / 1000000f;
        float tempDelta = junctionTemperature - 25f;
        float temperatureFactor = 1f + (alpha * tempDelta);
        
        // Apply degradation from thermal stress
        return actualResistance * temperatureFactor * degradationFactor;
    }
    
    /// <summary>
    /// Update voltage and current from simulation
    /// Called by ElectricalSimulationSystem
    /// </summary>
    public void UpdateElectricalState(float voltage, float current)
    {
        currentVoltage = voltage;
        currentCurrent = current;
        
        // Calculate power dissipation: P = I² × R
        powerDissipation = current * current * GetCurrentResistance();
        
        // Alternative formula for verification: P = V × I
        float powerCheck = voltage * current;
        
        // Use average of both methods for numerical stability
        powerDissipation = (powerDissipation + powerCheck) / 2f;
    }
    
    /// <summary>
    /// Thermal model using Newton's law of cooling
    /// ΔT = P × Rθ (steady state)
    /// dT/dt = (T_target - T_current) / τ (transient)
    /// </summary>
    void UpdateThermalModel()
    {
        // Calculate target temperature based on power dissipation
        // T_junction = T_ambient + (P × R_thermal)
        float targetTemperature = ambientTemperature + (powerDissipation * thermalResistance);
        
        // Exponential approach to target temperature
        float tempDelta = targetTemperature - junctionTemperature;
        junctionTemperature += (tempDelta / thermalTimeConstant) * Time.deltaTime;
        
        // Degrade resistance if operating above 70% of max temperature
        float tempRatio = (junctionTemperature - ambientTemperature) / (maxTemperature - ambientTemperature);
        if (tempRatio > 0.7f)
        {
            // Accelerated aging when hot
            float agingRate = (tempRatio - 0.7f) * 0.01f * Time.deltaTime;
            degradationFactor += agingRate;
            healthPercentage = Mathf.Max(0f, 100f - ((degradationFactor - 1f) * 100f));
        }
    }
    
    /// <summary>
    /// Check for resistor failure conditions
    /// </summary>
    void CheckForFailure()
    {
        // Power overload: exceeding rated power
        if (powerDissipation > powerRating * 1.5f)
        {
            float overloadDuration = powerDissipation / powerRating;
            float failureChance = (overloadDuration - 1.5f) * 0.1f * Time.deltaTime;
            
            if (Random.value < failureChance)
            {
                BurnOut("power overload");
                return;
            }
        }
        
        // Thermal failure: junction temperature too high
        if (junctionTemperature > maxTemperature)
        {
            float tempExcess = junctionTemperature - maxTemperature;
            float failureChance = tempExcess * 0.01f * Time.deltaTime;
            
            if (Random.value < failureChance)
            {
                BurnOut("thermal runaway");
                return;
            }
        }
        
        // Degradation failure: resistance drifted too far
        if (degradationFactor > 2f)
        {
            BurnOut("excessive degradation");
        }
    }
    
    /// <summary>
    /// Catastrophic failure - resistor burns out
    /// </summary>
    void BurnOut(string reason)
    {
        isBurnedOut = true;
        degradationFactor = float.MaxValue;
        healthPercentage = 0f;
        
        Debug.LogWarning($"[RESISTOR] BURNED OUT due to {reason}! " +
                        $"Final stats: P={powerDissipation:F3}W (rated {powerRating}W), " +
                        $"T={junctionTemperature:F1}°C (max {maxTemperature}°C)");
        
        // Update visual
        if (resistorMaterial != null)
        {
            resistorMaterial.color = burnedColor;
            
            // Optional: add emissive for "smoking" effect
            if (resistorMaterial.HasProperty("_EmissionColor"))
            {
                resistorMaterial.EnableKeyword("_EMISSION");
                resistorMaterial.SetColor("_EmissionColor", Color.red * 0.3f);
            }
        }
        
        // Notify simulation system that component failed
        if (placedComponent != null && placedComponent.placedObjectData != null)
        {
            // Set resistance to very high value in the node data
            // This will be picked up by the simulation system
        }
    }
    
    /// <summary>
    /// Update visual appearance based on operating conditions
    /// </summary>
    void UpdateVisualFeedback()
    {
        if (resistorMaterial == null || isBurnedOut) return;
        
        // Calculate stress factor (0 = no stress, 1 = max stress)
        float powerStress = Mathf.Clamp01(powerDissipation / powerRating);
        float tempStress = Mathf.Clamp01((junctionTemperature - ambientTemperature) / (maxTemperature - ambientTemperature));
        float overallStress = Mathf.Max(powerStress, tempStress);
        
        // Color interpolation based on stress
        Color targetColor;
        if (overallStress < 0.5f)
        {
            // Normal to warm
            targetColor = Color.Lerp(normalColor, warmColor, overallStress * 2f);
        }
        else
        {
            // Warm to overheated
            targetColor = Color.Lerp(warmColor, overheatedColor, (overallStress - 0.5f) * 2f);
        }
        
        resistorMaterial.color = targetColor;
        
        // Add subtle emissive glow when hot
        if (overallStress > 0.6f && resistorMaterial.HasProperty("_EmissionColor"))
        {
            resistorMaterial.EnableKeyword("_EMISSION");
            float emissionStrength = (overallStress - 0.6f) * 0.5f;
            resistorMaterial.SetColor("_EmissionColor", warmColor * emissionStrength);
        }
    }
    
    /// <summary>
    /// Get detailed status information for UI display
    /// </summary>
    public string GetStatusReport()
    {
        if (isBurnedOut)
        {
            return "STATUS: BURNED OUT\n" +
                   "Resistance: OPEN CIRCUIT\n" +
                   "Component requires replacement";
        }
        
        float tempRise = junctionTemperature - ambientTemperature;
        float powerPercent = (powerDissipation / powerRating) * 100f;
        
        return $"Nominal: {FormatResistance(nominalResistance)}\n" +
               $"Actual: {FormatResistance(GetCurrentResistance())}\n" +
               $"Voltage: {currentVoltage:F3}V\n" +
               $"Current: {FormatCurrent(currentCurrent)}\n" +
               $"Power: {powerDissipation:F3}W ({powerPercent:F1}% of {powerRating}W)\n" +
               $"Temp: {junctionTemperature:F1}°C (ΔT: +{tempRise:F1}°C)\n" +
               $"Health: {healthPercentage:F1}%";
    }
    
    // Helper formatting methods
    string FormatResistance(float ohms)
    {
        if (ohms >= 1000000f) return $"{ohms / 1000000f:F2}MΩ";
        if (ohms >= 1000f) return $"{ohms / 1000f:F2}kΩ";
        return $"{ohms:F2}Ω";
    }
    
    string FormatCurrent(float amps)
    {
        if (amps >= 1f) return $"{amps:F3}A";
        if (amps >= 0.001f) return $"{amps * 1000f:F2}mA";
        if (amps >= 0.000001f) return $"{amps * 1000000f:F2}µA";
        return $"{amps * 1000000000f:F2}nA";
    }
    
    // Public getters for simulation system
    public float GetPowerDissipation() => powerDissipation;
    public float GetJunctionTemperature() => junctionTemperature;
    public float GetHealthPercentage() => healthPercentage;
    public bool IsBurnedOut() => isBurnedOut;
    
    void OnDestroy()
    {
        // Clean up material instance
        if (resistorMaterial != null)
        {
            Destroy(resistorMaterial);
        }
    }
}