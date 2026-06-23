using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChunkGenerationManager : MonoBehaviour
{
    public static GameObject _wallPart; // szükséges a cell osztály konstruktorához hogy létrehozzam a falat ha az a típus

    public Dictionary<Vector2Int, CellType> map;
    public Dictionary<Vector2Int, GameObject> world;

    public Dictionary<Vector2Int, SmallChunk> smallChunks;
    public Dictionary<Vector2Int, LargeChunk> largeChunks;
    public Dictionary<Vector2Int, SmallChunk> activeRooms;
    public Dictionary<Vector2Int, Cell> cellWorld;

    public int largeChunkRenderDistance = 2; // ha jól tudom tényleg large chunkokkal foglalkozik, de az átnevezés csak egy tipp. eredeti: renderDistance
    public int smallChunkRenderDistance = 1;
    public int smallChunkSize = 10;
    public int largeChunkSize = 20; // 50

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
    }

    private void ManageSmallChunkVisibility()
    {
        foreach (KeyValuePair<Vector2Int, SmallChunk> pair in smallChunks)
        {
            SmallChunk chunk = pair.Value;
            float dist = Vector2Int.Distance(pair.Value.worldPosition, new Vector2Int(Mathf.RoundToInt(worldTracker.playerTransformPos.position.x), Mathf.RoundToInt(worldTracker.playerTransformPos.position.z)));

            Debug.Log($"Smallchunk count: {smallChunks.Count}");

            if (dist <= smallChunkRenderDistance)
            {
                chunk.LoadChunk();
            }
            else
            {
                Debug.Log($"unload, dist: {dist}, smallChunkRenderDistance : {smallChunkRenderDistance}");
                chunk.UnloadChunk();
            }
        }
    }

    bool isGenerating = false;

    private void ManageChunksLoading()
    {
        // Csak SetActive logika - ez gyors
        ManageSmallChunkVisibility();

        // Generálás csak ha kell, és nem fut már
        StartCoroutine(GenerateNewChunksIfNeeded());
    }

    private IEnumerator GenerateNewChunksIfNeeded()
    {
        isGenerating = true;

        // --- LargeChunk réteg ---
        // playerRoomPosition SmallChunk-index → LargeChunk-index
        Vector2Int playerLargeChunkIndex = new Vector2Int(
            Mathf.FloorToInt((float)worldTracker.playerSmallChunkPosition.x / (largeChunkSize / smallChunkSize)),
            Mathf.FloorToInt((float)worldTracker.playerSmallChunkPosition.y / (largeChunkSize / smallChunkSize))
        );

        // Melyik LargeChunk-oknak kell létezni (játékos + 1 körös gyűrű)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int currentLargeChunkIndexPos = playerLargeChunkIndex + new Vector2Int(x, y);
                Vector2Int currentLargeChunkWorldPos = (playerLargeChunkIndex + new Vector2Int(x, y)) * largeChunkSize;

                //Debug.Log($"distance: {Vector2Int.Distance(currentLargeChunkWorldPos, playerLargeChunkIndex * largeChunkSize)}, currentLargeChunkPos : {currentLargeChunkIndexPos}, currentLargeChunkWorldPos : {currentLargeChunkWorldPos}");

                int chebyshevDist = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                if (largeChunks.ContainsKey(currentLargeChunkIndexPos) || chebyshevDist > largeChunkRenderDistance) continue;

                LargeChunk newChunk = new LargeChunk(acgRef, this);
                largeChunks.Add(currentLargeChunkIndexPos, newChunk);

                newChunk.GenerateLargeChunk(currentLargeChunkIndexPos, worldTracker);

                yield return null;
            }
        }

        isGenerating = false;
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
            this.worldPosition.x - (cgmRef.smallChunkSize / 2),
            this.worldPosition.y - (cgmRef.smallChunkSize / 2)
        );
    }

    public void EnableSmallChunk()
    {
        if (Active) return;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (pair.Value.wallGameObject.activeSelf) continue;
            pair.Value.wallGameObject.SetActive(true);
        }

        Active = true;
    }

    public void DisableSmallChunk()
    {
        if (!Active) return;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (pair.Value.wallGameObject == null || !pair.Value.wallGameObject.activeSelf) continue;
            pair.Value.wallGameObject.SetActive(false);
        }

        Active = false;
    }

    public void LoadChunk()
    {
        if (Exists) return;

        Exists = true;

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            pair.Value.wallGameObject = _parentChunkRef.acgRef.CreateCellGameObject(pair.Key, pair.Value.CellType);
        }
    }

    public void UnloadChunk()
    {
        Debug.Log("unload 0");

        if (!Exists) return;

        Debug.Log($"unload 1, smallchunk cells db: {cells.Count}");

        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            switch (pair.Value.CellType)
            {
                case CellType.Wall:
                    _parentChunkRef.acgRef.DestroyCellGameObject(pair.Value.wallGameObject);
                    break;
                case CellType.LightEmpty:
                    _parentChunkRef.acgRef.DestroyCellGameObject(pair.Value.lightGameObject);
                    break;
            }
        }
        Exists = false;
        Debug.Log("unload 2");
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
        Vector2Int origin = largeChunkWorldPivotPos - new Vector2Int(cgmRef.largeChunkSize / 2, cgmRef.largeChunkSize / 2);
        Vector2Int worldPos = cell.WorldPosition - origin;
        Vector2Int smallChunkKey = worldPos / cgmRef.smallChunkSize;

        this.cells.Add(cell.WorldPosition, cell);
        smallChunks[smallChunkKey].cells.Add(cell.WorldPosition, cell); // key volt az index (mellékes)
    }

    public void GenerateLargeChunk(Vector2Int nextLargeChunkPos, WorldTracker tracker)
    {
        int largeChunkSize = cgmRef.largeChunkSize;

        largeChunkWorldPivotPos = new(
            nextLargeChunkPos.x * largeChunkSize,
            nextLargeChunkPos.y * largeChunkSize
        );

        for (int worldX = Mathf.RoundToInt(largeChunkWorldPivotPos.x - (largeChunkSize / 2)); worldX < largeChunkWorldPivotPos.x + (largeChunkSize / 2); worldX++)
        {
            for (int worldZ = Mathf.RoundToInt(largeChunkWorldPivotPos.y - (largeChunkSize / 2)); worldZ < largeChunkWorldPivotPos.y + (largeChunkSize / 2); worldZ++)
            {
                Vector2Int currentWorldPos = new Vector2Int(worldX, worldZ);
                Vector2Int currentSmallChunkIndexPos = new Vector2Int(
                    Mathf.FloorToInt((float)(worldX - (largeChunkWorldPivotPos.x - largeChunkSize / 2)) / cgmRef.smallChunkSize),
                    Mathf.FloorToInt((float)(worldZ - (largeChunkWorldPivotPos.y - largeChunkSize / 2)) / cgmRef.smallChunkSize)
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

        acgRef.StartCoroutine(acgRef.FillWithWalls(this));
        acgRef.StartCoroutine(acgRef.GenerateMap(acgRef.labirynthCount, this));
        acgRef.StartCoroutine(acgRef.GenerateRooms(30, this)); // Random.Range(12, 18)
        acgRef.StartCoroutine(acgRef.MakePatches(60, this)); // 250

        //areas.Add(acgRef.MakeArea(this));

        /*acgRef.FillWithWalls(this);
        GenerateMap(acgRef.labirynthCount, this);
        acgRef.GenerateRooms(30, this);
        acgRef.MakePatches(15, this);

        // notCategorisedCells feltöltése a generálás után
        notCategorisedCells = 0;
        foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
        {
            if (pair.Value.CellType == CellType.HallwayEmpty)
                notCategorisedCells++;
        }

        Debug.Log($"1, notCategorisedCells amount: {notCategorisedCells}");

        Area newArea = acgRef.MakeArea(this);
        if (newArea != null)
        {
            areas.Add(newArea);

            // notCategorisedCells frissítése
            notCategorisedCells -= newArea.cellMembers.Count;
        }

        Debug.Log($"2, notCategorisedCells amount: {notCategorisedCells}");*/

        // Area-k generálása amíg van kategorizálatlan cella
        /*while (notCategorisedCells > 0)
        {
            Area newArea = acgRef.MakeArea(this);
            if (newArea == null) break;

            areas.Add(newArea);

            // notCategorisedCells frissítése
            notCategorisedCells -= newArea.cellMembers.Count;
        }

        Debug.Log("2");

        foreach (Area area in areas)
        {
            if (area.cellMembers.Count <= 5)
            {
                area.FillWholeArea();
            }
        }*/
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