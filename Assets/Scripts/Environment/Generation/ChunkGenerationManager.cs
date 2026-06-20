using System.Collections;
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
    public Dictionary<Vector2Int, Cell> cellWorld;

    [SerializeField] private int largeChunkRenderDistance = 1; // ha jól tudom tényleg large chunkokkal foglalkozik, de az átnevezés csak egy tipp. eredeti: renderDistance
    [SerializeField] private int smallChunkRenderDistance = 6;
    public static int smallChunkSize = 10;
    public static int largeChunkSize = 20; // 50

    private void Start()
    {
        map = new Dictionary<Vector2Int, CellType>();
        world = new Dictionary<Vector2Int, GameObject>();

        smallChunks = new Dictionary<Vector2Int, SmallChunk>();
        largeChunks = new Dictionary<Vector2Int, LargeChunk>();
        activeRooms = new Dictionary<Vector2Int, SmallChunk>();
        cellWorld = new Dictionary<Vector2Int, Cell>();

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

    private void ManageSmallChunkVisibility()
    {
        // --- SmallChunk/Cell réteg (SetActive) ---
        // Ez a már Enable-olt LargeChunk-okon belül dolgozik
        foreach (KeyValuePair<Vector2Int, SmallChunk> pair in smallChunks)
        {
            SmallChunk chunk = pair.Value;

            // SmallChunk localPosition = SmallChunk-index a LargeChunk-on belül
            // Ehhez kell a globális SmallChunk-index:
            // a LargeChunk-index * 5 + localPosition
            // De egyszerűbb a worldPosition alapján számolni
            Vector2Int chunkSmallIndex = new Vector2Int(
                Mathf.FloorToInt((float)chunk.worldPosition.x / smallChunkSize),
                Mathf.FloorToInt((float)chunk.worldPosition.y / smallChunkSize)
            );

            float dist = Vector2Int.Distance(chunkSmallIndex, worldTracker.playerRoomPosition);

            if (dist <= smallChunkRenderDistance)
            {
                if (!chunk.Active) chunk.EnableSmallChunk();
            }
            else
            {
                if (chunk.Active) chunk.DisableSmallChunk();
            }
        }
    }

    bool isGenerating = false;

    private void ManageChunksLoading()
    {
        // Csak SetActive logika - ez gyors
        ManageSmallChunkVisibility();

        // Generálás csak ha kell, és nem fut már
        if (!isGenerating)
            StartCoroutine(GenerateNewChunksIfNeeded());
    }

    private IEnumerator GenerateNewChunksIfNeeded()
    {
        isGenerating = true;

        // --- LargeChunk réteg ---
        // playerRoomPosition SmallChunk-index → LargeChunk-index
        Vector2Int playerLargeChunkIndex = new Vector2Int(
            Mathf.FloorToInt((float)worldTracker.playerRoomPosition.x / (largeChunkSize / smallChunkSize)),
            Mathf.FloorToInt((float)worldTracker.playerRoomPosition.y / (largeChunkSize / smallChunkSize))
        );

        // Melyik LargeChunk-oknak kell létezni (játékos + 1 körös gyűrű)
        HashSet<Vector2Int> neededLargeChunks = new();
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                neededLargeChunks.Add(playerLargeChunkIndex + new Vector2Int(x, y));

        // LargeChunk-ok amik kellenek de még nem léteznek
        foreach (Vector2Int idx in neededLargeChunks)
        {
            if (largeChunks.ContainsKey(idx)) continue;

            LargeChunk newChunk = new LargeChunk(idx, acgRef);
            largeChunks.Add(idx, newChunk);

            yield return null; // következő frame-re vár, nem fagy le
        }

        isGenerating = false;
    }

    [SerializeField] private Player player;
    [SerializeField] private WorldTracker worldTracker;
    [SerializeField] private ACGen acgRef;
}

public class Cell
{
    public CellType CellType
    {
        get { return _cellType; }
        set
        {
            _cellType = value;
            if (value != CellType.Wall)
                wallWeight = -1;
        }
    }
    private CellType _cellType;

    public GameObject gm;
    public Vector2Int worldPosition;

    public int wallWeight = -1;

    public Cell(CellType cellType)
    {
        this.CellType = cellType;
        if (cellType == CellType.Wall) wallWeight = 0;
    }
}

public class SmallChunk
{
    private bool _exists = true;
    public bool Active = true;

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

    public void EnableSmallChunk()
    {
        if (Active) return;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (pair.Value.gm.activeSelf) continue;
            pair.Value.gm.SetActive(true);
        }

        Active = true;
    }

    public void DisableSmallChunk()
    {
        if (!Active) return;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (!pair.Value.gm.activeSelf) continue;
            pair.Value.gm.SetActive(false);
        }

        Active = false;
    }

    public void LoadChunk()
    {
        if (_exists) return;

        _exists = true;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            pair.Value.gm = _parentChunkRef.acgRef.CreateCellGameObject(pair.Key, pair.Value.CellType);
        }
    }

    public void UnloadChunk()
    {
        if (!_exists) return;

        _exists = false;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            _parentChunkRef.acgRef.DestroyCellGameObject(pair.Value.gm);
        }
    }
}

public class LargeChunk
{
    public bool IsEnabled = true;
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

    public void EnableLargeChunk()
    {
        foreach (KeyValuePair<Vector2Int, SmallChunk> pair in smallChunks)
        {
            pair.Value.LoadChunk();
        }
        IsEnabled = true;
    }

    public void DisableLargeChunk()
    {
        foreach (KeyValuePair<Vector2Int, SmallChunk> pair in smallChunks)
        {
            pair.Value.UnloadChunk();
        }
        IsEnabled = false;
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

        //Debug.Log($"origin: {origin}, worldPos: {worldPos}, cell's worldPos: {cell.worldPosition}");

        this.cells.Add(cell.worldPosition, cell);
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
                //Debug.Log($"{worldX}, {worldZ}");
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

        acgRef.FillWithWalls(this);
        GenerateMap(acgRef.labirynthCount, this);

        acgRef.MakePatches(1, this);
        acgRef.GenerateRooms(10, this);
        //acgRef.GenerateWallBlocks(3);
    }


    public ACGen acgRef;
}