using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CellType
{
    Wall,
    HallwayEmpty,
    RoomEmpty
}

public class ACGen : MonoBehaviour
{
    //public int renderDist = 2;
    public Vector2Int center = new Vector2Int(0, 0);
    [SerializeField] private GameObject _wallPart;

    public Dictionary<Vector2Int, CellType> map;
    public Dictionary<Vector2Int, GameObject> world;

    public int size = 20; // 20

    [Space]

    public int labirynthCount = 30; // 60
    [SerializeField] static int roomCount = 10; // 30
    [SerializeField] static int wallBlockCount = 10; // 10

    [Space]

    [SerializeField] static float STOP_COLLISION_PROBABILITY = 0.08f;
    [SerializeField] static float mapFillPrecentage = 0f;
    [SerializeField] static float randomFactorForDoors = 1f;
    int randomRoomMinSize = 3; // 5
    int randomRoomMaxSize = 10; // 15

    void Start()
    {
        map = new();
        world = new();

        /*LargeChunk firstLargeChunk = new(new Vector2Int(0, 0), this);
        cgm.largeChunks.Add(new Vector2Int(0, 0), firstLargeChunk);*/

        //worldTracker.playerChunkPosChanged += () => { GenerateMap(labirynthCount); };
        /*GenerateMap(labirynthCount, firstLargeChunk);
        GenerateRooms(roomCount);
        GenerateWallBlocks(wallBlockCount);*/
    }


    public static void GenerateMap(int labirynth, LargeChunk targetChunk)
    {
        //Debug.Log("GenerateMap() ran");
        List<Vector2Int> visitedCells = new();
        bool firstIteration = true;

        for (int f = 0; f < labirynth; f++)
        {
            int randPosIndex = Random.Range(0, targetChunk.cells.Count);
            Vector2Int parentPos = targetChunk.cells.ElementAt(randPosIndex).Key;

            if (!visitedCells.Contains(parentPos))
                visitedCells.Add(parentPos);

            targetChunk.cells[parentPos] = new Cell(CellType.HallwayEmpty);
            Destroy(targetChunk.cells[parentPos].gm); // GENERATE FLOOR!!! IF WALKABLE/ EMPTY


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
                    Destroy(targetChunk.cells[childPos].gm);

                    Vector2Int doorwayPos = new();

                    if (frontier[childPos].y > childPos.y && frontier[childPos].x == childPos.x)
                    {
                        doorwayPos = new Vector2Int(childPos.x, childPos.y + 1);

                        if (targetChunk.cells[doorwayPos].CellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].gm);
                        }
                    }
                    else if (frontier[childPos].y < childPos.y && frontier[childPos].x == childPos.x)
                    {
                        doorwayPos = new Vector2Int(childPos.x, childPos.y - 1);

                        if (targetChunk.cells[doorwayPos].CellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].gm);
                        }
                    }
                    else if (frontier[childPos].x > childPos.x && frontier[childPos].y == childPos.y)
                    {
                        doorwayPos = new Vector2Int(childPos.x + 1, childPos.y);

                        if (targetChunk.cells[doorwayPos].CellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].gm);
                        }
                    }
                    else if (frontier[childPos].x < childPos.x && frontier[childPos].y == childPos.y)
                    {
                        doorwayPos = new Vector2Int(childPos.x - 1, childPos.y);

                        if (targetChunk.cells[doorwayPos].CellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].CellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].gm);
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
                    firstIteration = false;
                }
            }
        }
    }

    public void MakePatches(int loops, LargeChunk targetLargeChunk)
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
                }

                /*if (currentCell.CellType == CellType.HallwayEmpty && wallNeighbourCount >= 3)
                    toMakeWall.Add(currentCell);*/
            }
        }

        foreach (Cell cell in toMakeEmpty)
        {
            cell.CellType = CellType.HallwayEmpty;
            Destroy(cell.gm);
            cell.gm = null;
        }

        /*foreach (Cell cell in toMakeWall)
        {
            cell.CellType = CellType.Wall;
            cell.gm = Instantiate(_wallPart);
            cell.gm.transform.position = new Vector3(cell.worldPosition.x, 1, cell.worldPosition.y);
        }*/
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

    public void GenerateRooms(int roomsCount, LargeChunk currentLargeChunk)
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

            for (int roomX = centerPos.x - (Mathf.RoundToInt(roomSizeX / 2)); roomX < centerPos.x + (Mathf.RoundToInt(roomSizeX / 2)); roomX++)
            {
                for (int roomY = centerPos.y - (Mathf.RoundToInt(roomSizeY / 2)); roomY < centerPos.y + (Mathf.RoundToInt(roomSizeY / 2)); roomY++)
                {
                    Vector2Int roomPos = new Vector2Int(roomX, roomY);

                    if (!currentLargeChunk.cells.ContainsKey(roomPos)) continue;

                    currentLargeChunk.cells[roomPos].CellType = CellType.RoomEmpty;
                    Destroy(currentLargeChunk.cells[roomPos].gm);
                }
            }
        }
    }
    
    public void GenerateWallBlocks(int wallBlocksCount)
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
    }

    public void FillWithWalls(LargeChunk largeChunk)
    {
        for (int x = Mathf.RoundToInt(largeChunk.largeChunkPivot.x - (ChunkGenerationManager.largeChunkSize / 2)); x < Mathf.RoundToInt(largeChunk.largeChunkPivot.x + (ChunkGenerationManager.largeChunkSize / 2)); x++)
        {
            for (int z = Mathf.RoundToInt(largeChunk.largeChunkPivot.y - (ChunkGenerationManager.largeChunkSize / 2)); z < Mathf.RoundToInt(largeChunk.largeChunkPivot.y + (ChunkGenerationManager.largeChunkSize / 2)); z++)
            {
                Vector2Int currentWorldPos = new Vector2Int(x, z);

                if (map.ContainsKey(currentWorldPos)) continue;

                Cell newCell = new(CellType.Wall);
                newCell.worldPosition = currentWorldPos;

                largeChunk.AddCell(newCell);

                newCell.gm = Instantiate(_wallPart);
                newCell.gm.transform.position = new Vector3(
                    (currentWorldPos.x * 2),
                    1,
                    (currentWorldPos.y * 2)
                );

                map.Add(currentWorldPos, CellType.Wall);
                world.Add(currentWorldPos, newCell.gm);
                cgm.cellWorld.Add(currentWorldPos, newCell);
            }
        }
    }

    public int GetWeight(int depthOfCalculation, Cell targetCell, LargeChunk targetChunk)
    {
        if (targetCell.CellType != CellType.Wall) return -1;
        if (!targetChunk.cells.ContainsKey(targetCell.worldPosition))
        {
            Debug.LogWarning("Doesn't contain the key!");
            return 0;
        }

        int weight = 0;
        List<Vector2Int> neighbors = GetNeighbors(targetCell.worldPosition, targetChunk);

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

    public GameObject CreateCellGameObject(Vector2Int worldPosition, CellType cellType)
    {
        if (!map.ContainsKey(worldPosition) || cellType != CellType.Wall) return null;

        GameObject gm = Instantiate(_wallPart);
        gm.transform.position = new Vector3(
            (worldPosition.x * 2),
            1,
            (worldPosition.y * 2)
        );

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

    private int NeighbourCountByCellType(Cell mainCell, LargeChunk parentLargeChunk, CellType targetType)
    {
        Vector2Int currentIndexPos = new Vector2Int(
            Mathf.FloorToInt((float)mainCell.worldPosition.x / 2),
            Mathf.FloorToInt((float)mainCell.worldPosition.y / 2)
        );

        int returnValue = 0;

        List<Vector2Int> neighbours = GetNeighbors(currentIndexPos, parentLargeChunk);

        foreach (Vector2Int index in neighbours)
        {
            if (!parentLargeChunk.cells.ContainsKey(index)) continue;

            if (parentLargeChunk.cells[index].CellType == CellType.Wall)
                returnValue++;
        }

        return returnValue;
    }

    private void ClearMap()
    {
        map.Clear();

        foreach (KeyValuePair<Vector2Int, GameObject> pair in world)
            Destroy(pair.Value);

        world.Clear();
    }

    [SerializeField] WorldTracker worldTracker;
    [SerializeField] ChunkGenerationManager cgm;
}