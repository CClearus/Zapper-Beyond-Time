using UnityEngine;
using System.Collections.Generic;

public class GridSystem : MonoBehaviour
{
    [Header("Grid Reference (Read from Shader)")]
    public Renderer gridRenderer;

    // Grid data for calculations
    private float gridWidth = 10f;
    private float gridHeight = 10f;
    private float cellSize = 1f;
    private Vector3 gridCenter;
    private Vector3 gridWorldSize;

    void Start()
    {
        if (gridRenderer == null)
            gridRenderer = GetComponent<Renderer>();
            
        UpdateGridDataFromShader();
    }

    void UpdateGridDataFromShader()
    {
        if (gridRenderer != null && gridRenderer.material != null)
        {
            Material mat = gridRenderer.material;
            float mainGridWidth = mat.GetFloat("_GridWidth");
            float mainGridHeight = mat.GetFloat("_GridHeight");
            float mainCellSize = mat.GetFloat("_CellSize");
            int subDivisions = mat.GetInt("_SubGridDivisions");
            
            // Calculate sub-grid dimensions
            gridWidth = mainGridWidth * subDivisions;
            gridHeight = mainGridHeight * subDivisions;
            cellSize = mainCellSize / subDivisions;
        }
        
        gridCenter = transform.position;
        gridWorldSize = new Vector3(gridWidth * cellSize, 0, gridHeight * cellSize);
    }

    void Update()
    {
        // Update grid data each frame in case shader properties changed
        UpdateGridDataFromShader();
    }

    // Utility methods for grid placement
    public Vector3 WorldToGridPosition(Vector3 worldPos)
    {
        // Convert world position to local position relative to grid center
        Vector3 localPos = worldPos - gridCenter;

        // Convert to grid coordinates - each unit in Unity equals one grid cell
        float x = Mathf.Round(localPos.x / cellSize);
        float z = Mathf.Round(localPos.z / cellSize);

        return new Vector3(x, 0, z);
    }

    public Vector3 GridToWorldPosition(Vector3 gridPos)
    {
        Vector3 worldPos = gridCenter + new Vector3(gridPos.x * cellSize, 0, gridPos.z * cellSize);
        return worldPos;
    }

    public Vector3 GridToWorldPosition(int gridX, int gridZ)
    {
        return GridToWorldPosition(new Vector3(gridX, 0, gridZ));
    }

    public bool IsValidGridPosition(Vector3 gridPos)
    {
        return gridPos.x >= -gridWidth / 2 && gridPos.x < gridWidth / 2 &&
               gridPos.z >= -gridHeight / 2 && gridPos.z < gridHeight / 2;
    }

    public bool IsValidGridPosition(int gridX, int gridZ)
    {
        return IsValidGridPosition(new Vector3(gridX, 0, gridZ));
    }

    // Get grid bounds
    public Vector2Int GetGridBounds()
    {
        return new Vector2Int(Mathf.RoundToInt(gridWidth), Mathf.RoundToInt(gridHeight));
    }

    // Get all valid grid positions
    public Vector3[] GetAllGridPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        int halfWidth = Mathf.RoundToInt(gridWidth) / 2;
        int halfHeight = Mathf.RoundToInt(gridHeight) / 2;

        for (int x = -halfWidth; x < halfWidth; x++)
        {
            for (int z = -halfHeight; z < halfHeight; z++)
            {
                positions.Add(new Vector3(x, 0, z));
            }
        }
        return positions.ToArray();
    }

    // Snap any world position to nearest grid point
    public Vector3 SnapToGrid(Vector3 worldPos)
    {
        Vector3 gridPos = WorldToGridPosition(worldPos);
        return GridToWorldPosition(gridPos);
    }

    // Get cell size for external calculations
    public float GetCellSize()
    {
        return cellSize;
    }

    // Get grid dimensions for external calculations
    public Vector2Int GetGridDimensions()
    {
        return new Vector2Int(Mathf.RoundToInt(gridWidth), Mathf.RoundToInt(gridHeight));
    }

    // Get world bounds of the entire grid
    public Bounds GetWorldBounds()
    {
        return new Bounds(gridCenter, gridWorldSize);
    }

    // Check if a world position is within the grid bounds
    public bool IsWorldPositionInGrid(Vector3 worldPos)
    {
        Vector3 gridPos = WorldToGridPosition(worldPos);
        return IsValidGridPosition(gridPos);
    }

    // Get the exact center of a grid cell in world space
    public Vector3 GetCellCenter(int gridX, int gridZ)
    {
        return GridToWorldPosition(new Vector3(gridX, 0, gridZ));
    }

    // Get the corner positions of a specific grid cell
    public Vector3[] GetCellCorners(int gridX, int gridZ)
    {
        Vector3 center = GetCellCenter(gridX, gridZ);
        float halfCell = cellSize * 0.5f;

        return new Vector3[]
        {
            center + new Vector3(-halfCell, 0, -halfCell), // Bottom-left
            center + new Vector3(halfCell, 0, -halfCell),  // Bottom-right
            center + new Vector3(-halfCell, 0, halfCell),  // Top-left
            center + new Vector3(halfCell, 0, halfCell)    // Top-right
        };
    }

    // Manual update methods for runtime changes
    public void UpdateGrid()
    {
        UpdateGridDataFromShader();
    }

    // Get grid position from world coordinates (returns integer grid coordinates)
    public Vector2Int GetGridCoordinates(Vector3 worldPos)
    {
        Vector3 gridPos = WorldToGridPosition(worldPos);
        return new Vector2Int(Mathf.RoundToInt(gridPos.x), Mathf.RoundToInt(gridPos.z));
    }

    // Check if grid coordinates are within bounds
    public bool AreCoordinatesValid(int x, int z)
    {
        return x >= -gridWidth / 2 && x < gridWidth / 2 &&
               z >= -gridHeight / 2 && z < gridHeight / 2;
    }

    // Additional methods for main grid reference
    public Vector3 SubGridToMainGrid(Vector3 subGridPos)
    {
        if (gridRenderer != null && gridRenderer.material != null)
        {
            int subDivisions = gridRenderer.material.GetInt("_SubGridDivisions");
            return new Vector3(
                Mathf.Floor(subGridPos.x / subDivisions),
                0,
                Mathf.Floor(subGridPos.z / subDivisions)
            );
        }
        return subGridPos;
    }

    public Vector3 MainGridToSubGrid(Vector3 mainGridPos)
    {
        if (gridRenderer != null && gridRenderer.material != null)
        {
            int subDivisions = gridRenderer.material.GetInt("_SubGridDivisions");
            return new Vector3(
                mainGridPos.x * subDivisions,
                0,
                mainGridPos.z * subDivisions
            );
        }
        return mainGridPos;
    }

    // Get which main grid cell this sub-grid position belongs to
    public Vector2Int GetParentMainGridCell(Vector3 subGridPos)
    {
        Vector3 mainGridPos = SubGridToMainGrid(subGridPos);
        return new Vector2Int(Mathf.RoundToInt(mainGridPos.x), Mathf.RoundToInt(mainGridPos.z));
    }

    // Get the actual subdivision values for reference
    public int GetSubGridDivisions()
    {
        if (gridRenderer != null && gridRenderer.material != null)
        {
            return gridRenderer.material.GetInt("_SubGridDivisions");
        }   
        return 4; // Default
    }

    public float GetMainCellSize()
    {
        if (gridRenderer != null && gridRenderer.material != null)
        {
            return gridRenderer.material.GetFloat("_CellSize");
        }
        return 1f; // Default
    }
}