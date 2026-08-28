using UnityEngine;
/*
[CreateAssetMenu(fileName = "Stage_", menuName = "Game/Stage Configuration", order = 1)]
public class StageConfigSO : ScriptableObject
{
    [Tooltip("Stage number (1, 2, 3, etc.)")]
    public int stageNumber;

    [Header("Level Configuration")]
    [Tooltip("How many levels the player must complete in this stage")]
    public int numberOfLevels;

    [Header("Dungeon Generation Parameters")]
    [Tooltip("Configuration for each dungeon/level in this stage")]
    public DungeonConfig[] dungeonConfigs;

    [Header("Difficulty Scaling")]
    [Tooltip("Enemy spawn rate multiplier for this stage")]
    public float enemySpawnMultiplier = 1f;

    // Nested class to hold individual dungeon configuration
    [System.Serializable]
    public class DungeonConfig
    {
        [Tooltip("Minimum main artery rooms for this dungeon")]
        public int minMainArteryRooms = 3;

        [Tooltip("Maximum main artery rooms for this dungeon")]
        public int maxMainArteryRooms = 7;

        [Tooltip("Main room spacing for this dungeon")]
        public float mainRoomSpacing = 8f;

        [Range(0f, 1f)]
        [Tooltip("Chance for L-turns in this dungeon")]
        public float chanceForLTurn = 0.3f;

        [Range(0f, 1f)]
        [Tooltip("Distributive node chance per segment")]
        public float distributiveNodeChancePerSegment = 0.4f;
    }

    /// <summary>
    /// Creates a MapParameters object from this stage config for a specific level
    /// </summary>
    public MapParameters GetMapParameters(int levelIndex, MapParameters baseParameters)
    {
        // Get the dungeon config for this level (with wrapping if needed)
        int configIndex = levelIndex % dungeonConfigs.Length;
        DungeonConfig config = dungeonConfigs[configIndex];

        // Create a new MapParameters based on the base
        MapParameters mapParams = new MapParameters
        {
            // Override with stage-specific values
            minMainArteryRooms = config.minMainArteryRooms,
            maxMainArteryRooms = config.maxMainArteryRooms,
            mainRoomSpacing = config.mainRoomSpacing,
            chanceForLTurn = config.chanceForLTurn,
            distributiveNodeChancePerSegment = config.distributiveNodeChancePerSegment,

            // Copy all other values from base parameters
            mainArteryPositionJitter = baseParameters.mainArteryPositionJitter,
            minLeafNodesPerDistributive = baseParameters.minLeafNodesPerDistributive,
            maxLeafNodesPerDistributive = baseParameters.maxLeafNodesPerDistributive,
            leafBranchLength = baseParameters.leafBranchLength,
            leafNodePositionJitter = baseParameters.leafNodePositionJitter,
            baseStartRoomSize = baseParameters.baseStartRoomSize,
            baseEndRoomSize = baseParameters.baseEndRoomSize,
            baseMainArteryRoomSize = baseParameters.baseMainArteryRoomSize,
            baseDistributiveRoomSize = baseParameters.baseDistributiveRoomSize,
            baseLeafRoomSize = baseParameters.baseLeafRoomSize,
            baseCornerRoomSize = baseParameters.baseCornerRoomSize,
            roomSizeVariationPercentage = baseParameters.roomSizeVariationPercentage,
            minRoomDistance = baseParameters.minRoomDistance,
            maxRepositionAttempts = baseParameters.maxRepositionAttempts,
            repositionSearchRadius = baseParameters.repositionSearchRadius,
            corridorWidth = baseParameters.corridorWidth
        };

        return mapParams;
    }
}
*/
