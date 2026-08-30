using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum CellType
{
    Wall,
    OptimizedWall,
    HallwayEmpty,
    RoomEmpty,
    LightEmpty,
    WallDoor
}

public enum Biome
{
    ScatteredRooms_Lit,
    ScatteredRooms_Dark,
    SemiOpenSpaces
}

public class ACGen : MonoBehaviour
{
    //public int renderDist = 2;
    public Vector2Int center = new Vector2Int(0, 0);

    [SerializeField] private GameObject DEV_Part;

    [Space]

    public int labirynthCount;
    public int roomCount;
    public int patchIterationCount;

    [Space]

    [SerializeField] public BiomeData[] biomes;

    private Dictionary<Biome, BiomeData> _biomeDict;

    [Space]

    /*public Dictionary<Biome, int> _BiomeGenData_LabirynthCount;
    public Dictionary<Biome, int> _BiomeGenData_RoomCount; 
    public Dictionary<Biome, int> _BiomeGenData_PatchIterationCount;

    [SerializeField] Dictionary<Biome, int> _BiomeGenData_RoomMinSizeByBiome;
    [SerializeField] Dictionary<Biome, int> _BiomeGenData_RoomMaxSizeByBiome;

    [SerializeField] Dictionary<Biome, float> _BiomeGenData_stopCollisionProbality;
    [SerializeField] Dictionary<Biome, float> _BiomeGenData_RandomFactorForDoors;

    [SerializeField] Dictionary<Biome, float> _BiomeGenData_LightRandomness;*/

    [Space]

    public int seed = 0;
    public float scale;

    [SerializeField] private int logicalDoorGenerationDepthSearch = 0;
    [SerializeField] private float surroundRoomWithWallChance = .4f;

    private void Start()
    {
        SetAllBiomeDictionaries();

        if (seed == 0) seed = Random.Range(1, 10000);
        Random.InitState(seed);
    }

    public IEnumerator GenerateMap(LargeChunk targetChunk)
    {
        Dictionary<Biome, Dictionary<Vector2Int, Cell>> biomes = GetCellsByBiome(targetChunk);

        foreach (var biomeDataPair in biomes)
        {
            Biome currentBiome = biomeDataPair.Key;
            Dictionary<Vector2Int, Cell> cells = biomeDataPair.Value;

            PrimGeneration(currentBiome, cells, targetChunk);

            yield return null;
        }
    }

    private void PrimGeneration(Biome currentBiome, Dictionary<Vector2Int, Cell> cells, LargeChunk targetChunk)
    {
        List<Vector2Int> visitedCells = new();

        for (int f = 0; f < _biomeDict[currentBiome].labirynthCount; f++)
        {
            int randPosIndex = Random.Range(0, cells.Count);
            Vector2Int parentPos = cells.ElementAt(randPosIndex).Key;

            if (!visitedCells.Contains(parentPos))
                visitedCells.Add(parentPos);

            if (!cells.ContainsKey(parentPos)) continue;

            // generate walkable area

            cells[parentPos].CellType = CellType.HallwayEmpty;
            Destroy(cells[parentPos].wallGameObject); // GENERATE FLOOR!!! IF WALKABLE/ EMPTY

            if (Random.value < _biomeDict[currentBiome].lightChance/* && parentPos.x % 4 == 0 && parentPos.y % 4 == 0*/)
            {
                cells[parentPos].lightGameObject = Instantiate(_lightPart);
                cells[parentPos].lightGameObject.transform.position = new Vector3(
                    cells[parentPos].WorldPosition.x,
                    1,
                    cells[parentPos].WorldPosition.y
                );
                cells[parentPos].CellType = CellType.LightEmpty;
            }

            // start of the main logic

            Dictionary<Vector2Int, Vector2Int> frontier = new();

            foreach (Vector2Int childP in GetNeighbors(parentPos, cells))
            {
                if (frontier.ContainsKey(childP)) continue;
                frontier.Add(childP, parentPos);
            }

            int forwardCells = 0;
            char direction = 'x';

            while (frontier.Count > 0)
            {
                int randChoice = Random.Range(0, frontier.Count - 1);
                Vector2Int childPos = frontier.ElementAt(randChoice).Key;

                /*if (forwardCells < 4)
                {
                    if (direction == 'x')
                    {
                        int attempts = 0;
                        while (frontier[childPos].x != childPos.x && attempts < frontier.Count)
                        {
                            randChoice = Random.Range(0, frontier.Count - 1);
                            childPos = frontier.ElementAt(randChoice).Key;
                            attempts++;
                        }
                    }
                    else if (direction == 'y')
                    {
                        int attempts = 0;
                        while (frontier[childPos].y != childPos.y && attempts < frontier.Count)
                        {
                            randChoice = Random.Range(0, frontier.Count - 1);
                            childPos = frontier.ElementAt(randChoice).Key;
                            attempts++;
                        }
                    }
                }
                else
                {
                    if (frontier[childPos].x > childPos.x || frontier[childPos].x < childPos.x)
                    {
                        direction = 'y';
                        forwardCells = 0;
                    }
                    else if (frontier[childPos].y > childPos.y || frontier[childPos].y < childPos.y)
                    {
                        direction = 'x';
                        forwardCells = 0;
                    }
                }*/

                Vector2Int currentDir = Vector2Int.zero;

                if (parentPos.x == childPos.x && parentPos.y == childPos.y)
                    currentDir = new Vector2Int(0, 0);
                else if (parentPos.x > childPos.x && parentPos.y == childPos.y)
                    currentDir = new Vector2Int(1, 0);
                else if (parentPos.x > childPos.x && parentPos.y > childPos.y)
                    currentDir = new Vector2Int(1, 1);
                else if (parentPos.x > childPos.x && parentPos.y < childPos.y)
                    currentDir = new Vector2Int(1, -1);
                else if (parentPos.x < childPos.x && parentPos.y == childPos.y)
                    currentDir = new Vector2Int(-1, 0);
                else if (parentPos.x < childPos.x && parentPos.y > childPos.y)
                    currentDir = new Vector2Int(-1, 1);
                else if (parentPos.x < childPos.x && parentPos.y < childPos.y)
                    currentDir = new Vector2Int(-1, -1);

                List<Vector2Int> sameDirectionCandidates = frontier
                   .Where(kvp =>
                   {
                       Vector2Int diff = kvp.Key - kvp.Value; // child - parent = irány
                       return (diff.x != 0) == (currentDir.x != 0); // ugyanaz a tengely
                   })
                   .Select(kvp => kvp.Key)
                   .ToList();

                if (forwardCells < 10 && sameDirectionCandidates.Count > 0)
                    childPos = sameDirectionCandidates[Random.Range(0, sameDirectionCandidates.Count)];
                else
                    childPos = frontier.ElementAt(Random.Range(0, frontier.Count - 1)).Key;


                if (Random.value < _biomeDict[currentBiome].stopCollisionProbability)
                {
                    frontier.Clear();
                    continue;
                }

                if (cells[childPos].CellType == CellType.Wall)
                {
                    cells[childPos].CellType = CellType.HallwayEmpty;
                    Destroy(cells[childPos].wallGameObject);
                    targetChunk.notCategorisedCells++; // ? which way is not categorized in the LargeChunk?? Could be connected to biomes?? Same for the all of these lines coming up

                    Vector2Int doorwayPos = new();

                    if (frontier[childPos].y > childPos.y && frontier[childPos].x == childPos.x)
                    {
                        doorwayPos = new Vector2Int(childPos.x, childPos.y + 1);

                        if (cells.ContainsKey(doorwayPos) && cells[doorwayPos].CellType == CellType.Wall && Random.value < _biomeDict[currentBiome].randomFactorForDoors)
                        {
                            cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(cells[doorwayPos].wallGameObject);
                            targetChunk.notCategorisedCells++;
                        }
                    }
                    else if (frontier[childPos].y < childPos.y && frontier[childPos].x == childPos.x)
                    {
                        doorwayPos = new Vector2Int(childPos.x, childPos.y - 1);

                        if (cells.ContainsKey(doorwayPos) && cells[doorwayPos].CellType == CellType.Wall && Random.value < _biomeDict[currentBiome].randomFactorForDoors)
                        {
                            cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(cells[doorwayPos].wallGameObject);
                            targetChunk.notCategorisedCells++;
                        }
                    }
                    else if (frontier[childPos].x > childPos.x && frontier[childPos].y == childPos.y)
                    {
                        doorwayPos = new Vector2Int(childPos.x + 1, childPos.y);

                        if (cells.ContainsKey(doorwayPos) && cells[doorwayPos].CellType == CellType.Wall && Random.value < _biomeDict[currentBiome].randomFactorForDoors)
                        {
                            cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(cells[doorwayPos].wallGameObject);
                            targetChunk.notCategorisedCells++;
                        }
                    }
                    else if (frontier[childPos].x < childPos.x && frontier[childPos].y == childPos.y)
                    {
                        doorwayPos = new Vector2Int(childPos.x - 1, childPos.y);

                        if (cells.ContainsKey(doorwayPos) && cells[doorwayPos].CellType == CellType.Wall && Random.value < _biomeDict[currentBiome].randomFactorForDoors)
                        {
                            cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(cells[doorwayPos].wallGameObject);
                            targetChunk.notCategorisedCells++;
                        }
                    }

                    List<Vector2Int> neighborList = new();

                    foreach (Vector2Int childP in GetNeighbors(childPos, cells))
                    {
                        if (neighborList.Contains(childP)) continue;
                        neighborList.Add(childP);
                    }

                    int randomIndex = Random.Range(0, neighborList.Count - 1);

                    if (neighborList.Count > 0 && !frontier.ContainsKey(neighborList[randomIndex]))
                    {
                        frontier.Add(neighborList[randomIndex], childPos);
                    }

                    frontier.Remove(childPos);
                }
            }
        }
    }

    public IEnumerator MakePatches(LargeChunk targetChunk)
    {
        Dictionary<Biome, Dictionary<Vector2Int, Cell>> biomes = GetCellsByBiome(targetChunk);


        foreach (var biomeDataPair in biomes)
        {
            Biome currentBiome = biomeDataPair.Key;
            Dictionary<Vector2Int, Cell> cells = biomeDataPair.Value;

            List<Cell> toMakeWall = new();
            List<Cell> toMakeEmpty = new();

            for (int _ = 0; _ < _biomeDict[currentBiome].patchIterationCount; _++)
            {
                foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
                {
                    Cell currentCell = pair.Value;
                    int wallNeighbourCount = GetWallNeighbors(pair.Key, targetChunk).Count;

                    if (currentCell.CellType == CellType.Wall && wallNeighbourCount == 0)
                    {
                        toMakeEmpty.Add(currentCell);
                        //Debug.Log($"key: {pair.Key}, celltype: {currentCell.CellType}, falak száma körülötte: {wallNeighbourCount}", currentCell.wallGameObject);
                    }

                    /*if (currentCell.CellType == CellType.Wall && GetWeight(2, currentCell, targetChunk) <= 1 && wallNeighbourCount <= 1)
                    {
                        toMakeEmpty.Add(currentCell);
                    }*/

                    /*if (currentCell.CellType == CellType.HallwayEmpty && wallNeighbourCount >= 3)
                        toMakeWall.Add(currentCell);*/
                }
            }

            foreach (Cell cell in toMakeEmpty)
            {
                cell.CellType = CellType.HallwayEmpty;
                Destroy(cell.wallGameObject);
                cell.wallGameObject = null;
            }

            yield return null;
        }
    }

    public IEnumerator OptimiseWalls(LargeChunk targetLargeChunk)
    {
        List<Cell> cellsToDestroy = new();

        foreach (KeyValuePair<Vector2Int, Cell> pair in targetLargeChunk.cells)
        {
            Cell currentCell = pair.Value;

            if (currentCell.CellType != CellType.Wall || GetWallNeighbors(pair.Key, targetLargeChunk).Count < 4 || currentCell.wallGameObject == null || cellsToDestroy.Contains(currentCell)) continue;

            cellsToDestroy.Add(currentCell);
        }

        foreach (Cell c in cellsToDestroy)
        {
            Destroy(c.wallGameObject);
            c.CellType = CellType.OptimizedWall;
        }

        yield return null;
    }

    private List<Vector2Int> GetWallNeighbors(Vector2Int pos, LargeChunk lc)
    {
        List<Vector2Int> neighbors = new();

        Vector2Int currentPos = new Vector2Int(pos.x + 1, pos.y);
        if (lc.cells.ContainsKey(currentPos) && lc.cells[currentPos].CellType == CellType.Wall)
        {
            neighbors.Add(currentPos);
        }

        currentPos = new Vector2Int(pos.x, pos.y + 1);
        if (lc.cells.ContainsKey(currentPos) && lc.cells[currentPos].CellType == CellType.Wall)
        {
            neighbors.Add(currentPos);
        }

        currentPos = new Vector2Int(pos.x - 1, pos.y);
        if (lc.cells.ContainsKey(currentPos) && lc.cells[currentPos].CellType == CellType.Wall)
        {
            neighbors.Add(currentPos);
        }

        currentPos = new Vector2Int(pos.x, pos.y - 1);
        if (lc.cells.ContainsKey(currentPos) && lc.cells[currentPos].CellType == CellType.Wall)
        {
            neighbors.Add(currentPos);
        }

        return neighbors;
    }

    public IEnumerator GenerateRooms(LargeChunk targetChunk)
    {
        Dictionary<Biome, Dictionary<Vector2Int, Cell>> biomes = GetCellsByBiome(targetChunk);
        List<Cell> doorWays = new();

        foreach (var biomeDataPair in biomes)
        {
            Biome currentBiome = biomeDataPair.Key;
            Dictionary<Vector2Int, Cell> cells = biomeDataPair.Value; // INDEX POS

            List<Vector2Int> emptyParts = new();

            foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
            {
                if (emptyParts.Contains(pair.Key)) continue;
                emptyParts.Add(pair.Key);
            }

            for (int f = 0; f < _biomeDict[currentBiome].roomCount; f++)
            {
                Vector2Int centerPos = emptyParts[Random.Range(0, emptyParts.Count - 1)];


                int roomSizeX = Random.Range(_biomeDict[currentBiome].roomMinSize, _biomeDict[currentBiome].roomMaxSize);
                int roomSizeY = Random.Range(_biomeDict[currentBiome].roomMinSize, _biomeDict[currentBiome].roomMaxSize);

                //Debug.Log($"centerPosition: {centerPos}, roomSizeX: {roomSizeX}, Y: {roomSizeY}");

                for (int roomX = centerPos.x - (roomSizeX / 2) /*+ 1*/; roomX < centerPos.x + (roomSizeX / 2) /*- 1*/; roomX++)
                {
                    for (int roomY = centerPos.y - (roomSizeY / 2) /*+ 1*/; roomY < centerPos.y + (roomSizeY / 2) /*- 1*/; roomY++)
                    {
                        Vector2Int roomPos = new Vector2Int(roomX, roomY);

                        //Debug.Log($"roomPos: {roomPos}");

                        if (!cells.ContainsKey(roomPos)) continue;

                        cells[roomPos].CellType = CellType.RoomEmpty;
                        Destroy(cells[roomPos].wallGameObject);

                        if (roomPos.x % 2 == 0 && roomPos.y % 2 == 0 && Random.value < _biomeDict[currentBiome].lightChance)
                        {
                            bool canGen = true;

                            foreach (var pos in GetNeighbors(cells[roomPos].WorldPosition, cells))
                            {
                                if (!cells.ContainsKey(pos)) continue;

                                if (cells[pos].CellType == CellType.LightEmpty)
                                {
                                    canGen = false;
                                    break;
                                }
                            }

                            if (canGen)
                            {
                                cells[roomPos].CellType = CellType.LightEmpty;
                                cells[roomPos].lightGameObject = Instantiate(_lightPart);
                                cells[roomPos].lightGameObject.transform.position = new Vector3(
                                    cells[roomPos].WorldPosition.x,
                                    1,
                                    cells[roomPos].WorldPosition.y
                                );
                            }
                        }

                        if (roomX == centerPos.x - roomSizeX / 2)
                        {
                            Vector2Int doorwayCheckPos = new Vector2Int(roomX - 1, roomY);
                            bool canGenerateDoor = false; // we check if there isN't any wall behind the door (search depth determined by 'logicalDoorGenerationDepthSearch'). If there is then we just don't generate it.

                            if (!cells.ContainsKey(doorwayCheckPos)) continue;

                            if (cells[doorwayCheckPos].CellType != CellType.Wall && cells[doorwayCheckPos].CellType != CellType.WallDoor)
                            {
                                for (int i = 1; i < logicalDoorGenerationDepthSearch; i++)
                                {
                                    Vector2Int doorwayBehindCheckPos = new Vector2Int(roomX - i, roomY);

                                    if (!cells.ContainsKey(doorwayBehindCheckPos) || cells[doorwayBehindCheckPos].CellType != CellType.HallwayEmpty) 
                                        break;

                                    if (i == logicalDoorGenerationDepthSearch - 1)
                                        canGenerateDoor = true;
                                }

                                Vector2Int doorwayInFrontOfCheckPos = new Vector2Int(roomX + 1, roomY);
                                if (!cells.ContainsKey(doorwayInFrontOfCheckPos) || cells[doorwayInFrontOfCheckPos].CellType == CellType.Wall || cells[doorwayInFrontOfCheckPos].CellType == CellType.WallDoor)
                                    canGenerateDoor = false;

                                if (Random.value < surroundRoomWithWallChance)
                                    canGenerateDoor = false;

                                if (!canGenerateDoor) // if we can't generate the door, but the small chance succeeds then we place a wall instead
                                {
                                    bool canGenWall = false;

                                    Vector2Int wallOffCheck1 = new Vector2Int(doorwayCheckPos.x + 1, doorwayCheckPos.y);
                                    Vector2Int wallOffCheck2 = new Vector2Int(doorwayCheckPos.x - 1, doorwayCheckPos.y);
                                    if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                    {
                                        canGenWall = true;
                                    }

                                    if (canGenWall)
                                    {
                                        cells[doorwayCheckPos].CellType = CellType.Wall;
                                        cells[doorwayCheckPos].wallGameObject = Instantiate(_wallPart);
                                        cells[doorwayCheckPos].wallGameObject.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 1, cells[doorwayCheckPos].WorldPosition.y);

                                        if (cells[doorwayCheckPos].lightGameObject != null) Destroy(cells[doorwayCheckPos].lightGameObject);

                                        /*GameObject tempGm = Instantiate(DEV_Part);
                                        tempGm.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 5, cells[doorwayCheckPos].WorldPosition.y);*/
                                    }
                                }
                                else
                                {
                                    bool canGenDoor = false;

                                    Vector2Int wallOffCheck1 = new Vector2Int(doorwayCheckPos.x + 1, doorwayCheckPos.y);
                                    Vector2Int wallOffCheck2 = new Vector2Int(doorwayCheckPos.x - 1, doorwayCheckPos.y);
                                    if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                    {
                                        canGenDoor = true;
                                    }

                                    if (canGenDoor)
                                    {
                                        cells[doorwayCheckPos].CellType = CellType.WallDoor;
                                        cells[doorwayCheckPos].doorGameObject = Instantiate(_doorPart);
                                        cells[doorwayCheckPos].doorGameObject.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 1, cells[doorwayCheckPos].WorldPosition.y);
                                        cells[doorwayCheckPos].doorRotation = cells[doorwayCheckPos].doorGameObject.transform.rotation;

                                        if (cells[doorwayCheckPos].lightGameObject != null) Destroy(cells[doorwayCheckPos].lightGameObject);
                                    }

                                    // sorrounding the door with walls

                                    Vector2Int wallDoorFrame = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y - 1);

                                    if (cells.ContainsKey(wallDoorFrame) && cells[wallDoorFrame].CellType != CellType.Wall && cells[wallDoorFrame].CellType != CellType.WallDoor)
                                    {
                                        wallOffCheck1 = new Vector2Int(wallDoorFrame.x + 1, doorwayCheckPos.y);
                                        wallOffCheck2 = new Vector2Int(wallDoorFrame.x - 1, doorwayCheckPos.y);
                                        if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                        {
                                            cells[wallDoorFrame].CellType = CellType.Wall;
                                            cells[wallDoorFrame].doorGameObject = Instantiate(_wallPart);
                                            cells[wallDoorFrame].doorGameObject.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 1, cells[wallDoorFrame].WorldPosition.y);

                                            GameObject tempGmee = Instantiate(DEV_Part);
                                            tempGmee.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 5, cells[wallDoorFrame].WorldPosition.y);
                                        }
                                    }

                                    wallDoorFrame = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y + 1);

                                    if (cells.ContainsKey(wallDoorFrame) && cells[wallDoorFrame].CellType != CellType.Wall && cells[wallDoorFrame].CellType != CellType.WallDoor)
                                    {
                                        wallOffCheck1 = new Vector2Int(wallDoorFrame.x + 1, doorwayCheckPos.y);
                                        wallOffCheck2 = new Vector2Int(wallDoorFrame.x - 1, doorwayCheckPos.y);
                                        if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                        {
                                            cells[wallDoorFrame].CellType = CellType.Wall;
                                            cells[wallDoorFrame].doorGameObject = Instantiate(_wallPart);
                                            cells[wallDoorFrame].doorGameObject.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 1, cells[wallDoorFrame].WorldPosition.y);

                                            GameObject tempGmee = Instantiate(DEV_Part);
                                            tempGmee.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 5, cells[wallDoorFrame].WorldPosition.y);
                                        }
                                    }


                                    GameObject tempGm = Instantiate(DEV_Part);
                                    tempGm.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 5, cells[doorwayCheckPos].WorldPosition.y);
                                }
                            }
                        }
                        else if (roomX == (centerPos.x + roomSizeX / 2))
                        {
                            Vector2Int doorwayCheckPos = new Vector2Int(roomX + 1, roomY);
                            bool canGenerateDoor = false;

                            if (!cells.ContainsKey(doorwayCheckPos)) continue;

                            if (cells[doorwayCheckPos].CellType != CellType.Wall && cells[doorwayCheckPos].CellType != CellType.WallDoor)
                            {
                                for (int i = 1; i < logicalDoorGenerationDepthSearch; i++)
                                {
                                    Vector2Int doorwayBehindCheckPos = new Vector2Int(roomX + i, roomY);

                                    if (!cells.ContainsKey(doorwayBehindCheckPos) || cells[doorwayBehindCheckPos].CellType != CellType.HallwayEmpty) break;

                                    if (i == logicalDoorGenerationDepthSearch - 1)
                                        canGenerateDoor = true;
                                }

                                Vector2Int doorwayInFrontOfCheckPos = new Vector2Int(roomX - 1, roomY);
                                if (!cells.ContainsKey(doorwayInFrontOfCheckPos) || cells[doorwayInFrontOfCheckPos].CellType == CellType.Wall || cells[doorwayInFrontOfCheckPos].CellType == CellType.WallDoor) canGenerateDoor = false;

                                if (Random.value < surroundRoomWithWallChance)
                                    canGenerateDoor = false;

                                if (!canGenerateDoor)
                                {
                                    bool canGenWall = false;

                                    Vector2Int wallOffCheck1 = new Vector2Int(doorwayCheckPos.x + 1, doorwayCheckPos.y);
                                    Vector2Int wallOffCheck2 = new Vector2Int(doorwayCheckPos.x - 1, doorwayCheckPos.y);
                                    if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                    {
                                        canGenWall = true;
                                    }

                                    if (canGenWall)
                                    {
                                        cells[doorwayCheckPos].CellType = CellType.Wall;
                                        cells[doorwayCheckPos].wallGameObject = Instantiate(_wallPart);
                                        cells[doorwayCheckPos].wallGameObject.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 1, cells[doorwayCheckPos].WorldPosition.y);
                                        cells[doorwayCheckPos].doorRotation = cells[doorwayCheckPos].wallGameObject.transform.rotation;

                                        if (cells[doorwayCheckPos].lightGameObject != null) Destroy(cells[doorwayCheckPos].lightGameObject);

                                        /*GameObject tempGm = Instantiate(DEV_Part);
                                        tempGm.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 5, cells[doorwayCheckPos].WorldPosition.y);*/
                                    }
                                }
                                else
                                {
                                    bool canGenDoor = false;

                                    Vector2Int wallOffCheck1 = new Vector2Int(doorwayCheckPos.x + 1, doorwayCheckPos.y);
                                    Vector2Int wallOffCheck2 = new Vector2Int(doorwayCheckPos.x - 1, doorwayCheckPos.y);
                                    if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                    {
                                        canGenDoor = true;
                                    }

                                    if (canGenDoor)
                                    {
                                        cells[doorwayCheckPos].CellType = CellType.WallDoor;
                                        cells[doorwayCheckPos].doorGameObject = Instantiate(_doorPart);
                                        cells[doorwayCheckPos].doorGameObject.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 1, cells[doorwayCheckPos].WorldPosition.y);

                                        if (cells[doorwayCheckPos].lightGameObject != null) Destroy(cells[doorwayCheckPos].lightGameObject);
                                    }

                                    // sorrounding the door with walls

                                    Vector2Int wallDoorFrame = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y - 1);

                                    if (cells.ContainsKey(wallDoorFrame) && cells[wallDoorFrame].CellType != CellType.Wall && cells[wallDoorFrame].CellType != CellType.WallDoor)
                                    {
                                        wallOffCheck1 = new Vector2Int(wallDoorFrame.x + 1, doorwayCheckPos.y);
                                        wallOffCheck2 = new Vector2Int(wallDoorFrame.x - 1, doorwayCheckPos.y);
                                        if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                        {
                                            cells[wallDoorFrame].CellType = CellType.Wall;
                                            cells[wallDoorFrame].doorGameObject = Instantiate(_wallPart);
                                            cells[wallDoorFrame].doorGameObject.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 1, cells[wallDoorFrame].WorldPosition.y);

                                            /*GameObject tempGmee = Instantiate(DEV_Part);
                                            tempGmee.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 5, cells[wallDoorFrame].WorldPosition.y);*/
                                        }
                                    }

                                    wallDoorFrame = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y + 1);

                                    if (cells.ContainsKey(wallDoorFrame) && cells[wallDoorFrame].CellType != CellType.Wall && cells[wallDoorFrame].CellType != CellType.WallDoor)
                                    {
                                        wallOffCheck1 = new Vector2Int(wallDoorFrame.x + 1, doorwayCheckPos.y);
                                        wallOffCheck2 = new Vector2Int(wallDoorFrame.x - 1, doorwayCheckPos.y);
                                        if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                        {
                                            cells[wallDoorFrame].CellType = CellType.Wall;
                                            cells[wallDoorFrame].doorGameObject = Instantiate(_wallPart);
                                            cells[wallDoorFrame].doorGameObject.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 1, cells[wallDoorFrame].WorldPosition.y);

                                            /*GameObject tempGmee = Instantiate(DEV_Part);
                                            tempGmee.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 5, cells[wallDoorFrame].WorldPosition.y);*/
                                        }
                                    }


                                    GameObject tempGm = Instantiate(DEV_Part);
                                    tempGm.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 5, cells[doorwayCheckPos].WorldPosition.y);
                                }
                            }
                        }
                        else if (roomY == centerPos.y - roomSizeY / 2)
                        {
                            Vector2Int doorwayCheckPos = new Vector2Int(roomX, roomY - 1);
                            bool canGenerateDoor = false;

                            if (!cells.ContainsKey(doorwayCheckPos)) continue;

                            if (cells[doorwayCheckPos].CellType != CellType.Wall && cells[doorwayCheckPos].CellType != CellType.WallDoor)
                            {
                                for (int i = 1; i < logicalDoorGenerationDepthSearch; i++)
                                {
                                    Vector2Int doorwayBehindCheckPos = new Vector2Int(roomX, roomY - i);

                                    if (!cells.ContainsKey(doorwayBehindCheckPos) || cells[doorwayBehindCheckPos].CellType != CellType.HallwayEmpty) break;

                                    if (i == logicalDoorGenerationDepthSearch - 1)
                                        canGenerateDoor = true;
                                }

                                Vector2Int doorwayInFrontOfCheckPos = new Vector2Int(roomX, roomY + 1);
                                if (!cells.ContainsKey(doorwayInFrontOfCheckPos) || cells[doorwayInFrontOfCheckPos].CellType == CellType.Wall || cells[doorwayInFrontOfCheckPos].CellType == CellType.WallDoor)
                                    canGenerateDoor = false;

                                if (Random.value < surroundRoomWithWallChance)
                                    canGenerateDoor = false;

                                if (!canGenerateDoor)
                                {
                                    bool canGenWall = false;

                                    Vector2Int wallOffCheck1 = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y + 1);
                                    Vector2Int wallOffCheck2 = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y - 1);
                                    if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                    {
                                        canGenWall = true;
                                    }

                                    if (canGenWall)
                                    {
                                        cells[doorwayCheckPos].CellType = CellType.Wall;
                                        cells[doorwayCheckPos].wallGameObject = Instantiate(_wallPart);
                                        cells[doorwayCheckPos].wallGameObject.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 1, cells[doorwayCheckPos].WorldPosition.y);

                                        if (cells[doorwayCheckPos].lightGameObject != null) Destroy(cells[doorwayCheckPos].lightGameObject);

                                        /*GameObject tempGm = Instantiate(DEV_Part);
                                        tempGm.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 5, cells[doorwayCheckPos].WorldPosition.y);*/
                                    }
                                }
                                else
                                {
                                    bool canGenDoor = false;

                                    Vector2Int wallOffCheck1 = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y + 1);
                                    Vector2Int wallOffCheck2 = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y - 1);
                                    if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                    {
                                        canGenDoor = true;
                                    }

                                    if (canGenDoor)
                                    {
                                        cells[doorwayCheckPos].CellType = CellType.WallDoor;
                                        cells[doorwayCheckPos].doorGameObject = Instantiate(_doorPart);
                                        cells[doorwayCheckPos].doorGameObject.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 1, cells[doorwayCheckPos].WorldPosition.y);
                                        cells[doorwayCheckPos].doorGameObject.transform.rotation =
                                            Quaternion.Euler(
                                                cells[doorwayCheckPos].doorGameObject.transform.rotation.x,
                                                90f,
                                                cells[doorwayCheckPos].doorGameObject.transform.rotation.y
                                            );
                                        cells[doorwayCheckPos].doorRotation =
                                            Quaternion.Euler(
                                                cells[doorwayCheckPos].doorGameObject.transform.rotation.x,
                                                90f,
                                                cells[doorwayCheckPos].doorGameObject.transform.rotation.y
                                            );

                                        if (cells[doorwayCheckPos].lightGameObject != null) Destroy(cells[doorwayCheckPos].lightGameObject);
                                    }

                                    // sorrounding the door with walls

                                    Vector2Int wallDoorFrame = new Vector2Int(doorwayCheckPos.x - 1, doorwayCheckPos.y);

                                    if (cells.ContainsKey(wallDoorFrame) && cells[wallDoorFrame].CellType != CellType.Wall && cells[wallDoorFrame].CellType != CellType.WallDoor)
                                    {
                                        wallOffCheck1 = new Vector2Int(wallDoorFrame.x + 1, doorwayCheckPos.y);
                                        wallOffCheck2 = new Vector2Int(wallDoorFrame.x - 1, doorwayCheckPos.y);
                                        if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                        {
                                            cells[wallDoorFrame].CellType = CellType.Wall;
                                            cells[wallDoorFrame].doorGameObject = Instantiate(_wallPart);
                                            cells[wallDoorFrame].doorGameObject.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 1, cells[wallDoorFrame].WorldPosition.y);

                                            /*GameObject tempGmee = Instantiate(DEV_Part);
                                            tempGmee.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 5, cells[wallDoorFrame].WorldPosition.y);*/
                                        }
                                    }

                                    wallDoorFrame = new Vector2Int(doorwayCheckPos.x + 1, doorwayCheckPos.y);

                                    if (cells.ContainsKey(wallDoorFrame) && cells[wallDoorFrame].CellType != CellType.Wall && cells[wallDoorFrame].CellType != CellType.WallDoor)
                                    {
                                        wallOffCheck1 = new Vector2Int(wallDoorFrame.x + 1, doorwayCheckPos.y);
                                        wallOffCheck2 = new Vector2Int(wallDoorFrame.x - 1, doorwayCheckPos.y);
                                        if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                        {
                                            cells[wallDoorFrame].CellType = CellType.Wall;
                                            cells[wallDoorFrame].doorGameObject = Instantiate(_wallPart);
                                            cells[wallDoorFrame].doorGameObject.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 1, cells[wallDoorFrame].WorldPosition.y);

                                            /*GameObject tempGmee = Instantiate(DEV_Part);
                                            tempGmee.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 5, cells[wallDoorFrame].WorldPosition.y);*/
                                        }
                                    }


                                    GameObject tempGm = Instantiate(DEV_Part);
                                    tempGm.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 5, cells[doorwayCheckPos].WorldPosition.y);
                                }
                            }
                        }
                        else if (roomY == (centerPos.y + roomSizeY / 2) /*- 1*/)
                        {
                            Vector2Int doorwayCheckPos = new Vector2Int(roomX, roomY + 1);
                            bool canGenerateDoor = false;

                            if (!cells.ContainsKey(doorwayCheckPos)) continue;

                            if (cells[doorwayCheckPos].CellType != CellType.Wall && cells[doorwayCheckPos].CellType != CellType.WallDoor)
                            {
                                for (int i = 1; i < logicalDoorGenerationDepthSearch; i++)
                                {
                                    Vector2Int doorwayBehindCheckPos = new Vector2Int(roomX, roomY + i);

                                    if (!cells.ContainsKey(doorwayBehindCheckPos) || cells[doorwayBehindCheckPos].CellType != CellType.HallwayEmpty) break;

                                    if (i == logicalDoorGenerationDepthSearch - 1)
                                        canGenerateDoor = true;
                                }

                                Vector2Int doorwayInFrontOfCheckPos = new Vector2Int(roomX, roomY - 1);
                                if (!cells.ContainsKey(doorwayInFrontOfCheckPos) || cells[doorwayInFrontOfCheckPos].CellType == CellType.Wall || cells[doorwayInFrontOfCheckPos].CellType == CellType.WallDoor)
                                    canGenerateDoor = false;

                                if (Random.value < surroundRoomWithWallChance)
                                    canGenerateDoor = false;

                                if (!canGenerateDoor)
                                {
                                    bool canGenWall = false;

                                    Vector2Int wallOffCheck1 = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y + 1);
                                    Vector2Int wallOffCheck2 = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y - 1);
                                    if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                    {
                                        canGenWall = true;
                                    }

                                    if (canGenWall)
                                    {
                                        cells[doorwayCheckPos].CellType = CellType.Wall;
                                        cells[doorwayCheckPos].wallGameObject = Instantiate(_wallPart);
                                        cells[doorwayCheckPos].wallGameObject.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 1, cells[doorwayCheckPos].WorldPosition.y);

                                        if (cells[doorwayCheckPos].lightGameObject != null) Destroy(cells[doorwayCheckPos].lightGameObject);

                                        /*GameObject tempGm = Instantiate(DEV_Part);
                                        tempGm.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 5, cells[doorwayCheckPos].WorldPosition.y);*/
                                    }
                                }
                                else
                                {
                                    bool canGenDoor = false;

                                    Vector2Int wallOffCheck1 = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y + 1);
                                    Vector2Int wallOffCheck2 = new Vector2Int(doorwayCheckPos.x, doorwayCheckPos.y - 1);
                                    if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                    {
                                        canGenDoor = true;
                                    }

                                    if (canGenDoor)
                                    {
                                        cells[doorwayCheckPos].CellType = CellType.WallDoor;
                                        cells[doorwayCheckPos].doorGameObject = Instantiate(_doorPart);
                                        cells[doorwayCheckPos].doorGameObject.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 1, cells[doorwayCheckPos].WorldPosition.y);
                                        cells[doorwayCheckPos].doorGameObject.transform.rotation =
                                            Quaternion.Euler(
                                                cells[doorwayCheckPos].doorGameObject.transform.rotation.x,
                                                90f,
                                                cells[doorwayCheckPos].doorGameObject.transform.rotation.y
                                            );
                                        cells[doorwayCheckPos].doorRotation =
                                            Quaternion.Euler(
                                                cells[doorwayCheckPos].doorGameObject.transform.rotation.x,
                                                90f,
                                                cells[doorwayCheckPos].doorGameObject.transform.rotation.y
                                            );

                                        if (cells[doorwayCheckPos].lightGameObject != null) Destroy(cells[doorwayCheckPos].lightGameObject);
                                    }

                                    // sorrounding the door with walls

                                    Vector2Int wallDoorFrame = new Vector2Int(doorwayCheckPos.x + 1, doorwayCheckPos.y);

                                    if (cells.ContainsKey(wallDoorFrame) && cells[wallDoorFrame].CellType != CellType.Wall && cells[wallDoorFrame].CellType != CellType.WallDoor)
                                    {
                                        wallOffCheck1 = new Vector2Int(wallDoorFrame.x + 1, doorwayCheckPos.y);
                                        wallOffCheck2 = new Vector2Int(wallDoorFrame.x - 1, doorwayCheckPos.y);
                                        if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                        {
                                            cells[wallDoorFrame].CellType = CellType.Wall;
                                            cells[wallDoorFrame].doorGameObject = Instantiate(_wallPart);
                                            cells[wallDoorFrame].doorGameObject.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 1, cells[wallDoorFrame].WorldPosition.y);

                                            /*GameObject tempGmee = Instantiate(DEV_Part);
                                            tempGmee.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 5, cells[wallDoorFrame].WorldPosition.y);*/
                                        }
                                    }

                                    wallDoorFrame = new Vector2Int(doorwayCheckPos.x - 1, doorwayCheckPos.y);

                                    if (cells.ContainsKey(wallDoorFrame) && cells[wallDoorFrame].CellType != CellType.Wall && cells[wallDoorFrame].CellType != CellType.WallDoor)
                                    {
                                        wallOffCheck1 = new Vector2Int(wallDoorFrame.x + 1, doorwayCheckPos.y);
                                        wallOffCheck2 = new Vector2Int(wallDoorFrame.x - 1, doorwayCheckPos.y);
                                        if (cells.ContainsKey(wallOffCheck1) && cells.ContainsKey(wallOffCheck2) && cells[wallOffCheck1].CellType != CellType.Wall && cells[wallOffCheck2].CellType != CellType.Wall && cells[wallOffCheck1].CellType != CellType.WallDoor && cells[wallOffCheck2].CellType != CellType.WallDoor)
                                        {
                                            cells[wallDoorFrame].CellType = CellType.Wall;
                                            cells[wallDoorFrame].doorGameObject = Instantiate(_wallPart);
                                            cells[wallDoorFrame].doorGameObject.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 1, cells[wallDoorFrame].WorldPosition.y);

                                            /*GameObject tempGmee = Instantiate(DEV_Part);
                                            tempGmee.transform.position = new Vector3(cells[wallDoorFrame].WorldPosition.x, 5, cells[wallDoorFrame].WorldPosition.y);*/
                                        }
                                    }


                                    GameObject tempGm = Instantiate(DEV_Part);
                                    tempGm.transform.position = new Vector3(cells[doorwayCheckPos].WorldPosition.x, 5, cells[doorwayCheckPos].WorldPosition.y);
                                }
                            }
                        }
                    }
                }
            }

            yield return null;
        }
    }

    /*public void GenerateWallBlocks(int wallBlocksCount)
    {
        List<Vector2Int> emptyParts = new();

        foreach (KeyValuePair<Vector2Int, CellType> pair in map)
        {
            if (emptyParts.Contains(pair.Key)) continue;
            emptyParts.Add(pair.Key);
        }

        for (int f = 0; f < wallBlocksCount; f++)
        {
            Vector2Int centerPos = emptyParts[Random.Range(0, emptyParts.Count - 1)];

            int roomSizeX = Random.Range(randomRoomMinSize, randomRoomMaxSize);
            int roomSizeY = Random.Range(randomRoomMinSize, randomRoomMaxSize);

            for (int roomX = centerPos.x - (Mathf.RoundToInt(roomSizeX / 2)); roomX < centerPos.x + (Mathf.RoundToInt(roomSizeX / 2)); roomX++)
            {
                for (int roomY = centerPos.y - (Mathf.RoundToInt(roomSizeY / 2)); roomY < centerPos.y + (Mathf.RoundToInt(roomSizeY / 2)); roomY++)
                {
                    Vector2Int roomPos = new Vector2Int(roomX, roomY);

                    if (!world.ContainsKey(roomPos)) continue;

                    map[roomPos] = CellType.Wall;
                    world[roomPos] = Instantiate(_wallPart);

                    world[roomPos].transform.position = new Vector3(
                        (roomPos.x * 2),
                        1,
                        (roomPos.y * 2)
                    );
                }
            }

     
        }
    }*/

    public IEnumerator FillWithWalls(LargeChunk largeChunk)
    {
        for (int x = Mathf.RoundToInt(largeChunk.largeChunkWorldPivotPos.x - (cgm.largeChunkSize / 2)); x < Mathf.RoundToInt(largeChunk.largeChunkWorldPivotPos.x + (cgm.largeChunkSize / 2)); x++)
        {
            for (int z = Mathf.RoundToInt(largeChunk.largeChunkWorldPivotPos.y - (cgm.largeChunkSize / 2)); z < Mathf.RoundToInt(largeChunk.largeChunkWorldPivotPos.y + (cgm.largeChunkSize / 2)); z++)
            {
                Vector2Int currentIndexPos = new Vector2Int(x, z);
                Vector2Int currentWorldPos = new Vector2Int(x * cgm.cellSize, z * cgm.cellSize);

                if (cgm.cellWorld.ContainsKey(currentIndexPos)) continue;

                Cell newCell = new(CellType.Wall, currentWorldPos);
                largeChunk.AddCell(newCell);

                /*float noise = Mathf.PerlinNoise(
                    (newCell.WorldPosition.x * scale) + seed,
                    (newCell.WorldPosition.y * scale) + seed
                ) +
                Mathf.PerlinNoise(
                    ((newCell.WorldPosition.x * scale) + seed) * 2,
                    ((newCell.WorldPosition.y * scale) + seed) * 2
                ) * 0.5f +
                Mathf.PerlinNoise(
                    ((newCell.WorldPosition.x * scale) + seed) * 4,
                    ((newCell.WorldPosition.y * scale) + seed) * 4
                ) * 0.25f;*/

                float noise = Mathf.PerlinNoise(
                    (newCell.WorldPosition.x * scale) + seed,
                    (newCell.WorldPosition.y * scale) + seed
                );

                //noise /= 1 + 0.5f + 0.25f;
                noise = Mathf.Clamp(noise, 0f, 1f);

                //Debug.Log($"biomeDict: {_biomeDict.Count}");

                float sum = 0f;
                foreach (BiomeData biome in _biomeDict.Values.OrderBy(b => b.biomeType).ToList()) // IF YOU GET NULL REF ERROR THEN THE NOISE VAULE WAS SMALLER THAN THE SUM => BAD GENERATION CHANCES
                {
                    sum += biome.generationChance;
                    if (noise < sum)
                    {
                        newCell.Biome = biome.biomeType;

                        /*GameObject tempGm = Instantiate(DEV_Part);
                        tempGm.transform.position = new Vector3(newCell.WorldPosition.x, 5, newCell.WorldPosition.y);
                        tempGm.GetComponent<Renderer>().material.color = biome.debugColor;*/
                        
                        break;
                    }
                }
                //Debug.Log("-----");

                newCell.wallGameObject = Instantiate(_wallPart);
                newCell.wallGameObject.transform.position = new Vector3(
                    (currentWorldPos.x),
                    1,
                    (currentWorldPos.y)
                );

                cgm.cellWorld.Add(currentIndexPos, newCell);
            }
        }

        yield return null;
    }

    public int GetWeight(int depthOfCalculation, Cell targetCell, LargeChunk targetChunk)
    {
        if (targetCell.CellType != CellType.Wall) return -1;
        if (!targetChunk.cells.ContainsKey(targetCell.WorldPosition))
        {
            Debug.LogWarning("Doesn't contain the key!");
            return 0;
        }

        int weight = 0;
        List<Vector2Int> neighbors = OGGetNeighbors(targetCell.WorldPosition, targetChunk);

        for (int depthC = 0; depthC < depthOfCalculation; depthC++)
        {
            List<Vector2Int> toAdd = new();
            List<Vector2Int> toRemove = new();

            foreach (Vector2Int pos in neighbors)
            {
                if (!targetChunk.cells.ContainsKey(pos)) continue;

                if (targetChunk.cells[pos].CellType == CellType.Wall)
                {
                    toAdd.AddRange(OGGetNeighbors(pos, targetChunk));
                    weight++;
                }

                toRemove.Add(pos);
            }

            /*foreach (Vector2Int r in toRemove)
                neighbors.Remove(r);*/

            neighbors.AddRange(toAdd);
        }

        return weight;
    }

    public Area MakeArea(LargeChunk largeChunkRef, CellType targetType)
    {
        if (largeChunkRef.notCategorisedCells <= 0) return null;

        Cell currentCell;
        Area newArea = new(largeChunkRef);

        // GET ALL EMPTY CELLS IN LARGECHUNK

        List<Vector2Int> allPossibleCellPosesForArea = new();

        foreach (KeyValuePair<Vector2Int, Cell> pair in largeChunkRef.cells)
        {
            if (pair.Value.CellType == targetType && pair.Value.areaParent == null)
                allPossibleCellPosesForArea.Add(pair.Key);
        }

        // Set the starting cell randomly (by world position), and the first one is already gets added to the final area.

        Vector2Int _worldPos = allPossibleCellPosesForArea[Random.Range(0, allPossibleCellPosesForArea.Count - 1)];
        currentCell = largeChunkRef.cells[_worldPos];

        allPossibleCellPosesForArea.Clear();

        allPossibleCellPosesForArea.Add(_worldPos);

        int temp = 0;

        // we go while we can't find any more empty cells directly next to the already checked ones.
        while (allPossibleCellPosesForArea.Count > 0)
        {
            temp++;

            if (temp > 3) break;

            List<Vector2Int> toBeRemoved = new();
            List<Vector2Int> toBeAdded = new();  // <- külön lista

            foreach (Vector2Int possibleWorldPos in allPossibleCellPosesForArea)
            {
                if (!largeChunkRef.cells.ContainsKey(possibleWorldPos) ||
                    largeChunkRef.cells[possibleWorldPos].areaParent != null)
                    continue;

                foreach (Vector2Int v in OGGetNeighbors(possibleWorldPos, largeChunkRef))
                {
                    if (!largeChunkRef.cells.ContainsKey(v)) continue;
                    Debug.Log("0");

                    if (targetType == CellType.Wall)
                        if (largeChunkRef.cells[v].CellType == CellType.HallwayEmpty || largeChunkRef.cells[v].CellType == CellType.RoomEmpty) continue;
                        else if (targetType == CellType.HallwayEmpty || targetType == CellType.RoomEmpty)
                            if (largeChunkRef.cells[v].CellType == CellType.Wall || largeChunkRef.cells[v].CellType == CellType.Wall) continue;

                    Debug.Log("1");
                    if (newArea.cellMembers.ContainsKey(v)) continue;
                    Debug.Log("2");
                    if (toBeAdded.Contains(v)) continue;

                    Debug.Log("added to toBeAdded list");
                    toBeAdded.Add(v);
                }

                newArea.cellMembers.Add(possibleWorldPos, largeChunkRef.cells[possibleWorldPos]);
                largeChunkRef.cells[possibleWorldPos].areaParent = newArea;
                toBeRemoved.Add(possibleWorldPos);
            }

            HashSet<Vector2Int> toBeRemovedSet = new(toBeRemoved);
            allPossibleCellPosesForArea.RemoveAll(e => toBeRemoved.Contains(e));
            allPossibleCellPosesForArea.AddRange(toBeAdded);  // <- itt adjuk hozzá
        }

        return newArea;
    }

    public GameObject CreateCellGameObject(Vector2Int worldPosition, CellType cellType)
    {
        if (cellType == CellType.OptimizedWall) return null;

        GameObject gm = null;

        if (cellType == CellType.Wall)
        {
            gm = Instantiate(_wallPart);
            gm.transform.position = new Vector3(
                (worldPosition.x * 2),
                1,
                (worldPosition.y * 2)
            );
        }
        else if (cellType == CellType.LightEmpty)
        {
            gm = Instantiate(_lightPart);
            gm.transform.position = new Vector3(
                (worldPosition.x * 2),
                1,
                (worldPosition.y * 2)
            );
        }

        return gm;
    }

    public void DestroyCellGameObject(GameObject gm)
    {
        Destroy(gm);
    }

    // submethods

    /// <summary>
    /// pos parameter is a world position
    /// </summary>
    /// <returns></returns>
    private List<Vector2Int> GetNeighbors(Vector2Int pos, Dictionary<Vector2Int, Cell> currentBiomeCellPair)
    {
        List<Vector2Int> neighbors = new();

        List<int> used = new();
        int randomIndex;

        for (int i = 0; i < 4; i++)
        {
            randomIndex = Random.Range(1, 5);

            if (randomIndex == 1 && !used.Contains(1) && currentBiomeCellPair.ContainsKey(new Vector2Int(pos.x - 1, pos.y)) && currentBiomeCellPair[new Vector2Int(pos.x - 1, pos.y)].CellType != CellType.HallwayEmpty && currentBiomeCellPair[new Vector2Int(pos.x - 1, pos.y)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x - 1, pos.y));
                used.Add(1);
            }
            if (randomIndex == 2 && !used.Contains(2) && currentBiomeCellPair.ContainsKey(new Vector2Int(pos.x + 1, pos.y)) && currentBiomeCellPair[new Vector2Int(pos.x + 1, pos.y)].CellType != CellType.HallwayEmpty && currentBiomeCellPair[new Vector2Int(pos.x + 1, pos.y)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x + 1, pos.y));
                used.Add(2);
            }
            if (randomIndex == 3 && !used.Contains(3) && currentBiomeCellPair.ContainsKey(new Vector2Int(pos.x, pos.y - 1)) && currentBiomeCellPair[new Vector2Int(pos.x, pos.y - 1)].CellType != CellType.HallwayEmpty && currentBiomeCellPair[new Vector2Int(pos.x, pos.y - 1)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x, pos.y - 1));
                used.Add(3);
            }
            if (randomIndex == 4 && !used.Contains(4) && currentBiomeCellPair.ContainsKey(new Vector2Int(pos.x, pos.y + 1)) && currentBiomeCellPair[new Vector2Int(pos.x, pos.y + 1)].CellType != CellType.HallwayEmpty && currentBiomeCellPair[new Vector2Int(pos.x, pos.y + 1)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x, pos.y + 1));
                used.Add(4);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// pos parameter is a world position
    /// </summary>
    /// <returns></returns>
    private static List<Vector2Int> OGGetNeighbors(Vector2Int pos, LargeChunk targetChunk)
    {
        List<Vector2Int> neighbors = new();

        List<int> used = new();
        int randomIndex;

        for (int i = 0; i < 4; i++)
        {
            randomIndex = Random.Range(1, 5);

            if (randomIndex == 1 && !used.Contains(1) && targetChunk.cells.ContainsKey(new Vector2Int(pos.x - 2, pos.y)) && targetChunk.cells[new Vector2Int(pos.x - 2, pos.y)].CellType != CellType.HallwayEmpty && targetChunk.cells[new Vector2Int(pos.x - 2, pos.y)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x - 2, pos.y));
                used.Add(1);
            }
            if (randomIndex == 2 && !used.Contains(2) && targetChunk.cells.ContainsKey(new Vector2Int(pos.x + 2, pos.y)) && targetChunk.cells[new Vector2Int(pos.x + 2, pos.y)].CellType != CellType.HallwayEmpty && targetChunk.cells[new Vector2Int(pos.x + 2, pos.y)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x + 2, pos.y));
                used.Add(2);
            }
            if (randomIndex == 3 && !used.Contains(3) && targetChunk.cells.ContainsKey(new Vector2Int(pos.x, pos.y - 2)) && targetChunk.cells[new Vector2Int(pos.x, pos.y - 2)].CellType != CellType.HallwayEmpty && targetChunk.cells[new Vector2Int(pos.x, pos.y - 2)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x, pos.y - 2));
                used.Add(3);
            }
            if (randomIndex == 4 && !used.Contains(4) && targetChunk.cells.ContainsKey(new Vector2Int(pos.x, pos.y + 2)) && targetChunk.cells[new Vector2Int(pos.x, pos.y + 2)].CellType != CellType.HallwayEmpty && targetChunk.cells[new Vector2Int(pos.x, pos.y + 2)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x, pos.y + 2));
                used.Add(4);
            }
        }

        return neighbors;
    }

    private Dictionary<Biome, Dictionary<Vector2Int, Cell>> GetCellsByBiome(LargeChunk targetChunk)
    {
        Dictionary<Biome, Dictionary<Vector2Int, Cell>> biomes = new();

        foreach (KeyValuePair<Vector2Int, Cell> pair in targetChunk.cells)
        {
            Cell c = pair.Value;

            if (!biomes.ContainsKey(c.Biome))
                biomes.Add(c.Biome, new() { [pair.Key * cgm.cellSize] = pair.Value }); //  change:  * cgm.cellSize
            else
            {
                if (biomes[c.Biome].ContainsKey(pair.Key)) continue;
                biomes[c.Biome].Add(pair.Key, pair.Value);
            }
        }

        return biomes;
    }
    public void SetAllBiomeDictionaries()
    {
        _biomeDict = new Dictionary<Biome, BiomeData>();

        Debug.Log("biomeDict created");

        if (biomes.Length <= 0 || _biomeDict == null) return;

        Debug.Log("return passed");

        foreach (BiomeData b in biomes)
        {
            _biomeDict.Add(b.biomeType, b);
            Debug.Log("added biome");
        }
    }

    private void Awake()
    {
        /*biomes = Resources.LoadAll<BiomeData>("Biomes");
        SetAllBiomeDictionaries();*/
    }

    [Space]
    [SerializeField] GameObject _wallPart;
    public GameObject _ceilingPart;
    public GameObject _lightPart;
    [SerializeField] GameObject _doorPart;

    [Space]

    [SerializeField] WorldTracker worldTracker;
    [SerializeField] ChunkGenerationManager cgm;
}