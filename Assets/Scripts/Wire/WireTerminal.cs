using UnityEngine;

/// <summary>
/// Optional helper script you can put on terminal objects.
/// The system also reads the GameObject tag for polarity, but this stores it too.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WireTerminal : MonoBehaviour
{
    [Tooltip("Optional explicit polarity. If empty, system will read GameObject.tag.")]
    public string explicitPolarity = "";

    public string Polarity
    {
        get
        {
            if (!string.IsNullOrEmpty(explicitPolarity)) return explicitPolarity;
            return gameObject.tag;
        }
    }
}
