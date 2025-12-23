using System.Collections.Generic;
using UnityEngine;

public class Octree
{
    public OctreeNode root;
    public int maxDepth = 8;
    public int maxTrianglesPerNode = 4;
    public float minOctreeNodeSize = 0.01f;

    // Constructor — builds the full tree directly
    public Octree(List<TriangleInfo> triangles)
    {
        if (triangles == null || triangles.Count == 0)
        {
            root = new OctreeNode(new Bounds(Vector3.zero, Vector3.zero));
            return;
        }

        Bounds totalBounds = ComputeBounds(triangles);
        root = BuildOctreeNode(triangles, totalBounds, 0);
    }


    // Recursive build
    private OctreeNode BuildOctreeNode(List<TriangleInfo> tris, Bounds bounds, int depth)
    {
        OctreeNode node = new OctreeNode(bounds);

        // Leaf condition
        Vector3 size = bounds.size;
        bool tooSmall = size.x < minOctreeNodeSize || size.y < minOctreeNodeSize || size.z < minOctreeNodeSize;

        if (tris.Count <= maxTrianglesPerNode || depth >= maxDepth || tooSmall)
        {
            node.triangles = tris;
            return node;
        }

        // Subdivide node
        node.children = CreateChildren(bounds);

        // Distribute triangles to children
        bool anyChildHasTriangles = false;

        for (int i = 0; i < node.children.Length; i++)
        {
            OctreeNode child = node.children[i];
            List<TriangleInfo> childTris = new();

            foreach (var tri in tris)
            {
                if (child.bounds.Intersects(tri.bounds))
                    childTris.Add(tri);
            }

            if (childTris.Count > 0)
            {
                anyChildHasTriangles = true;
                node.children[i] = BuildOctreeNode(childTris, child.bounds, depth + 1); // must reassign or the child subtree won't link to the tree
            }
        }

        // Optimization: avoid meaningless subdivision
        // If all children are empty, revert this node to a leaf.
        if (!anyChildHasTriangles)
        {
            node.children = null;
            node.triangles = tris;
        }

        return node;
    }

    // Create 8 child cubes
    private OctreeNode[] CreateChildren(Bounds parentBounds)
    {
        OctreeNode[] children = new OctreeNode[8];
        Vector3 childBoundSize = parentBounds.size / 2f; // child box size (half of parent)
        Vector3 parentCenter = parentBounds.center;

        int i = 0;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 dir = new Vector3(x, y, z); // direction to corner: ±1 in each axis
                    Vector3 halfChildBoundSize = childBoundSize * 0.5f; // half of child size = quarter of parent size
                    Vector3 scaledOffset = Vector3.Scale(dir, halfChildBoundSize); // element-wise scale 
                    Bounds childBounds = new Bounds(parentCenter + scaledOffset, childBoundSize);
                    children[i] = new OctreeNode(childBounds);
                    i++;
                }
            }
        }

        return children;
    }

    // Compute the total AABB around all triangles
    private Bounds ComputeBounds(List<TriangleInfo> tris)
    {
        Bounds b = tris[0].bounds;

        for (int i = 1; i < tris.Count; i++)
            b.Encapsulate(tris[i].bounds);

        return b;
    }
}
