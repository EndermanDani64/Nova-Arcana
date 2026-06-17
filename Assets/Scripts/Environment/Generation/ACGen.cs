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

    public int size = 50;

    [Space]

    [SerializeField] static int labirynthCount = 100;
    [SerializeField] static int roomCount = 10;
    [SerializeField] static int wallBlockCount = 15;

    [Space]

    [SerializeField] static float STOP_COLLISION_PROBABILITY = 0.005f;
    [SerializeField] static float mapFillPrecentage = 0f;
    [SerializeField] static float randomFactorForDoors = 1f;
    int randomRoomMinSize = 4;
    int randomRoomMaxSize = 10;

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
        Debug.Log("GenerateMap() ran");
        List<Vector2Int> visitedCells = new();
        bool firstIteration = true;

        for (int f = 0; f < labirynth; f++)
        {
            int randPosIndex = Random.Range(0, targetChunk.cells.Count);
            Vector2Int parentPos = targetChunk.cells.ElementAt(randPosIndex).Key;

            Debug.Log("GenerateMap() 0 ran");

            if (!visitedCells.Contains(parentPos))
                visitedCells.Add(parentPos);

            targetChunk.cells[parentPos] = new Cell(CellType.HallwayEmpty);
            Destroy(targetChunk.cells[parentPos].gm); // GENERATE FLOOR!!! IF WALKABLE/ EMPTY

            Debug.Log("GenerateMap() 1 ran");

            Dictionary<Vector2Int, Vector2Int> frontier = new();

            foreach (Vector2Int childP in GetNeighbours(parentPos, targetChunk))
            {
                if (frontier.ContainsKey(childP)) continue;
                frontier.Add(childP, parentPos);
            }

            while (/*visitedCells.Count / (size * size) < mapFillPrecentage*/ frontier.Count > 0)
            {
                int randChoice = Random.Range(0, frontier.Count - 1);
                Vector2Int childPos = frontier.ElementAt(randChoice).Key; // x, y rand | line 71

                if (Random.value < STOP_COLLISION_PROBABILITY)
                {
                    //frontier.Remove(childPos);
                    frontier.Clear();
                    continue;
                }

                if (targetChunk.cells[childPos].cellType == CellType.Wall)
                {
                    targetChunk.cells[childPos].cellType = CellType.HallwayEmpty;
                    Destroy(targetChunk.cells[childPos].gm);

                    Vector2Int doorwayPos = new();

                    if (frontier[childPos].y > childPos.y && frontier[childPos].x == childPos.x)
                    {
                        doorwayPos = new Vector2Int(childPos.x, childPos.y + 1);

                        if (targetChunk.cells[doorwayPos].cellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].cellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].gm);
                        }
                    }
                    else if (frontier[childPos].y < childPos.y && frontier[childPos].x == childPos.x)
                    {
                        doorwayPos = new Vector2Int(childPos.x, childPos.y - 1);

                        if (targetChunk.cells[doorwayPos].cellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].cellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].gm);
                        }
                    }
                    else if (frontier[childPos].x > childPos.x && frontier[childPos].y == childPos.y)
                    {
                        doorwayPos = new Vector2Int(childPos.x + 1, childPos.y);

                        if (targetChunk.cells[doorwayPos].cellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].cellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].gm);
                        }
                    }
                    else if (frontier[childPos].x < childPos.x && frontier[childPos].y == childPos.y)
                    {
                        doorwayPos = new Vector2Int(childPos.x - 1, childPos.y);

                        if (targetChunk.cells[doorwayPos].cellType == CellType.Wall && Random.value < randomFactorForDoors)
                        {
                            targetChunk.cells[doorwayPos].cellType = CellType.HallwayEmpty;
                            Destroy(targetChunk.cells[doorwayPos].gm);
                        }
                    }

                    List<Vector2Int> neighborList = new();

                    foreach (Vector2Int childP in GetNeighbours(childPos, targetChunk))
                    {
                        if (neighborList.Contains(childP)) continue;
                        neighborList.Add(childP);
                        //frontier.Add(childP, childPos); // childPos = parentPos
                    }

                    int randomIndex = Random.Range(0, neighborList.Count - 1);

                    Debug.Log($"neigbourList count: {neighborList.Count} | randomIndex: {randomIndex}");

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

    private void GenerateRooms(int roomsCount)
    {
        List<Vector2Int> emptyParts = new();

        foreach (KeyValuePair<Vector2Int, CellType> pair in map)
        {
            if (pair.Value != CellType.RoomEmpty || emptyParts.Contains(pair.Key)) continue;
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

                    if (!world.ContainsKey(roomPos)) continue;

                    map[roomPos] = CellType.RoomEmpty;
                    Destroy(world[roomPos]);
                }
            }
        }
    }

    private void GenerateWallBlocks(int wallBlocksCount)
    {
        List<Vector2Int> emptyParts = new();

        foreach (KeyValuePair<Vector2Int, CellType> pair in map)
        {
            if (pair.Value != CellType.Wall || emptyParts.Contains(pair.Key)) continue;
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
                        world[roomPos].transform.position.y,
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
            }
        }
    }

    // submethods

    private static List<Vector2Int> GetNeighbours(Vector2Int pos, LargeChunk currentChunk)
    {
        List<Vector2Int> neighbors = new();

        List<int> used = new();
        int randomIndex;

        for (int i = 0; i < 4; i++)
        {
            randomIndex = Random.Range(1, 5);

            if (randomIndex == 1 && !used.Contains(1) && currentChunk.cells.ContainsKey(new Vector2Int(pos.x - 2, pos.y)) && currentChunk.cells[new Vector2Int(pos.x - 2, pos.y)].cellType != CellType.HallwayEmpty && currentChunk.cells[new Vector2Int(pos.x - 2, pos.y)].cellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x - 2, pos.y));
                used.Add(1);
            }
            if (randomIndex == 2 && !used.Contains(2) && currentChunk.cells.ContainsKey(new Vector2Int(pos.x + 2, pos.y)) && currentChunk.cells[new Vector2Int(pos.x + 2, pos.y)].cellType != CellType.HallwayEmpty && currentChunk.cells[new Vector2Int(pos.x + 2, pos.y)].cellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x + 2, pos.y));
                used.Add(2);
            }
            if (randomIndex == 3 && !used.Contains(3) && currentChunk.cells.ContainsKey(new Vector2Int(pos.x, pos.y - 2)) && currentChunk.cells[new Vector2Int(pos.x, pos.y - 2)].cellType != CellType.HallwayEmpty && currentChunk.cells[new Vector2Int(pos.x, pos.y - 2)].cellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x, pos.y - 2));
                used.Add(3);
            }
            if (randomIndex == 4 && !used.Contains(4) && currentChunk.cells.ContainsKey(new Vector2Int(pos.x, pos.y + 2)) && currentChunk.cells[new Vector2Int(pos.x, pos.y + 2)].cellType != CellType.HallwayEmpty && currentChunk.cells[new Vector2Int(pos.x, pos.y + 2)].cellType != CellType.RoomEmpty)
            {
                neighbors.Add(new Vector2Int(pos.x, pos.y + 2));
                used.Add(4);
            }
        }

        return neighbors;
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