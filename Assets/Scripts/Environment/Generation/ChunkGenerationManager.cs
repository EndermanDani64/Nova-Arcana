using System.Collections.Generic;
using UnityEngine;
using static ACGen;

public class ChunkGenerationManager : MonoBehaviour
{
    public static GameObject _wallPart; // szükséges a cell osztály konstruktorához hogy létrehozzam a falat ha az a típus

    public Dictionary<Vector2Int, CellType> map;
    public Dictionary<Vector2Int, GameObject> world;

    public Dictionary<Vector2Int, SmallChunk> smallChunks;
    public Dictionary<Vector2Int, LargeChunk> largeChunks;
    public Dictionary<Vector2Int, SmallChunk> activeRooms;

    [SerializeField] private int renderDistance = 1;
    public static int smallChunkSize = 30;
    public static int largeChunkSize = 150;

    private void Start()
    {
        map = new Dictionary<Vector2Int, CellType>();
        world = new Dictionary<Vector2Int, GameObject>();

        smallChunks = new Dictionary<Vector2Int, SmallChunk>();
        largeChunks = new Dictionary<Vector2Int, LargeChunk>();
        activeRooms = new Dictionary<Vector2Int, SmallChunk>();

        ManageChunksLoading();

        worldTracker.playerChunkPosChanged += ManageChunksLoading;
        worldTracker.playerChunkPosChanged += DeloadChunks;
    }

    private void DeloadChunks()
    {
        foreach (KeyValuePair<Vector2Int, SmallChunk> room in smallChunks)
        {
            SmallChunk g = room.Value;

            /*if (Vector3.Distance(g.transform.position, player.GetComponent<Transform>().position) > 120)
            {
                g.GetComponent<RoomNode>().DeloadNode();
                activeRooms.Remove(room.Key);
            }*/
        }
    }

    private void ManageChunksLoading()
    {
        Vector2Int currentPlayerPos = worldTracker.playerRoomPosition;
        LargeChunk currentLargeChunk;

        for (int x = currentPlayerPos.x - renderDistance; x < currentPlayerPos.x + renderDistance; x++)
        {
            for (int y = currentPlayerPos.y - renderDistance; y < currentPlayerPos.y + renderDistance; y++)
            {
                Vector2Int currentLargeChunkIndexPos = new Vector2Int(x, y); // ? -> worldpos vagy sem

                if (!largeChunks.ContainsKey(currentLargeChunkIndexPos))
                {
                    currentLargeChunk = new LargeChunk(currentLargeChunkIndexPos, acgRef); // zzz !!! KeyNotFoundException: The given key '(0, 0)' was not present in the dictionary.
                    largeChunks.Add(currentLargeChunkIndexPos, currentLargeChunk);
                }
                else
                {

                    if (!activeRooms.ContainsKey(currentLargeChunkIndexPos)) activeRooms.Add(currentLargeChunkIndexPos, smallChunks[currentLargeChunkIndexPos]);
                }
            }
        }
    }

    [SerializeField] private Player player;
    [SerializeField] private WorldTracker worldTracker;
    [SerializeField] private ACGen acgRef;
}

public class Cell
{
    public CellType cellType;
    public GameObject gm;
    public Vector2Int worldPosition;

    public Cell(CellType cellType)
    {
        this.cellType = cellType;
    }
}

public class SmallChunk
{
    private bool active = true;

    /// <summary>
    /// Vector2Int: world pos
    /// </summary>
    public Dictionary<Vector2Int, Cell> cells = new();
    private LargeChunk _parentChunkRef;

    // ezeket generálásnál beállítom
    public Vector2Int localPosition;
    public Vector2Int worldPosition; // mikor generálom a Prim-et (nagy chunk-on belül) akkor a cellákat elhelyezem a "smallChunks" Dic-ben.

    private Vector2Int _worldOriginPos;

    public SmallChunk(LargeChunk reference, Vector2Int localPosition, Vector2Int worldPosition)
    {
        _parentChunkRef = reference;

        this.localPosition = localPosition;
        this.worldPosition = worldPosition;

        _worldOriginPos = new Vector2Int(
            this.worldPosition.x - (ChunkGenerationManager.smallChunkSize / 2),
            this.worldPosition.y - (ChunkGenerationManager.smallChunkSize / 2)
        );
    }

    public void LoadChunk()
    {
        if (active) return;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (pair.Value.gm.activeSelf) continue;
            pair.Value.gm.SetActive(true);
        }
    }

    public void DeloadChunk()
    {
        if (!active) return;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (!pair.Value.gm.activeSelf) continue;
            pair.Value.gm.SetActive(false);
        }
    }
}

public class LargeChunk
{
    private bool active = true;
    private bool isGenerated = false;

    /// <summary>
    /// Vector2Int: local pos for now
    /// </summary>
    public Dictionary<Vector2Int, SmallChunk> smallChunks = new();

    /// <summary>
    /// Vector2Int: world pos
    /// </summary>
    public Dictionary<Vector2Int, Cell> cells = new();

    public Vector2Int largeChunkPivot = Vector2Int.zero;

    /// <param name="thisLargeChunkPos">The current instance of LargeChunk's world position (Vector2Int)</param>
    public LargeChunk(Vector2Int thisLargeChunkPos, ACGen acgRef)
    {
        this.acgRef = acgRef;
        GenerateLargeChunk(thisLargeChunkPos);
    }

    /// <summary>
    /// Adds the cell to it's respectable SmallChunk. 
    /// </summary>
    /// <param name="cell"></param>
    public void AddCell(Cell cell)
    {
        Vector2Int origin = largeChunkPivot - new Vector2Int(Mathf.RoundToInt(ChunkGenerationManager.largeChunkSize / 2), Mathf.RoundToInt(ChunkGenerationManager.largeChunkSize / 2));
        Vector2Int worldPos = cell.worldPosition - origin;
        Vector2Int smallChunkKey = worldPos / ChunkGenerationManager.smallChunkSize;

        Debug.Log($"origin: {origin}, worldPos: {worldPos}, cell's worldPos: {cell.worldPosition}");

        this.cells.Add(worldPos, cell);
        smallChunks[smallChunkKey].cells.Add(worldPos, cell); // key volt az index (mellékes)
    }

    private void GenerateLargeChunk(Vector2Int nextLargeChunkPos)
    {
        int largeChunkSize = ChunkGenerationManager.largeChunkSize;

        largeChunkPivot = new(
            nextLargeChunkPos.x * largeChunkSize,
            nextLargeChunkPos.y * largeChunkSize
        );

        int indexPosX = -1 * (largeChunkSize / 2);
        int indexPosZ = largeChunkSize / 2;

        for (int worldX = Mathf.RoundToInt(largeChunkPivot.x - (largeChunkSize / 2)); worldX < largeChunkPivot.x + (largeChunkSize / 2); worldX++, indexPosX++)
        {
            for (int worldZ = Mathf.RoundToInt(largeChunkPivot.y - (largeChunkSize / 2)); worldZ < largeChunkPivot.y + (largeChunkSize / 2); worldZ++, indexPosZ++)
            {
                Debug.Log($"{worldX}, {worldZ}");
                Vector2Int currentWorldPos = new Vector2Int(worldX, worldZ);

                Vector2Int currentSmallChunkIndexPos = new Vector2Int(
                    Mathf.FloorToInt((float)(worldX - (largeChunkPivot.x - largeChunkSize / 2)) / ChunkGenerationManager.smallChunkSize),
                    Mathf.FloorToInt((float)(worldZ - (largeChunkPivot.y - largeChunkSize / 2)) / ChunkGenerationManager.smallChunkSize)
                );

                if (smallChunks.ContainsKey(currentSmallChunkIndexPos)) continue;

                SmallChunk newSmallChunk = new SmallChunk(this, currentSmallChunkIndexPos, currentWorldPos);
                newSmallChunk.worldPosition = currentWorldPos;
                newSmallChunk.localPosition = currentSmallChunkIndexPos;

                smallChunks.Add(
                    currentSmallChunkIndexPos, newSmallChunk
                );
            }
        }

        Debug.Log("0");

        acgRef.FillWithWalls(this);

        Debug.Log("1");

        GenerateMap(100, this);
    }

    ACGen acgRef;
}