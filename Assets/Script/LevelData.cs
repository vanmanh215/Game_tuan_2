using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelData : MonoBehaviour
{
    [Header("Level-Specific References")]
    public Tilemap groundTilemap;
    public Tilemap itemsTilemap;
    public Tilemap snakeTilemap;
    public PlayerController playerController;
}