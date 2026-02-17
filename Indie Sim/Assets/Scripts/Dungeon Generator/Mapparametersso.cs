using UnityEngine;

/// <summary>
/// ScriptableObject for storing dungeon generation parameters.
/// Create via: Right-click in Project > Create > Dungeon > Map Parameters
/// This allows you to create multiple preset configurations and swap between them.
/// </summary>
[CreateAssetMenu(fileName = "New Map Parameters", menuName = "Dungeon/Map Parameters", order = 1)]
public class MapParametersSO : ScriptableObject
{
    [Header("Main Artery Control")]
    public int nodeCount = 5;
    public float mainRoomSpacing = 8f;
    [Range(0f, 1f)] public float chanceForLTurn = 0.3f;
    public float mainArteryPositionJitter = 2f;

    [Header("Distributive Node & Leaf Control")]
    [Range(0f, 1f)] public float distributiveNodeChancePerSegment = 0.4f;
    public int minLeafNodesPerDistributive = 1;
    public int maxLeafNodesPerDistributive = 3;
    public float leafBranchLength = 6f;
    public float leafNodePositionJitter = 1.5f;

    [Header("Room Size Control")]
    public Vector2Int baseStartRoomSize = new Vector2Int(8, 8);
    public Vector2Int baseEndRoomSize = new Vector2Int(8, 8);
    public Vector2Int baseMainArteryRoomSize = new Vector2Int(6, 6);
    public Vector2Int baseDistributiveRoomSize = new Vector2Int(4, 4);
    public Vector2Int baseLeafRoomSize = new Vector2Int(5, 5);
    public Vector2Int baseCornerRoomSize = new Vector2Int(3, 3);
    [Range(0f, 1f)] public float roomSizeVariationPercentage = 0.2f;

    [Header("Collision & Placement Control")]
    public float minRoomDistance = 2f;
    public int maxRepositionAttempts = 10;
    public float repositionSearchRadius = 3f;

    [Header("Start Room Isolation")]
    public float startRoomToArteryDistance = 12f;
    public float minStartToEndDistance = 40f;

    [Header("Room Rotation")]
    [Range(0f, 1f)] public float roomRotationChance = 0.5f;

    [Header("Corridor Settings")]
    public int corridorWidth = 2;

    /// <summary>
    /// Converts this ScriptableObject to a runtime MapParameters instance.
    /// This is used internally by the dungeon generator.
    /// </summary>
    public MapParameters ToMapParameters()
    {
        return new MapParameters
        {
            nodeCount = this.nodeCount,
            mainRoomSpacing = this.mainRoomSpacing,
            chanceForLTurn = this.chanceForLTurn,
            mainArteryPositionJitter = this.mainArteryPositionJitter,
            distributiveNodeChancePerSegment = this.distributiveNodeChancePerSegment,
            minLeafNodesPerDistributive = this.minLeafNodesPerDistributive,
            maxLeafNodesPerDistributive = this.maxLeafNodesPerDistributive,
            leafBranchLength = this.leafBranchLength,
            leafNodePositionJitter = this.leafNodePositionJitter,
            baseStartRoomSize = this.baseStartRoomSize,
            baseEndRoomSize = this.baseEndRoomSize,
            baseMainArteryRoomSize = this.baseMainArteryRoomSize,
            baseDistributiveRoomSize = this.baseDistributiveRoomSize,
            baseLeafRoomSize = this.baseLeafRoomSize,
            baseCornerRoomSize = this.baseCornerRoomSize,
            roomSizeVariationPercentage = this.roomSizeVariationPercentage,
            minRoomDistance = this.minRoomDistance,
            maxRepositionAttempts = this.maxRepositionAttempts,
            repositionSearchRadius = this.repositionSearchRadius,
            startRoomToArteryDistance = this.startRoomToArteryDistance,
            minStartToEndDistance = this.minStartToEndDistance,
            roomRotationChance = this.roomRotationChance,
            corridorWidth = this.corridorWidth
        };
    }
}