using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Dungeon/Room Generator")]
public class RoomGenerator : ScriptableObject
{
    [Header("References")]
    public TileBase floorTile;

    /// <summary>
    /// Attempts to create a rectangular room at the given position.
    /// Ensures no overlaps with existing rooms. Returns true & outputs room bounds if successful.
    /// </summary>
    public bool TryCreateRoom(
        Tilemap tilemap,
        List<BoundsInt> existingRooms,
        Vector2Int position,
        Vector2Int size,
        out BoundsInt roomBounds)
    {
        Vector3Int start = (Vector3Int)position;
        roomBounds = new BoundsInt(start, new Vector3Int(size.x, size.y, 1));

        // Check overlaps
        foreach (var existing in existingRooms)
        {
            if (IsOverlapping(existing, roomBounds))
                return false;
        }

        // Paint room floor tiles
        for (int x = roomBounds.xMin; x < roomBounds.xMax; x++)
            for (int y = roomBounds.yMin; y < roomBounds.yMax; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);

        return true;
    }

    /// <summary>
    /// Simple AABB overlap check between two integer bounds.
    /// </summary>
    private bool IsOverlapping(BoundsInt a, BoundsInt b)
    {
        return (a.xMin < b.xMax && a.xMax > b.xMin &&
                a.yMin < b.yMax && a.yMax > b.yMin);
    }
}
