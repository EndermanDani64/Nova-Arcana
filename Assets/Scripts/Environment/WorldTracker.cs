using UnityEngine;

public class WorldTracker : MonoBehaviour
{
    public System.Action playerLargeChunkPosChanged;
    public System.Action playerSmallChunkPosChanged;

    void Update()
    {
        if (playerSmallChunkPosition != new Vector2Int(
                Mathf.RoundToInt(playerTransformPos.position.x / (cgmRef.smallChunkSize * cgmRef.cellSize)),
                Mathf.RoundToInt(playerTransformPos.position.z / (cgmRef.smallChunkSize * cgmRef.cellSize))
            ))
        {
            playerSmallChunkPosition = new Vector2Int(
                Mathf.RoundToInt(playerTransformPos.position.x / (cgmRef.smallChunkSize * cgmRef.cellSize)),
                Mathf.RoundToInt(playerTransformPos.position.z / (cgmRef.smallChunkSize * cgmRef.cellSize))
            );
            playerSmallChunkPosChanged?.Invoke();
        }

        if (playerLargeChunkPosition != new Vector2Int(
                Mathf.RoundToInt(playerTransformPos.position.x / (cgmRef.largeChunkSize * cgmRef.cellSize)),
                Mathf.RoundToInt(playerTransformPos.position.z / (cgmRef.largeChunkSize * cgmRef.cellSize))
            ))
        {
            playerLargeChunkPosition = new Vector2Int(
                Mathf.RoundToInt(playerTransformPos.position.x / (cgmRef.largeChunkSize * cgmRef.cellSize)),
                Mathf.RoundToInt(playerTransformPos.position.z / (cgmRef.largeChunkSize * cgmRef.cellSize))
            );
            playerLargeChunkPosChanged?.Invoke();
        }

        playerCellPosition = new Vector2Int(
            Mathf.RoundToInt(playerTransformPos.position.x / cgmRef.cellSize),
            Mathf.RoundToInt(playerTransformPos.position.z / cgmRef.cellSize)
        );
    }

    public Vector2Int playerLargeChunkPosition;
    public Vector2Int playerSmallChunkPosition;
    public Vector2Int playerCellPosition;
    public Transform playerTransformPos;

    [SerializeField] private ChunkGenerationManager cgmRef;
}
