using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CellType
{
    Wall,
    OptimizedWall,
    HallwayEmpty,
    RoomEmpty,
    LightEmpty
}

public enum Biome
{
    ScatteredRooms_Lit,
    ScatteredRooms_Dark
}

public class ACGen : MonoBehaviour
{
    //public int renderDist = 2;
    public Vector2Int center = new Vector2Int(0, 0);

    [Space]

    public int labirynthCount;
    public int roomCount;
    public int patchIterationCount;

    [Space]

    [SerializeField] float STOP_COLLISION_PROBABILITY;
    [SerializeField] float randomFactorForDoors;

    [Space]

    [SerializeField] int randomRoomMinSize;
    [SerializeField] int randomRoomMaxSize;

    [Space]

    public Dictionary<Biome, int> _BiomeGenData_LabirynthCount;
    public Dictionary<Biome, int> _BiomeGenData_RoomCount; 
    public Dictionary<Biome, int> _BiomeGenData_PatchIterationCount;

    [SerializeField] Dictionary<Biome, int> _BiomeGenData_RoomMinSizeByBiome;
    [SerializeField] Dictionary<Biome, int> _BiomeGenData_RoomMaxSizeByBiome;

    [SerializeField] Dictionary<Biome, float> _BiomeGenData_stopCollisionProbality;
    [SerializeField] Dictionary<Biome, float> _BiomeGenData_RandomFactorForDoors;

    [Space]

    [SerializeField] float lightRandomness;

    [Space]

    public int seed = 0;
    public float scale;

    private void Start()
    {
        SetAllBiomeDictionaries();

        if (seed == 0) seed = Random.Range(1, int.MaxValue); 
        Random.InitState(seed);
    }

    public IEnumerator GenerateMap(int labirynth, LargeChunk targetChunk)
    {
        Dictionary<Biome, List<Cell>> biomes = new();
        List<Vector2Int> visitedCells = new();
        
        foreach (KeyValuePair<Vector2Int, Cell> pair in targetChunk.cells)
        {
            Cell c = pair.Value;

            if (!biomes.ContainsKey(c.Biome))
                biomes.Add(c.Biome, new() { c });
            else
                biomes[c.Biome].Add(c);
        }

        for (int f = 0; f < labirynth; f++)
        {
            int randPosIndex = Random.Range(0, targetChunk.cells.Count);
            Vector2Int parentPos = targetChunk.cells.ElementAt(randPosIndex).Key;

            if (!visitedCells.Contains(parentPos))
                visitedCells.Add(parentPos);

            // generate walkable area

            if (!targetChunk.cells.ContainsKey(parentPos)) continue;

            targetChunk.cells[parentPos].CellType = CellType.HallwayEmpty;
            Destroy(targetChunk.cells[parentPos].wallGameObject); // GENERATE FLOOR!!! IF WALKABLE/ EMPTY

            if (Random.value < lightRandomness)
            {
                targetChunk.cells[parentPos].lightGameObject = Instantiate(_lightPart);
                targetChunk.cells[parentPos].lightGameObject.transform.position = new Vector3(
                    targetChunk.cells[parentPos].WorldPosition.x * cgm.cellSize,
                    1,
                    targetChunk.cells[parentPos].WorldPosition.y * cgm.cellSize
                );
                targetChunk.cells[parentPos].CellType = CellType.LightEmpty;
            }

            // start of the main logic

            Dictionary<Vector2Int, Vector2Int> frontier = new();

            foreach (Vector2Int childP in GetNeighbors(parentPos, targetChunk))
            {
                if (frontier.ContainsKey(childP)) continue;
                frontier.Add(childP, parentPos);
            }

            while (frontier.Count > 0)
            {
                int randChoice = Random.Range(0, frontier.Count - 1);
                Vector2Int childPos = frontier.ElementAt(randChoice).Key; // x, y rand | line 71

                if (Random.value < STOP_COLLISION_PROBABILITY)
                {
                    frontier.Clear();
                    continue;
                }

                if (targetChunk.cells[childPos].CellType == CellType.Wall)
                {
                    targetChunk.cells[childPos].CellType = CellType.HallwayEmpty;
                    Destroy(targetChunk.cells[childPos].wallGameObject);
                    targetChunk.notCategorisedCells++;

                    Vector2Int doorwayPos = new();

                    if (frontier[childPos].y > childPos.y && frontier[childPos].x == childPos.x)
                    {
                        doorwayPos = new Vector2Int(childPos.x, childPos.y + 1);

                        if (targetChunk.cells[doorwayPos].CellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].wallGameObject);
                            targetChunk.notCategorisedCells++;
                        }
                    }
                    else if (frontier[childPos].y < childPos.y && frontier[childPos].x == childPos.x)
                    {
                        doorwayPos = new Vector2Int(childPos.x, childPos.y - 1);

                        if (targetChunk.cells[doorwayPos].CellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].wallGameObject);
                            targetChunk.notCategorisedCells++;
                        }
                    }
                    else if (frontier[childPos].x > childPos.x && frontier[childPos].y == childPos.y)
                    {
                        doorwayPos = new Vector2Int(childPos.x + 1, childPos.y);

                        if (targetChunk.cells[doorwayPos].CellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].wallGameObject);
                            targetChunk.notCategorisedCells++;
                        }
                    }
                    else if (frontier[childPos].x < childPos.x && frontier[childPos].y == childPos.y)
                    {
                        doorwayPos = new Vector2Int(childPos.x - 1, childPos.y);

                        if (targetChunk.cells[doorwayPos].CellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].wallGameObject);
                            targetChunk.notCategorisedCells++;
                        }
                    }

                    List<Vector2Int> neighborList = new();

                    foreach (Vector2Int childP in GetNeighbors(childPos, targetChunk))
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

        yield return null;
    }

    public IEnumerator MakePatches(int loops, LargeChunk targetLargeChunk)
    {
        List<Cell> toMakeWall = new();
        List<Cell> toMakeEmpty = new();

        for (int _ = 0; _ < loops; _++)
        {
            foreach (KeyValuePair<Vector2Int, Cell> pair in targetLargeChunk.cells)
            {
                Cell currentCell = pair.Value;
                int wallNeighbourCount = GetWallNeighbors(pair.Key, targetLargeChunk).Count;

                if (currentCell.CellType == CellType.Wall && (wallNeighbourCount == 0 /*|| (Random.value < .001 && wallNeighbourCount <= 2)*/))
                {
                    toMakeEmpty.Add(currentCell);
                    //Debug.Log($"key: {pair.Key}, celltype: {currentCell.CellType}, falak száma körülötte: {wallNeighbourCount}", currentCell.wallGameObject);
                }

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

    public IEnumerator OptimiseWalls(LargeChunk targetLargeChunk)
    {
        foreach (KeyValuePair<Vector2Int, Cell> pair in targetLargeChunk.cells)
        {
            Cell currentCell = pair.Value;

            if (currentCell.CellType != CellType.Wall || GetWallNeighbors(pair.Key, targetLargeChunk).Count < 4 || currentCell.wallGameObject == null) continue;

            Destroy(currentCell.wallGameObject);
            currentCell.CellType = CellType.OptimizedWall;
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

    public IEnumerator GenerateRooms(int roomsCount, LargeChunk currentLargeChunk)
    {
        List<Vector2Int> emptyParts = new();

        foreach (KeyValuePair<Vector2Int, Cell> pair in currentLargeChunk.cells)
        {
            if (emptyParts.Contains(pair.Key)) continue;
            emptyParts.Add(pair.Key);
        }

        for (int f = 0; f < roomsCount; f++)
        {
            Vector2Int centerPos = emptyParts[Random.Range(0, emptyParts.Count - 1)];

            int roomSizeX = Random.Range(randomRoomMinSize, randomRoomMaxSize);
            int roomSizeY = Random.Range(randomRoomMinSize, randomRoomMaxSize);

            for (int roomX = centerPos.x - (Mathf.RoundToInt(roomSizeX / cgm.cellSize)); roomX < centerPos.x + (Mathf.RoundToInt(roomSizeX / cgm.cellSize)); roomX++)
            {
                for (int roomY = centerPos.y - (Mathf.RoundToInt(roomSizeY / cgm.cellSize)); roomY < centerPos.y + (Mathf.RoundToInt(roomSizeY / cgm.cellSize)); roomY++)
                {
                    Vector2Int roomPos = new Vector2Int(roomX, roomY);

                    if (!currentLargeChunk.cells.ContainsKey(roomPos)) continue;

                    currentLargeChunk.cells[roomPos].CellType = CellType.RoomEmpty;
                    Destroy(currentLargeChunk.cells[roomPos].wallGameObject);
                }
            }
        }

        yield return null;
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
                Vector2Int currentWorldPos = new Vector2Int(x, z);

                if (cgm.cellWorld.ContainsKey(currentWorldPos)) continue;

                Cell newCell = new(CellType.Wall, currentWorldPos); // !!! <- currentWorldPos : most akk melyik? (duplázott vagy nem)
                largeChunk.AddCell(newCell);

                float noise = Mathf.PerlinNoise(
                    (newCell.WorldPosition.x * scale) + seed,
                    (newCell.WorldPosition.y * scale) + seed
                );

                if (noise < .88)
                    newCell.Biome = Biome.ScatteredRooms_Lit;
                else
                    newCell.Biome = Biome.ScatteredRooms_Dark;

                newCell.wallGameObject = Instantiate(_wallPart);
                newCell.wallGameObject.transform.position = new Vector3(
                    (currentWorldPos.x * 2),
                    1,
                    (currentWorldPos.y * 2)
                );

                cgm.cellWorld.Add(currentWorldPos, newCell);
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
        List<Vector2Int> neighbors = GetNeighbors(targetCell.WorldPosition, targetChunk);

        for (int depthC = 0; depthC < depthOfCalculation; depthC++)
        {
            List<Vector2Int> toAdd = new();
            List<Vector2Int> toRemove = new();

            foreach (Vector2Int pos in neighbors)
            {
                if (!targetChunk.cells.ContainsKey(pos)) continue;

                if (targetChunk.cells[pos].CellType == CellType.Wall)
                {
                    toAdd.AddRange(GetNeighbors(pos, targetChunk));
                    weight++;
                }

                toRemove.Add(pos);
            }

            foreach (Vector2Int r in toRemove)
                neighbors.Remove(r);

            neighbors.AddRange(toAdd);
        }

        return weight;
    }

    public Area MakeArea(LargeChunk largeChunkRef)
    {
        if (largeChunkRef.notCategorisedCells <= 0) return null;

        Cell currentCell;
        Area newArea = new(largeChunkRef);

        // GET ALL EMPTY CELLS IN LARGECHUNK

        List<Vector2Int> allPossibleCellPosesForArea = new();

        foreach (KeyValuePair<Vector2Int, Cell> pair in largeChunkRef.cells)
        {
            if (pair.Value.CellType == CellType.HallwayEmpty && pair.Value.areaParent == null)
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

                foreach (Vector2Int v in GetNeighbors(possibleWorldPos, largeChunkRef))
                {
                    if (!largeChunkRef.cells.ContainsKey(v)) continue;
                    Debug.Log("0");
                    if (largeChunkRef.cells[v].CellType == CellType.Wall) continue;
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
    private static List<Vector2Int> GetNeighbors(Vector2Int pos, LargeChunk currentChunk)
    {
        List<Vector2Int> neighbors = new();

        List<int> used = new();
        int randomIndex;

        for (int i = 0; i < 4; i++)
        {
            randomIndex = Random.Range(1, 5);

            if (randomIndex == 1 && !used.Contains(1) && currentChunk.cells.ContainsKey(new Vector2Int(pos.x - 2, pos.y)) && currentChunk.cells[new Vector2Int(pos.x - 2, pos.y)].CellType != CellType.HallwayEmpty && currentChunk.cells[new Vector2Int(pos.x - 2, pos.y)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x - 2, pos.y));
                used.Add(1);
            }
            if (randomIndex == 2 && !used.Contains(2) && currentChunk.cells.ContainsKey(new Vector2Int(pos.x + 2, pos.y)) && currentChunk.cells[new Vector2Int(pos.x + 2, pos.y)].CellType != CellType.HallwayEmpty && currentChunk.cells[new Vector2Int(pos.x + 2, pos.y)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x + 2, pos.y));
                used.Add(2);
            }
            if (randomIndex == 3 && !used.Contains(3) && currentChunk.cells.ContainsKey(new Vector2Int(pos.x, pos.y - 2)) && currentChunk.cells[new Vector2Int(pos.x, pos.y - 2)].CellType != CellType.HallwayEmpty && currentChunk.cells[new Vector2Int(pos.x, pos.y - 2)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x, pos.y - 2));
                used.Add(3);
            }
            if (randomIndex == 4 && !used.Contains(4) && currentChunk.cells.ContainsKey(new Vector2Int(pos.x, pos.y + 2)) && currentChunk.cells[new Vector2Int(pos.x, pos.y + 2)].CellType != CellType.HallwayEmpty && currentChunk.cells[new Vector2Int(pos.x, pos.y + 2)].CellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x, pos.y + 2));
                used.Add(4);
            }
        }

        return neighbors;
    }

    private void SetAllBiomeDictionaries()
    {
        _BiomeGenData_LabirynthCount = new()
        {
            [Biome.ScatteredRooms_Lit] = 25,
            [Biome.ScatteredRooms_Dark] = 25
        };
        _BiomeGenData_RoomCount = new()
        {
            [Biome.ScatteredRooms_Lit] = 14,
            [Biome.ScatteredRooms_Dark] = 14
        };
        _BiomeGenData_PatchIterationCount = new()
        {
            [Biome.ScatteredRooms_Lit] = 60,
            [Biome.ScatteredRooms_Dark] = 60
        };

        _BiomeGenData_RoomMinSizeByBiome = new()
        {
            [Biome.ScatteredRooms_Lit] = 2,
            [Biome.ScatteredRooms_Dark] = 2
        };
        _BiomeGenData_RoomMaxSizeByBiome = new()
        {
            [Biome.ScatteredRooms_Lit] = 4,
            [Biome.ScatteredRooms_Dark] = 4
        };

        _BiomeGenData_stopCollisionProbality = new()
        {
            [Biome.ScatteredRooms_Lit] = 0.05f,
            [Biome.ScatteredRooms_Dark] = 0.05f
        };
        _BiomeGenData_RandomFactorForDoors = new()
        {
            [Biome.ScatteredRooms_Lit] = 1,
            [Biome.ScatteredRooms_Dark] = 1
        };
    }

    [Space]
    [SerializeField] private GameObject _wallPart;
    public GameObject _ceilingPart;
    public GameObject _lightPart;

    [Space]

    [SerializeField] WorldTracker worldTracker;
    [SerializeField] ChunkGenerationManager cgm;
}