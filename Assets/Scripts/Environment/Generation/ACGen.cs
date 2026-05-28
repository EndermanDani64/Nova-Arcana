using System.Collections.Generic;
using System.Linq;
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
	public Dictionary<Vector2Int, bool> visited; // added

	public int size = 50;

	[SerializeField] float STOP_COLLISION_PROBABILITY = 0.5f;
	[SerializeField] float randomFactor = 1f;
	[SerializeField] float randomStopFactor = .3f;
	[SerializeField] float randomRoomFactor = .1f;
	[SerializeField] int randomRoomMinSize = 2;
	[SerializeField] int randomRoomMaxSize = 4;

	void Start()
	{
		map = new();
		world = new();
		visited = new(); // added
		GenerateMap(1);
	}

	private void Update()
	{
		/*if (lastSize != size)
		{
			GenerateMap(15);
		}*/

		/*Debug.Log($"map len: {map.Count} | world len: {world.Count}");

		for (int f = 0; f < labirynth; f++)
		{
			int randPosIndex = Random.Range(0, map.Count);
			Vector2Int parentPos = map.ElementAt(randPosIndex).Key;

			if (visited.ContainsKey(parentPos)) // added
			{
				if (visited[parentPos]) continue;
			}
			
			map[parentPos] = CellType.Empty;
			Destroy(world[parentPos]); // GENERATE FLOOR!!! IF WALKABLE/ EMPTY

			Dictionary<Vector2Int, Vector2Int> frontier = new();

			foreach (Vector2Int childP in GetNeighbours(parentPos))
			{
				if (frontier.ContainsKey(childP)) continue;
				frontier.Add(childP, parentPos);
			}

			while (frontier.Count > 0)
			{
				int randChoice = Random.Range(0, frontier.Count - 1);
				Vector2Int childPos = frontier.ElementAt(randChoice).Key; // x, y rand | line 71

				if (map[childPos] == CellType.Wall)
				{
					map[childPos] = CellType.Empty;
					Destroy(world[childPos]);
					
					Vector2Int doorwayPos = new();

					if (frontier[childPos].y > childPos.y && frontier[childPos].x == childPos.x)
					{
						doorwayPos = new Vector2Int(childPos.x, childPos.y + 1);

						if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (frontier[childPos].y < childPos.y && frontier[childPos].x == childPos.x)
					{
						doorwayPos = new Vector2Int(childPos.x, childPos.y - 1);

						if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (frontier[childPos].x > childPos.x && frontier[childPos].y == childPos.y)
					{
						doorwayPos = new Vector2Int(childPos.x + 1, childPos.y);

						if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (frontier[childPos].x < childPos.x && frontier[childPos].y == childPos.y)
					{
						doorwayPos = new Vector2Int(childPos.x - 1, childPos.y);

						if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}

					//if (Random.value < STOP_COLLISION_PROBABILITY) break;

					foreach (Vector2Int childP in GetNeighbours(childPos))
					{
						Debug.Log("update");
						if (frontier.ContainsKey(childP)) continue;
						frontier.Add(childP, childPos); // childPos = parentPos
					}

					frontier.Remove(childPos);
				}
			}
		}*/
	}

	private void GenerateMap(int labirynth)
	{
		FillWithWalls();

		bool firstIteration = true;

		/*for (int f = 0; f < labirynth; f++)
		{
			//int randPosIndex = Random.Range(0, map.Count - 1);
			//Vector2Int startPos = map.ElementAt(randPosIndex).Key;

			int randX = Random.Range(tracker.playerRoomPosition.x - (size / 2), tracker.playerRoomPosition.x + (size / 2));
			int randZ = Random.Range(tracker.playerRoomPosition.y - (size / 2), tracker.playerRoomPosition.y + (size / 2));
			Vector2Int startPos = new Vector2Int(randX, randZ);

			List<Vector2Int> frontier = new();

			frontier.Add(startPos);
			//Dictionary<Vector2Int, Vector2Int> frontier = new() { parentPos };

			while (frontier.Count > 0)
			{
				foreach (Vector2Int childP in GetNeighbours(startPos))
				{
					if (visited[childP]) continue;

					if (!frontier.Contains(childP)) frontier.Add(childP);
				}

				int randPosIndex = Random.Range(0, frontier.Count - 1);
				Vector2Int parentPos = frontier[randPosIndex];

				if (!visited[parentPos]) visited[parentPos] = true;
				frontier.Remove(parentPos);

				map[parentPos] = CellType.Empty;
				Destroy(world[parentPos]); // GENERATE FLOOR!!! IF WALKABLE/ EMPTY

				if (Random.value < STOP_COLLISION_PROBABILITY)
				{
					//frontier.Add(nextCell);
					//map[nextCell] = CellType.Empty;
					//Destroy(world[nextCell]);
					continue;
				}

				List<Vector2Int> neighbors = new();

				foreach (Vector2Int childP in GetNeighbours(parentPos))
				{
					if (visited[childP]) continue;

					neighbors.Add(childP);
					if (!frontier.Contains(childP)) frontier.Add(childP);
				}

				if (neighbors.Count > 0)
				{
					randPosIndex = Random.Range(0, neighbors.Count - 1);
					Vector2Int nextCell = neighbors[randPosIndex];



					if (nextCell.y < parentPos.y && nextCell.x == parentPos.x)
					{
						Vector2Int doorwayPos = new Vector2Int(parentPos.x, parentPos.y - 1);

						if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							//frontier.Add(nextCell);
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (nextCell.y > parentPos.y && nextCell.x == parentPos.x)
					{
						Vector2Int doorwayPos = new Vector2Int(parentPos.x, parentPos.y + 1);

						if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							//frontier.Add(nextCell);
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (nextCell.y == parentPos.y && nextCell.x < parentPos.x)
					{
						Vector2Int doorwayPos = new Vector2Int(parentPos.x - 1, parentPos.y);

						if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							//frontier.Add(nextCell);
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (nextCell.y == parentPos.y && nextCell.x > parentPos.x)
					{
						Vector2Int doorwayPos = new Vector2Int(parentPos.x + 1, parentPos.y);

						if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							//frontier.Add(nextCell);
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
				}

				//frontier.AddRange(neighbors);
			}
		}*/

		for (int f = 0; f < labirynth; f++)
		{
			int randPosIndex = Random.Range(0, map.Count);
			Vector2Int parentPos = map.ElementAt(randPosIndex).Key;

			if (visited.ContainsKey(parentPos)) // added
			{
				if (visited[parentPos]) continue;
			}

			map[parentPos] = CellType.Empty;
			Destroy(world[parentPos]); // GENERATE FLOOR!!! IF WALKABLE/ EMPTY

			Dictionary<Vector2Int, Vector2Int> frontier = new();

			foreach (Vector2Int childP in GetNeighbours(parentPos))
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
					frontier.Remove(childPos);
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

						if (Random.value < randomRoomFactor)
						{
							int roomSizeX = Random.Range(randomRoomMinSize, randomRoomMaxSize);
                            int roomSizeY = Random.Range(randomRoomMinSize, randomRoomMaxSize);

							for (int roomX = doorwayPos.x - (roomSizeX / 2); roomX < doorwayPos.x + (roomSizeX / 2); roomX++)
							{
                                for (int roomY = doorwayPos.y - (roomSizeY / 2); roomY < doorwayPos.y + (roomSizeY / 2); roomY++)
                                {
									Vector2Int roomPos = new Vector2Int(roomX, roomY);

									if (!world.ContainsKey(roomPos)) continue;

                                    map[roomPos] = CellType.Empty;
                                    Destroy(world[roomPos]);
                                }
                            }
							continue;
						}

						if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (frontier[childPos].y < childPos.y && frontier[childPos].x == childPos.x)
					{
						doorwayPos = new Vector2Int(childPos.x, childPos.y - 1);

                        if (Random.value < randomRoomFactor)
                        {
                            int roomSizeX = Random.Range(randomRoomMinSize, randomRoomMaxSize);
                            int roomSizeY = Random.Range(randomRoomMinSize, randomRoomMaxSize);

                            for (int roomX = doorwayPos.x - (roomSizeX / 2); roomX < doorwayPos.x + (roomSizeX / 2); roomX++)
                            {
                                for (int roomY = doorwayPos.y - (roomSizeY / 2); roomY < doorwayPos.y + (roomSizeY / 2); roomY++)
                                {
                                    Vector2Int roomPos = new Vector2Int(roomX, roomY);

                                    if (!world.ContainsKey(roomPos)) continue;

                                    map[roomPos] = CellType.Empty;
                                    Destroy(world[roomPos]);
                                }
                            }
                            continue;
                        }

                        if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (frontier[childPos].x > childPos.x && frontier[childPos].y == childPos.y)
					{
						doorwayPos = new Vector2Int(childPos.x + 1, childPos.y);

                        if (Random.value < randomRoomFactor)
                        {
                            int roomSizeX = Random.Range(randomRoomMinSize, randomRoomMaxSize);
                            int roomSizeY = Random.Range(randomRoomMinSize, randomRoomMaxSize);

                            for (int roomX = doorwayPos.x - (roomSizeX / 2); roomX < doorwayPos.x + (roomSizeX / 2); roomX++)
                            {
                                for (int roomY = doorwayPos.y - (roomSizeY / 2); roomY < doorwayPos.y + (roomSizeY / 2); roomY++)
                                {
                                    Vector2Int roomPos = new Vector2Int(roomX, roomY);

                                    if (!world.ContainsKey(roomPos)) continue;

                                    map[roomPos] = CellType.Empty;
                                    Destroy(world[roomPos]);
                                }
                            }
                            continue;
                        }

                        if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}
					else if (frontier[childPos].x < childPos.x && frontier[childPos].y == childPos.y)
					{
						doorwayPos = new Vector2Int(childPos.x - 1, childPos.y);

                        if (Random.value < randomRoomFactor)
                        {
                            int roomSizeX = Random.Range(randomRoomMinSize, randomRoomMaxSize);
                            int roomSizeY = Random.Range(randomRoomMinSize, randomRoomMaxSize);

                            for (int roomX = doorwayPos.x - (roomSizeX / 2); roomX < doorwayPos.x + (roomSizeX / 2); roomX++)
                            {
                                for (int roomY = doorwayPos.y - (roomSizeY / 2); roomY < doorwayPos.y + (roomSizeY / 2); roomY++)
                                {
                                    Vector2Int roomPos = new Vector2Int(roomX, roomY);

                                    if (!world.ContainsKey(roomPos)) continue;

                                    map[roomPos] = CellType.Empty;
                                    Destroy(world[roomPos]);
                                }
                            }
                            continue;
                        }

                        if (map[doorwayPos] == CellType.Wall && Random.value < randomFactor)
						{
							map[doorwayPos] = CellType.Empty;
							Destroy(world[doorwayPos]);
						}
					}

					if (Random.value < randomStopFactor && !firstIteration) continue;

					foreach (Vector2Int childP in GetNeighbours(childPos))
					{
						Debug.Log("update");
						if (frontier.ContainsKey(childP)) continue;
						frontier.Add(childP, childPos); // childPos = parentPos
					}

					frontier.Remove(childPos);
					firstIteration = false;
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

				if (map.ContainsKey(currentPos) || visited.ContainsKey(currentPos)) continue;

				map.Add(currentPos, CellType.Wall);
				world.Add(currentPos, Instantiate(_wallPart));
				world[currentPos].transform.position = new Vector3(
					(currentPos.x * 2),
					world[currentPos].transform.position.y,
					(currentPos.y * 2)
				);
				visited.Add(currentPos, false); // added
			}
		}
	}

	private List<Vector2Int> GetNeighbours(Vector2Int pos)
	{
		List<Vector2Int> neighbors = new();
		
		if (/*pos.x > tracker.playerRoomPosition.x - (size / 2) && */map.ContainsKey(new Vector2Int(pos.x - 2, pos.y)) && map[new Vector2Int(pos.x - 2, pos.y)] != CellType.Empty) 
		{
			neighbors.Add(new Vector2Int(pos.x - 2, pos.y));
		}
		if (/*pos.x < tracker.playerRoomPosition.x + (size / 2) && */map.ContainsKey(new Vector2Int(pos.x + 2, pos.y)) && map[new Vector2Int(pos.x + 2, pos.y)] != CellType.Empty)
		{
			neighbors.Add(new Vector2Int(pos.x + 2, pos.y));
		}
		if (/*pos.y > tracker.playerRoomPosition.y - (size / 2) && */map.ContainsKey(new Vector2Int(pos.x, pos.y - 2)) && map[new Vector2Int(pos.x, pos.y - 2)] != CellType.Empty)
		{
			neighbors.Add(new Vector2Int(pos.x, pos.y - 2));
		}
		if (/*pos.y < tracker.playerRoomPosition.y + (size / 2) && */map.ContainsKey(new Vector2Int(pos.x, pos.y + 2)) && map[new Vector2Int(pos.x, pos.y + 2)] != CellType.Empty)
		{
			neighbors.Add(new Vector2Int(pos.x, pos.y + 2));
		}

		return neighbors;
	}

	[SerializeField] WorldTracker tracker;
}