using UnityEngine;

public class WorldTracker : MonoBehaviour
{
    public System.Action playerChunkPosChanged;

    void Update()
    {
        if (playerRoomPosition != new Vector2Int(
                Mathf.RoundToInt(playerTransformPos.position.x / 10f),
                Mathf.RoundToInt(playerTransformPos.position.z / 10f)
            ))
        {
            playerRoomPosition = new Vector2Int(
                Mathf.RoundToInt(playerTransformPos.position.x / 10f),
                Mathf.RoundToInt(playerTransformPos.position.z / 10f)
            );
            playerChunkPosChanged?.Invoke();
        } 
    }

    public Vector2Int playerRoomPosition;
    public Transform playerTransformPos;
}
