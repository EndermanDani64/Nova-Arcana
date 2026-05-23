using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerationManager : MonoBehaviour
{
    [SerializeField] private GameObject initialNode;
    public Dictionary<Vector2Int, GameObject> rooms;
    public Dictionary<Vector2Int, GameObject> activeRooms;

    [SerializeField] private List<GameObject> _allPossibleNodes = new List<GameObject>();

    [SerializeField] private int renderDistance = 4;

    private Vector2Int[] offsets = new Vector2Int[]
    {
        new Vector2Int(0, 1), // fel
        new Vector2Int(0, -1), // le
        new Vector2Int(1, 0), // jobb
        new Vector2Int(-1, 0) // bal
    };

    private void Start()
    {
        rooms = new Dictionary<Vector2Int, GameObject>();
        activeRooms = new Dictionary<Vector2Int, GameObject>();
        //rooms.Add(new Vector2Int(0, 0), initialNode);
        //activeRooms.Add(new Vector2Int(0, 0), initialNode);

        ManageChunksLoading();

        worldTracker.playerChunkPosChanged += ManageChunksLoading;
        worldTracker.playerChunkPosChanged += DeloadChunks;
    }

    private void DeloadChunks()
    {
        foreach (KeyValuePair<Vector2Int, GameObject> room in rooms)
        {
            GameObject g = room.Value;

            if (Vector3.Distance(g.transform.position, player.GetComponent<Transform>().position) > 120)
            {
                g.GetComponent<RoomNode>().DeloadNode();
                activeRooms.Remove(room.Key);
            }
        }
    }

    private void ManageChunksLoading()
    {
        Vector2Int currentPlayerPos = worldTracker.playerRoomPosition;

        for (int x = currentPlayerPos.x - renderDistance; x < currentPlayerPos.x + renderDistance; x++)
        {
            for (int y = currentPlayerPos.y - renderDistance; y < currentPlayerPos.y + renderDistance; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);

                if (!rooms.ContainsKey(currentPos))
                {
                    List<GameObject> potentionalNodes = new List<GameObject>(_allPossibleNodes);

                    Vector3 worldPos = new Vector3(
                        (currentPos.x * 15) + 15,
                        0,
                        (currentPos.y * 15) + 15
                    );

                    for (int i = 0; i < offsets.Length; i++) // végigmegyünk az összes szomszédon
                    {
                        Vector2Int neighbourPos = new Vector2Int(x + offsets[i].x, y + offsets[i].y);

                        if (rooms.ContainsKey(neighbourPos))
                        {
                            switch (i)
                            {
                                case 0: // felül
                                    PopNodes(
                                        potentionalNodes,
                                        rooms[neighbourPos].GetComponent<RoomNode>().nodeConnection.Top
                                    );
                                    break;
                                case 1: // alul
                                    PopNodes(
                                        potentionalNodes,
                                        rooms[neighbourPos].GetComponent<RoomNode>().nodeConnection.Bottom
                                    );
                                    break;
                                case 2:
                                    PopNodes(
                                        potentionalNodes,
                                        rooms[neighbourPos].GetComponent<RoomNode>().nodeConnection.Right
                                    );
                                    break;
                                case 3:
                                    PopNodes(
                                        potentionalNodes,
                                        rooms[neighbourPos].GetComponent<RoomNode>().nodeConnection.Left
                                    );
                                    break;
                            }
                            //Debug.Log($"jelen poz: {currentPos}, szomszéd pozició: {neighbourPos}, potentionalNodes = ({TEMP_Print(potentionalNodes)})", rooms[neighbourPos]);

                            if (potentionalNodes.Count == 0)
                            {
                                potentionalNodes.Add(initialNode);
                                continue;
                            }
                        }
                        else
                        {
                            //Debug.Log($"nem volt a szomszédos helyen senki, jelen poz: {currentPos}, szomszéd pozició: {neighbourPos}");
                            continue;
                        }
                    }

                    int ranIndex = Random.Range(0, potentionalNodes.Count - 1);

                    if (ranIndex <= potentionalNodes.Count)
                    {
                        GameObject newRoom = Instantiate(potentionalNodes[ranIndex], worldPos, Quaternion.identity);
                        rooms.Add(currentPos, newRoom);
                        activeRooms.Add(currentPos, newRoom);
                    }
                    else
                    {
                        GameObject newRoom = Instantiate(potentionalNodes[0], worldPos, Quaternion.identity);
                        rooms.Add(currentPos, newRoom);
                        activeRooms.Add(currentPos, newRoom);
                    }
                    //Debug.Log($"GENERÁLVA poz: {currentPos}", rooms[currentPos]);
                }
                else
                {
                    rooms[currentPos].GetComponent<RoomNode>().LoadNode();
                    if (!activeRooms.ContainsKey(currentPos)) activeRooms.Add(currentPos, rooms[currentPos]);
                }
            }
        }
    }

    private void PopNodes(List<GameObject> potentionalNodes, List<GameObject> validNodes)
    {
        for (int i = potentionalNodes.Count - 1; i > -1; i--)
        {
            if (!validNodes.Contains(potentionalNodes[i]))
            {
                potentionalNodes.RemoveAt(i);
            }
        }
    }

    [SerializeField] private Player player;
    [SerializeField] private WorldTracker worldTracker;
}