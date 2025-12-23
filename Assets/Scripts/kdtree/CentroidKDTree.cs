using System.Collections.Generic;
using UnityEngine;

public class CentroidKDTree : IKDTree
{
    public KDNode root;
    public int maxTrianglesPerLeaf = 256;
    public int maxDepth = 32; // or log2(n)

    public KDNode RootNode
    {
        get { return root; }
    }

    public CentroidKDTree(List<TriangleInfo> triangleList)
    {
        root = Build(triangleList, 0);
    }

    private KDNode Build(List<TriangleInfo> tris, int depth)
    {
        KDNode node = new KDNode();
        node.bounds = ComputeBounds(tris);

        Vector3 size = node.bounds.size;
        float minSize = 0.005f; // or even 0.001f depending on model units

        if (tris.Count <= maxTrianglesPerLeaf || depth >= maxDepth || (size.x < minSize || size.y < minSize || size.z < minSize))
        {
            //Debug.Log($"Leaf at depth {depth} due to size {size} with {tris.Count} triangles");
            node.triangles = tris;
            return node;
        }
        
        int axis = GetLongestAxis(node.bounds.size);
        float mid = (node.bounds.max[axis] + node.bounds.min[axis]) / 2;

        // Implicit axis-aligned split plane
        node.axis = axis;
        node.splitPosition = mid;

        List<TriangleInfo> leftTris = new();
        List<TriangleInfo> rightTris = new();

        // Object-based KD-Tree: partitions triangles by centroid position (no duplicates)
        foreach (var tri in tris)
        {
            float c = tri.centroid[axis];

            if (c < node.splitPosition)
                leftTris.Add(tri);
            else
                rightTris.Add(tri);
        }

        // Fallback if split fails
        if (leftTris.Count == 0 || rightTris.Count == 0 || leftTris.Count == tris.Count || rightTris.Count == tris.Count)
        {
            //Debug.LogWarning($"Fallback split at depth {depth}: axis {axis}, mid {mid}, size {size}, count {tris.Count}");
            node.triangles = tris;
            return node;
        }

        node.left = Build(leftTris, depth + 1);
        node.right = Build(rightTris, depth + 1);

        return node;
    }

    private Bounds ComputeBounds(List<TriangleInfo> tris)
    {
        if (tris.Count == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        Bounds b = tris[0].bounds;

        for (int i = 1; i < tris.Count; i++)
            b.Encapsulate(tris[i].bounds);

        return b;
    }

    private int GetLongestAxis(Vector3 size)
    {
        if (size.x >= size.y && size.x >= size.z) return 0;
        if (size.y >= size.z) return 1;
        return 2;
    }
}
