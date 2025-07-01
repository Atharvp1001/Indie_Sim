using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Tilemaps;

public enum RoomType
{
    MAIN_ARTERY_ROOM,
    DISTRIBUTIVE_NODE_ROOM,
    LEAF_NODE_ROOM,
    ARTERY_CORNER_ROOM
}

public enum ConnectionType
{
    ARTERY_PATH,
    VEIN_PATH
}

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

[System.Serializable]
public class Room
{
    public int uniqueId;
    public Vector2 worldPosition;
    public RoomType type;
    public Vector2Int size;
    public List<RoomConnection> connections;

    public Room()
    {
        connections = new List<RoomConnection>();
    }

    public Bounds GetBounds()
    {
        return new Bounds(worldPosition, new Vector3(size.x, size.y, 0));
    }
}

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

    [Header("Room Size Control")]
    public Vector2Int baseMainArteryRoomSize = new Vector2Int(6, 6);
    public Vector2Int baseDistributiveRoomSize = new Vector2Int(4, 4);
    public Vector2Int baseLeafRoomSize = new Vector2Int(5, 5);
    public Vector2Int baseCornerRoomSize = new Vector2Int(3, 3);
    [Range(0f, 0.5f)] public float roomSizeVariationPercentage = 0.2f;

    [Header("Collision & Placement Control")]
    public float minRoomDistance = 2f;
    public int maxRepositionAttempts = 10;
    public float repositionSearchRadius = 3f;

    [Header("Corridor Settings")]
    public int corridorWidth = 2;
}

public class MapData
{
    public Dictionary<int, Room> rooms;
    public HashSet<Vector2Int> floorTiles;
    public HashSet<Vector2Int> wallTiles;

    public MapData()
    {
        rooms = new Dictionary<int, Room>();
        floorTiles = new HashSet<Vector2Int>();
        wallTiles = new HashSet<Vector2Int>();
    }
}

public class DungeonMapGenerator : MonoBehaviour
{
    [SerializeField] private MapParameters parameters;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] public bool showGizmos = true;

    private MapData currentMapData;
    private System.Random rng;

    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;

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
        Debug.Log($"Floor tiles: {currentMapData.floorTiles.Count}, Wall tiles: {currentMapData.wallTiles.Count}");
        PaintTiles(currentMapData);
    }

    public MapData GenerateDungeon(MapParameters param)
    {
        var mapData = new MapData();
        var mainPathIds = new List<int>();
        int nextRoomId = 0;

        // Generate the room network
        GenerateMainArtery(param, mapData.rooms, mainPathIds, ref nextRoomId);
        InsertDistributiveNodesAndSproutLeaves(param, mapData.rooms, mainPathIds, ref nextRoomId);
        
        // Generate floor tiles (rooms + corridors)
        GenerateFloorTiles(param, mapData.rooms, mapData.floorTiles);
        
        // Generate walls around all floor areas
        GenerateWallTiles(mapData.floorTiles, mapData.wallTiles);

        return mapData;
    }

    private void PaintTiles(MapData mapData)
    {
        if (floorTilemap == null || wallTilemap == null)
        {
            Debug.LogError("Tilemaps not assigned!");
            return;
        }

        if (floorTile == null || wallTile == null)
        {
            Debug.LogError("Tiles not assigned!");
            return;
        }

        // Clear both tilemaps
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        Debug.Log($"Painting {mapData.floorTiles.Count} floor tiles and {mapData.wallTiles.Count} wall tiles");

        // Paint floor tiles
        foreach (var floorPos in mapData.floorTiles)
        {
            floorTilemap.SetTile((Vector3Int)floorPos, floorTile);
        }

        // Paint wall tiles
        foreach (var wallPos in mapData.wallTiles)
        {
            wallTilemap.SetTile((Vector3Int)wallPos, wallTile);
        }

        Debug.Log("Tile painting complete");
    }

    private Vector2Int GetRoomSizeForType(RoomType roomType, MapParameters param)
    {
        Vector2Int baseSize = roomType switch
        {
            RoomType.MAIN_ARTERY_ROOM => param.baseMainArteryRoomSize,
            RoomType.DISTRIBUTIVE_NODE_ROOM => param.baseDistributiveRoomSize,
            RoomType.LEAF_NODE_ROOM => param.baseLeafRoomSize,
            RoomType.ARTERY_CORNER_ROOM => param.baseCornerRoomSize,
            _ => new Vector2Int(3, 3)
        };

        int widthVariation = Mathf.RoundToInt(baseSize.x * param.roomSizeVariationPercentage * RandomRange(-1f, 1f));
        int heightVariation = Mathf.RoundToInt(baseSize.y * param.roomSizeVariationPercentage * RandomRange(-1f, 1f));

        return new Vector2Int(
            Mathf.Max(2, baseSize.x + widthVariation),
            Mathf.Max(2, baseSize.y + heightVariation)
        );
    }

    private Room CreateRoom(Vector2 position, RoomType type, MapParameters param, Dictionary<int, Room> rooms, ref int nextRoomId)
    {
        var size = GetRoomSizeForType(type, param);
        var finalPosition = FindValidRoomPosition(position, size, param, rooms);

        var newRoom = new Room
        {
            uniqueId = nextRoomId++,
            worldPosition = finalPosition,
            type = type,
            size = size
        };

        rooms[newRoom.uniqueId] = newRoom;
        return newRoom;
    }

    private Vector2 FindValidRoomPosition(Vector2 desiredPosition, Vector2Int roomSize, MapParameters param, Dictionary<int, Room> rooms)
    {
        if (IsPositionValidForRoom(desiredPosition, roomSize, param.minRoomDistance, rooms))
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

                if (IsPositionValidForRoom(testPosition, roomSize, param.minRoomDistance, rooms))
                {
                    return testPosition;
                }
            }
        }

        return desiredPosition;
    }

    private bool IsPositionValidForRoom(Vector2 testPosition, Vector2Int testSize, float minDistance, Dictionary<int, Room> rooms)
    {
        var newRoomBounds = new Bounds(testPosition, new Vector3(testSize.x + minDistance * 2, testSize.y + minDistance * 2, 0));

        foreach (var existingRoom in rooms.Values)
        {
            var existingBounds = new Bounds(existingRoom.worldPosition, 
                new Vector3(existingRoom.size.x + minDistance * 2, existingRoom.size.y + minDistance * 2, 0));

            if (newRoomBounds.Intersects(existingBounds))
            {
                return false;
            }
        }

        return true;
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

    private void GenerateMainArtery(MapParameters param, Dictionary<int, Room> rooms, List<int> mainPathIds, ref int nextRoomId)
    {
        Vector2 currentPos = Vector2.zero;
        Vector2 currentDirection = Vector2.right;

        var firstRoom = CreateRoom(currentPos, RoomType.MAIN_ARTERY_ROOM, param, rooms, ref nextRoomId);
        mainPathIds.Add(firstRoom.uniqueId);

        for (int i = 1; i < param.numMainArteryRooms; i++)
        {
            bool isLTurn = RandomValue() < param.chanceForLTurn;

            if (isLTurn && i < param.numMainArteryRooms - 1)
            {
                currentPos += currentDirection * param.mainRoomSpacing;
                currentPos += RandomJitterVector(param.mainArteryPositionJitter);

                var cornerRoom = CreateRoom(currentPos, RoomType.ARTERY_CORNER_ROOM, param, rooms, ref nextRoomId);
                ConnectRooms(mainPathIds.Last(), cornerRoom.uniqueId, ConnectionType.ARTERY_PATH, rooms);
                mainPathIds.Add(cornerRoom.uniqueId);

                currentDirection = GetRandomPerpendicularDirection(currentDirection);
            }

            currentPos += currentDirection * param.mainRoomSpacing;
            currentPos += RandomJitterVector(param.mainArteryPositionJitter);

            var mainRoom = CreateRoom(currentPos, RoomType.MAIN_ARTERY_ROOM, param, rooms, ref nextRoomId);
            ConnectRooms(mainPathIds.Last(), mainRoom.uniqueId, ConnectionType.ARTERY_PATH, rooms);
            mainPathIds.Add(mainRoom.uniqueId);
        }
    }

    private void InsertDistributiveNodesAndSproutLeaves(MapParameters param, Dictionary<int, Room> rooms, List<int> mainPathIds, ref int nextRoomId)
    {
        for (int i = 0; i < mainPathIds.Count - 1; i++)
        {
            if (RandomValue() < param.distributiveNodeChancePerSegment)
            {
                var room1 = rooms[mainPathIds[i]];
                var room2 = rooms[mainPathIds[i + 1]];

                Vector2 midPos = Vector2.Lerp(room1.worldPosition, room2.worldPosition, 0.5f);
                var distributiveRoom = CreateRoom(midPos, RoomType.DISTRIBUTIVE_NODE_ROOM, param, rooms, ref nextRoomId);

                room1.connections.RemoveAll(c => c.connectedRoomId == room2.uniqueId);
                room2.connections.RemoveAll(c => c.connectedRoomId == room1.uniqueId);

                ConnectRooms(room1.uniqueId, distributiveRoom.uniqueId, ConnectionType.ARTERY_PATH, rooms);
                ConnectRooms(distributiveRoom.uniqueId, room2.uniqueId, ConnectionType.ARTERY_PATH, rooms);

                int leafCount = RandomRangeInt(param.minLeafNodesPerDistributive, param.maxLeafNodesPerDistributive);
                for (int j = 0; j < leafCount; j++)
                {
                    float angle = j * (2 * Mathf.PI / leafCount) + RandomRange(0, Mathf.PI / 4);
                    SproutLeafNode(distributiveRoom, angle, param, rooms, ref nextRoomId);
                }
            }
        }
    }

    private void SproutLeafNode(Room fromNode, float initialAngle, MapParameters param, Dictionary<int, Room> rooms, ref int nextRoomId)
    {
        Vector2 direction = new Vector2(Mathf.Cos(initialAngle), Mathf.Sin(initialAngle));
        Vector2 leafPos = fromNode.worldPosition + direction * param.leafBranchLength;
        leafPos += RandomJitterVector(param.leafNodePositionJitter);

        var leafRoom = CreateRoom(leafPos, RoomType.LEAF_NODE_ROOM, param, rooms, ref nextRoomId);
        ConnectRooms(fromNode.uniqueId, leafRoom.uniqueId, ConnectionType.VEIN_PATH, rooms);
    }

    private void GenerateFloorTiles(MapParameters param, Dictionary<int, Room> rooms, HashSet<Vector2Int> floorTiles)
    {
        floorTiles.Clear();

        // Add all room tiles
        foreach (var room in rooms.Values)
        {
            AddRoomTiles(room, floorTiles);
        }

        // Add corridor tiles
        var processedConnections = new HashSet<string>();
        foreach (var room in rooms.Values)
        {
            foreach (var connection in room.connections)
            {
                string connectionKey = $"{Mathf.Min(room.uniqueId, connection.connectedRoomId)}-{Mathf.Max(room.uniqueId, connection.connectedRoomId)}";
                if (processedConnections.Contains(connectionKey)) continue;
                processedConnections.Add(connectionKey);

                var connectedRoom = rooms[connection.connectedRoomId];
                AddCorridorTiles(room.worldPosition, connectedRoom.worldPosition, param.corridorWidth, floorTiles);
            }
        }
    }

    private void AddRoomTiles(Room room, HashSet<Vector2Int> floorTiles)
    {
        int minX = Mathf.RoundToInt(room.worldPosition.x - room.size.x / 2f);
        int maxX = Mathf.RoundToInt(room.worldPosition.x + room.size.x / 2f);
        int minY = Mathf.RoundToInt(room.worldPosition.y - room.size.y / 2f);
        int maxY = Mathf.RoundToInt(room.worldPosition.y + room.size.y / 2f);

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                floorTiles.Add(new Vector2Int(x, y));
            }
        }
    }

    private void AddCorridorTiles(Vector2 start, Vector2 end, int width, HashSet<Vector2Int> floorTiles)
    {
        var startInt = new Vector2Int(Mathf.RoundToInt(start.x), Mathf.RoundToInt(start.y));
        var endInt = new Vector2Int(Mathf.RoundToInt(end.x), Mathf.RoundToInt(end.y));

        var lineTiles = GetLineTiles(startInt, endInt);

        foreach (var tile in lineTiles)
        {
            for (int x = -width / 2; x <= width / 2; x++)
            {
                for (int y = -width / 2; y <= width / 2; y++)
                {
                    floorTiles.Add(new Vector2Int(tile.x + x, tile.y + y));
                }
            }
        }
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

    private void GenerateWallTiles(HashSet<Vector2Int> floorTiles, HashSet<Vector2Int> wallTiles)
    {
        wallTiles.Clear();

        // For each floor tile, check its neighbors
        foreach (var floorTile in floorTiles)
        {
            // Check all 8 directions around each floor tile
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue; // Skip the center tile

                    Vector2Int neighborTile = new Vector2Int(floorTile.x + x, floorTile.y + y);
                    
                    // If this neighbor is not a floor tile, it should be a wall
                    if (!floorTiles.Contains(neighborTile))
                    {
                        wallTiles.Add(neighborTile);
                    }
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
                               new Vector3(room.size.x, room.size.y, 1));
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
    }

    public MapData GetCurrentMapData() => currentMapData;
    public MapParameters GetParameters() => parameters;
}