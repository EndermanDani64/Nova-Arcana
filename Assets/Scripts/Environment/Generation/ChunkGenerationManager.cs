using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
                    rooms[currentRoomPos].GetComponent<Chunk>().LoadChunk();
                }
                else
                {
                    GameObject o = Instantiate(baseChunk, worldPos, Quaternion.identity);
                    o.GetComponent<Chunk>().cgm = this;

                    rooms.Add(currentRoomPos, o);
                }
            }
        }
    }

    [SerializeField] private GameObject baseChunk;
    [SerializeField] private WorldTracker worldTracker;
}