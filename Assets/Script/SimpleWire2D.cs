using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleWire2D : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public Transform startPoint;
    public Transform dragPoint;
    public Transform targetPoint;

    public float snapDistance = 0.5f; // In world units

    private LineRenderer line;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;

        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
    }

    private void Update()
    {
        // Draw wire from start to drag point
        line.SetPosition(0, startPoint.position);
        line.SetPosition(1, dragPoint.position);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Optional feedback
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(eventData.position);
        mouseWorldPos.z = 0;
        dragPoint.position = mouseWorldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float distance = Vector2.Distance(dragPoint.position, targetPoint.position);

        if (distance <= snapDistance)
        {
            dragPoint.position = targetPoint.position;
            Debug.Log("Wire connected!");
        }
        else
        {
            // Reset to start point
            dragPoint.position = startPoint.position;
        }
    }
}
