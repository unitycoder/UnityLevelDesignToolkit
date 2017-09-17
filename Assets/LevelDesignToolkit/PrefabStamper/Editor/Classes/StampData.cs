using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
class StampData
{
    public Vector3 center = Vector3.zero;
    public Bounds bounds = new Bounds();
    public List<StampChild> prefabs = new List<StampChild>();
}