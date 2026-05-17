using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NodeConnection : MonoBehaviour
{
    public List<RoomNode> CompatibleNodes = new List<RoomNode>();
}
