using System.Collections.Generic;
using UnityEngine;

public class RoomNode : MonoBehaviour
{
    public Vector2Int NodePos;
    public GameObject Prefab;

    public NodeConnection nodeConnection;

    public int NodeWidth;
    public int NodeDepth;

    private bool isLit = true;
    [SerializeField] private List<Light> lights = new();

    // unity methods

    private void Start()
    {
        if (Random.Range(0, 100) > 95) ToggleLightsWhole();

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

    private void ToggleLightsWhole()
    {
        foreach (Light l in lights)
            l.enabled = !l.enabled;
    }

    [SerializeField] private Transform origin;
}