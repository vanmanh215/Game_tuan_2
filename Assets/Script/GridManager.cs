using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{ 
    private Tilemap groundTilemap;
    private Tilemap itemsTilemap;
    private Tilemap snakeTilemap;

    [Header("Data Configuration")]
    [SerializeField] private List<ItemData> itemDataList;
    [SerializeField] private TileBase openHoleTile;

    private Dictionary<TileBase, ItemData> dataFromTile;
    public static GridManager Instance { get; private set; }

    public Tilemap ItemsTilemap => itemsTilemap;
    public Tilemap SnakeTilemap => snakeTilemap;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeData();
    }

    private void InitializeData()
    {
        dataFromTile = new Dictionary<TileBase, ItemData>();
        foreach (var itemData in itemDataList)
        {
            if (itemData.tile != null)
            {
                dataFromTile[itemData.tile] = itemData;
            }
        }
    }

    public void SetTilemaps(Tilemap ground, Tilemap items, Tilemap snake)
    {
        this.groundTilemap = ground;
        this.itemsTilemap = items;
        this.snakeTilemap = snake;
        Debug.Log("GridManager has received and set new tilemaps!");
    }
    public TileType GetTileTypeAt(Vector3Int position)
    {
        if (itemsTilemap == null) return TileType.None;
        TileBase tile = itemsTilemap.GetTile(position);
        if (tile != null && dataFromTile.ContainsKey(tile))
        {
            return dataFromTile[tile].type;
        }
        return TileType.None;
    }

    public void ClearTile(Tilemap map, Vector3Int position)
    {
        map.SetTile(position, null);
    }

    public TileBase GetTileAt(Vector3Int position)
    {
        if (itemsTilemap == null) return null;
        return itemsTilemap.GetTile(position);
    }

    public void MoveTile(Vector3Int oldPosition, Vector3Int newPosition)
    {
        if (itemsTilemap == null) return;
        TileBase tile = GetTileAt(oldPosition);
        if (tile != null)
        {
            itemsTilemap.SetTile(oldPosition, null);
            itemsTilemap.SetTile(newPosition, tile);
        }
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        if (itemsTilemap == null) return Vector3Int.zero; // Trả về giá trị an toàn
        return itemsTilemap.WorldToCell(worldPosition);
    }

    public void OpenHoleTile(Vector3Int position)
    {
        if (itemsTilemap == null) return;
        itemsTilemap.SetTile(position, openHoleTile);
    }

    public Vector3 GetCellCenter(Vector3Int cellPosition)
    {
        // Có thể dùng bất kỳ tilemap nào, miễn là nó tồn tại
        if (groundTilemap != null) return groundTilemap.GetCellCenterWorld(cellPosition);
        if (itemsTilemap != null) return itemsTilemap.GetCellCenterWorld(cellPosition);
        return Vector3.zero; // Fallback
    }

    public BoundsInt GetMapBounds()
    {
        if (itemsTilemap == null) return new BoundsInt();
        return itemsTilemap.cellBounds;
    }
}