using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Tilemaps;

// Enum for Node/Room Types
public enum RoomType
{
    MAIN_ARTERY_ROOM,      // Main node
    DISTRIBUTIVE_NODE_ROOM, // Connective node
    LEAF_NODE_ROOM,        // Leaf node
    ARTERY_CORNER_ROOM     // Corner node
}

// Enum for Connection Types
public enum ConnectionType
{
    ARTERY_PATH,
    VEIN_PATH
}

// Room Connection Data Structure
[System.Serializable]
public struct RoomConnection
{
    public int connectedRoomId;
    public ConnectionType type;

    public RoomConnection(int roomId, ConnectionType connectionType)
    {
        connectedRoomId = roomId;
        type = connectionType;
    }
}

// Room Data Structure
[System.Serializable]
public class Room
{
    public int uniqueId;
    public Vector2 worldPosition;
    public RoomType type;
    public Vector2Int actualSize;
    public List<Vector2Int> relativeShapeTiles;
    public List<RoomConnection> connections;

    public Room()
    {
        relativeShapeTiles = new List<Vector2Int>();
        connections = new List<RoomConnection>();
    }

    public Bounds GetBounds()
    {
        return new Bounds(worldPosition, new Vector3(actualSize.x, actualSize.y, 0));
    }
}

// Map Generation Parameters
[System.Serializable]
public class MapParameters
{
    [Header("Main Artery Control")]
    public int numMainArteryRooms = 5;
    public float mainRoomSpacing = 8f;
    [Range(0f, 1f)] public float chanceForLTurn = 0.3f;
    public float mainArteryPositionJitter = 2f;

    [Header("Distributive Node & Leaf Control")]
    [Range(0f, 1f)] public float distributiveNodeChancePerSegment = 0.4f;
    public int minLeafNodesPerDistributive = 1;
    public int maxLeafNodesPerDistributive = 3;
    public float leafBranchLength = 6f;
    public float leafNodePositionJitter = 1.5f;
    [Range(0f, 1f)] public float rareRoomChanceOnConnectiveAndCorner = 0.1f;

    [Header("Room Size & Shape Control")]
    public Vector2Int baseMainArteryRoomSize = new Vector2Int(6, 6);
    public Vector2Int baseDistributiveRoomSize = new Vector2Int(4, 4);
    public Vector2Int baseLeafRoomSize = new Vector2Int(5, 5);
    public Vector2Int baseCornerRoomSize = new Vector2Int(3, 3);
    [Range(0f, 0.5f)] public float roomSizeVariationPercentage = 0.2f;

    [Header("Collision & Placement Control")]
    public float minRoomDistance = 2f;
    public int maxRepositionAttempts = 10;
    public float repositionSearchRadius = 3f;

    [Header("Corridor and Wall Settings")]
    public int corridorWidth = 2;
    public int wallThickness = 1;
}

// Map Data Structure
public class MapData
{
    public Dictionary<int, Room> rooms;
    public HashSet<Vector2Int> occupiedTiles;
    public HashSet<Vector2Int> corridorTiles;
    public HashSet<Vector2Int> wallTiles;

    public MapData()
    {
        rooms = new Dictionary<int, Room>();
        occupiedTiles = new HashSet<Vector2Int>();
        corridorTiles = new HashSet<Vector2Int>();
        wallTiles = new HashSet<Vector2Int>();
    }
}

public class DungeonMapGenerator : MonoBehaviour
{
    [SerializeField] private MapParameters parameters;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool showGizmos = true;

    private MapData currentMapData;
    private System.Random rng;

    [Header("Tilemap and Tileset Settings")]
    [SerializeField] private Tilemap tilemap;                 // Reference to Unity Tilemap
    [SerializeField] private TileBase mainRoomTile;           // For MAIN_ARTERY_ROOM
    [SerializeField] private TileBase distributiveRoomTile;   // For DISTRIBUTIVE_NODE_ROOM
    [SerializeField] private TileBase leafRoomTile;           // For LEAF_NODE_ROOM
    [SerializeField] private TileBase cornerRoomTile;         // For ARTERY_CORNER_ROOM
    [SerializeField] private TileBase corridorTile;           // For corridors
    [SerializeField] private TileBase wallTile;               // For walls

    void Start()
    {
        if (generateOnStart)
        {
            GenerateNewMap();
        }
    }

    [ContextMenu("Generate New Map")]
    public void GenerateNewMap()
    {
        rng = new System.Random();
        currentMapData = GenerateDungeon(parameters);
        Debug.Log($"Generated dungeon with {currentMapData.rooms.Count} rooms");
        PaintTiles(currentMapData); // ? Add tile painting step
    }

    public MapData GenerateDungeon(MapParameters param)
    {
        var mapData = new MapData();
        var mainPathIds = new List<int>();
        int nextRoomId = 0;

        GenerateMainArtery(param, mapData.rooms, mainPathIds, mapData.occupiedTiles, ref nextRoomId);
        InsertDistributiveNodesAndSproutLeaves(param, mapData.rooms, mainPathIds, mapData.occupiedTiles, ref nextRoomId);
        FinalizeRoomShapesAndSpreading(param, mapData.rooms, mapData.occupiedTiles, ref nextRoomId);
        GenerateCorridorTiles(param, mapData.rooms, mapData.corridorTiles);
        GenerateWalls(param, mapData.rooms, mapData.corridorTiles, mapData.occupiedTiles, mapData.wallTiles);

        return mapData;
    }

    private void PaintTiles(MapData mapData)
    {
        if (tilemap == null) return;

        tilemap.ClearAllTiles();

        foreach (var room in mapData.rooms.Values)
        {
            TileBase tileToUse = mainRoomTile;
            switch (room.type)
            {
                case RoomType.MAIN_ARTERY_ROOM: tileToUse = mainRoomTile; break;
                case RoomType.DISTRIBUTIVE_NODE_ROOM: tileToUse = distributiveRoomTile; break;
                case RoomType.LEAF_NODE_ROOM: tileToUse = leafRoomTile; break;
                case RoomType.ARTERY_CORNER_ROOM: tileToUse = cornerRoomTile; break;
            }

            foreach (var localTile in room.relativeShapeTiles)
            {
                Vector2Int worldTile = Vector2Int.RoundToInt(room.worldPosition) + localTile;
                tilemap.SetTile((Vector3Int)worldTile, tileToUse);
            }
        }

        foreach (var corridorPos in mapData.corridorTiles)
        {
            tilemap.SetTile((Vector3Int)corridorPos, corridorTile);
        }

        foreach (var wallPos in mapData.wallTiles)
        {
            tilemap.SetTile((Vector3Int)wallPos, wallTile);
        }
    }
    private Vector2Int GetRoomSizeForType(RoomType roomType, MapParameters param)
    {
        Vector2Int baseSize;
        switch (roomType)
        {
            case RoomType.MAIN_ARTERY_ROOM: baseSize = param.baseMainArteryRoomSize; break;
            case RoomType.DISTRIBUTIVE_NODE_ROOM: baseSize = param.baseDistributiveRoomSize; break;
            case RoomType.LEAF_NODE_ROOM: baseSize = param.baseLeafRoomSize; break;
            case RoomType.ARTERY_CORNER_ROOM: baseSize = param.baseCornerRoomSize; break;
            default: baseSize = new Vector2Int(3, 3); break;
        }

        int widthVariation = Mathf.RoundToInt(baseSize.x * param.roomSizeVariationPercentage * RandomRange(-1f, 1f));
        int heightVariation = Mathf.RoundToInt(baseSize.y * param.roomSizeVariationPercentage * RandomRange(-1f, 1f));

        return new Vector2Int(
            Mathf.Max(2, baseSize.x + widthVariation),
            Mathf.Max(2, baseSize.y + heightVariation)
        );
    }

    private List<Vector2Int> GenerateRoomShapeTiles(RoomType roomType, Vector2Int actualRoomSize)
    {
        var shapeTiles = new List<Vector2Int>();

        // Default rectangular shape
        for (int x = 0; x < actualRoomSize.x; x++)
        {
            for (int y = 0; y < actualRoomSize.y; y++)
            {
                shapeTiles.Add(new Vector2Int(x, y));
            }
        }

        return shapeTiles;
    }

    private Room CreateRoom(Vector2 position, RoomType type, MapParameters param,
                           Dictionary<int, Room> rooms, HashSet<Vector2Int> occupiedTiles,
                           ref int nextRoomId, bool enforceCollision = true)
    {
        var actualSize = GetRoomSizeForType(type, param);
        var finalPosition = position;

        if (enforceCollision)
        {
            finalPosition = FindValidRoomPosition(position, actualSize, param, rooms);
        }

        var newRoom = new Room
        {
            uniqueId = nextRoomId++,
            worldPosition = finalPosition,
            type = type,
            actualSize = actualSize,
            relativeShapeTiles = GenerateRoomShapeTiles(type, actualSize)
        };

        rooms[newRoom.uniqueId] = newRoom;
        MarkRoomTilesAsOccupied(newRoom, occupiedTiles);

        return newRoom;
    }

    private Vector2 FindValidRoomPosition(Vector2 desiredPosition, Vector2Int roomActualSize,
                                         MapParameters param, Dictionary<int, Room> rooms,
                                         int excludeRoomId = -1)
    {
        if (IsPositionValidForRoom(desiredPosition, roomActualSize, param.minRoomDistance, rooms, excludeRoomId))
        {
            return desiredPosition;
        }

        for (int attempt = 1; attempt <= param.maxRepositionAttempts; attempt++)
        {
            float radius = param.repositionSearchRadius * attempt;
            for (int angleStep = 0; angleStep < 8; angleStep++)
            {
                float angle = angleStep * Mathf.PI * 2 / 8;
                Vector2 testPosition = desiredPosition + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                if (IsPositionValidForRoom(testPosition, roomActualSize, param.minRoomDistance, rooms, excludeRoomId))
                {
                    return testPosition;
                }
            }
        }

        return desiredPosition; // Fallback
    }

    private bool IsPositionValidForRoom(Vector2 testPosition, Vector2Int testActualSize,
                                       float minClearanceDistance, Dictionary<int, Room> rooms,
                                       int excludeRoomId = -1)
    {
        var newRoomBounds = new Bounds(testPosition, new Vector3(testActualSize.x + minClearanceDistance * 2,
                                                               testActualSize.y + minClearanceDistance * 2, 0));

        foreach (var existingRoom in rooms.Values)
        {
            if (existingRoom.uniqueId == excludeRoomId) continue;

            var existingBounds = new Bounds(existingRoom.worldPosition,
                                          new Vector3(existingRoom.actualSize.x + minClearanceDistance * 2,
                                                    existingRoom.actualSize.y + minClearanceDistance * 2, 0));

            if (newRoomBounds.Intersects(existingBounds))
            {
                return false;
            }
        }

        return true;
    }

    private void MarkRoomTilesAsOccupied(Room room, HashSet<Vector2Int> occupiedTiles)
    {
        int roomMinTileX = Mathf.RoundToInt(room.worldPosition.x - room.actualSize.x / 2f);
        int roomMinTileY = Mathf.RoundToInt(room.worldPosition.y - room.actualSize.y / 2f);

        foreach (var relativeTile in room.relativeShapeTiles)
        {
            occupiedTiles.Add(new Vector2Int(roomMinTileX + relativeTile.x, roomMinTileY + relativeTile.y));
        }
    }

    private void ConnectRooms(int room1Id, int room2Id, ConnectionType connectionType, Dictionary<int, Room> rooms)
    {
        if (rooms.ContainsKey(room1Id) && rooms.ContainsKey(room2Id))
        {
            var connection1 = new RoomConnection(room2Id, connectionType);
            var connection2 = new RoomConnection(room1Id, connectionType);

            if (!rooms[room1Id].connections.Any(c => c.connectedRoomId == room2Id))
                rooms[room1Id].connections.Add(connection1);

            if (!rooms[room2Id].connections.Any(c => c.connectedRoomId == room1Id))
                rooms[room2Id].connections.Add(connection2);
        }
    }

    private void GenerateMainArtery(MapParameters param, Dictionary<int, Room> rooms,
                                   List<int> mainPathIds, HashSet<Vector2Int> occupiedTiles,
                                   ref int nextRoomId)
    {
        Vector2 currentPos = Vector2.zero;
        Vector2 currentDirection = Vector2.right;

        // Create first main artery room
        var firstRoom = CreateRoom(currentPos, RoomType.MAIN_ARTERY_ROOM, param, rooms, occupiedTiles, ref nextRoomId);
        mainPathIds.Add(firstRoom.uniqueId);

        for (int i = 1; i < param.numMainArteryRooms; i++)
        {
            // Decide if this should be an L-turn
            bool isLTurn = RandomValue() < param.chanceForLTurn;

            if (isLTurn && i < param.numMainArteryRooms - 1) // Don't do L-turn on last room
            {
                // Create corner room
                currentPos += currentDirection * param.mainRoomSpacing;
                currentPos += RandomJitterVector(param.mainArteryPositionJitter);

                var cornerRoom = CreateRoom(currentPos, RoomType.ARTERY_CORNER_ROOM, param, rooms, occupiedTiles, ref nextRoomId);
                ConnectRooms(mainPathIds.Last(), cornerRoom.uniqueId, ConnectionType.ARTERY_PATH, rooms);
                mainPathIds.Add(cornerRoom.uniqueId);

                // Change direction (90 degree turn)
                currentDirection = GetRandomPerpendicularDirection(currentDirection);
            }

            // Create next main artery room
            currentPos += currentDirection * param.mainRoomSpacing;
            currentPos += RandomJitterVector(param.mainArteryPositionJitter);

            var mainRoom = CreateRoom(currentPos, RoomType.MAIN_ARTERY_ROOM, param, rooms, occupiedTiles, ref nextRoomId);
            ConnectRooms(mainPathIds.Last(), mainRoom.uniqueId, ConnectionType.ARTERY_PATH, rooms);
            mainPathIds.Add(mainRoom.uniqueId);
        }
    }

    private void InsertDistributiveNodesAndSproutLeaves(MapParameters param, Dictionary<int, Room> rooms,
                                                       List<int> mainPathIds, HashSet<Vector2Int> occupiedTiles,
                                                       ref int nextRoomId)
    {
        var newMainPath = new List<int>(mainPathIds);

        for (int i = 0; i < mainPathIds.Count - 1; i++)
        {
            if (RandomValue() < param.distributiveNodeChancePerSegment)
            {
                var room1 = rooms[mainPathIds[i]];
                var room2 = rooms[mainPathIds[i + 1]];

                // Create distributive node between the two rooms
                Vector2 midPos = Vector2.Lerp(room1.worldPosition, room2.worldPosition, 0.5f);
                var distributiveRoom = CreateRoom(midPos, RoomType.DISTRIBUTIVE_NODE_ROOM, param, rooms, occupiedTiles, ref nextRoomId);

                // Reconnect through distributive node
                room1.connections.RemoveAll(c => c.connectedRoomId == room2.uniqueId);
                room2.connections.RemoveAll(c => c.connectedRoomId == room1.uniqueId);

                ConnectRooms(room1.uniqueId, distributiveRoom.uniqueId, ConnectionType.ARTERY_PATH, rooms);
                ConnectRooms(distributiveRoom.uniqueId, room2.uniqueId, ConnectionType.ARTERY_PATH, rooms);

                // Sprout leaf nodes
                int leafCount = RandomRangeInt(param.minLeafNodesPerDistributive, param.maxLeafNodesPerDistributive);
                for (int j = 0; j < leafCount; j++)
                {
                    float angle = j * (2 * Mathf.PI / leafCount) + RandomRange(0, Mathf.PI / 4);
                    SproutLeafNode(distributiveRoom, angle, param, rooms, occupiedTiles, ref nextRoomId);
                }
            }
        }
    }

    private void SproutLeafNode(Room fromNode, float initialAngle, MapParameters param,
                               Dictionary<int, Room> rooms, HashSet<Vector2Int> occupiedTiles,
                               ref int nextRoomId)
    {
        Vector2 direction = new Vector2(Mathf.Cos(initialAngle), Mathf.Sin(initialAngle));
        Vector2 leafPos = fromNode.worldPosition + direction * param.leafBranchLength;
        leafPos += RandomJitterVector(param.leafNodePositionJitter);

        var leafRoom = CreateRoom(leafPos, RoomType.LEAF_NODE_ROOM, param, rooms, occupiedTiles, ref nextRoomId);
        ConnectRooms(fromNode.uniqueId, leafRoom.uniqueId, ConnectionType.VEIN_PATH, rooms);
    }

    private void FinalizeRoomShapesAndSpreading(MapParameters param, Dictionary<int, Room> rooms,
                                               HashSet<Vector2Int> occupiedTiles, ref int nextRoomId)
    {
        foreach (var room in rooms.Values.ToList())
        {
            if ((room.type == RoomType.ARTERY_CORNER_ROOM || room.type == RoomType.DISTRIBUTIVE_NODE_ROOM) &&
                RandomValue() < param.rareRoomChanceOnConnectiveAndCorner)
            {
                // Make rare rooms potentially larger
                var newSize = GetRoomSizeForType(room.type, param);
                newSize.x = Mathf.RoundToInt(newSize.x * 1.5f);
                newSize.y = Mathf.RoundToInt(newSize.y * 1.5f);

                // Remove old tiles and add new ones
                RemoveRoomFromOccupiedTiles(room, occupiedTiles);
                room.actualSize = newSize;
                room.relativeShapeTiles = GenerateRoomShapeTiles(room.type, newSize);
                MarkRoomTilesAsOccupied(room, occupiedTiles);
            }
        }
    }

    private void RemoveRoomFromOccupiedTiles(Room room, HashSet<Vector2Int> occupiedTiles)
    {
        int roomMinTileX = Mathf.RoundToInt(room.worldPosition.x - room.actualSize.x / 2f);
        int roomMinTileY = Mathf.RoundToInt(room.worldPosition.y - room.actualSize.y / 2f);

        foreach (var relativeTile in room.relativeShapeTiles)
        {
            occupiedTiles.Remove(new Vector2Int(roomMinTileX + relativeTile.x, roomMinTileY + relativeTile.y));
        }
    }

    private void GenerateCorridorTiles(MapParameters param, Dictionary<int, Room> rooms, HashSet<Vector2Int> corridorTiles)
    {
        corridorTiles.Clear();
        var processedConnections = new HashSet<string>();

        foreach (var room in rooms.Values)
        {
            foreach (var connection in room.connections)
            {
                string connectionKey = $"{Mathf.Min(room.uniqueId, connection.connectedRoomId)}-{Mathf.Max(room.uniqueId, connection.connectedRoomId)}";
                if (processedConnections.Contains(connectionKey)) continue;
                processedConnections.Add(connectionKey);

                var connectedRoom = rooms[connection.connectedRoomId];
                var pathTiles = GetThickLineTiles(room.worldPosition, connectedRoom.worldPosition, param.corridorWidth);

                foreach (var tile in pathTiles)
                {
                    corridorTiles.Add(tile);
                }
            }
        }
    }

    private List<Vector2Int> GetThickLineTiles(Vector2 start, Vector2 end, int thickness)
    {
        var tiles = new List<Vector2Int>();
        var startInt = new Vector2Int(Mathf.RoundToInt(start.x), Mathf.RoundToInt(start.y));
        var endInt = new Vector2Int(Mathf.RoundToInt(end.x), Mathf.RoundToInt(end.y));

        // Simple line drawing with thickness
        var lineTiles = GetLineTiles(startInt, endInt);

        foreach (var tile in lineTiles)
        {
            for (int x = -thickness / 2; x <= thickness / 2; x++)
            {
                for (int y = -thickness / 2; y <= thickness / 2; y++)
                {
                    tiles.Add(new Vector2Int(tile.x + x, tile.y + y));
                }
            }
        }

        return tiles;
    }

    private List<Vector2Int> GetLineTiles(Vector2Int start, Vector2Int end)
    {
        var tiles = new List<Vector2Int>();
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        int x = start.x;
        int y = start.y;

        int x_inc = (end.x > start.x) ? 1 : -1;
        int y_inc = (end.y > start.y) ? 1 : -1;
        int error = dx - dy;

        for (int n = dx + dy; n > 0; n--)
        {
            tiles.Add(new Vector2Int(x, y));

            if (error > 0)
            {
                x += x_inc;
                error -= dy;
            }
            else
            {
                y += y_inc;
                error += dx;
            }
        }

        return tiles;
    }

    private void GenerateWalls(MapParameters param, Dictionary<int, Room> rooms,
                              HashSet<Vector2Int> corridorTiles, HashSet<Vector2Int> occupiedRoomTiles,
                              HashSet<Vector2Int> wallTiles)
    {
        wallTiles.Clear();

        // Generate walls around rooms
        foreach (var room in rooms.Values)
        {
            AddWallsAroundRoom(room, param.wallThickness, wallTiles);
        }

        // Generate walls around corridors
        foreach (var corridorTile in corridorTiles)
        {
            AddWallsAroundTile(corridorTile, param.wallThickness, wallTiles);
        }

        // Remove walls that overlap with rooms or corridors
        wallTiles.ExceptWith(occupiedRoomTiles);
        wallTiles.ExceptWith(corridorTiles);
    }

    private void AddWallsAroundRoom(Room room, int thickness, HashSet<Vector2Int> wallTiles)
    {
        int minX = Mathf.RoundToInt(room.worldPosition.x - room.actualSize.x / 2f) - thickness;
        int maxX = Mathf.RoundToInt(room.worldPosition.x + room.actualSize.x / 2f) + thickness;
        int minY = Mathf.RoundToInt(room.worldPosition.y - room.actualSize.y / 2f) - thickness;
        int maxY = Mathf.RoundToInt(room.worldPosition.y + room.actualSize.y / 2f) + thickness;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (x == minX || x == maxX || y == minY || y == maxY)
                {
                    wallTiles.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    private void AddWallsAroundTile(Vector2Int tile, int thickness, HashSet<Vector2Int> wallTiles)
    {
        for (int x = tile.x - thickness; x <= tile.x + thickness; x++)
        {
            for (int y = tile.y - thickness; y <= tile.y + thickness; y++)
            {
                if (Mathf.Abs(x - tile.x) == thickness || Mathf.Abs(y - tile.y) == thickness)
                {
                    wallTiles.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    // Utility Functions
    private float RandomValue() => (float)rng.NextDouble();
    private float RandomRange(float min, float max) => min + (float)rng.NextDouble() * (max - min);
    private int RandomRangeInt(int min, int max) => rng.Next(min, max + 1);

    private Vector2 RandomJitterVector(float maxJitter)
    {
        return new Vector2(RandomRange(-maxJitter, maxJitter), RandomRange(-maxJitter, maxJitter));
    }

    private Vector2 GetRandomPerpendicularDirection(Vector2 current)
    {
        return RandomValue() < 0.5f ? new Vector2(-current.y, current.x) : new Vector2(current.y, -current.x);
    }

    // Gizmos for visualization
    void OnDrawGizmos()
    {
        if (!showGizmos || currentMapData == null) return;

        // Draw rooms
        foreach (var room in currentMapData.rooms.Values)
        {
            Color roomColor = room.type switch
            {
                RoomType.MAIN_ARTERY_ROOM => Color.red,
                RoomType.DISTRIBUTIVE_NODE_ROOM => Color.blue,
                RoomType.LEAF_NODE_ROOM => Color.green,
                RoomType.ARTERY_CORNER_ROOM => Color.yellow,
                _ => Color.white
            };

            Gizmos.color = roomColor;
            Gizmos.DrawWireCube(new Vector3(room.worldPosition.x, room.worldPosition.y, 0),
                               new Vector3(room.actualSize.x, room.actualSize.y, 1));
        }

        // Draw connections
        Gizmos.color = Color.white;
        var drawnConnections = new HashSet<string>();

        foreach (var room in currentMapData.rooms.Values)
        {
            foreach (var connection in room.connections)
            {
                string connectionKey = $"{Mathf.Min(room.uniqueId, connection.connectedRoomId)}-{Mathf.Max(room.uniqueId, connection.connectedRoomId)}";
                if (drawnConnections.Contains(connectionKey)) continue;
                drawnConnections.Add(connectionKey);

                var connectedRoom = currentMapData.rooms[connection.connectedRoomId];
                Gizmos.color = connection.type == ConnectionType.ARTERY_PATH ? Color.red : Color.green;
                Gizmos.DrawLine(new Vector3(room.worldPosition.x, room.worldPosition.y, 0),
                               new Vector3(connectedRoom.worldPosition.x, connectedRoom.worldPosition.y, 0));
            }
        }

        // Draw corridor tiles
        Gizmos.color = Color.cyan;
        foreach (var tile in currentMapData.corridorTiles)
        {
            Gizmos.DrawWireCube(new Vector3(tile.x, tile.y, 0), Vector3.one * 0.8f);
        }

        // Draw wall tiles
        Gizmos.color = Color.black;
        foreach (var tile in currentMapData.wallTiles)
        {
            Gizmos.DrawCube(new Vector3(tile.x, tile.y, 0), Vector3.one * 0.6f);
        }
    }

    // Public access to map data
    public MapData GetCurrentMapData() => currentMapData;
    public MapParameters GetParameters() => parameters;
}