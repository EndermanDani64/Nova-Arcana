using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomNode", menuName = "Nodes/RoomNode")]
[System.Serializable]
public class RoomNode : MonoBehaviour
{
    public Vector2Int NodePos;
    public GameObject Prefab;

    public NodeConnection Top;
    public NodeConnection Bottom;
    public NodeConnection Left;
    public NodeConnection Right;

    public int NodeWidth;
    public int NodeDepth;

    // unity methods

    private void Start()
    {
        NodeWidth = Mathf.RoundToInt(gameObject.GetComponent<Transform>().localScale.x / 1.5f);
        NodeDepth = Mathf.RoundToInt(gameObject.GetComponent<Transform>().localScale.z / 1.5f);
        Prefab = gameObject;
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
}