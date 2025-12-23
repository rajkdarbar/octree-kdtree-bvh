using System.Collections.Generic;
using UnityEngine;

public class KDNode
{
    public Bounds bounds;
    public List<TriangleInfo> triangles;
    public KDNode left;
    public KDNode right;

    // Implicit axis-aligned split plane: axis + splitPosition
    public int axis; // splitting axis: 0 = X, 1 = Y, 2 = Z
    public float splitPosition;

    public bool IsLeaf
    {
        get
        {
            return left == null && right == null;
        }
    }
}
