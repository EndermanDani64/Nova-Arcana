using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class NodeConnection : MonoBehaviour
{
    [Header("Compatible Node Lists")]
    public List<GameObject> Top = new List<GameObject>();
    public List<GameObject> Bottom = new List<GameObject>();
    public List<GameObject> Left = new List<GameObject>();
    public List<GameObject> Right = new List<GameObject>();
}
