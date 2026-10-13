using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class WirePlacementSystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject wirePrefab;
    public GameObject nodePrefab;
    public LayerMask terminalLayerMask;
    public LayerMask nodeLayerMask;
    public LayerMask raycastIgnoreMask;
    private List<WireData> allWires = new List<WireData>();
    private BoxCollider startConnectionPoint = null;
    

    [Header("Settings")]
    public string plusTag = "Plus";
    public string minusTag = "Minus";
    public string wireLayerName = "Wire";

    [Header("Wire Materials")]
    public Material plusWireMaterial;
    public Material minusWireMaterial;

    private LineRenderer currentWire;
    private List<Vector3> currentPoints = new List<Vector3>();
    private string originPolarity = null;
    private float lockedY = 0f;
    private GameObject currentWireGO;
    private List<GameObject> spawnedNodesThisWire = new List<GameObject>();

    // Track if we started from a node (so we don't delete it on cancel)
    private WireNode startNode = null;
    private GameObject startTerminal = null;

    // Track terminals that already have complete paths
    private HashSet<GameObject> terminalsWithCompletePaths = new HashSet<GameObject>();

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (wirePrefab == null) Debug.LogError("WirePlacementSystem: wirePrefab not assigned.");
        if (nodePrefab == null) Debug.LogError("WirePlacementSystem: nodePrefab not assigned.");
    }

    private void Update()
    {

            if (Input.GetKeyDown(KeyCode.J))
    {
        TryConvertNodeToJunction();
        return;
    }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelCurrentWire();
            return;
        }

        if (currentWire == null)
        {
            if (Input.GetMouseButtonDown(0)) TryStartWire();
        }
        else
        {
            UpdatePreview();
            if (Input.GetMouseButtonDown(0)) TryPlaceSegmentOrConnect();
        }
    }

    private void TryStartWire()
    {
        var ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // First check if clicking on a node
        if (Physics.Raycast(ray, out RaycastHit nodeHit, 100f, nodeLayerMask))
        {
            WireNode node = nodeHit.collider.GetComponent<WireNode>();
            if (node != null)
            {
                StartWireAtNode(node);
                return;
            }
        }

        // Otherwise check for terminal
        if (Physics.Raycast(ray, out RaycastHit terminalHit, 100f, terminalLayerMask))
        {
            GameObject terminal = terminalHit.collider.gameObject;
            string tTag = terminal.tag;

            if (tTag == plusTag || tTag == minusTag)
            {
                // ✅ Check if terminal already has a complete path
                if (terminalsWithCompletePaths.Contains(terminal))
                {
                    Debug.Log("This terminal already has a complete circuit path!");
                    return;
                }

                StartWireAtTerminal(terminal);
            }
        }
    }

    private void StartWireAtNode(WireNode node)
    {
        if (!WireModeManager.Instance.wiringMode) return;

        originPolarity = node.polarity;
        lockedY = node.transform.position.y;
        startNode = node;
        startTerminal = null;

        currentWireGO = Instantiate(wirePrefab, Vector3.zero, Quaternion.identity);
        int wireLayer = LayerMask.NameToLayer(wireLayerName);
        if (wireLayer >= 0) currentWireGO.layer = wireLayer;

        currentWire = currentWireGO.GetComponent<LineRenderer>();
        if (currentWire == null)
        {
            Debug.LogError("wirePrefab must contain a LineRenderer component.");
            Destroy(currentWireGO);
            currentWireGO = null;
            startNode = null;
            return;
        }

        // ✅ Set material based on polarity
        ApplyWireMaterial(currentWire, originPolarity);

        currentPoints.Clear();
        Vector3 startPos = node.transform.position;
        startPos.y = lockedY;
        currentPoints.Add(startPos);
        UpdateLineRendererPositions();

        node.AddConnection(currentWire);
        spawnedNodesThisWire.Clear();
    }

private void StartWireAtTerminal(GameObject terminal)
{
    if (!WireModeManager.Instance.wiringMode) return;

    originPolarity = terminal.tag;
    lockedY = terminal.transform.position.y;
    startNode = null;
    startTerminal = terminal;

    // ✅ FIXED: Find and store the BoxCollider connection point
    BoxCollider terminalCollider = terminal.GetComponent<BoxCollider>();
    if (terminalCollider == null)
    {
        terminalCollider = terminal.GetComponentInChildren<BoxCollider>();
    }
    
    // ✅ CRITICAL: Store this for later use
    startConnectionPoint = terminalCollider;

    currentWireGO = Instantiate(wirePrefab, Vector3.zero, Quaternion.identity);
    int wireLayer = LayerMask.NameToLayer(wireLayerName);
    if (wireLayer >= 0) currentWireGO.layer = wireLayer;

    currentWire = currentWireGO.GetComponent<LineRenderer>();
    if (currentWire == null)
    {
        Debug.LogError("wirePrefab must contain a LineRenderer component.");
        Destroy(currentWireGO);
        currentWireGO = null;
        startTerminal = null;
        startConnectionPoint = null; // ✅ Also clear this
        return;
    }

    // ✅ Set material based on polarity
    ApplyWireMaterial(currentWire, originPolarity);

    currentPoints.Clear();
    Vector3 startPos = terminal.transform.position;
    startPos.y = lockedY;
    currentPoints.Add(startPos);
    UpdateLineRendererPositions();

    spawnedNodesThisWire.Clear();
}

    // ✅ NEW: Apply material based on wire polarity
    private void ApplyWireMaterial(LineRenderer lr, string polarity)
    {
        if (lr == null) return;

        if (polarity == plusTag && plusWireMaterial != null)
        {
            lr.material = plusWireMaterial;
        }
        else if (polarity == minusTag && minusWireMaterial != null)
        {
            lr.material = minusWireMaterial;
        }
    }

    private void UpdatePreview()
    {
        if (currentWire == null || currentPoints.Count == 0) return;

        Vector3 last = currentPoints[currentPoints.Count - 1];
        if (!MousePointOnPlane(lockedY, out Vector3 mousePoint)) mousePoint = last;

        float dx = Mathf.Abs(mousePoint.x - last.x);
        float dz = Mathf.Abs(mousePoint.z - last.z);
        Vector3 previewPoint = (dx > dz)
            ? new Vector3(mousePoint.x, lockedY, last.z)
            : new Vector3(last.x, lockedY, mousePoint.z);

        UpdateLineRendererWithPreview(previewPoint);
    }

    private void TryPlaceSegmentOrConnect()
    {
        var ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        // Check if clicking on a terminal
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, terminalLayerMask))
        {
            GameObject hitObj = hit.collider.gameObject;
            string tTag = hitObj.tag;

            // ✅ Prevent connecting to the same starting terminal
            if (hitObj == startTerminal)
            {
                Debug.Log("Cannot connect wire to the same terminal it started from!");
                return;
            }

            // ✅ Check if target terminal already has a complete path
            if (terminalsWithCompletePaths.Contains(hitObj))
            {
                Debug.Log("Target terminal already has a complete circuit path!");
                return;
            }

            if (tTag != originPolarity)
            {
                // ✅ Connect to opposite terminal
                ConnectToOppositeTerminal(hitObj);
                return;
            }
            else
            {
                // ✅ BLOCK: Cannot place wire on same polarity terminal
                Debug.Log("Cannot connect wire to a terminal of the same polarity!");
                return;
            }
        }

        // Check if clicking on a node
        if (Physics.Raycast(ray, out hit, 100f, nodeLayerMask))
        {
            GameObject nodeGO = hit.collider.gameObject;
            WireNode node = nodeGO.GetComponent<WireNode>();

            if (node != null)
            {
                // ✅ CHANGED: Connecting to ANY node (same or opposite polarity) FINISHES the wire
                ConnectToNodeAndFinish(node);
                return;
            }
            else return;
        }

        // Place a new node in empty space
        Vector3 last = currentPoints[currentPoints.Count - 1];
        if (!MousePointOnPlane(lockedY, out Vector3 mousePoint)) return;

        float dx2 = Mathf.Abs(mousePoint.x - last.x);
        float dz2 = Mathf.Abs(mousePoint.z - last.z);
        Vector3 newPoint = (dx2 > dz2)
            ? new Vector3(mousePoint.x, lockedY, last.z)
            : new Vector3(last.x, lockedY, mousePoint.z);

        if (Vector3.Distance(last, newPoint) < 0.001f) return;

        FinalizeSegmentAndSpawnNode(newPoint);
    }

    

    // ✅ NEW: Check if proposed wire segment intersects with existing wires
    private bool WouldIntersectExistingWire(Vector3 start, Vector3 end)
    {
        // Get all existing wire LineRenderers
        LineRenderer[] allLineRenderers = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);

        foreach (LineRenderer lr in allLineRenderers)
        {
            // Skip the current wire we're placing
            if (lr == currentWire) continue;

            // Check each segment of the existing wire
            for (int i = 0; i < lr.positionCount - 1; i++)
            {
                Vector3 segStart = lr.GetPosition(i);
                Vector3 segEnd = lr.GetPosition(i + 1);

                // Check if segments intersect (2D check on XZ plane since Y is locked)
                if (DoSegmentsIntersect2D(start, end, segStart, segEnd))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // ✅ NEW: Connect to any node (same or opposite polarity) and finish the wire
    private void ConnectToNodeAndFinish(WireNode node)
    {
        Vector3 nodePos = node.transform.position;
        nodePos.y = lockedY;

        // ✅ Check for wire collision before connecting
        Vector3 lastPoint = currentPoints[currentPoints.Count - 1];
        if (WouldIntersectExistingWire(lastPoint, nodePos))
        {
            Debug.Log("Cannot connect to node - path would intersect with existing wire!");
            return;
        }

        currentPoints.Add(nodePos);
        UpdateLineRendererPositions();

        // Connect this wire to the node
        node.AddConnection(currentWire);

        // ✅ FINISH the wire (make it permanent)
        FinishCurrentWire(null);
    }

    // ✅ NEW: Check if two line segments intersect in 2D (XZ plane)
    private bool DoSegmentsIntersect2D(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        // Convert to 2D vectors (ignore Y)
        Vector2 a1 = new Vector2(p1.x, p1.z);
        Vector2 a2 = new Vector2(p2.x, p2.z);
        Vector2 b1 = new Vector2(p3.x, p3.z);
        Vector2 b2 = new Vector2(p4.x, p4.z);

        // Allow very close proximity (same position) without counting as intersection
        float minDistance = 0.1f;
        if (Vector2.Distance(a1, b1) < minDistance || Vector2.Distance(a1, b2) < minDistance ||
            Vector2.Distance(a2, b1) < minDistance || Vector2.Distance(a2, b2) < minDistance)
        {
            return false; // Endpoints touching is OK (nodes connecting)
        }

        // Line segment intersection formula
        float d = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);

        if (Mathf.Abs(d) < 0.0001f) return false; // Parallel lines

        float t = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / d;
        float u = ((b1.x - a1.x) * (a2.y - a1.y) - (b1.y - a1.y) * (a2.x - a1.x)) / d;

        // Check if intersection point is within both segments
        return (t >= 0 && t <= 1 && u >= 0 && u <= 1);
    }

    private void ConnectToOppositeTerminal(GameObject terminal)
{
    Vector3 terminalPos = terminal.transform.position;
    terminalPos.y = lockedY;

    Vector3 lastPoint = currentPoints[currentPoints.Count - 1];
    if (WouldIntersectExistingWire(lastPoint, terminalPos))
    {
        Debug.Log("Cannot complete wire - path would intersect with existing wire!");
        return;
    }

    currentPoints.Add(terminalPos);
    UpdateLineRendererPositions();

    // ✅ Get the BoxCollider from the end terminal
    BoxCollider endConnectionPoint = terminal.GetComponent<BoxCollider>();
    if (endConnectionPoint == null)
    {
        endConnectionPoint = terminal.GetComponentInChildren<BoxCollider>();
    }

    // ✅ Mark both terminals as having complete paths
    if (startTerminal != null)
    {
        terminalsWithCompletePaths.Add(startTerminal);
    }
    terminalsWithCompletePaths.Add(terminal);

    FinishCurrentWire(terminal, endConnectionPoint);
}

    private void ConnectToOppositeNode(WireNode node)
    {
        Vector3 nodePos = node.transform.position;
        nodePos.y = lockedY;

        // ✅ Check for wire collision before connecting
        Vector3 lastPoint = currentPoints[currentPoints.Count - 1];
        if (WouldIntersectExistingWire(lastPoint, nodePos))
        {
            Debug.Log("Cannot connect to node - path would intersect with existing wire!");
            return;
        }

        currentPoints.Add(nodePos);
        UpdateLineRendererPositions();

        node.AddConnection(currentWire);

        FinishCurrentWire(null);
    }

    private void FinalizeSegmentAndSpawnNode(Vector3 newPoint)
    {
        // ✅ Check for wire collision before placing
        Vector3 lastPoint = currentPoints[currentPoints.Count - 1];
        if (WouldIntersectExistingWire(lastPoint, newPoint))
        {
            Debug.Log("Cannot place wire here - would intersect with existing wire!");
            return;
        }

        currentPoints.Add(newPoint);
        UpdateLineRendererPositions();

        GameObject nodeGO = Instantiate(nodePrefab, newPoint, Quaternion.identity);
        SetGameObjectLayer(nodeGO, GetLayerFromLayerMask(nodeLayerMask));

        WireNode wn = nodeGO.GetComponent<WireNode>();
        if (wn == null) wn = nodeGO.AddComponent<WireNode>();
        wn.polarity = originPolarity;
        wn.AddConnection(currentWire);

        spawnedNodesThisWire.Add(nodeGO);
    }

    private void FinalizeSegmentAndAttachToNode(WireNode node)
    {
        Vector3 nodePos = node.transform.position;
        nodePos.y = lockedY;

        // ✅ Check for wire collision before attaching
        Vector3 lastPoint = currentPoints[currentPoints.Count - 1];
        if (WouldIntersectExistingWire(lastPoint, nodePos))
        {
            Debug.Log("Cannot connect to node - path would intersect with existing wire!");
            return;
        }

        currentPoints.Add(nodePos);
        UpdateLineRendererPositions();
        node.AddConnection(currentWire);
    }

    private void FinishCurrentWire(GameObject endTerminal = null, BoxCollider endConnectionPoint = null)
{
    if (currentWire != null)
    {
        UpdateLineRendererPositions();
        int wireLayer = LayerMask.NameToLayer(wireLayerName);
        if (wireLayer >= 0 && currentWireGO != null) SetGameObjectLayer(currentWireGO, wireLayer);

        var meta = currentWireGO.AddComponent<WireMeta>();
        meta.originPolarity = originPolarity;
        meta.connectedNodes = new List<GameObject>(spawnedNodesThisWire);
        meta.startTerminal = startTerminal;
        meta.endTerminal = endTerminal;
        
        // ✅ NEW: Store BoxCollider references
        meta.startConnectionPoint = startConnectionPoint;
        meta.endConnectionPoint = endConnectionPoint;
    }

    currentWire = null;
    currentWireGO = null;
    currentPoints.Clear();
    originPolarity = null;
    spawnedNodesThisWire.Clear();
    startNode = null;
    startTerminal = null;
    startConnectionPoint = null; // Reset connection point
}
    private void CancelCurrentWire()
    {
        if (currentWireGO != null) Destroy(currentWireGO);

        foreach (var n in spawnedNodesThisWire)
        {
            if (n != null) Destroy(n);
        }

        if (startNode != null && currentWire != null)
        {
            startNode.RemoveConnection(currentWire);
        }

        currentWire = null;
        currentWireGO = null;
        currentPoints.Clear();
        originPolarity = null;
        spawnedNodesThisWire.Clear();
        startNode = null;
        startTerminal = null;
        startConnectionPoint = null;
    }

    private void UpdateLineRendererWithPreview(Vector3 previewPoint)
    {
        int finalCount = currentPoints.Count + 1;
        currentWire.positionCount = finalCount;
        for (int i = 0; i < currentPoints.Count; i++)
            currentWire.SetPosition(i, currentPoints[i]);
        currentWire.SetPosition(finalCount - 1, previewPoint);
    }

    private void UpdateLineRendererPositions()
    {
        if (currentWire == null) return;
        currentWire.positionCount = currentPoints.Count;
        for (int i = 0; i < currentPoints.Count; i++)
            currentWire.SetPosition(i, currentPoints[i]);
    }

    private bool MousePointOnPlane(float y, out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        Plane plane = new Plane(Vector3.up, new Vector3(0, y, 0));
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }
        return false;
    }

    private void SetGameObjectLayer(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetGameObjectLayer(t.gameObject, layer);
    }

    private int GetLayerFromLayerMask(LayerMask lm)
    {
        int mask = lm.value;
        for (int i = 0; i < 32; i++)
            if ((mask & (1 << i)) != 0) return i;
        return 0;
    }

    public void RemoveWiresConnectedToObject(GameObject target)
    {
        List<GameObject> wiresToRemove = new List<GameObject>();

        foreach (WireMeta meta in FindObjectsByType<WireMeta>(FindObjectsSortMode.None))
        {
            if (meta.connectedNodes.Contains(target))
            {
                wiresToRemove.Add(meta.gameObject);

                // ✅ Remove terminals from complete path tracking when wire is deleted
                if (meta.startTerminal != null)
                {
                    terminalsWithCompletePaths.Remove(meta.startTerminal);
                }
                if (meta.endTerminal != null)
                {
                    terminalsWithCompletePaths.Remove(meta.endTerminal);
                }
            }
        }

        foreach (GameObject wire in wiresToRemove)
        {
            DestroyImmediate(wire);
        }
    }

    // ✅ NEW: Public method to manually remove a wire and free up terminals
    public void RemoveWire(GameObject wireObject)
    {
        WireMeta meta = wireObject.GetComponent<WireMeta>();
        if (meta != null)
        {
            if (meta.startTerminal != null)
            {
                terminalsWithCompletePaths.Remove(meta.startTerminal);
            }
            if (meta.endTerminal != null)
            {
                terminalsWithCompletePaths.Remove(meta.endTerminal);
            }
        }

        Destroy(wireObject);
    }
    
    // ✅ NEW: Public method for electrical system to get all wires
// ✅ UPDATED: Public method for electrical system to get all wires WITH nodes
public List<WireData> GetAllWires()
{
    List<WireData> wires = new List<WireData>();
    
    foreach (WireMeta meta in FindObjectsByType<WireMeta>(FindObjectsSortMode.None))
    {
        if (meta.startConnectionPoint != null && meta.endConnectionPoint != null)
        {
            WireData wireData = new WireData
            {
                wireObject = meta.gameObject,
                startPoint = meta.startConnectionPoint,
                endPoint = meta.endConnectionPoint,
                intermediateNodes = meta.connectedNodes != null ? new List<GameObject>(meta.connectedNodes) : new List<GameObject>() // Include nodes
            };
            wires.Add(wireData);
        }
        else
        {
            Debug.LogWarning($"[WIRE] Wire missing connection points: {meta.gameObject.name}");
        }
    }
    
    Debug.Log($"[WIRE] GetAllWires returned {wires.Count} wires");
    return wires;
}

private void TryConvertNodeToJunction()
{
    var ray = playerCamera.ScreenPointToRay(Input.mousePosition);
    
    if (Physics.Raycast(ray, out RaycastHit hit, 100f, nodeLayerMask))
    {
        WireNode node = hit.collider.GetComponent<WireNode>();
        if (node != null)
        {
            node.ConvertToJunction();
            
            // Change visual to indicate junction
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                mat.color = Color.cyan; // Cyan = junction
                Debug.Log("[WIRE] Node visual changed to junction color");
            }
        }
        else
        {
            Debug.Log("[WIRE] No WireNode component found on hit object");
        }
    }
    else
    {
        Debug.Log("[WIRE] No node detected under cursor");
    }
}
}

public class WireMeta : MonoBehaviour
{
    public string originPolarity;
    public List<GameObject> connectedNodes;
    public GameObject startTerminal;
    public GameObject endTerminal;
    
    // NEW: Store the actual BoxCollider connection points
    public BoxCollider startConnectionPoint;
    public BoxCollider endConnectionPoint;
}

[System.Serializable]
public class WireData
{
    public GameObject wireObject;
    public BoxCollider startPoint;
    public BoxCollider endPoint;
    public List<GameObject> intermediateNodes; // NEW: Track nodes along the wire path
}