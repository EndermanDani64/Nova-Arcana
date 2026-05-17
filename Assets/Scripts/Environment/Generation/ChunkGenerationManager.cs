using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkGenerationManager : MonoBehaviour
{
    public Dictionary<Vector2Int, GameObject> rooms;
    [SerializeField] private int renderDistance = 8;

    private void Start()
    {
        rooms = new();
        ManageChunksLoading();

        worldTracker.playerChunkPosChanged += ManageChunksLoading;
    }

    private void ManageChunksLoading()
    {
        for (int x = worldTracker.playerRoomPosition.x - renderDistance; x < worldTracker.playerRoomPosition.x + renderDistance; x++)
        {
            for (int y = worldTracker.playerRoomPosition.y - renderDistance; y < worldTracker.playerRoomPosition.y + renderDistance; y++)
            {
                Vector2Int currentRoomPos = new Vector2Int(x, y);

                Vector3 worldPos = new Vector3(
                    (x * 15) + 15,
                    0,
                    (y * 15) + 15
                );

                if (rooms.Keys.Contains(currentRoomPos))
                {
                    rooms[currentRoomPos].GetComponent<RoomNode>().LoadChunk();
                }
                else
                {

                    // collapse logic

                    GameObject o = Instantiate(roomNodes[0], worldPos, Quaternion.identity);

                    rooms.Add(currentRoomPos, o);
                }
            }
        }
    }

    [SerializeField] private GameObject[] roomNodes;


    [SerializeField] private WorldTracker worldTracker;
}