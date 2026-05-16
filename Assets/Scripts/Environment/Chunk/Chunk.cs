using NUnit.Framework;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Vector2Int chunkPos;
    public Vector2Int[] chunkPoses;

    public int chunkWidth;
    public int chunkDepth;

    // unity methods

    private void Start()
    {
        chunkWidth = Mathf.RoundToInt(gameObject.GetComponent<Transform>().localScale.x / 1.5f);
        chunkDepth = Mathf.RoundToInt(gameObject.GetComponent<Transform>().localScale.z / 1.5f);
    }

    public void ToggleChunk()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
    public void LoadChunk()
    {
        if (gameObject.activeSelf) return;

        gameObject.SetActive(true);
    }
    public void DeloadChunk()
    {
        if (!gameObject.activeSelf) return;

        gameObject.SetActive(false);
    }

    [SerializeField] private Transform origin;
    public ChunkGenerationManager cgm;
}