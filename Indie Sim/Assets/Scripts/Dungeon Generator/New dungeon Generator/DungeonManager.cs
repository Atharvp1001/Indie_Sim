using UnityEngine;
using UnityEngine.Tilemaps;
public class DungeonManager : MonoBehaviour
{
    public TileBase floorTile;

    void Awake()
    {
        RoomConfig.FloorTile = floorTile;
    }
}
