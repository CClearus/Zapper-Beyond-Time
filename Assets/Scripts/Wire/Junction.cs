using UnityEngine;

public class ElectricalJunction : MonoBehaviour
{
    public enum JunctionType
    {
        Parallel,  // Splits current, maintains voltage
        Series     // Maintains current, adds voltage
    }
    
    public JunctionType junctionType = JunctionType.Parallel;
    
    // Visual feedback
    private Renderer junctionRenderer;
    private Material junctionMaterial;
    
    void Start()
    {
        junctionRenderer = GetComponent<Renderer>();
        if (junctionRenderer != null)
        {
            junctionMaterial = junctionRenderer.material;
            UpdateVisual();
        }
    }
    
    public void ToggleType()
    {
        junctionType = junctionType == JunctionType.Parallel ? JunctionType.Series : JunctionType.Parallel;
        UpdateVisual();
        Debug.Log($"[JUNCTION] Type changed to: {junctionType}");
    }
    
    void UpdateVisual()
    {
        if (junctionMaterial != null)
        {
            // Parallel = Blue, Series = Orange
            Color color = junctionType == JunctionType.Parallel ? Color.cyan : new Color(1f, 0.5f, 0f);
            junctionMaterial.color = color;
        }
    }
}