using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerationManager : MonoBehaviour
{
    [SerializeField] private GameObject initialNode;
    public Dictionary<Vector2Int, GameObject> rooms;

    [SerializeField] private List<GameObject> _allPossibleNodes = new List<GameObject>();

    [SerializeField] private int renderDistance = 8;

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
        rooms.Add(new Vector2Int(0, 0), initialNode);

        ManageChunksLoading();

        worldTracker.playerChunkPosChanged += ManageChunksLoading;
    }

    private void ManageChunksLoading()
    {
        Vector2Int currentPlayerPos = worldTracker.playerRoomPosition;
        int chunkLoadingQueueLength = 0;

        Debug.Log("ran");

        for (int x = currentPlayerPos.x - renderDistance; x < currentPlayerPos.x + renderDistance; x++)
        {
            for (int y = currentPlayerPos.y - renderDistance; y < currentPlayerPos.y + renderDistance; y++)
            {
                List<GameObject> potentionalNodes = new List<GameObject>(_allPossibleNodes);

                for (int i = 0; i < offsets.Length; i++) // végigmegyünk az összes szomszédon
                {
                    Vector2Int neighbourPos = currentPlayerPos + (offsets[i] * chunkLoadingQueueLength);

                    if (!rooms.ContainsKey(neighbourPos))
                    {
                        switch (i)
                        {
                            case 0:
                                PopNodes(
                                    potentionalNodes,
                                    rooms[currentPlayerPos].GetComponent<RoomNode>().nodeConnection.Top
                                );
                                break;
                            case 1:
                                PopNodes(
                                    potentionalNodes,
                                    rooms[currentPlayerPos].GetComponent<RoomNode>().nodeConnection.Bottom
                                );
                                break;
                            case 2:
                                PopNodes(
                                    potentionalNodes,
                                    rooms[currentPlayerPos].GetComponent<RoomNode>().nodeConnection.Right
                                );
                                break;
                            case 3:
                                PopNodes(
                                    potentionalNodes,
                                    rooms[currentPlayerPos].GetComponent<RoomNode>().nodeConnection.Left
                                );
                                break;
                        }

                        Vector3 worldPos = new Vector3(
                            (neighbourPos.x * 15) + 15,
                            0,
                            (neighbourPos.y * 15) + 15
                        );

                        GameObject newRoom = Instantiate(potentionalNodes[Random.Range(0, potentionalNodes.Count)], worldPos, Quaternion.identity);
                        rooms.Add(neighbourPos, newRoom);
                    }
                    else
                    {
                        rooms[neighbourPos].GetComponent<RoomNode>().LoadNode();
                    }
                }
            }
        }

        /*while (chunkLoadingQueueLength < renderDistance)
        {
            List<GameObject> potentionalNodes = new List<GameObject>(_allPossibleNodes);

            for (int i = 0; i < offsets.Length; i++) // végigmegyünk az összes szomszédon
            {
                Vector2Int neighbourPos = currentPlayerPos + (offsets[i] * chunkLoadingQueueLength);

                if (!rooms.ContainsKey(neighbourPos))
                {
                    switch (i)
                    {
                        case 0:
                            PopNodes(
                                potentionalNodes,
                                rooms[currentPlayerPos].GetComponent<RoomNode>().nodeConnection.Top
                            );
                            break;
                        case 1:
                            PopNodes(
                                potentionalNodes,
                                rooms[currentPlayerPos].GetComponent<RoomNode>().nodeConnection.Bottom
                            );
                            break;
                        case 2:
                            PopNodes(
                                potentionalNodes,
                                rooms[currentPlayerPos].GetComponent<RoomNode>().nodeConnection.Right
                            );
                            break;
                        case 3:
                            PopNodes(
                                potentionalNodes,
                                rooms[currentPlayerPos].GetComponent<RoomNode>().nodeConnection.Left
                            );
                            break;
                    }

                    Vector3 worldPos = new Vector3(
                        (neighbourPos.x * 15) + 15,
                        0,
                        (neighbourPos.y * 15) + 15
                    );

                    GameObject newRoom = Instantiate(potentionalNodes[Random.Range(0, potentionalNodes.Count)], worldPos, Quaternion.identity);
                    rooms.Add(neighbourPos, newRoom);
                }
                else
                {
                    rooms[neighbourPos].GetComponent<RoomNode>().LoadNode();
                }
            }

            chunkLoadingQueueLength++;
        }*/
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
    
    [SerializeField] private WorldTracker worldTracker;
}