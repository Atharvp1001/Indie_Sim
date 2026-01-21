using UnityEngine;
using System.Collections.Generic;

public class JumpFloodPathfinding : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 100;
    [SerializeField] private int gridHeight = 100;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 gridOrigin = Vector2.zero;
    
    [Header("Pathfinding Settings")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float updateInterval = 0.2f; // Update flow field every 0.2s
    [SerializeField] private int jumpFloodIterations = 8; // Number of JFA passes
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGrid = false;
    [SerializeField] private bool showFlowField = false;
    
    // Static instance
    public static JumpFloodPathfinding Instance;
    
    // Grid data
    private Vector2[,] flowField; // Direction to move at each cell
    private float[,] distanceField; // Distance to goal at each cell
    private bool[,] obstacleGrid; // True if cell is blocked
    
    private Transform player;
    private float updateTimer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        // Initialize grids
        flowField = new Vector2[gridWidth, gridHeight];
        distanceField = new float[gridWidth, gridHeight];
        obstacleGrid = new bool[gridWidth, gridHeight];
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        // Initialize obstacle grid
        BuildObstacleGrid();
        
        // Initial flow field calculation
        if (player != null)
        {
            CalculateFlowField(player.position);
        }
    }

    void Update()
    {
        if (player == null) return;
        
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0f)
        {
            updateTimer = updateInterval;
            
            // Rebuild obstacle grid periodically (in case of dynamic obstacles)
            BuildObstacleGrid();
            
            // Recalculate flow field toward player
            CalculateFlowField(player.position);
        }
    }

    void BuildObstacleGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 worldPos = GridToWorld(x, y);
                
                // Check if this cell has an obstacle
                // Use a smaller check radius to ensure 2-tile width paths
                Collider2D hit = Physics2D.OverlapCircle(worldPos, cellSize * 0.3f, obstacleLayer);
                obstacleGrid[x, y] = (hit != null);
            }
        }
    }

    void CalculateFlowField(Vector3 goalPosition)
    {
        // Convert goal to grid coordinates
        Vector2Int goalCell = WorldToGrid(goalPosition);
        
        // Clamp to grid bounds
        goalCell.x = Mathf.Clamp(goalCell.x, 0, gridWidth - 1);
        goalCell.y = Mathf.Clamp(goalCell.y, 0, gridHeight - 1);
        
        // Initialize distance field
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                distanceField[x, y] = float.MaxValue;
            }
        }
        
        // Set goal distance to 0
        distanceField[goalCell.x, goalCell.y] = 0f;
        
        // Jump Flood Algorithm
        int maxDimension = Mathf.Max(gridWidth, gridHeight);
        int jumpDistance = maxDimension / 2;
        
        for (int iteration = 0; iteration < jumpFloodIterations; iteration++)
        {
            if (jumpDistance < 1) jumpDistance = 1;
            
            JumpFloodPass(goalCell, jumpDistance);
            
            jumpDistance /= 2;
        }
        
        // Final pass with jump distance 1
        JumpFloodPass(goalCell, 1);
        
        // Generate flow field from distance field
        GenerateFlowField();
    }

    void JumpFloodPass(Vector2Int goal, int jumpDist)
    {
        float[,] newDistances = (float[,])distanceField.Clone();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Skip obstacles
                if (obstacleGrid[x, y]) continue;
                
                float bestDist = distanceField[x, y];
                
                // Check neighbors at jump distance
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx * jumpDist;
                        int ny = y + dy * jumpDist;
                        
                        // Bounds check
                        if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                            continue;
                        
                        // Calculate distance through this neighbor
                        float distToGoal = Vector2.Distance(
                            new Vector2(x, y),
                            new Vector2(goal.x, goal.y)
                        );
                        
                        if (distToGoal < bestDist)
                        {
                            bestDist = distToGoal;
                        }
                    }
                }
                
                newDistances[x, y] = bestDist;
            }
        }
        
        distanceField = newDistances;
    }

    void GenerateFlowField()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Skip obstacles
                if (obstacleGrid[x, y])
                {
                    flowField[x, y] = Vector2.zero;
                    continue;
                }
                
                // Find the neighbor with lowest distance
                Vector2 bestDirection = Vector2.zero;
                float lowestDistance = distanceField[x, y];
                
                // Check all 8 neighbors
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        int nx = x + dx;
                        int ny = y + dy;
                        
                        // Bounds check
                        if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                            continue;
                        
                        // Skip obstacles
                        if (obstacleGrid[nx, ny]) continue;
                        
                        if (distanceField[nx, ny] < lowestDistance)
                        {
                            lowestDistance = distanceField[nx, ny];
                            bestDirection = new Vector2(dx, dy).normalized;
                        }
                    }
                }
                
                flowField[x, y] = bestDirection;
            }
        }
    }

    public Vector2 GetNextStep(Vector3 currentPosition, Vector3 goalPosition)
    {
        Vector2Int cellPos = WorldToGrid(currentPosition);
        
        // Bounds check
        if (cellPos.x < 0 || cellPos.x >= gridWidth || cellPos.y < 0 || cellPos.y >= gridHeight)
        {
            // Out of bounds, return direct path
            return goalPosition;
        }
        
        // Get flow direction at current cell
        Vector2 flowDirection = flowField[cellPos.x, cellPos.y];
        
        if (flowDirection == Vector2.zero)
        {
            // No valid flow, return direct path
            return goalPosition;
        }
        
        // Return next position based on flow
        return (Vector2)currentPosition + flowDirection * cellSize;
    }

    public Vector2 GetFlowDirection(Vector3 position)
    {
        Vector2Int cellPos = WorldToGrid(position);
        
        // Bounds check
        if (cellPos.x < 0 || cellPos.x >= gridWidth || cellPos.y < 0 || cellPos.y >= gridHeight)
        {
            return Vector2.zero;
        }
        
        return flowField[cellPos.x, cellPos.y];
    }

    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector2 relativePos = (Vector2)worldPos - gridOrigin;
        int x = Mathf.RoundToInt(relativePos.x / cellSize);
        int y = Mathf.RoundToInt(relativePos.y / cellSize);
        return new Vector2Int(x, y);
    }

    Vector2 GridToWorld(int x, int y)
    {
        return gridOrigin + new Vector2(x * cellSize, y * cellSize);
    }

    void OnDrawGizmos()
    {
        if (!showDebugGrid && !showFlowField) return;
        if (flowField == null) return;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 worldPos = GridToWorld(x, y);
                
                // Draw grid cells
                if (showDebugGrid)
                {
                    if (obstacleGrid != null && obstacleGrid[x, y])
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.8f);
                    }
                    else
                    {
                        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
                        Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.9f);
                    }
                }
                
                // Draw flow field arrows
                if (showFlowField && flowField[x, y] != Vector2.zero)
                {
                    Gizmos.color = Color.cyan;
                    Vector2 endPos = worldPos + flowField[x, y] * cellSize * 0.4f;
                    Gizmos.DrawLine(worldPos, endPos);
                    
                    // Arrow head
                    Vector2 dir = flowField[x, y];
                    Vector2 right = new Vector2(-dir.y, dir.x) * 0.1f;
                    Gizmos.DrawLine(endPos, endPos - (Vector2)(dir * 0.15f) + right);
                    Gizmos.DrawLine(endPos, endPos - (Vector2)(dir * 0.15f) - right);
                }
            }
        }
    }
}