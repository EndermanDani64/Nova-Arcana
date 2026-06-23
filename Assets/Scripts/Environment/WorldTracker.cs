using UnityEngine;

public class WorldTracker : MonoBehaviour
{
    public System.Action playerChunkPosChanged;

    void Update()
    {
        if (playerSmallChunkPosition != new Vector2Int(
                Mathf.RoundToInt(playerTransformPos.position.x / 10f),
                Mathf.RoundToInt(playerTransformPos.position.z / 10f)
            ))
        {
            playerSmallChunkPosition = new Vector2Int(
                Mathf.RoundToInt(playerTransformPos.position.x / 10f),
                Mathf.RoundToInt(playerTransformPos.position.z / 10f)
            );
        }
        if (playerLargeChunkPosition != new Vector2Int(
                Mathf.RoundToInt(playerTransformPos.position.x / 20f),
                Mathf.RoundToInt(playerTransformPos.position.z / 20f)
            ))
        {
            playerLargeChunkPosition = new Vector2Int(
                Mathf.RoundToInt(playerTransformPos.position.x / 20f),
                Mathf.RoundToInt(playerTransformPos.position.z / 20f)
            );
            playerChunkPosChanged?.Invoke();
        }
    }

    public Vector2Int playerLargeChunkPosition;
    public Vector2Int playerSmallChunkPosition;
    public Transform playerTransformPos;
}
