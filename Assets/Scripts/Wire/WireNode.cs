using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Connection node created at ends/turns. Stores polarity/tag and connected line renderers.
/// Multiple wires can connect to the same node.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WireNode : MonoBehaviour
{
    [Tooltip("Set by the placement system. Should match terminal tag: e.g. 'Plus' or 'Minus'")]
    public string polarity;
    
    // Junction functionality
    public bool isJunction = false;
    public ElectricalJunction junctionComponent;

    // Connected LineRenderers (wires)
    public List<LineRenderer> connectedWires = new List<LineRenderer>();

    /// <summary>
    /// Called when a wire connects to this node.
    /// </summary>
    public void AddConnection(LineRenderer lr)
    {
        if (!connectedWires.Contains(lr))
            connectedWires.Add(lr);
    }

    /// <summary>
    /// Called when a wire disconnects (if you implement removal later).
    /// </summary>
    public void RemoveConnection(LineRenderer lr)
    {
        if (connectedWires.Contains(lr))
            connectedWires.Remove(lr);
    }

    /// <summary>
    /// Convert this node into a junction point for mixed circuits.
    /// </summary>
    public void ConvertToJunction()
    {
        if (!isJunction)
        {
            isJunction = true;
            junctionComponent = gameObject.AddComponent<ElectricalJunction>();
            Debug.Log("[WIRE] Node converted to junction");
        }
    }
}