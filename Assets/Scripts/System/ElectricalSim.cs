using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class ElectricalSimulationSystem : MonoBehaviour
{
    [Header("References")]
    public ObjectPlacementSystem placementSystem;
    public WirePlacementSystem wirePlacementSystem;
    public Button runButton;  // ← KEEP THIS - it's the Button component
    public RawImage runButtonImage;  // ← ADD THIS - it's the RawImage component
    public Texture runSimulationTexture1;  // ← ADD THIS
    public Texture runSimulationTexture2;  // ← ADD THIS
    
    public Button inspectButton;

    [Header("UI References")]
    public GameObject voltageDisplayPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI voltageText;
    public TextMeshProUGUI resistanceText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI maxVoltageText;
    public TextMeshProUGUI maxAmperageText;
    public TextMeshProUGUI switchStatusText;  // NEW: For switch status display
    public TextMeshProUGUI circuitStatusText;
    // Separated value/unit fields
    public TextMeshProUGUI voltageValueText;
    public TextMeshProUGUI resistanceValueText;
    public TextMeshProUGUI currentValueText;
    public TextMeshProUGUI powerValueText;
    public TextMeshProUGUI maxVoltageValueText;
    public TextMeshProUGUI maxAmperageValueText;
    public TextMeshProUGUI noiseAmplitudeValueText;
    public TextMeshProUGUI noiseRateValueText;

    public RectTransform graphContainer;

    [Header("UI Dynamic Positioning")]
    [Tooltip("Color for numeric values")]
    public Color valueColor = Color.white;
    [Tooltip("Color for unit text (V, A, Ω, etc.)")]
    public Color unitColor = new Color(0.67f, 0.67f, 0.67f, 1f); // Light gray
    [Tooltip("Unit text size as percentage (100 = same size as value)")]
    [Range(50, 100)]
    public int unitSizePercent = 75;
    

    [Header("Graph Settings")]
    public int maxGraphPoints = 100;
    public float graphUpdateInterval = 0.1f;
    public Color graphLineColor = Color.yellow;
    public float graphLineThickness = 2f;

    [Header("Simulation Settings")]
    public float simulationUpdateRate = 0.05f;
    public int maxIterations = 50; // For circuit solving
    public float convergenceThreshold = 0.001f;

    [Header("Visual Feedback")]
    public Color normalRunButtonColor = Color.white;
    public Color activeRunButtonColor = Color.green;
    public Color inspectActiveColor = Color.cyan;

    // Simulation state
    private bool isSimulationRunning = false;
    private bool isInspectMode = false;
    private float lastSimulationUpdate = 0f;
    private float lastGraphUpdate = 0f;
    private List<ParallelCircuitGroup> parallelGroups = new List<ParallelCircuitGroup>();

    // Electrical data
    private Dictionary<PlacedGridObject, ElectricalNode> electricalNodes = new Dictionary<PlacedGridObject, ElectricalNode>();
    private List<ElectricalConnection> electricalConnections = new List<ElectricalConnection>();
    private List<Circuit> closedCircuits = new List<Circuit>();

    // Graph data
    private Dictionary<GameObject, List<float>> voltageHistory = new Dictionary<GameObject, List<float>>();
    private Dictionary<GameObject, List<float>> currentHistory = new Dictionary<GameObject, List<float>>();
    private GameObject currentlyInspectedObject;
    private List<GameObject> graphLineSegments = new List<GameObject>();

    // UI
    private Image inspectButtonImage;
    private Camera playerCamera;

    private List<MixedCircuitGroup> mixedGroups = new List<MixedCircuitGroup>();

    void Start()
    {
        playerCamera = Camera.main;

        if (placementSystem == null)
            placementSystem = FindFirstObjectByType<ObjectPlacementSystem>();

        if (wirePlacementSystem == null)
            wirePlacementSystem = FindFirstObjectByType<WirePlacementSystem>();

        if (runButton != null)
        {
            runButton.onClick.AddListener(ToggleSimulation);

            // Set initial texture
            if (runButtonImage != null && runSimulationTexture1 != null)
            {
                runButtonImage.texture = runSimulationTexture1;
            }
        }

        if (inspectButton != null)
        {
            inspectButtonImage = inspectButton.GetComponent<Image>();
            inspectButton.onClick.AddListener(ToggleInspectMode);
        }

        if (voltageDisplayPanel != null)
            voltageDisplayPanel.SetActive(false);

        SetSimulationRunning(false);
    }

    void Update()
    {
        if (isSimulationRunning)
        {
            if (Time.time - lastSimulationUpdate >= simulationUpdateRate)
            {
                UpdateElectricalSimulation();
                lastSimulationUpdate = Time.time;
            }

            if (Time.time - lastGraphUpdate >= graphUpdateInterval)
            {
                UpdateGraphData();
                lastGraphUpdate = Time.time;
            }
        }

        if (isInspectMode)
        {
            HandleInspectMode();
        }
    }

    public void ToggleSimulation()
    {
        SetSimulationRunning(!isSimulationRunning);
    }

    void SetSimulationRunning(bool running)
    {
        isSimulationRunning = running;

        if (running)
        {
            InitializeSimulation();
            Debug.Log("[ELECTRICAL] Simulation started");
        }
        else
        {
            Debug.Log("[ELECTRICAL] Simulation stopped");
            ClearGraph();
        }

        // Swap textures based on simulation state
        if (runButtonImage != null)
        {
            if (running && runSimulationTexture2 != null)
            {
                runButtonImage.texture = runSimulationTexture2;
            }
            else if (!running && runSimulationTexture1 != null)
            {
                runButtonImage.texture = runSimulationTexture1;
            }
        }
    }

    void InitializeSimulation()
    {
        electricalNodes.Clear();
        electricalConnections.Clear();
        closedCircuits.Clear();
        voltageHistory.Clear();
        currentHistory.Clear();

        List<PlacedGridObject> placedObjects = placementSystem.GetAllPlacedObjects();

        // Create nodes
        foreach (PlacedGridObject obj in placedObjects)
        {
            if (obj == null || obj.objectData == null) continue;

            ElectricalNode node = new ElectricalNode
            {
                placedObject = obj,
                voltage = 0f,
                current = 0f,
                baseVoltage = obj.objectData.voltage,
                resistance = obj.objectData.resistance > 0 ? obj.objectData.resistance : 0.01f,
                maxVoltage = obj.objectData.maxVoltage,
                maxAmperage = obj.objectData.maxAmperage,
                isSource = obj.objectData.isVoltageSource || obj.objectData.objectTypeName.Equals("Source", System.StringComparison.OrdinalIgnoreCase),
                isFunctional = true,
                voltageNoiseAmplitude = obj.objectData.voltageNoiseAmplitude,
                voltageNoiseRate = obj.objectData.voltageNoiseRate,
                noiseOffset = Random.Range(0f, 1000f)
            };

            if (node.isSource)
            {
                node.voltage = node.baseVoltage;
                Debug.Log($"[ELECTRICAL] Source found: {obj.objectData.displayName} with {node.baseVoltage}V");
            }

            electricalNodes[obj] = node;
            if (obj.gameObject != null)
            {
                voltageHistory[obj.gameObject] = new List<float>();
                currentHistory[obj.gameObject] = new List<float>();
            }
        }

        BuildElectricalConnections();
        DetectClosedCircuits();
        parallelGroups = DetectParallelCircuits();
        //mixedGroups = DetectMixedCircuits();
            mixedGroups.Clear(); // ✅ FORCE EMPTY

        Debug.Log($"[ELECTRICAL] Initialized {electricalNodes.Count} nodes, {electricalConnections.Count} connections, {closedCircuits.Count} closed circuits");
    }

    void BuildElectricalConnections()
    {
        electricalConnections.Clear();
        List<WireData> wires = wirePlacementSystem.GetAllWires();

        Debug.Log($"[ELECTRICAL] Building connections from {wires.Count} wires");

        foreach (WireData wire in wires)
        {
            if (wire == null || wire.startPoint == null || wire.endPoint == null)
            {
                Debug.LogWarning("[ELECTRICAL] Wire has null connection points - skipping");
                continue;
            }

            Debug.Log($"[ELECTRICAL] Processing wire:");
            Debug.Log($"  Start collider: {wire.startPoint.gameObject.name} (Tag: {wire.startPoint.tag})");
            Debug.Log($"  End collider: {wire.endPoint.gameObject.name} (Tag: {wire.endPoint.tag})");
            Debug.Log($"  Intermediate nodes: {wire.intermediateNodes.Count}");

            PlacedGridObject startObj = FindPlacedObjectByConnectionPoint(wire.startPoint);
            PlacedGridObject endObj = FindPlacedObjectByConnectionPoint(wire.endPoint);

            if (startObj != null)
                Debug.Log($"  ✓ Start object: {startObj.objectData.displayName}");
            else
                Debug.LogError($"  ✗ Start object: NULL");

            if (endObj != null)
                Debug.Log($"  ✓ End object: {endObj.objectData.displayName}");
            else
                Debug.LogError($"  ✗ End object: NULL");

            // ✅ CRITICAL: Skip if both start and end are the same object
            if (startObj == endObj)
            {
                Debug.LogWarning($"[ELECTRICAL] Skipping wire - connects to same object!");
                continue;
            }

            if (startObj != null && endObj != null && electricalNodes.ContainsKey(startObj) && electricalNodes.ContainsKey(endObj))
            {
                // Check for duplicate connections
                bool exists = electricalConnections.Any(c =>
                    (c.node1.placedObject == startObj && c.node2.placedObject == endObj) ||
                    (c.node2.placedObject == startObj && c.node1.placedObject == endObj));

                if (!exists)
                {
                    ElectricalConnection connection = new ElectricalConnection
                    {
                        wire = wire,
                        node1 = electricalNodes[startObj],
                        node2 = electricalNodes[endObj],
                        resistance = 0.01f,
                        current = 0f
                    };

                    electricalConnections.Add(connection);
                    Debug.Log($"[ELECTRICAL] ✓✓✓ CREATED CONNECTION: {startObj.objectData.displayName} ↔ {endObj.objectData.displayName}");
                }
                else
                {
                    Debug.Log($"[ELECTRICAL] Connection already exists (duplicate avoided)");
                }
            }
            else
            {
                Debug.LogError($"[ELECTRICAL] ✗✗✗ FAILED to create connection!");

                if (startObj == null)
                    Debug.LogError($"  Reason: Could not find PlacedObject for START terminal");
                else if (!electricalNodes.ContainsKey(startObj))
                    Debug.LogError($"  Reason: START object not in electricalNodes dictionary");

                if (endObj == null)
                    Debug.LogError($"  Reason: Could not find PlacedObject for END terminal");
                else if (!electricalNodes.ContainsKey(endObj))
                    Debug.LogError($"  Reason: END object not in electricalNodes dictionary");
            }
        }

        Debug.Log($"[ELECTRICAL] ═══════════════════════════════════");
        Debug.Log($"[ELECTRICAL] FINAL: Created {electricalConnections.Count} connections");
        Debug.Log($"[ELECTRICAL] ═══════════════════════════════════");
    }

    PlacedGridObject FindPlacedObjectByConnectionPoint(BoxCollider connectionPoint)
    {
        if (connectionPoint == null)
        {
            Debug.LogError("[ELECTRICAL] Connection point is NULL!");
            return null;
        }

        Debug.Log($"[ELECTRICAL] Searching for PlacedObject from collider: {connectionPoint.name} on GameObject: {connectionPoint.gameObject.name}");



        PlacedObjectComponent component = connectionPoint.GetComponentInParent<PlacedObjectComponent>();

        // ADD THIS:
        Debug.Log($"[ELECTRICAL] Found component: {(component != null ? component.gameObject.name : "NULL")}, PlacedData: {(component?.placedObjectData != null ? component.placedObjectData.objectData.displayName : "NULL")}");

        if (component != null && component.placedObjectData != null)
        {
            Debug.Log($"[ELECTRICAL] ✓ Found PlacedObjectComponent: {component.placedObjectData.objectData.displayName}");
            return component.placedObjectData;
        }
        else
        {
            Debug.LogError($"[ELECTRICAL] ✗ No PlacedObjectComponent found in parents of {connectionPoint.name}!");

            // Debug: Print hierarchy
            Transform current = connectionPoint.transform;
            string hierarchy = connectionPoint.name;
            while (current.parent != null)
            {
                current = current.parent;
                hierarchy = current.name + " > " + hierarchy;
            }
            Debug.LogError($"[ELECTRICAL] Hierarchy: {hierarchy}");
        }

        return null;
    }

    // Detect closed circuits using DFS starting from each source node.
    void DetectClosedCircuits()
{
    closedCircuits.Clear();

    Debug.Log($"[CIRCUIT] ═══════════════════════════════════");
    Debug.Log($"[CIRCUIT] Starting circuit detection...");
    Debug.Log($"[CIRCUIT] Total nodes: {electricalNodes.Count}");
    Debug.Log($"[CIRCUIT] Total connections: {electricalConnections.Count}");

    // Build adjacency
    Dictionary<ElectricalNode, List<ElectricalNode>> adjacency = new Dictionary<ElectricalNode, List<ElectricalNode>>();

    foreach (var conn in electricalConnections)
    {
        if (conn.node1 == null || conn.node2 == null) continue;
        if (!conn.node1.isFunctional || !conn.node2.isFunctional) continue;
        if (conn.node1 == conn.node2) continue;

        if (!adjacency.ContainsKey(conn.node1)) adjacency[conn.node1] = new List<ElectricalNode>();
        if (!adjacency.ContainsKey(conn.node2)) adjacency[conn.node2] = new List<ElectricalNode>();

        if (!adjacency[conn.node1].Contains(conn.node2)) adjacency[conn.node1].Add(conn.node2);
        if (!adjacency[conn.node2].Contains(conn.node1)) adjacency[conn.node2].Add(conn.node1);
    }

    Debug.Log($"[CIRCUIT] Built adjacency graph with {adjacency.Count} nodes");

    int sourcesFound = 0;
    foreach (var kvp in electricalNodes)
    {
        ElectricalNode source = kvp.Value;
        if (!source.isSource || !source.isFunctional) continue;

        sourcesFound++;
        Debug.Log($"[CIRCUIT] ═══ Searching from source: {source.placedObject.objectData.displayName} ═══");

        List<List<ElectricalNode>> loops = FindAllPathsBackToSource(source, adjacency);

        Debug.Log($"[CIRCUIT] Found {loops.Count} RAW loops from this source");

        // ✅ NEW: Filter out sub-paths (keep only the longest unique circuits)
        List<List<ElectricalNode>> filteredLoops = new List<List<ElectricalNode>>();
        
        foreach (var loop in loops)
        {
            if (loop.Count < 2) continue;
            
            // Check if this loop is a subset of any existing loop
            bool isSubset = false;
            foreach (var existingLoop in filteredLoops)
            {
                if (IsSubPath(loop, existingLoop))
                {
                    isSubset = true;
                    break;
                }
            }
            
            if (!isSubset)
            {
                // Check if any existing loop is a subset of this one
                filteredLoops.RemoveAll(existing => IsSubPath(existing, loop));
                filteredLoops.Add(loop);
            }
        }

        Debug.Log($"[CIRCUIT] After filtering: {filteredLoops.Count} unique circuits");

        // Add filtered circuits
        foreach (var loop in filteredLoops)
        {
            Circuit circuit = new Circuit
            {
                nodes = new List<ElectricalNode>(loop),
                hasSource = true,
                totalResistance = 0f
            };

            if (!CircuitAlreadyExists(circuit))
            {
                closedCircuits.Add(circuit);
                string pathStr = string.Join(" → ", loop.Select(n => n.placedObject.objectData.displayName));
                Debug.Log($"[CIRCUIT] ✓✓✓ ADDED CIRCUIT ({loop.Count} nodes)");
                Debug.Log($"[CIRCUIT] Path: {pathStr}");
            }
        }
    }

    Debug.Log($"[CIRCUIT] ═══════════════════════════════════");
    Debug.Log($"[CIRCUIT] FINAL: {closedCircuits.Count} closed circuits from {sourcesFound} sources");
    Debug.Log($"[CIRCUIT] ═══════════════════════════════════");
}
    List<ParallelCircuitGroup> DetectParallelCircuits()
    {
        List<ParallelCircuitGroup> parallelGroups = new List<ParallelCircuitGroup>();

        // Group circuits by their source node
        Dictionary<ElectricalNode, List<Circuit>> circuitsBySource = new Dictionary<ElectricalNode, List<Circuit>>();

        foreach (var circuit in closedCircuits)
        {
            var source = circuit.nodes.FirstOrDefault(n => n.isSource);
            if (source != null)
            {
                if (!circuitsBySource.ContainsKey(source))
                    circuitsBySource[source] = new List<Circuit>();

                circuitsBySource[source].Add(circuit);
            }
        }

        // Create parallel groups for sources with multiple circuits
        foreach (var kvp in circuitsBySource)
        {
            if (kvp.Value.Count > 1)
            {
                ParallelCircuitGroup group = new ParallelCircuitGroup
                {
                    sharedSource = kvp.Key,
                    parallelCircuits = kvp.Value
                };
                parallelGroups.Add(group);

                Debug.Log($"[PARALLEL] Found {kvp.Value.Count} parallel circuits sharing source: {kvp.Key.placedObject.objectData.displayName}");
            }
        }

        return parallelGroups;
    }

    List<MixedCircuitGroup> DetectMixedCircuits()
{
    List<MixedCircuitGroup> mixedGroups = new List<MixedCircuitGroup>();

    // Group circuits by their source node
    Dictionary<ElectricalNode, List<Circuit>> circuitsBySource = new Dictionary<ElectricalNode, List<Circuit>>();

    foreach (var circuit in closedCircuits)
    {
        var source = circuit.nodes.FirstOrDefault(n => n.isSource);
        if (source != null)
        {
            if (!circuitsBySource.ContainsKey(source))
                circuitsBySource[source] = new List<Circuit>();

            circuitsBySource[source].Add(circuit);
        }
    }

    // Analyze each source's circuits for mixed topology
    foreach (var kvp in circuitsBySource)
    {
        if (kvp.Value.Count < 2) continue; // Need at least 2 paths for mixed

        // Find shared nodes (excluding source) - TRUE junctions only
        Dictionary<ElectricalNode, List<Circuit>> nodeToCircuits = new Dictionary<ElectricalNode, List<Circuit>>();
        
        foreach (var circuit in kvp.Value)
        {
            foreach (var node in circuit.nodes)
            {
                if (node.isSource) continue; // ✅ CRITICAL: Skip source
                
                if (!nodeToCircuits.ContainsKey(node))
                    nodeToCircuits[node] = new List<Circuit>();
                
                nodeToCircuits[node].Add(circuit);
            }
        }

        // Find REAL junction points (non-source nodes shared by multiple circuits)
        List<ElectricalNode> junctionNodes = nodeToCircuits
            .Where(pair => pair.Value.Count > 1 && !pair.Key.isSource) // ✅ Double-check not source
            .Select(pair => pair.Key)
            .ToList();

        // ✅ CRITICAL: Only create mixed group if there are REAL junctions (not just source)
        if (junctionNodes.Count > 0)
        {
            // Verify these junctions create actual series-parallel topology
            bool hasTrueSeriesParallelTopology = false;
            
            foreach (var junction in junctionNodes)
            {
                // Check if junction divides the circuit (not at the end)
                var circuitsWithJunction = nodeToCircuits[junction];
                if (circuitsWithJunction.Count >= 2)
                {
                    // Check if there are nodes AFTER this junction in the paths
                    foreach (var circuit in circuitsWithJunction)
                    {
                        int junctionIdx = circuit.nodes.IndexOf(junction);
                        int sourceIdx = circuit.nodes.FindIndex(n => n.isSource);
                        
                        // Junction should NOT be adjacent to source
                        if (Mathf.Abs(junctionIdx - sourceIdx) > 1)
                        {
                            hasTrueSeriesParallelTopology = true;
                            break;
                        }
                    }
                }
                if (hasTrueSeriesParallelTopology) break;
            }

            if (hasTrueSeriesParallelTopology)
            {
                // This is a REAL mixed circuit with junctions
                MixedCircuitGroup mixedGroup = new MixedCircuitGroup
                {
                    sharedSource = kvp.Key,
                    circuits = kvp.Value,
                    junctionNodes = junctionNodes
                };

                // Identify parallel segments (branches between junctions)
                mixedGroup.parallelSegments = IdentifyParallelSegments(kvp.Value, junctionNodes, kvp.Key);

                mixedGroups.Add(mixedGroup);
                Debug.Log($"[MIXED] Found TRUE mixed circuit with {kvp.Value.Count} paths, {junctionNodes.Count} junctions from source: {kvp.Key.placedObject.objectData.displayName}");
            }
            else
            {
                Debug.Log($"[MIXED] False positive - junctions are at circuit endpoints, treating as pure parallel");
            }
        }
        else
        {
            Debug.Log($"[MIXED] No internal junctions found - this is pure parallel, not mixed");
        }
    }

    return mixedGroups;
}

List<ParallelSegment> IdentifyParallelSegments(List<Circuit> circuits, List<ElectricalNode> junctions, ElectricalNode source)
{
    List<ParallelSegment> segments = new List<ParallelSegment>();

    // Add source as a junction point for analysis
    List<ElectricalNode> allJunctions = new List<ElectricalNode>(junctions);
    if (!allJunctions.Contains(source))
        allJunctions.Insert(0, source);

    // For each pair of junctions, find paths between them
    for (int i = 0; i < allJunctions.Count - 1; i++)
    {
        ElectricalNode junctionA = allJunctions[i];
        
        for (int j = i + 1; j < allJunctions.Count; j++)
        {
            ElectricalNode junctionB = allJunctions[j];
            
            List<List<ElectricalNode>> pathsBetween = new List<List<ElectricalNode>>();

            foreach (var circuit in circuits)
            {
                List<ElectricalNode> path = ExtractPathBetweenNodes(circuit.nodes, junctionA, junctionB);
                if (path != null && path.Count > 0)
                {
                    pathsBetween.Add(path);
                }
            }

            if (pathsBetween.Count > 1)
            {
                // Multiple paths between these junctions = parallel segment
                ParallelSegment segment = new ParallelSegment
                {
                    startJunction = junctionA,
                    endJunction = junctionB,
                    parallelPaths = pathsBetween
                };

                segments.Add(segment);
                Debug.Log($"[MIXED] Parallel segment: {pathsBetween.Count} paths between {junctionA.placedObject.objectData.displayName} and {junctionB.placedObject.objectData.displayName}");
            }
        }
    }

    return segments;
}

List<ElectricalNode> ExtractPathBetweenNodes(List<ElectricalNode> circuitPath, ElectricalNode start, ElectricalNode end)
{
    int startIdx = circuitPath.IndexOf(start);
    int endIdx = circuitPath.IndexOf(end);

    if (startIdx == -1 || endIdx == -1) return null;

    // Ensure start comes before end
    if (startIdx > endIdx)
    {
        int temp = startIdx;
        startIdx = endIdx;
        endIdx = temp;
    }

    List<ElectricalNode> path = new List<ElectricalNode>();
    for (int i = startIdx; i <= endIdx; i++)
    {
        path.Add(circuitPath[i]);
    }

    return path.Count > 1 ? path : null;
}

    // ✅ NEW: Helper function to check if one path is a subset of another
    bool IsSubPath(List<ElectricalNode> shorter, List<ElectricalNode> longer)
    {
        if (shorter.Count >= longer.Count) return false;

        HashSet<ElectricalNode> shorterSet = new HashSet<ElectricalNode>(shorter);
        HashSet<ElectricalNode> longerSet = new HashSet<ElectricalNode>(longer);

        return shorterSet.IsSubsetOf(longerSet);
    }


    List<List<ElectricalNode>> FindAllPathsBackToSource(ElectricalNode source, Dictionary<ElectricalNode, List<ElectricalNode>> adjacency)
{
    List<List<ElectricalNode>> results = new List<List<ElectricalNode>>();

    if (!adjacency.ContainsKey(source) || adjacency[source].Count == 0)
    {
        Debug.Log($"[CIRCUIT] Source has no connections in adjacency");
        return results;
    }

    // Try starting from EACH neighbor of the source
    foreach (var startNeighbor in adjacency[source])
    {
        if (!startNeighbor.isFunctional) continue;
        
        HashSet<ElectricalNode> visited = new HashSet<ElectricalNode>();
        List<ElectricalNode> currentPath = new List<ElectricalNode>();
        
        // Add source at the start
        currentPath.Add(source);
        visited.Add(source);
        
        // Start exploring from this neighbor
        ExplorePath(startNeighbor, source, adjacency, visited, currentPath, results, 0);
    }

    return results;
}

void ExplorePath(ElectricalNode current, ElectricalNode source, 
    Dictionary<ElectricalNode, List<ElectricalNode>> adjacency,
    HashSet<ElectricalNode> visited, List<ElectricalNode> currentPath,
    List<List<ElectricalNode>> results, int depth)
{
    if (depth > 20) return; // Safety limit
    
    // Add current to path
    currentPath.Add(current);
    visited.Add(current);
    
    if (!adjacency.ContainsKey(current))
    {
        // Dead end - backtrack
        currentPath.RemoveAt(currentPath.Count - 1);
        visited.Remove(current);
        return;
    }
    
    // Check all neighbors
    bool foundPathBack = false;
    foreach (var neighbor in adjacency[current])
    {
        if (!neighbor.isFunctional) continue;
        
        // Found path back to source!
        if (neighbor == source && currentPath.Count >= 2)
        {
            List<ElectricalNode> completePath = new List<ElectricalNode>(currentPath);
            results.Add(completePath);
            foundPathBack = true;
            
            string pathStr = string.Join(" → ", completePath.Select(n => n.placedObject.objectData.displayName));
            Debug.Log($"[CIRCUIT] ✓ Found path: {pathStr}");
            continue; // Keep exploring other paths
        }
        
        // Continue exploring if not visited
        if (!visited.Contains(neighbor))
        {
            ExplorePath(neighbor, source, adjacency, visited, currentPath, results, depth + 1);
        }
    }
    
    // Backtrack
    currentPath.RemoveAt(currentPath.Count - 1);
    visited.Remove(current);
}

    List<List<ElectricalNode>> FindClosedLoopsFromSource(ElectricalNode source, Dictionary<ElectricalNode, List<ElectricalNode>> adjacency)
    {
        List<List<ElectricalNode>> results = new List<List<ElectricalNode>>();
        Stack<ElectricalNode> stack = new Stack<ElectricalNode>();
        HashSet<ElectricalNode> inPath = new HashSet<ElectricalNode>();

        void DFS(ElectricalNode current, int depth)
        {
            if (depth > 20) return; // limit depth to avoid explosions

            if (!adjacency.ContainsKey(current)) return;

            foreach (var neighbor in adjacency[current])
            {
                if (!neighbor.isFunctional) continue;

                // If neighbor is the source and path length >= 2, we found a loop
                if (neighbor == source && stack.Count >= 1)
                {
                    var loop = new List<ElectricalNode>(stack.Reverse());
                    // ensure source at front
                    if (loop.Count >= 1 && loop[0] != source)
                    {
                        loop.Insert(0, source);
                    }
                    else if (loop.Count == 0)
                    {
                        loop.Add(source);
                    }

                    // Make sure loop is at least 3 nodes (source + at least 2 others)
                    if (loop.Count >= 3)
                    {
                        results.Add(loop);
                        Debug.Log($"[CIRCUIT] ✓ Complete loop found with {loop.Count} nodes (source start)");
                    }
                    continue;
                }

                if (inPath.Contains(neighbor)) continue;

                // Normal DFS
                stack.Push(neighbor);
                inPath.Add(neighbor);
                DFS(neighbor, depth + 1);
                inPath.Remove(neighbor);
                stack.Pop();
            }
        }

        // Start path with source
        stack.Push(source);
        inPath.Add(source);
        DFS(source, 0);
        inPath.Remove(source);
        stack.Pop();

        return results;
    }

    List<ElectricalNode> GetConnectedNodes(ElectricalNode node)
    {
        List<ElectricalNode> connected = new List<ElectricalNode>();

        foreach (ElectricalConnection conn in electricalConnections)
        {
            if (conn.node1 == node && conn.node2.isFunctional)
                connected.Add(conn.node2);
            else if (conn.node2 == node && conn.node1.isFunctional)
                connected.Add(conn.node1);
        }

        return connected;
    }

    float CalculateCircuitResistance(List<ElectricalNode> circuitPath)
    {
        float totalR = 0f;

        Debug.Log($"[RESISTANCE] Calculating for circuit with {circuitPath.Count} nodes:");

        for (int i = 0; i < circuitPath.Count; i++)
        {
            float nodeR = circuitPath[i].resistance;
            totalR += nodeR;
            Debug.Log($"[RESISTANCE]   Node {i} ({circuitPath[i].placedObject.objectData.displayName}): {nodeR}Ω");

            // Add wire resistance between current and next
            ElectricalNode current = circuitPath[i];
            ElectricalNode next = circuitPath[(i + 1) % circuitPath.Count];

            ElectricalConnection conn = electricalConnections.FirstOrDefault(c =>
                (c.node1 == current && c.node2 == next) || (c.node2 == current && c.node1 == next));

            if (conn != null)
            {
                totalR += conn.resistance;
                Debug.Log($"[RESISTANCE]   Wire {i}->{(i + 1) % circuitPath.Count}: {conn.resistance}Ω");
            }
        }

        Debug.Log($"[RESISTANCE] TOTAL: {totalR}Ω (minimum enforced: {Mathf.Max(totalR, 0.01f)}Ω)");
        return Mathf.Max(totalR, 0.01f);
    }

    bool CircuitAlreadyExists(Circuit newCircuit)
    {
        foreach (Circuit existingCircuit in closedCircuits)
        {
            if (existingCircuit.nodes.Count != newCircuit.nodes.Count)
                continue;

            HashSet<ElectricalNode> existingNodes = new HashSet<ElectricalNode>(existingCircuit.nodes);
            HashSet<ElectricalNode> newNodes = new HashSet<ElectricalNode>(newCircuit.nodes);

            if (existingNodes.SetEquals(newNodes))
                return true;
        }
        return false;
    }

    void UpdateElectricalSimulation()
    {
        // Reset all
        foreach (var conn in electricalConnections)
            conn.current = 0f;

        foreach (var kvp in electricalNodes)
        {
            ElectricalNode node = kvp.Value;
            node.current = 0f;
            if (node.isSource && node.isFunctional)
                node.voltage = node.baseVoltage;
            else
                node.voltage = 0f;
        }

        // Process each circuit independently
        // First, check if any circuits are part of parallel groups
HashSet<Circuit> processedCircuits = new HashSet<Circuit>();

// Process parallel groups FIRST
// Process parallel groups FIRST
// Process parallel groups FIRST
foreach (var parallelGroup in parallelGroups)
{
    if (parallelGroup.parallelCircuits.Count < 2) continue;
    
    ElectricalNode source = parallelGroup.sharedSource;
    if (!source.isFunctional) continue;
    
    float sourceVoltage = source.baseVoltage;
    
    // Apply noise if any
    ElectricalNode noiseNode = parallelGroup.parallelCircuits[0].nodes.FirstOrDefault(n => n.voltageNoiseAmplitude > 0f);
    if (noiseNode != null)
    {
        float noiseValue = Mathf.PerlinNoise(Time.time * noiseNode.voltageNoiseRate + noiseNode.noiseOffset, 0f);
        float noiseFactor = 1f + (noiseValue - 0.5f) * 2f * noiseNode.voltageNoiseAmplitude;
        sourceVoltage *= noiseFactor;
    }
    
    Debug.Log($"[PARALLEL] Processing {parallelGroup.parallelCircuits.Count} parallel branches from source {source.placedObject.objectData.displayName} with {sourceVoltage:F2}V");
    
    // Calculate resistance for each branch
    List<float> branchResistances = new List<float>();
    
    foreach (var circuit in parallelGroup.parallelCircuits)
    {
        float branchR = 0f;
        
        // Sum resistance of all non-source components
        foreach (var node in circuit.nodes)
        {
            if (!node.isSource)
                branchR += node.resistance;
        }
        
        // Add wire resistances
        for (int i = 0; i < circuit.nodes.Count; i++)
        {
            ElectricalNode current = circuit.nodes[i];
            ElectricalNode next = circuit.nodes[(i + 1) % circuit.nodes.Count];
            
            ElectricalConnection conn = electricalConnections.FirstOrDefault(c =>
                (c.node1 == current && c.node2 == next) || (c.node2 == current && c.node1 == next));
            
            if (conn != null)
                branchR += conn.resistance;
        }
        
        branchR = Mathf.Max(branchR, 0.01f);
        branchResistances.Add(branchR);
        
        Debug.Log($"[PARALLEL] Branch {parallelGroup.parallelCircuits.IndexOf(circuit)}: Total R = {branchR:F3}Ω");
    }
    
    // Calculate current for each branch (I = V/R for each independent branch)
    List<float> branchCurrents = new List<float>();
    for (int i = 0; i < branchResistances.Count; i++)
    {
        float branchCurrent = sourceVoltage / branchResistances[i];
        branchCurrents.Add(branchCurrent);
        Debug.Log($"[PARALLEL] Branch {i}: I = {branchCurrent:F3}A (V={sourceVoltage:F2}V / R={branchResistances[i]:F3}Ω)");
    }
    
    // ✅ CRITICAL FIX: For parallel circuits, voltage distribution is proportional
    // Each component gets voltage based on its resistance ratio within its branch
    for (int branchIdx = 0; branchIdx < parallelGroup.parallelCircuits.Count; branchIdx++)
    {
        Circuit circuit = parallelGroup.parallelCircuits[branchIdx];
        float branchCurrent = branchCurrents[branchIdx];
        float branchTotalR = branchResistances[branchIdx];
        
        Debug.Log($"[PARALLEL] Applying to branch {branchIdx}:");
        
        // Apply current and voltage to each component in this branch
        foreach (var node in circuit.nodes)
        {
            if (!node.isFunctional) continue;
            
            // All components in this branch carry the same current
            node.current = branchCurrent;
            
            if (node.isSource)
            {
                node.voltage = sourceVoltage;
            }
            else
            {
                // ✅ FIX: Voltage across component = (R_component / R_total) × V_source
                // This represents the voltage DROP across this specific component
                float voltageAcrossComponent = (node.resistance / branchTotalR) * sourceVoltage;
                node.voltage = voltageAcrossComponent;
                
                Debug.Log($"[PARALLEL]   {node.placedObject.objectData.displayName}: V={voltageAcrossComponent:F2}V (R={node.resistance:F3}Ω), I={branchCurrent:F3}A, P={voltageAcrossComponent * branchCurrent:F2}W");
            }
        }
        
        // Update connection currents
        for (int i = 0; i < circuit.nodes.Count; i++)
        {
            ElectricalNode current = circuit.nodes[i];
            ElectricalNode next = circuit.nodes[(i + 1) % circuit.nodes.Count];
            
            ElectricalConnection conn = electricalConnections.FirstOrDefault(c =>
                (c.node1 == current && c.node2 == next) || (c.node2 == current && c.node1 == next));
            
            if (conn != null)
                conn.current = branchCurrent;
        }
        
        processedCircuits.Add(circuit);
    }
    
    // Total current drawn from source = sum of all branch currents
    float totalSourceCurrent = branchCurrents.Sum();
    source.current = totalSourceCurrent;
    Debug.Log($"[PARALLEL] Total source current: {totalSourceCurrent:F3}A (sum of {branchCurrents.Count} branches)");
}

// Then process remaining series circuits (those not in parallel groups)
foreach (Circuit circuit in closedCircuits)
{
    if (processedCircuits.Contains(circuit)) continue; // Already processed as parallel
    
    if (circuit.nodes == null || circuit.nodes.Count < 2) continue;
    if (!circuit.nodes.Any(n => n.isSource && n.isFunctional)) continue;

    // ✅ Verify this is actually a CLOSED circuit
    ElectricalNode source = circuit.nodes.First(n => n.isSource && n.isFunctional);

    bool isProperlyConnected = VerifyCircuitConnectivity(circuit, source);
    if (!isProperlyConnected)
    {
        Debug.LogWarning($"[CIRCUIT] Circuit not properly closed - skipping");
        continue;
    }

    float sourceVoltage = source.baseVoltage;

    // Apply noise if any
    ElectricalNode noiseNode = circuit.nodes.FirstOrDefault(n => n.voltageNoiseAmplitude > 0f);
    if (noiseNode != null)
    {
        float noiseValue = Mathf.PerlinNoise(Time.time * noiseNode.voltageNoiseRate + noiseNode.noiseOffset, 0f);
        float noiseFactor = 1f + (noiseValue - 0.5f) * 2f * noiseNode.voltageNoiseAmplitude;
        sourceVoltage *= noiseFactor;
    }

    // Calculate total resistance (excluding source)
    float totalResistance = 0f;
    foreach (var node in circuit.nodes)
    {
        if (!node.isSource)
            totalResistance += node.resistance;
    }

    // Add wire resistance
    for (int i = 0; i < circuit.nodes.Count; i++)
    {
        ElectricalNode current = circuit.nodes[i];
        ElectricalNode next = circuit.nodes[(i + 1) % circuit.nodes.Count];

        ElectricalConnection conn = electricalConnections.FirstOrDefault(c =>
            (c.node1 == current && c.node2 == next) || (c.node2 == current && c.node1 == next));

        if (conn != null)
            totalResistance += conn.resistance;
    }

    totalResistance = Mathf.Max(totalResistance, 0.01f);
    circuit.totalResistance = totalResistance;

    // Calculate circuit current (same throughout series circuit)
    float circuitCurrent = sourceVoltage / totalResistance;

    Debug.Log($"[SERIES] Processing: {string.Join(" → ", circuit.nodes.Select(n => n.placedObject.objectData.displayName))}");
    Debug.Log($"[SERIES] V={sourceVoltage:F2}V, R={totalResistance:F3}Ω, I={circuitCurrent:F3}A");

    // For series circuit: voltage drop across each component = I × R
    for (int i = 0; i < circuit.nodes.Count; i++)
    {
        ElectricalNode node = circuit.nodes[i];
        if (!node.isFunctional) continue;

        // All nodes get the same current in series
        node.current = circuitCurrent;

        // Voltage across each component (V = I × R)
        if (!node.isSource)
        {
            float voltageAcrossComponent = circuitCurrent * node.resistance;
            node.voltage = voltageAcrossComponent;

            Debug.Log($"[SERIES]   {node.placedObject.objectData.displayName}: V={voltageAcrossComponent:F2}V, R={node.resistance:F3}Ω, I={circuitCurrent:F3}A");
        }

        // Update connection current
        ElectricalNode next = circuit.nodes[(i + 1) % circuit.nodes.Count];
        ElectricalConnection conn = electricalConnections.FirstOrDefault(c =>
            (c.node1 == node && c.node2 == next) || (c.node2 == node && c.node1 == next));
        if (conn != null)
            conn.current = circuitCurrent;
    }
}

        // Check for damage
        bool anyDamage = false;
        foreach (var kvp in electricalNodes)
        {
            ElectricalNode node = kvp.Value;
            if (!node.isFunctional) continue;

            if (node.maxVoltage > 0f && node.voltage > node.maxVoltage)
            {
                node.isFunctional = false;
                node.voltage = 0f;
                node.current = 0f;
                SetObjectColor(node.placedObject.gameObject, Color.red);
                Debug.LogWarning($"[ELECTRICAL] {node.placedObject.objectData.displayName} DAMAGED! Exceeded max voltage ({node.maxVoltage}V)!");
                anyDamage = true;
            }

            if (node.maxAmperage > 0f && Mathf.Abs(node.current) > node.maxAmperage)
            {
                node.isFunctional = false;
                node.voltage = 0f;
                node.current = 0f;
                SetObjectColor(node.placedObject.gameObject, Color.red);
                Debug.LogWarning($"[ELECTRICAL] {node.placedObject.objectData.displayName} DAMAGED! Exceeded max amperage ({node.maxAmperage}A)!");
                anyDamage = true;
            }
        }

        if (anyDamage)
        {
            DetectClosedCircuits();
        }
    }

bool VerifyCircuitConnectivity(Circuit circuit, ElectricalNode source)
{
    if (circuit.nodes.Count < 2) return false;
    
    // Verify each consecutive pair of nodes has a physical wire connection
    for (int i = 0; i < circuit.nodes.Count; i++)
    {
        ElectricalNode current = circuit.nodes[i];
        ElectricalNode next = circuit.nodes[(i + 1) % circuit.nodes.Count];
        
        // Check if there's a wire connecting these two nodes
        bool connectionExists = electricalConnections.Any(c =>
            (c.node1 == current && c.node2 == next) || (c.node2 == current && c.node1 == next));
        
        if (!connectionExists)
        {
            Debug.LogWarning($"[CIRCUIT] Missing connection between {current.placedObject.objectData.displayName} and {next.placedObject.objectData.displayName}");
            return false;
        }
    }
    
    // Verify the path returns to source (closed loop)
    ElectricalNode lastNode = circuit.nodes[circuit.nodes.Count - 1];
    bool closedLoop = electricalConnections.Any(c =>
        (c.node1 == lastNode && c.node2 == source) || (c.node2 == lastNode && c.node1 == source));
    
    return closedLoop;
}

    void SetObjectColor(GameObject obj, Color color)
    {
        if (obj == null) return;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // IMPORTANT: material access will instantiate an instance
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                    mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
            }
        }
    }

    void UpdateGraphData()
    {
        foreach (var kvp in electricalNodes)
        {
            GameObject obj = kvp.Key.gameObject;
            float voltage = kvp.Value.voltage;
            float current = kvp.Value.current;

            if (!voltageHistory.ContainsKey(obj))
                voltageHistory[obj] = new List<float>();
            if (!currentHistory.ContainsKey(obj))
                currentHistory[obj] = new List<float>();

            voltageHistory[obj].Add(voltage);
            currentHistory[obj].Add(current);

            if (voltageHistory[obj].Count > maxGraphPoints)
                voltageHistory[obj].RemoveAt(0);
            if (currentHistory[obj].Count > maxGraphPoints)
                currentHistory[obj].RemoveAt(0);
        }

        if (currentlyInspectedObject != null)
        {
            UpdateGraphVisual(currentlyInspectedObject);
        }
    }

    public void ToggleInspectMode()
    {
        isInspectMode = !isInspectMode;

        if (!isInspectMode)
        {
            if (voltageDisplayPanel != null)
                voltageDisplayPanel.SetActive(false);

            currentlyInspectedObject = null;
            ClearGraph();
        }

        if (inspectButtonImage != null)
        {
            inspectButtonImage.color = isInspectMode ? inspectActiveColor : normalRunButtonColor;
        }

        Debug.Log($"[ELECTRICAL] Inspect mode {(isInspectMode ? "enabled" : "disabled")}");
    }

    void HandleInspectMode()
    {
        if (!isSimulationRunning)
        {
            if (voltageDisplayPanel != null && voltageDisplayPanel.activeSelf)
            {
                if (statusText != null)
                    statusText.text = "Start simulation first!\nClick RUN SIMULATION button";
            }
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            PlacedObjectComponent component = hit.collider.GetComponentInParent<PlacedObjectComponent>();
            if (component != null && component.placedObjectData != null)
            {
                ShowVoltageInfo(component.placedObjectData);
                return;
            }

            LineRenderer lineRenderer = hit.collider.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                ShowWireVoltageInfo(lineRenderer.gameObject);
                return;
            }
        }

        if (voltageDisplayPanel != null && !Input.GetMouseButton(0))
        {
            voltageDisplayPanel.SetActive(false);
            currentlyInspectedObject = null;
            ClearGraph();
        }
    }

    void ShowVoltageInfo(PlacedGridObject obj)
{
    if (!electricalNodes.ContainsKey(obj))
    {
        Debug.LogWarning($"[ELECTRICAL] Object {obj.objectData.displayName} not found in nodes!");
        return;
    }

    ElectricalNode node = electricalNodes[obj];

    if (voltageDisplayPanel != null)
        voltageDisplayPanel.SetActive(true);

    // Update name text
    if (nameText != null)
        nameText.text = obj.objectData.displayName;

    // Update type text
    if (typeText != null)
        typeText.text = obj.objectData.objectTypeName;

    // ✅ NEW: Check if this is a switch
    ElectricalSwitch switchComponent = obj.gameObject.GetComponent<ElectricalSwitch>();
    
    // ✅ NEW: Show switch status
    if (switchStatusText != null)
    {
        if (switchComponent != null)
        {
            // It's a switch - show ON/OFF based on switch state
            switchStatusText.text = switchComponent.IsOn() ? "ON" : "OFF";
        }
        else
        {
            // Not a switch - show ON if current is flowing, OFF otherwise
            switchStatusText.text = (Mathf.Abs(node.current) > 0.001f) ? "ON" : "OFF";
        }
    }

    // Use formatted values with proper prefixes
    SetFormattedValue(voltageValueText, node.voltage, "V");
    SetFormattedValue(resistanceValueText, node.resistance, "Ω");
    SetFormattedValue(currentValueText, node.current, "A");
    SetFormattedValue(powerValueText, node.voltage * node.current, "W");
    SetFormattedValue(maxVoltageValueText, node.maxVoltage, "V");
    SetFormattedValue(maxAmperageValueText, node.maxAmperage, "A");

    // Noise display with ± symbol
    if (node.voltageNoiseAmplitude > 0f)
    {
        float noiseValue = node.voltageNoiseAmplitude * 100f;
        float noiseRateSeconds = 1f / node.voltageNoiseRate;

        if (noiseAmplitudeValueText != null)
        {
            noiseAmplitudeValueText.richText = true;
            noiseAmplitudeValueText.text = $"±{noiseValue:F1}";
        }

        if (noiseRateValueText != null)
        {
            noiseRateValueText.richText = true;
            string unitColorHex = ColorUtility.ToHtmlStringRGBA(unitColor);
            noiseRateValueText.text = $"{noiseRateSeconds:F2} <size={unitSizePercent}%><color=#{unitColorHex}>s</color></size>";
        }
    }
    else
    {
        if (noiseAmplitudeValueText != null)
        {
            noiseAmplitudeValueText.richText = true;
            noiseAmplitudeValueText.text = $"±0";
        }
        if (noiseRateValueText != null)
        {
            noiseRateValueText.richText = true;
            string unitColorHex = ColorUtility.ToHtmlStringRGBA(unitColor);
            noiseRateValueText.text = $"0 <size={unitSizePercent}%><color=#{unitColorHex}>s</color></size>";
        }
    }

    // Status
    if (statusText != null)
    {
        string status = node.isFunctional ? "Functional" : "DAMAGED";
        string sourceInfo = node.isSource ? " (Source)" : "";
        statusText.text = $"{status}{sourceInfo}";
    }

    // Circuit Status
    if (circuitStatusText != null)
    {
        string circuitInfo = IsNodeInClosedCircuit(node) ? "In closed circuit" : "NOT in closed circuit";
        circuitStatusText.text = circuitInfo;
    }

    if (currentlyInspectedObject != obj.gameObject)
        currentlyInspectedObject = obj.gameObject;
}

    void SetFormattedValue(TextMeshProUGUI textField, float value, string unit, int decimalPlaces = 2)
    {
        if (textField == null) return;

        // Force enable rich text
        textField.richText = true;

        string valueStr = value.ToString($"F{decimalPlaces}");

        // Convert Unity Color to RGBA hex (include alpha)
        string unitColorHex = ColorUtility.ToHtmlStringRGBA(unitColor);

        // Apply size and color
        textField.text = $"{valueStr} <size={unitSizePercent}%><color=#{unitColorHex}>{unit}</color></size>";
    }


    bool IsNodeInClosedCircuit(ElectricalNode node)
    {
        foreach (Circuit circuit in closedCircuits)
        {
            if (circuit.nodes.Contains(node))
                return true;
        }
        return false;
    }

    void ShowWireVoltageInfo(GameObject wireObject)
    {
        WireData wire = wirePlacementSystem.GetAllWires().FirstOrDefault(w => w.wireObject == wireObject);
        if (wire == null) return;

        PlacedGridObject startObj = FindPlacedObjectByConnectionPoint(wire.startPoint);
        PlacedGridObject endObj = FindPlacedObjectByConnectionPoint(wire.endPoint);

        if (startObj == null || endObj == null) return;

        if (!electricalNodes.ContainsKey(startObj) || !electricalNodes.ContainsKey(endObj))
            return;

        ElectricalNode node1 = electricalNodes[startObj];
        ElectricalNode node2 = electricalNodes[endObj];

        float avgVoltage = (node1.voltage + node2.voltage) / 2f;
        float voltageDiff = Mathf.Abs(node1.voltage - node2.voltage);

        // Find connection to get current
        ElectricalConnection conn = electricalConnections.FirstOrDefault(c =>
            (c.node1 == node1 && c.node2 == node2) || (c.node2 == node1 && c.node1 == node2));

        float current = conn != null ? conn.current : 0f;

        if (voltageDisplayPanel != null)
            voltageDisplayPanel.SetActive(true);

        // Update name and type for wire
        if (nameText != null)
        {
            nameText.text = "Wire";
        }

        if (typeText != null)
        {
            typeText.text = "Connection";
        }

        // Update voltage display for wire
        if (voltageText != null)
        {
            voltageText.text = $"{avgVoltage:F2}V";
        }

        // Update resistance (wires have minimal resistance)
        if (resistanceText != null)
        {
            resistanceText.text = $"{(conn != null ? conn.resistance : 0.01f):F2}Ω";
        }

        // Update status text for wire endpoints
        if (statusText != null)
        {
            statusText.text = $"From: {startObj.objectData.displayName}\nTo: {endObj.objectData.displayName}";
        }

        // Update current text
        if (currentText != null)
        {
            currentText.text = $"{current:F2}A";
        }

        // Update power text (voltage drop info)
        if (powerText != null)
        {
            powerText.text = $"Drop: {voltageDiff:F2}V";
        }

        // Clear other fields
        if (maxVoltageText != null)
            maxVoltageText.text = "N/A";
        if (maxAmperageText != null)
            maxAmperageText.text = "N/A";
        if (circuitStatusText != null)
            circuitStatusText.text = "";

        currentlyInspectedObject = null;
        ClearGraph();
    }

    void UpdateGraphVisual(GameObject obj)
    {
        if (!currentHistory.ContainsKey(obj) || graphContainer == null)
            return;

        List<float> history = currentHistory[obj];
        if (history.Count < 2)
        {
            ClearGraph();
            return;
        }

        // Find placed object for maxAmperage
        PlacedGridObject placedObj = electricalNodes.Keys.FirstOrDefault(k => k.gameObject == obj);

        float maxAmperage = 1f;
        if (placedObj != null && placedObj.objectData.maxAmperage > 0)
        {
            maxAmperage = placedObj.objectData.maxAmperage;
        }
        else
        {
            maxAmperage = Mathf.Max(0.1f, history.Max());
        }

        Rect rect = graphContainer.rect;
        float width = rect.width;
        float height = rect.height;

        float paddingX = width * 0.05f;
        float paddingY = height * 0.1f;
        float graphWidth = width - (paddingX * 2);
        float graphHeight = height - (paddingY * 2);

        Vector2[] positions = new Vector2[history.Count];
        for (int i = 0; i < history.Count; i++)
        {
            float normalizedX = i / (float)(history.Count - 1);
            float normalizedY = Mathf.Clamp01(history[i] / maxAmperage);

            float x = normalizedX * graphWidth + paddingX - (width / 2f);
            float y = normalizedY * graphHeight + paddingY - (height / 2f);

            positions[i] = new Vector2(x, y);
        }

        int neededSegments = history.Count - 1;

        // Remove extra segments
        while (graphLineSegments.Count > neededSegments)
        {
            GameObject oldSegment = graphLineSegments[graphLineSegments.Count - 1];
            graphLineSegments.RemoveAt(graphLineSegments.Count - 1);
            if (oldSegment != null) Destroy(oldSegment);
        }

        for (int i = 0; i < neededSegments; i++)
        {
            Vector2 start = positions[i];
            Vector2 end = positions[i + 1];

            GameObject lineSegment;
            if (i >= graphLineSegments.Count)
            {
                lineSegment = new GameObject($"GraphLine_{Time.frameCount}_{i}");
                lineSegment.transform.SetParent(graphContainer, false);

                Image img = lineSegment.AddComponent<Image>();
                img.color = graphLineColor;
                img.raycastTarget = false;

                graphLineSegments.Add(lineSegment);
            }
            else
            {
                lineSegment = graphLineSegments[i];
            }

            RectTransform rt = lineSegment.GetComponent<RectTransform>();

            Vector2 direction = end - start;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = start;
            rt.sizeDelta = new Vector2(distance, graphLineThickness);
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            rt.pivot = new Vector2(0, 0.5f);

            lineSegment.SetActive(true);
        }
    }

    void ClearGraph()
    {
        foreach (GameObject segment in graphLineSegments)
        {
            if (segment != null)
                Destroy(segment);
        }
        graphLineSegments.Clear();
    }

    public bool IsSimulationRunning()
    {
        return isSimulationRunning;
    }

    void OnDisable()
    {
        if (runButton != null)
            runButton.onClick.RemoveListener(ToggleSimulation);

        if (inspectButton != null)
            inspectButton.onClick.RemoveListener(ToggleInspectMode);

        ClearGraph();
    }
    
    
    public string FormatValueWithUnit(float value, string baseUnit)
{
    string prefix = "";
    float displayValue = value;
    
    if (Mathf.Abs(value) >= 1e9f)
    {
        displayValue = value / 1e9f;
        prefix = "G";
    }
    else if (Mathf.Abs(value) >= 1e6f)
    {
        displayValue = value / 1e6f;
        prefix = "M";
    }
    else if (Mathf.Abs(value) >= 1e3f)
    {
        displayValue = value / 1e3f;
        prefix = "k";
    }
    else if (Mathf.Abs(value) >= 1f)
    {
        displayValue = value;
        prefix = "";
    }
    else if (Mathf.Abs(value) >= 1e-3f)
    {
        displayValue = value * 1e3f;
        prefix = "m";
    }
    else if (Mathf.Abs(value) >= 1e-6f)
    {
        displayValue = value * 1e6f;
        prefix = "µ";
    }
    else if (Mathf.Abs(value) >= 1e-9f)
    {
        displayValue = value * 1e9f;
        prefix = "n";
    }
    else
    {
        return $"{value:E2}{baseUnit}";
    }
    
    return $"{displayValue:F2}{prefix}{baseUnit}";
}
}

[System.Serializable]
public class ElectricalNode
{
    public PlacedGridObject placedObject;
    public float voltage;
    public float current;
    public float baseVoltage;
    public float resistance;
    public float maxVoltage;
    public float maxAmperage;
    public bool isSource;
    public bool isFunctional;

    // Noise simulation
// Voltage noise simulation
public float voltageNoiseAmplitude;
public float voltageNoiseRate;
public float noiseOffset; // Random offset for Perlin noise
}

[System.Serializable]
public class ElectricalConnection
{
    public WireData wire;
    public ElectricalNode node1;
    public ElectricalNode node2;
    public float resistance;
    public float current;
}

[System.Serializable]
public class Circuit
{
    public List<ElectricalNode> nodes;
    public bool hasSource;
    public float totalResistance;
}

[System.Serializable]
public class ParallelCircuitGroup
{
    public List<Circuit> parallelCircuits = new List<Circuit>();
    public ElectricalNode sharedSource;
    public float totalCurrent;
}

[System.Serializable]
public class MixedCircuitGroup
{
    public List<Circuit> circuits = new List<Circuit>();
    public ElectricalNode sharedSource;
    public List<ElectricalNode> junctionNodes = new List<ElectricalNode>();
    public List<ParallelSegment> parallelSegments = new List<ParallelSegment>();
}

[System.Serializable]
public class ParallelSegment
{
    public ElectricalNode startJunction;
    public ElectricalNode endJunction;
    public List<List<ElectricalNode>> parallelPaths = new List<List<ElectricalNode>>();
}