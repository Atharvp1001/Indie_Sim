using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class PathGenerator : MonoBehaviour
{
    [Header("References")]
    public Tilemap tilemap;
    public RoomGenerator roomGenerator; // assign a component / prefab

    [Header("Path Settings")]
    public int pathLength = 8;
    public Vector2Int roomSizeMin = new(4, 4);
    public Vector2Int roomSizeMax = new(8, 8);
    public int roomAttempts = 3;

    private List<BoundsInt> placedRooms = new();

    void Start() => GeneratePath();

    public void GeneratePath()
    {
        tilemap.ClearAllTiles();
        placedRooms.Clear();

        Vector2Int currentPos = Vector2Int.zero;
        Vector2Int lastDir = Vector2Int.right;

        for (int i = 0; i < pathLength; i++)
        {
            bool roomCreated = false;
            BoundsInt room = default;

            for (int attempt = 0; attempt < roomAttempts; attempt++)
            {
                var size = new Vector2Int(
                    Random.Range(roomSizeMin.x, roomSizeMax.x + 1),
                    Random.Range(roomSizeMin.y, roomSizeMax.y + 1)
                );

                if (roomGenerator.TryCreateRoom(tilemap, placedRooms, currentPos, size, out room))
                {
                    placedRooms.Add(room);
                    roomCreated = true;
                    break;
                }
            }

            if (roomCreated && placedRooms.Count > 1)
            {
                var prev = placedRooms[placedRooms.Count - 2];
                CorridorUtils.CreateCorridor(tilemap, prev, room);
            }

            // Move along the winding path
            var dir = GetRandomDirectionExcluding(-lastDir);
            lastDir = dir;
            int offset = Mathf.Max(roomSizeMin.x, roomSizeMin.y) + 2;
            currentPos += dir * offset;
        }
    }

    private Vector2Int GetRandomDirectionExcluding(Vector2Int exclude)
    {
        var dirs = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        dirs.Remove(exclude);
        return dirs[Random.Range(0, dirs.Count)];
    }
}
