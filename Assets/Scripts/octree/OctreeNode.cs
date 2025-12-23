using System;
using System.Collections.Generic;
using UnityEngine;

public class OctreeNode
{
    public Bounds bounds;
    public List<TriangleInfo> triangles = new List<TriangleInfo>();
    public OctreeNode[] children;
    public bool IsLeaf => children == null;

    public OctreeNode(Bounds b)
    {
        bounds = b;
    }
}