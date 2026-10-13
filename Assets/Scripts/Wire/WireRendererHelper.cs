using UnityEngine;

/// <summary>
/// Small helper to initialize line renderer on a wire prefab.
/// Not required but useful for sane defaults.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class WireRendererHelper : MonoBehaviour
{
    public float startWidth = 0.05f;
    public float endWidth = 0.05f;

    private void Reset()
    {
        var lr = GetComponent<LineRenderer>();
        lr.positionCount = 0;
        lr.useWorldSpace = true;
        lr.startWidth = startWidth;
        lr.endWidth = endWidth;
        lr.numCapVertices = 2;
    }
}
