using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class ACGen : MonoBehaviour
{
	public enum CellType
	{
		Wall,
		Empty
	}

	//public int renderDist = 2;
	public Vector2Int center = new Vector2Int(0, 0);
	[SerializeField] private GameObject _wallPart;

	public Dictionary<Vector2Int, CellType> map;
	public Dictionary<Vector2Int, GameObject> world;

	public int size = 50;

    [Space]

    [SerializeField] int labirynthCount = 100;
	[SerializeField] int roomCount = 10;
	[SerializeField] int wallBlockCount = 15;

    [Space]

	[SerializeField] float STOP_COLLISION_PROBABILITY = 0.005f;
	[SerializeField] float mapFillPrecentage = 0f;
	[SerializeField] float randomFactorForDoors = 1f;
	int randomRoomMinSize = 4;
	int randomRoomMaxSize = 10;

	void Start()
	{
		map = new();
		world = new();

		lastNum1 = STOP_COLLISION_PROBABILITY;
		lastNum2 = mapFillPrecentage;
		lastNum3 = randomFactorForDoors;

        GenerateMap(labirynthCount);
		GenerateRooms(roomCount);
		GenerateWallBlocks(wallBlockCount);
    }

	float lastNum1 = 0;
	float lastNum2 = 0;
	float lastNum3 = 0;

    private void Update()
	{
		if (lastNum1 != STOP_COLLISION_PROBABILITY || lastNum2 != mapFillPrecentage || lastNum3 != randomFactorForDoors)
		{
			ClearMap();

            GenerateMap(labirynthCount);
            GenerateRooms(roomCount);
            GenerateWallBlocks(wallBlockCount);

            lastNum1 = STOP_COLLISION_PROBABILITY;
            lastNum2 = mapFillPrecentage;
            lastNum3 = randomFactorForDoors;
        }
	}

	private void GenerateMap(int labirynth)
	{
		FillWithWalls();
		List<Vector2Int> visitedCells = new();
		bool firstIteration = true;

		for (int f = 0; f < labirynth; f++)
		{
			int randPosIndex = Random.Range(0, map.Count);
			Vector2Int parentPos = map.ElementAt(randPosIndex).Key;

            if (!visitedCells.Contains(parentPos))
                visitedCells.Add(parentPos);

            map[parentPos] = CellType.Empty;
			Destroy(world[parentPos]); // GENERATE FLOOR!!! IF WALKABLE/ EMPTY

			Dictionary<Vector2Int, Vector2Int> frontier = new();

			foreach (Vector2Int childP in GetNeighbours(parentPos))
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

				if (map[childPos] == CellType.Wall)
				{
					map[childPos] = CellType.Empty;
					Destroy(world[childPos]);

					Vector2Int doorwayPos = new();

					if (frontier[childPos].y > childPos.y && frontier[childPos].x == childPos.x)
					{
						doorwayPos = new Vector2Int(childPos.x, childPos.y + 1);

						if (map[doorwayPos] == CellType.Wall && Random.value < randomFactorForDoors)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (frontier[childPos].y < childPos.y && frontier[childPos].x == childPos.x)
					{
						doorwayPos = new Vector2Int(childPos.x, childPos.y - 1);

                        if (map[doorwayPos] == CellType.Wall && Random.value < randomFactorForDoors)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (frontier[childPos].x > childPos.x && frontier[childPos].y == childPos.y)
					{
						doorwayPos = new Vector2Int(childPos.x + 1, childPos.y);

                        if (map[doorwayPos] == CellType.Wall && Random.value < randomFactorForDoors)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (frontier[childPos].x < childPos.x && frontier[childPos].y == childPos.y)
					{
						doorwayPos = new Vector2Int(childPos.x - 1, childPos.y);

                        if (map[doorwayPos] == CellType.Wall && Random.value < randomFactorForDoors)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}

					List<Vector2Int> neighborList = new();

					foreach (Vector2Int childP in GetNeighbours(childPos))
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
			if (pair.Value != CellType.Empty || emptyParts.Contains(pair.Key)) continue;
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

                    map[roomPos] = CellType.Empty;
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

    private void FillWithWalls()
	{
		for (int x = tracker.playerRoomPosition.x - (size / 2); x < tracker.playerRoomPosition.x + (size / 2); x++)
		{
			for (int z = tracker.playerRoomPosition.y - (size / 2); z < tracker.playerRoomPosition.y + (size / 2); z++)
			{
				Vector2Int currentPos = new Vector2Int(x, z);

				if (map.ContainsKey(currentPos)) continue;

				map.Add(currentPos, CellType.Wall);
				world.Add(currentPos, Instantiate(_wallPart));
				world[currentPos].transform.position = new Vector3(
					(currentPos.x * 2),
					world[currentPos].transform.position.y,
					(currentPos.y * 2)
				);
			}
		}
	}

	private List<Vector2Int> GetNeighbours(Vector2Int pos)
	{
		List<Vector2Int> neighbors = new();

		List<int> used = new();
		int randomIndex;

		for (int i = 0; i < 4; i++)
		{
            randomIndex = Random.Range(1, 4);

            if (randomIndex == 1 && !used.Contains(1) && map.ContainsKey(new Vector2Int(pos.x - 2, pos.y)) && map[new Vector2Int(pos.x - 2, pos.y)] != CellType.Empty)
            {
                neighbors.Add(new Vector2Int(pos.x - 2, pos.y));
				used.Add(1);
            }
            if (randomIndex == 2 && !used.Contains(2) && map.ContainsKey(new Vector2Int(pos.x + 2, pos.y)) && map[new Vector2Int(pos.x + 2, pos.y)] != CellType.Empty)
            {
                neighbors.Add(new Vector2Int(pos.x + 2, pos.y));
                used.Add(2);
            }
            if (randomIndex == 3 && !used.Contains(3) && map.ContainsKey(new Vector2Int(pos.x, pos.y - 2)) && map[new Vector2Int(pos.x, pos.y - 2)] != CellType.Empty)
            {
                neighbors.Add(new Vector2Int(pos.x, pos.y - 2));
                used.Add(3);
            }
            if (randomIndex == 4 && !used.Contains(4) && map.ContainsKey(new Vector2Int(pos.x, pos.y + 2)) && map[new Vector2Int(pos.x, pos.y + 2)] != CellType.Empty)
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

	[SerializeField] WorldTracker tracker;
}