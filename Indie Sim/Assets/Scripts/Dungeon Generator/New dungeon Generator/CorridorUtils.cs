using UnityEngine;
using UnityEngine.Tilemaps;

public static class CorridorUtils
{
    /// <summary>
    /// Draws an L-shaped corridor connecting the centers of two rooms on the provided Tilemap.
    /// </summary>
    public static void CreateCorridor(Tilemap tilemap, BoundsInt roomA, BoundsInt roomB)
    {
        Vector3Int centerA = new Vector3Int(
            roomA.xMin + roomA.size.x / 2,
            roomA.yMin + roomA.size.y / 2,
            0);
        Vector3Int centerB = new Vector3Int(
            roomB.xMin + roomB.size.x / 2,
            roomB.yMin + roomB.size.y / 2,
            0);

        // First draw horizontal corridor
        for (int x = Mathf.Min(centerA.x, centerB.x); x <= Mathf.Max(centerA.x, centerB.x); x++)
        {
            tilemap.SetTile(new Vector3Int(x, centerA.y, 0), RoomConfig.FloorTile);
        }

        // Then draw vertical corridor
        for (int y = Mathf.Min(centerA.y, centerB.y); y <= Mathf.Max(centerA.y, centerB.y); y++)
        {
            tilemap.SetTile(new Vector3Int(centerB.x, y, 0), RoomConfig.FloorTile);
        }
    }
}
