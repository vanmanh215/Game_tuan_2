using UnityEngine;
using UnityEngine.Tilemaps;
public enum TileType
{
    None,       // Rỗng
    Ground,     // Đất nền (có thể cần sau này)
    Banana,     // Chuối
    Chili,      // Ớt (tên asset của bạn là medicine)
    Ice,        // Băng
    Wood,       // Gỗ (vật cản)
    Stone,      // Đá (vật cản)
    Hole,       // Cổng đích
    SnakeBody , // Thân rắn (để rắn không tự đi vào người)
        HoleClosed
}

[CreateAssetMenu(fileName = "New ItemData", menuName = "SeroBoom/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Gameplay Info")]
    public TileType type; 

    [Header("Visuals")]
    public TileBase tile; 
}