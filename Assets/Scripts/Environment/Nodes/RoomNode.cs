using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomNode", menuName = "Nodes/RoomNode")]
[System.Serializable]
public class RoomNode : MonoBehaviour
{
    public Vector2Int NodePos;
    public GameObject Prefab;

    public NodeConnection nodeConnection;

    public int NodeWidth;
    public int NodeDepth;

    // unity methods

    private void Start()
    {
        NodeWidth = Mathf.RoundToInt(gameObject.GetComponent<Transform>().localScale.x / 1.5f);
        NodeDepth = Mathf.RoundToInt(gameObject.GetComponent<Transform>().localScale.z / 1.5f);
        Prefab = gameObject;
    }

    public void ToggleNode()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
    public void LoadNode()
    {
        if (gameObject.activeSelf) return;

        gameObject.SetActive(true);
    }
    public void DeloadNode()
    {
        if (!gameObject.activeSelf) return;

        gameObject.SetActive(false);
    }

    [SerializeField] private Transform origin;
}