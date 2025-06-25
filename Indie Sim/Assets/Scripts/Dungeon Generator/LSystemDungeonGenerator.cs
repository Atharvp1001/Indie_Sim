using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

// --- Room Types --
public enum RoomType
{
    MainArtery,
    DistributiveNode,
    LeafNode,
    ArteryCorner
}

public enum ConnectionType
{
    Artery,
    Vein
}

[System.Serializable]
public class LSystemRoom
{
    public int id;
    public Vector2 position;
    public RoomType type;
    public Vector2Int size;
    public List<RoomConnection> connections;

    public LSystemRoom(int id, Vector2 position, RoomType type, Vector2Int size)
    {
        this.id = id;
        this.position = position;
        this.type = type;
        this.size = size;
        this.connections = new List<RoomConnection>();
    }

    public Bounds GetBounds()
    {
        return new Bounds(position, new Vector3(size.x, size.y, 0));
    }

    public override string ToString()
    {
        return $"Room(id={id}, pos=({position.x:F2}, {position.y:F2}), type={type}, size={size})";
    }
}

[System.Serializable]
public class RoomConnection
{
    public int connectedRoomId;
    public ConnectionType connectionType;

    public RoomConnection(int roomId, ConnectionType type)
    {
        connectedRoomId = roomId;
        connectionType = type;
    }
}

[System.Serializable]
public class LSystemDungeonParams
{
    [Header("Main Artery Settings")]
    public int numMainArteryRooms = 12;
    public float mainRoomSpacing = 25f;
    [Range(0f, 1f)] public float chanceForLTurn = 0.7f;
    public float posJitterMainArtery = 0.5f;

    [Header("Distributive Node Settings")]
    [Range(0f, 1f)] public float distributiveNodeChancePerSegment = 0.8f;
    public int minLeafNodesPerDistributive = 1;
    public int maxLeafNodesPerDistributive = 2;
    public float leafBranchLength = 12f;
    public float posJitterLeaf = 1.5f;

    [Header("Room Size Settings")]
    public Vector2Int mainArteryRoomSize = new Vector2Int(8, 8);
    public Vector2Int distributiveRoomSize = new Vector2Int(6, 6);
    public Vector2Int leafRoomSize = new Vector2Int(4, 4);
    public Vector2Int cornerRoomSize = new Vector2Int(3, 3);

    [Header("Room Size Variation")]
    [Range(0f, 0.5f)] public float roomSizeVariation = 0.2f;

    [Header("Collision Detection")]
    public float minRoomDistance = 2f;
    public int maxRepositionAttempts = 10;
    public float repositionRadius = 5f;
}

public class LSystemDungeonGenerator : MonoBehaviour
{
    [Header("Unity References")]
    public Tilemap tilemap;
    public TileBase roomTile;
    public TileBase corridorTile;
    public TileBase mainArteryTile;
    public TileBase leafTile;

    [Header("L-System Parameters")]
    public LSystemDungeonParams parameters;

    [Header("Debug")]
    public bool showDebugGizmos = true;
    public bool generateOnStart = true;

    private Dictionary<int, LSystemRoom> rooms = new Dictionary<int, LSystemRoom>();
    private List<int> mainArteryPathIds = new List<int>();
    private HashSet<Vector2Int> occupiedTiles = new HashSet<Vector2Int>();
    private List<(Vector2, Vector2)> corridorPaths = new List<(Vector2, Vector2)>();
    private int nextRoomId = 0;

    private readonly float[] cardinalDirections = { 0f, Mathf.PI / 2f, Mathf.PI, 3f * Mathf.PI / 2f };

    void Start()
    {
        if (generateOnStart)
        {
            GenerateDungeon();
        }
    }

    public void GenerateDungeon()
    {
        ClearDungeon();
        GenerateMap();
        RenderDungeon();
    }

    void ClearDungeon()
    {
        rooms.Clear();
        mainArteryPathIds.Clear();
        occupiedTiles.Clear();
        corridorPaths.Clear();
        nextRoomId = 0;

        if (tilemap != null)
        {
            BoundsInt bounds = tilemap.cellBounds;
            TileBase[] emptyTiles = new TileBase[bounds.size.x * bounds.size.y * bounds.size.z];
            tilemap.SetTilesBlock(bounds, emptyTiles);
        }
    }

    bool IsPositionValid(Vector2 position, Vector2Int size, int excludeRoomId = -1)
    {
        Bounds newRoomBounds = new Bounds(position, new Vector3(size.x + parameters.minRoomDistance, size.y + parameters.minRoomDistance, 0));

        // Check against all existing rooms
        foreach (var existingRoom in rooms.Values)
        {
            if (existingRoom.id == excludeRoomId) continue;

            Bounds existingBounds = existingRoom.GetBounds();
            existingBounds.Expand(parameters.minRoomDistance);

            if (newRoomBounds.Intersects(existingBounds))
            {
                return false;
            }
        }

        return true;
    }

    Vector2 FindValidPosition(Vector2 desiredPosition, Vector2Int size, int excludeRoomId = -1)
    {
        // First try the desired position
        if (IsPositionValid(desiredPosition, size, excludeRoomId))
        {
            return desiredPosition;
        }

        // Try positions in increasing radius around the desired position
        for (int attempt = 0; attempt < parameters.maxRepositionAttempts; attempt++)
        {
            float radius = parameters.repositionRadius * (attempt + 1);

            // Try multiple angles at this radius
            for (int angleStep = 0; angleStep < 8; angleStep++)
            {
                float angle = angleStep * Mathf.PI * 2f / 8f;
                Vector2 testPosition = desiredPosition + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );

                if (IsPositionValid(testPosition, size, excludeRoomId))
                {
                    return testPosition;
                }
            }
        }

        // If no valid position found, return the desired position anyway
        Debug.LogWarning($"Could not find valid position for room at {desiredPosition}, using original position");
        return desiredPosition;
    }

    bool DoesPathIntersectExistingPaths(Vector2 start, Vector2 end)
    {
        foreach (var existingPath in corridorPaths)
        {
            if (LinesIntersect(start, end, existingPath.Item1, existingPath.Item2))
            {
                return true;
            }
        }
        return false;
    }

    bool LinesIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        // Check if two line segments intersect using orientation method
        int o1 = Orientation(p1, q1, p2);
        int o2 = Orientation(p1, q1, q2);
        int o3 = Orientation(p2, q2, p1);
        int o4 = Orientation(p2, q2, q1);

        // General case
        if (o1 != o2 && o3 != o4)
            return true;

        // Special cases for collinear points
        if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
        if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
        if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
        if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

        return false;
    }

    int Orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
        if (Mathf.Abs(val) < 0.001f) return 0; // Collinear
        return (val > 0) ? 1 : 2; // Clockwise or Counterclockwise
    }

    bool OnSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        return q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
               q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y);
    }

    LSystemRoom CreateRoom(Vector2 position, RoomType type, bool enforceCollision = true)
    {
        Vector2Int roomSize = GetRoomSizeForType(type);

        if (enforceCollision)
        {
            position = FindValidPosition(position, roomSize);
        }

        LSystemRoom room = new LSystemRoom(nextRoomId, position, type, roomSize);
        rooms[nextRoomId] = room;

        // Mark tiles as occupied
        MarkRoomTilesAsOccupied(room);

        nextRoomId++;
        return room;
    }

    void MarkRoomTilesAsOccupied(LSystemRoom room)
    {
        Vector2Int startTile = new Vector2Int(
            Mathf.RoundToInt(room.position.x - room.size.x / 2.0f),
            Mathf.RoundToInt(room.position.y - room.size.y / 2.0f)
        );

        for (int x = 0; x < room.size.x; x++)
        {
            for (int y = 0; y < room.size.y; y++)
            {
                occupiedTiles.Add(new Vector2Int(startTile.x + x, startTile.y + y));
            }
        }
    }

    Vector2Int GetRoomSizeForType(RoomType type)
    {
        Vector2Int baseSize;
        switch (type)
        {
            case RoomType.MainArtery:
                baseSize = parameters.mainArteryRoomSize;
                break;
            case RoomType.DistributiveNode:
                baseSize = parameters.distributiveRoomSize;
                break;
            case RoomType.LeafNode:
                baseSize = parameters.leafRoomSize;
                break;
            case RoomType.ArteryCorner:
                baseSize = parameters.cornerRoomSize;
                break;
            default:
                baseSize = parameters.mainArteryRoomSize;
                break;
        }

        // Apply size variation - Fixed to ensure proper bounds
        float variation = parameters.roomSizeVariation;
        int widthVariation = Mathf.RoundToInt(baseSize.x * variation * Random.Range(-1f, 1f));
        int heightVariation = Mathf.RoundToInt(baseSize.y * variation * Random.Range(-1f, 1f));

        return new Vector2Int(
            Mathf.Max(2, baseSize.x + widthVariation),
            Mathf.Max(2, baseSize.y + heightVariation)
        );
    }

    void ConnectRooms(int room1Id, int room2Id, ConnectionType connectionType)
    {
        // Enhanced null checking
        if (!rooms.ContainsKey(room1Id) || !rooms.ContainsKey(room2Id))
        {
            Debug.LogError($"Error: Room ID {room1Id} or {room2Id} not found for connection.");
            return;
        }

        LSystemRoom room1 = rooms[room1Id];
        LSystemRoom room2 = rooms[room2Id];

        // Ensure connections lists are initialized
        if (room1.connections == null) room1.connections = new List<RoomConnection>();
        if (room2.connections == null) room2.connections = new List<RoomConnection>();

        // Check if connection already exists
        if (!room1.connections.Any(c => c.connectedRoomId == room2Id && c.connectionType == connectionType))
        {
            room1.connections.Add(new RoomConnection(room2Id, connectionType));
        }
        if (!room2.connections.Any(c => c.connectedRoomId == room1Id && c.connectionType == connectionType))
        {
            room2.connections.Add(new RoomConnection(room1Id, connectionType));
        }

        // Add corridor path for intersection checking
        corridorPaths.Add((room1.position, room2.position));
    }

    void RemoveConnection(int room1Id, int room2Id, ConnectionType connectionType)
    {
        if (rooms.ContainsKey(room1Id) && rooms[room1Id].connections != null)
        {
            rooms[room1Id].connections.RemoveAll(c => c.connectedRoomId == room2Id && c.connectionType == connectionType);
        }
        if (rooms.ContainsKey(room2Id) && rooms[room2Id].connections != null)
        {
            rooms[room2Id].connections.RemoveAll(c => c.connectedRoomId == room1Id && c.connectionType == connectionType);
        }

        // Remove from corridor paths
        Vector2 pos1 = rooms.ContainsKey(room1Id) ? rooms[room1Id].position : Vector2.zero;
        Vector2 pos2 = rooms.ContainsKey(room2Id) ? rooms[room2Id].position : Vector2.zero;
        corridorPaths.RemoveAll(path =>
            (path.Item1 == pos1 && path.Item2 == pos2) ||
            (path.Item1 == pos2 && path.Item2 == pos1));
    }

    void GenerateMainArtery()
    {
        Vector2 startPos = Vector2.zero;
        LSystemRoom currentRoom = CreateRoom(startPos, RoomType.MainArtery);
        mainArteryPathIds.Add(currentRoom.id);

        float currentDirectionAngle = cardinalDirections[Random.Range(0, cardinalDirections.Length)];

        for (int i = 0; i < parameters.numMainArteryRooms - 1; i++)
        {
            float segmentLength = parameters.mainRoomSpacing / 2f;

            List<LSystemRoom> pathNodesForThisStep = new List<LSystemRoom>();
            List<(int, int, ConnectionType)> connectionsForThisStep = new List<(int, int, ConnectionType)>();

            if (Random.value < parameters.chanceForLTurn)
            {
                // Create L-turn with corner room
                float dxLeg1 = segmentLength * Mathf.Cos(currentDirectionAngle);
                float dyLeg1 = segmentLength * Mathf.Sin(currentDirectionAngle);
                Vector2 cornerPos = new Vector2(currentRoom.position.x + dxLeg1, currentRoom.position.y + dyLeg1);
                LSystemRoom cornerRoom = CreateRoom(cornerPos, RoomType.ArteryCorner);
                pathNodesForThisStep.Add(cornerRoom);
                connectionsForThisStep.Add((currentRoom.id, cornerRoom.id, ConnectionType.Artery));

                // Turn 90 degrees
                float turnAngle = Random.value < 0.5f ? Mathf.PI / 2f : -Mathf.PI / 2f;
                currentDirectionAngle = NormalizeAngle(currentDirectionAngle + turnAngle);

                float dxLeg2 = segmentLength * Mathf.Cos(currentDirectionAngle);
                float dyLeg2 = segmentLength * Mathf.Sin(currentDirectionAngle);
                Vector2 nextMainPos = new Vector2(cornerRoom.position.x + dxLeg2, cornerRoom.position.y + dyLeg2);
                LSystemRoom nextMainRoom = CreateRoom(nextMainPos, RoomType.MainArtery);
                pathNodesForThisStep.Add(nextMainRoom);
                connectionsForThisStep.Add((cornerRoom.id, nextMainRoom.id, ConnectionType.Artery));

                currentRoom = nextMainRoom;
            }
            else
            {
                // Straight line
                float dx = parameters.mainRoomSpacing * Mathf.Cos(currentDirectionAngle);
                float dy = parameters.mainRoomSpacing * Mathf.Sin(currentDirectionAngle);
                Vector2 nextMainPos = new Vector2(currentRoom.position.x + dx, currentRoom.position.y + dy);
                LSystemRoom nextMainRoom = CreateRoom(nextMainPos, RoomType.MainArtery);
                pathNodesForThisStep.Add(nextMainRoom);
                connectionsForThisStep.Add((currentRoom.id, nextMainRoom.id, ConnectionType.Artery));

                currentRoom = nextMainRoom;
            }

            // Apply position jitter with collision checking
            foreach (var node in pathNodesForThisStep)
            {
                Vector2 jitteredPos = node.position + new Vector2(
                    Random.Range(-parameters.posJitterMainArtery, parameters.posJitterMainArtery),
                    Random.Range(-parameters.posJitterMainArtery, parameters.posJitterMainArtery)
                );

                // Check if jittered position is valid
                if (IsPositionValid(jitteredPos, node.size, node.id))
                {
                    // Update occupied tiles
                    RemoveRoomFromOccupiedTiles(node);
                    node.position = jitteredPos;
                    MarkRoomTilesAsOccupied(node);
                }
            }

            // Create connections
            foreach (var (r1Id, r2Id, cType) in connectionsForThisStep)
            {
                ConnectRooms(r1Id, r2Id, cType);
            }

            // Add to main artery path
            foreach (var node in pathNodesForThisStep)
            {
                mainArteryPathIds.Add(node.id);
            }
        }
    }

    void RemoveRoomFromOccupiedTiles(LSystemRoom room)
    {
        Vector2Int startTile = new Vector2Int(
            Mathf.RoundToInt(room.position.x - room.size.x / 2.0f),
            Mathf.RoundToInt(room.position.y - room.size.y / 2.0f)
        );

        for (int x = 0; x < room.size.x; x++)
        {
            for (int y = 0; y < room.size.y; y++)
            {
                occupiedTiles.Remove(new Vector2Int(startTile.x + x, startTile.y + y));
            }
        }
    }

    // Fixed angle normalization
    float NormalizeAngle(float angle)
    {
        while (angle < 0) angle += 2f * Mathf.PI;
        while (angle >= 2f * Mathf.PI) angle -= 2f * Mathf.PI;
        return angle;
    }

    void InsertDistributiveNodesAndSproutLeaves()
    {
        List<int> tempArteryPathIds = new List<int>(mainArteryPathIds);
        int currentIdx = 0;
        int maxIterations = tempArteryPathIds.Count * 2; // Safety check
        int iterations = 0;

        while (currentIdx < tempArteryPathIds.Count - 1 && iterations < maxIterations)
        {
            iterations++;

            int roomAId = tempArteryPathIds[currentIdx];
            int roomBId = tempArteryPathIds[currentIdx + 1];

            // Safety check for room existence
            if (!rooms.ContainsKey(roomAId) || !rooms.ContainsKey(roomBId))
            {
                currentIdx++;
                continue;
            }

            LSystemRoom roomA = rooms[roomAId];
            LSystemRoom roomB = rooms[roomBId];

            if (roomA.type != RoomType.DistributiveNode &&
                roomB.type != RoomType.DistributiveNode &&
                Random.value < parameters.distributiveNodeChancePerSegment)
            {
                float t = Random.Range(0.3f, 0.7f);
                Vector2 distPos = Vector2.Lerp(roomA.position, roomB.position, t);

                LSystemRoom distNode = CreateRoom(distPos, RoomType.DistributiveNode);

                // Reconnect through distributive node
                RemoveConnection(roomA.id, roomB.id, ConnectionType.Artery);
                ConnectRooms(roomA.id, distNode.id, ConnectionType.Artery);
                ConnectRooms(distNode.id, roomB.id, ConnectionType.Artery);

                // Insert into path lists
                int insertPosInMain = mainArteryPathIds.IndexOf(roomBId);
                if (insertPosInMain >= 0)
                {
                    mainArteryPathIds.Insert(insertPosInMain, distNode.id);
                }
                tempArteryPathIds.Insert(currentIdx + 1, distNode.id);

                // Sprout leaf nodes
                int numLeafNodes = Random.Range(parameters.minLeafNodesPerDistributive, parameters.maxLeafNodesPerDistributive + 1);

                Vector2 arteryVec = roomB.position - roomA.position;
                float arterySegmentAngle = Mathf.Atan2(arteryVec.y, arteryVec.x);

                float[] potentialSproutAngles = {
                    NormalizeAngle(arterySegmentAngle + Mathf.PI / 2f),
                    NormalizeAngle(arterySegmentAngle - Mathf.PI / 2f)
                };

                // Snap to cardinal directions
                float[] sproutAngles = potentialSproutAngles.Select(SnapAngleToCardinal).ToArray();

                // Shuffle for randomness
                for (int i = 0; i < sproutAngles.Length; i++)
                {
                    float temp = sproutAngles[i];
                    int randomIndex = Random.Range(i, sproutAngles.Length);
                    sproutAngles[i] = sproutAngles[randomIndex];
                    sproutAngles[randomIndex] = temp;
                }

                for (int leafCount = 0; leafCount < numLeafNodes && leafCount < sproutAngles.Length; leafCount++)
                {
                    SproutLeafNode(distNode, sproutAngles[leafCount]);
                }

                currentIdx++;
            }
            currentIdx++;
        }
    }

    float SnapAngleToCardinal(float angle)
    {
        angle = NormalizeAngle(angle);

        float closestAngle = cardinalDirections[0];
        float minDifference = Mathf.Abs(angle - closestAngle);

        foreach (float cardinalAngle in cardinalDirections)
        {
            float difference = Mathf.Abs(angle - cardinalAngle);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestAngle = cardinalAngle;
            }
        }

        return closestAngle;
    }

    void SproutLeafNode(LSystemRoom fromNode, float initialAngle)
    {
        float dx = parameters.leafBranchLength * Mathf.Cos(initialAngle);
        float dy = parameters.leafBranchLength * Mathf.Sin(initialAngle);

        Vector2 initialLeafPos = new Vector2(
            fromNode.position.x + dx + Random.Range(-parameters.posJitterLeaf, parameters.posJitterLeaf),
            fromNode.position.y + dy + Random.Range(-parameters.posJitterLeaf, parameters.posJitterLeaf)
        );

        // Check for path intersection before creating leaf node
        bool pathIntersects = DoesPathIntersectExistingPaths(fromNode.position, initialLeafPos);

        Vector2 finalLeafPos = initialLeafPos;

        if (pathIntersects)
        {
            // Try different angles to avoid intersection
            float[] alternativeAngles = {
                NormalizeAngle(initialAngle + Mathf.PI / 4f),
                NormalizeAngle(initialAngle - Mathf.PI / 4f),
                NormalizeAngle(initialAngle + Mathf.PI / 2f),
                NormalizeAngle(initialAngle - Mathf.PI / 2f)
            };

            bool foundValidPosition = false;

            foreach (float angle in alternativeAngles)
            {
                float altDx = parameters.leafBranchLength * Mathf.Cos(angle);
                float altDy = parameters.leafBranchLength * Mathf.Sin(angle);

                Vector2 altLeafPos = new Vector2(
                    fromNode.position.x + altDx + Random.Range(-parameters.posJitterLeaf, parameters.posJitterLeaf),
                    fromNode.position.y + altDy + Random.Range(-parameters.posJitterLeaf, parameters.posJitterLeaf)
                );

                if (!DoesPathIntersectExistingPaths(fromNode.position, altLeafPos))
                {
                    finalLeafPos = altLeafPos;
                    foundValidPosition = true;
                    break;
                }
            }

            if (!foundValidPosition)
            {
                Debug.LogWarning($"Could not find non-intersecting path for leaf node from {fromNode.id}");
            }
        }

        LSystemRoom leafNode = CreateRoom(finalLeafPos, RoomType.LeafNode);
        ConnectRooms(fromNode.id, leafNode.id, ConnectionType.Vein);
    }

    void GenerateMap()
    {
        GenerateMainArtery();
        InsertDistributiveNodesAndSproutLeaves();
    }

    void RenderDungeon()
    {
        if (tilemap == null) return;

        // Render rooms
        foreach (var room in rooms.Values)
        {
            RenderRoom(room);
        }

        // Render corridors
        HashSet<(int, int)> drawnConnections = new HashSet<(int, int)>();

        foreach (var room in rooms.Values)
        {
            if (room.connections == null) continue;

            foreach (var connection in room.connections)
            {
                if (!rooms.ContainsKey(connection.connectedRoomId)) continue;

                var connectionKey = room.id < connection.connectedRoomId ?
                    (room.id, connection.connectedRoomId) :
                    (connection.connectedRoomId, room.id);

                if (!drawnConnections.Contains(connectionKey))
                {
                    drawnConnections.Add(connectionKey);
                    RenderCorridor(room, rooms[connection.connectedRoomId], connection.connectionType);
                }
            }
        }
    }

    void RenderRoom(LSystemRoom room)
    {
        if (room == null) return;

        TileBase tileToUse = GetTileForRoomType(room.type);

        // Fixed division issue by using float division
        Vector3Int startPos = new Vector3Int(
            Mathf.RoundToInt(room.position.x - room.size.x / 2.0f),
            Mathf.RoundToInt(room.position.y - room.size.y / 2.0f),
            0
        );

        BoundsInt roomBounds = new BoundsInt(startPos, new Vector3Int(room.size.x, room.size.y, 1));

        TileBase[] tiles = new TileBase[room.size.x * room.size.y];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = tileToUse;
        }

        tilemap.SetTilesBlock(roomBounds, tiles);
    }

    TileBase GetTileForRoomType(RoomType type)
    {
        switch (type)
        {
            case RoomType.MainArtery:
                return mainArteryTile != null ? mainArteryTile : roomTile;
            case RoomType.LeafNode:
                return leafTile != null ? leafTile : roomTile;
            default:
                return roomTile;
        }
    }

    void RenderCorridor(LSystemRoom room1, LSystemRoom room2, ConnectionType connectionType)
    {
        if (corridorTile == null || room1 == null || room2 == null) return;

        Vector2Int start = new Vector2Int(Mathf.RoundToInt(room1.position.x), Mathf.RoundToInt(room1.position.y));
        Vector2Int end = new Vector2Int(Mathf.RoundToInt(room2.position.x), Mathf.RoundToInt(room2.position.y));

        // Create L-shaped corridor
        // Horizontal first
        int stepX = start.x < end.x ? 1 : -1;
        for (int x = start.x; x != end.x; x += stepX)
        {
            tilemap.SetTile(new Vector3Int(x, start.y, 0), corridorTile);
        }

        // Then vertical
        int stepY = start.y < end.y ? 1 : -1;
        for (int y = start.y; y != end.y; y += stepY)
        {
            tilemap.SetTile(new Vector3Int(end.x, y, 0), corridorTile);
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || rooms == null) return;

        foreach (var room in rooms.Values)
        {
            if (room == null) continue;

            // Set color based on room type
            switch (room.type)
            {
                case RoomType.MainArtery:
                    Gizmos.color = Color.red;
                    break;
                case RoomType.DistributiveNode:
                    Gizmos.color = Color.yellow;
                    break;
                case RoomType.LeafNode:
                    Gizmos.color = Color.green;
                    break;
                case RoomType.ArteryCorner:
                    Gizmos.color = Color.gray;
                    break;
            }

            // Draw room bounds
            Vector3 center = new Vector3(room.position.x, room.position.y, 0);
            Vector3 size = new Vector3(room.size.x, room.size.y, 0);
            Gizmos.DrawWireCube(center, size);

            // Draw collision bounds (expanded by minRoomDistance)
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Vector3 expandedSize = new Vector3(room.size.x + parameters.minRoomDistance, room.size.y + parameters.minRoomDistance, 0);
            Gizmos.DrawWireCube(center, expandedSize);

            // Draw connections
            if (room.connections != null)
            {
                foreach (var connection in room.connections)
                {
                    if (rooms.ContainsKey(connection.connectedRoomId))
                    {
                        Vector3 targetPos = new Vector3(rooms[connection.connectedRoomId].position.x, rooms[connection.connectedRoomId].position.y, 0);
                        Gizmos.color = connection.connectionType == ConnectionType.Artery ? Color.red : Color.green;
                        Gizmos.DrawLine(center, targetPos);
                    }
                }
            }
        }

        // Draw corridor paths
        Gizmos.color = Color.blue;
        foreach (var path in corridorPaths)
        {
            Vector3 start = new Vector3(path.Item1.x, path.Item1.y, 0);
            Vector3 end = new Vector3(path.Item2.x, path.Item2.y, 0);
            Gizmos.DrawLine(start, end);
        }
    }

    [ContextMenu("Generate New Dungeon")]
    public void GenerateNewDungeon()
    {
        GenerateDungeon();
    }

    public Dictionary<int, LSystemRoom> GetRooms()
    {
        return new Dictionary<int, LSystemRoom>(rooms);
    }

    public List<int> GetMainArteryPath()
    {
        return new List<int>(mainArteryPathIds);
    }

    public HashSet<Vector2Int> GetOccupiedTiles()
    {
        return new HashSet<Vector2Int>(occupiedTiles);
    }

    public List<(Vector2, Vector2)> GetCorridorPaths()
    {
        return new List<(Vector2, Vector2)>(corridorPaths);
    }
}