using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerationManager : MonoBehaviour
{
    public static GameObject _wallPart; // szükséges a cell osztály konstruktorához hogy létrehozzam a falat ha az a típus

    public Dictionary<Vector2Int, CellType> map;
    public Dictionary<Vector2Int, GameObject> world;

    /// <summary>
    /// Vector2Int: worldpos
    /// </summary>
    public Dictionary<Vector2Int, SmallChunk> smallChunks;
    public Dictionary<Vector2Int, LargeChunk> largeChunks;
    public Dictionary<Vector2Int, SmallChunk> activeRooms;
    public Dictionary<Vector2Int, Cell> cellWorld;

    public int largeChunkRenderDistance; // ha jól tudom tényleg large chunkokkal foglalkozik, de az átnevezés csak egy tipp. eredeti: renderDistance
    public int smallChunkRenderDistance;
    [Space]
    public int cellSize;
    public int smallChunkSize;
    public int largeChunkSize; 

    private void Start()
    {
        map = new Dictionary<Vector2Int, CellType>();
        world = new Dictionary<Vector2Int, GameObject>();

        smallChunks = new Dictionary<Vector2Int, SmallChunk>();
        largeChunks = new Dictionary<Vector2Int, LargeChunk>();
        activeRooms = new Dictionary<Vector2Int, SmallChunk>();
        cellWorld = new Dictionary<Vector2Int, Cell>();

        GenerateNewChunks();

        worldTracker.playerLargeChunkPosChanged += GenerateNewChunks; 
        //worldTracker.playerSmallChunkPosChanged += ManageSmallChunkVisibility;
    }

    private void ManageSmallChunkVisibility()
    {
        foreach (KeyValuePair<Vector2Int, SmallChunk> pair in smallChunks)
        {
            SmallChunk chunk = pair.Value;
            float dist = Vector2.Distance(chunk.worldPosition * cellSize, new Vector2(worldTracker.playerTransformPos.position.x, worldTracker.playerTransformPos.position.z));

            if (dist <= smallChunkRenderDistance * smallChunkSize * cellSize) // 20 mivel ha én 6 smallchunk-ra állítom be akkor a számolás illeszkedjen a világ poz. számolással
                chunk.LoadChunk();
            else
            {
                //Debug.Log($"unload, dist: {dist / 20}, smallChunkRenderDistance : {smallChunkRenderDistance}, smallChunkDeloadingpos = {chunk.worldPosition / 20}, playerSmallChunkPos: {worldTracker.playerSmallChunkPosition}");
                chunk.UnloadChunk();
            }
        }
    }

    public IEnumerator GenerateSequentially(LargeChunk chunk)
    {
        yield return acgRef.StartCoroutine(acgRef.FillWithWalls(chunk));
        yield return acgRef.StartCoroutine(acgRef.GenerateMap(chunk));
        //yield return acgRef.StartCoroutine(acgRef.GenerateRooms(chunk));
        yield return acgRef.StartCoroutine(acgRef.MakePatches(chunk));
        yield return acgRef.StartCoroutine(acgRef.OptimiseWalls(chunk));
    }

    private void GenerateNewChunks()
    {
        StartCoroutine(GenerateNewChunksIfNeeded()); // StartCoroutine(GenerateNewChunksIfNeeded());
    }

    private IEnumerator GenerateNewChunksIfNeeded() 
    {
        // játékos + 1 körös gyűrű
        for (int x = -largeChunkRenderDistance; x <= largeChunkRenderDistance; x++)
        {   
            for (int y = -largeChunkRenderDistance; y <= largeChunkRenderDistance; y++)
            {
                Vector2Int currentLargeChunkIndexPos = worldTracker.playerLargeChunkPosition + new Vector2Int(x, y);
                Vector2Int currentLargeChunkWorldPos = (worldTracker.playerLargeChunkPosition + new Vector2Int(x, y)) * largeChunkSize;

                //Debug.Log($"0, currentLargeChunkIndexPos: {currentLargeChunkIndexPos}, playerLCPos: {worldTracker.playerLargeChunkPosition}");

                int chebyshevDist = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                if (largeChunks.ContainsKey(currentLargeChunkIndexPos)) continue;

                /*Debug.Log($"generating, currentLargeChunkIndexPos: {currentLargeChunkIndexPos}, playerLCPos: {worldTracker.playerLargeChunkPosition}");
                Debug.Log("");*/

                LargeChunk newChunk = new LargeChunk(acgRef, this);
                largeChunks.Add(currentLargeChunkIndexPos, newChunk);

                newChunk.GenerateLargeChunk(currentLargeChunkIndexPos, worldTracker);

                yield return null;
            }
        }
        //ManageSmallChunkVisibility();
    }

    [SerializeField] private Player player;
    [SerializeField] private WorldTracker worldTracker;
    [SerializeField] private ACGen acgRef;
}

public class Cell
{
    public GameObject wallGameObject;
    public GameObject ceilingGameObject;
    public GameObject lightGameObject;

    public Vector2Int WorldPosition;

    public CellType CellType
    {
        get { return _cellType; }
        set
        {
            _cellType = value;
            if (value != CellType.Wall)
                WallWeight = -1;
        }
    }
    private CellType _cellType;

    public Biome Biome;

    public SmallChunk parentChunk;
    public Area areaParent;

    public int WallWeight = -1;

    public Cell(CellType cellType, Vector2Int worldPos)
    {
        WorldPosition = worldPos;
        this.CellType = cellType;
        if (cellType == CellType.Wall) WallWeight = 0;
    }
}

public class SmallChunk
{
    public bool Exists = true;
    public bool Active = true;

    /// <summary>
    /// Vector2Int: world pos
    /// </summary>
    public Dictionary<Vector2Int, Cell> cells = new();
    private LargeChunk _parentChunkRef;
    private ChunkGenerationManager _cgmRef;

    // ezeket generálásnál beállítom
    public Vector2Int localPosition;
    public Vector2Int worldPosition; // mikor generálom a Prim-et (nagy chunk-on belül) akkor a cellákat elhelyezem a "smallChunks" Dic-ben.

    private Vector2Int _worldOriginPos;

    public SmallChunk(LargeChunk reference, ChunkGenerationManager cgmRef, Vector2Int localPosition, Vector2Int worldPosition)
    {
        _parentChunkRef = reference;
        _cgmRef = cgmRef;

        this.localPosition = localPosition;
        this.worldPosition = worldPosition;

        cgmRef.smallChunks.Add(worldPosition, this);

        _worldOriginPos = new Vector2Int(
            this.worldPosition.x - (cgmRef.smallChunkSize / cgmRef.cellSize),
            this.worldPosition.y - (cgmRef.smallChunkSize / cgmRef.cellSize)
        );
    }

    public void EnableSmallChunk()
    {
        if (Active) return;

        Debug.Log($"enabling: {this.localPosition}");

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (pair.Value.wallGameObject != null)
            {
                pair.Value.wallGameObject.SetActive(false);
            }
            else if (pair.Value.lightGameObject != null)
            {
                pair.Value.lightGameObject.SetActive(false);
            }
        }

        Active = true;
    }

    public void DisableSmallChunk()
    {
        if (!Active) return;

        Debug.Log($"disabling: {this.localPosition}");

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (pair.Value.wallGameObject != null)
            {
                pair.Value.wallGameObject.SetActive(false);
            }
            else if (pair.Value.lightGameObject != null)
            {
                pair.Value.lightGameObject.SetActive(false);
            }
        }

        Active = false;
    }

    public void LoadChunk()
    {
        if (Exists) return;

        Exists = true;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (pair.Value.CellType != CellType.Wall && pair.Value.CellType != CellType.LightEmpty) continue;
            
            pair.Value.wallGameObject = _parentChunkRef.acgRef.CreateCellGameObject(pair.Key, pair.Value.CellType);
            pair.Value.wallGameObject.transform.position = new Vector3(pair.Value.WorldPosition.x * _parentChunkRef.cgmRef.cellSize, 1, pair.Value.WorldPosition.y * _parentChunkRef.cgmRef.cellSize);
        }
    }

    public void UnloadChunk()
    {
        if (!Exists) return;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (pair.Value.CellType == CellType.Wall)
            {
                _parentChunkRef.acgRef.DestroyCellGameObject(pair.Value.wallGameObject);
            }
            if (pair.Value.CellType == CellType.LightEmpty)
            {
                Debug.Log("destroyed");
                _parentChunkRef.acgRef.DestroyCellGameObject(pair.Value.lightGameObject);
            }
        }

        Exists = false;
    }
}

public class LargeChunk
{
    public bool IsEnabled = true;

    /// <summary>
    /// Vector2Int: local pos for now
    /// </summary>
    public Dictionary<Vector2Int, SmallChunk> smallChunks = new();

    /// <summary>
    /// Vector2Int: world pos
    /// </summary>
    public Dictionary<Vector2Int, Cell> cells = new();
    public List<Area> areas = new();

    public Vector2Int largeChunkWorldPivotPos = Vector2Int.zero;

    public int notCategorisedCells = 0;

    /// <param name="thisLargeChunkPos">The current instance of LargeChunk's world position (Vector2Int)</param>
    public LargeChunk(ACGen acgRef, ChunkGenerationManager cgmRef)
    {
        this.acgRef = acgRef;
        this.cgmRef = cgmRef;
    }

    /// <summary>
    /// Adds the cell to it's respectable SmallChunk. 
    /// </summary>
    /// <param name="cell"></param>
    public void AddCell(Cell cell)
    {
        Vector2Int origin = largeChunkWorldPivotPos - new Vector2Int(cgmRef.largeChunkSize / 2, cgmRef.largeChunkSize / 2);
        Vector2Int worldPos = cell.WorldPosition - origin;
        Vector2Int smallChunkKey = worldPos / cgmRef.smallChunkSize;

        this.cells.Add(worldPos, cell); // cell.WorldPosition
        smallChunks[smallChunkKey].cells.Add(worldPos, cell); // key volt az index (mellékes)
    }

    public void GenerateLargeChunk(Vector2Int nextLargeChunkPos, WorldTracker tracker)
    {
        int largeChunkSize = cgmRef.largeChunkSize;

        largeChunkWorldPivotPos = new(
            nextLargeChunkPos.x * largeChunkSize,
            nextLargeChunkPos.y * largeChunkSize
        );

        for (int worldX = Mathf.RoundToInt(largeChunkWorldPivotPos.x - (largeChunkSize / cgmRef.cellSize)); worldX < largeChunkWorldPivotPos.x + (largeChunkSize / cgmRef.cellSize); worldX++)
        {
            for (int worldZ = Mathf.RoundToInt(largeChunkWorldPivotPos.y - (largeChunkSize / cgmRef.cellSize)); worldZ < largeChunkWorldPivotPos.y + (largeChunkSize / cgmRef.cellSize); worldZ++)
            {
                Vector2Int currentWorldPos = new Vector2Int(worldX, worldZ);
                Vector2Int currentSmallChunkIndexPos = new Vector2Int(
                    Mathf.FloorToInt((float)(worldX - (largeChunkWorldPivotPos.x - largeChunkSize / cgmRef.cellSize)) / cgmRef.smallChunkSize),
                    Mathf.FloorToInt((float)(worldZ - (largeChunkWorldPivotPos.y - largeChunkSize / cgmRef.cellSize)) / cgmRef.smallChunkSize)
                );

                if (smallChunks.ContainsKey(currentSmallChunkIndexPos)/* || 
                    Vector2Int.Distance(currentSmallChunkIndexPos * cgmRef.smallChunkSize, tracker.playerSmallChunkPosition) > cgmRef.smallChunkRenderDistance*/) 
                    continue;

                SmallChunk newSmallChunk = new SmallChunk(this, cgmRef, currentSmallChunkIndexPos, currentWorldPos);
                smallChunks.Add(
                    currentSmallChunkIndexPos, newSmallChunk
                );
            }
        }

        acgRef.StartCoroutine(cgmRef.GenerateSequentially(this));
    }

    public ACGen acgRef;
    public ChunkGenerationManager cgmRef;
}

public class Area
{
    public string biomeName;
    public BiomeType biomeType;

    public int CellCount = 0;

    /// <summary>
    /// Key: World Position
    /// </summary>
    public Dictionary<Vector2Int, Cell> cellMembers = new();

    public Area(LargeChunk largeChunkRef)
    {
        parentChunk = largeChunkRef;
    }

    public void FillWholeArea()
    {
        foreach (KeyValuePair<Vector2Int, Cell> pair in cellMembers)
        {
            Cell currentCell = pair.Value;

            if (currentCell.CellType == CellType.LightEmpty)
                parentChunk.acgRef.DestroyCellGameObject(currentCell.lightGameObject);

            currentCell.wallGameObject = parentChunk.acgRef.CreateCellGameObject(pair.Key, CellType.Wall);
            currentCell.CellType = CellType.Wall;
        }
        this.Dispose();
    }

    private void Dispose()
    {
        parentChunk = null;
        biomeName = null;
        CellCount = -1;
        cellMembers = null;
    }

    private LargeChunk parentChunk;
}

public enum BiomeType
{
    Yellow_Halls,
    DEV_Halls
}