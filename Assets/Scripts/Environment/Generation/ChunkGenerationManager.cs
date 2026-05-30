using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerationManager : MonoBehaviour
{
    [SerializeField] private GameObject initialNode;
    public Dictionary<Vector2Int, Chunk> rooms;
    public Dictionary<Vector2Int, Chunk> activeRooms;

    [SerializeField] private int renderDistance = 4;

    private void Start()
    {
        rooms = new Dictionary<Vector2Int, Chunk>();
        activeRooms = new Dictionary<Vector2Int, Chunk>();

        ManageChunksLoading();

        worldTracker.playerChunkPosChanged += ManageChunksLoading;
        worldTracker.playerChunkPosChanged += DeloadChunks;
    }

    private void DeloadChunks()
    {
        foreach (KeyValuePair<Vector2Int, Chunk> room in rooms)
        {
            Chunk g = room.Value;

            /*if (Vector3.Distance(g.transform.position, player.GetComponent<Transform>().position) > 120)
            {
                g.GetComponent<RoomNode>().DeloadNode();
                activeRooms.Remove(room.Key);
            }*/
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
                    
                }
                else
                {
                    //rooms[currentPos].GetComponent<RoomNode>().LoadNode();
                    if (!activeRooms.ContainsKey(currentPos)) activeRooms.Add(currentPos, rooms[currentPos]);
                }
            }
        }
    }

    [SerializeField] private Player player;
    [SerializeField] private WorldTracker worldTracker;
}

public class Chunk
{
    private bool active = true;
    public List<GameObject> walls = new();

    public void LoadChunk()
    {
        if (active) return;
        
        foreach (GameObject wall in walls)
        {
            if (wall.activeSelf) continue;
            wall.SetActive(true);
        }
    }

    public void DeloadChunk()
    {
        if (!active) return;
        
        foreach (GameObject wall in walls)
        {
            if (!wall.activeSelf) continue;
            wall.SetActive(false);
        }
    }
}