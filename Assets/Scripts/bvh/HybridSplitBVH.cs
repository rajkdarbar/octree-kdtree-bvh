using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HybridSplitBVH
{
    public BVHNode root;
    public int maxTrianglesPerLeaf = 64;
    public int sahThreshold = 256; // triangle count below which SAH is applied
    public static int numBins = 16;
    private const float EPSILON = 1e-5f;

    private List<TriangleInfo>[] bins = new List<TriangleInfo>[numBins];
    private List<TriangleInfo> left = new List<TriangleInfo>();
    private List<TriangleInfo> right = new List<TriangleInfo>();
    private List<TriangleInfo> leftSplit = new();
    private List<TriangleInfo> rightSplit = new();

    public HybridSplitBVH(List<TriangleInfo> triangleList)
    {
        for (int i = 0; i < numBins; i++)
            bins[i] = new List<TriangleInfo>();

        root = Build(triangleList, 0);
    }

    // Quickly update the BVH without rebuilding the entire tree 
    public void Refit(BVHNode node)
    {
        if (node == null) return;

        if (node.IsLeaf)
        {
            foreach (var tri in node.triangles)
                tri.UpdateBounds();

            node.bounds = ComputeBounds(node.triangles);
        }
        else
        {
            Refit(node.left);
            Refit(node.right);

            node.bounds = node.left.bounds;
            node.bounds.Encapsulate(node.right.bounds);
        }
    }

    // Uses median and Surface Area Heuristic (SAH) based splitting
    private BVHNode Build(List<TriangleInfo> tris, int depth)
    {
        BVHNode node = new BVHNode();
        node.bounds = ComputeBounds(tris);

        if (tris.Count <= maxTrianglesPerLeaf)
        {
            node.triangles = tris;
            return node;
        }

        Bounds centroidBounds = ComputeCentroidBounds(tris);
        int axis = GetLongestAxis(centroidBounds.size);

        List<TriangleInfo> leftTris, rightTris;

        if (tris.Count <= sahThreshold)
        {
            (leftTris, rightTris) = FindBestSAHSplit(tris, axis); // Surface Area Heuristic (SAH) based splitting
        }
        else
        {
            // Median based splitting 
            tris.Sort((a, b) => a.centroid[axis].CompareTo(b.centroid[axis])); // lambda expression
            int mid = tris.Count / 2;
            leftTris = tris.GetRange(0, mid);
            rightTris = tris.GetRange(mid, tris.Count - mid);
        }

        node.left = Build(leftTris, depth + 1);
        node.right = Build(rightTris, depth + 1);

        return node;
    }

    private (List<TriangleInfo>, List<TriangleInfo>) FindBestSAHSplit(List<TriangleInfo> tris, int axis)
    {
        Bounds centroidBounds = ComputeCentroidBounds(tris);
        float min = centroidBounds.min[axis];
        float max = centroidBounds.max[axis];

        // All triangle centroids (or nearly all) overlap at the same position along the chosen split axis.
        if (max - min < EPSILON)
        {
            // Degenerate case, fallback to median split
            tris.Sort((a, b) => a.centroid[axis].CompareTo(b.centroid[axis]));
            int mid = tris.Count / 2;
            return (tris.GetRange(0, mid), tris.GetRange(mid, tris.Count - mid));
        }

        float binSize = (max - min) / numBins;

        for (int i = 0; i < numBins; i++)
            bins[i].Clear();

        // Figures out which bin each triangle belongs to
        foreach (var tri in tris)
        {
            int binIndex = Mathf.Clamp((int)((tri.centroid[axis] - min) / binSize), 0, numBins - 1);
            bins[binIndex].Add(tri);
        }

        float bestCost = float.MaxValue;
        int bestSplit = -1;

        // Trying to split between bins 0..(i-1) and i..(numBins-1)
        // Start with i = 1 to avoid an empty left group.
        for (int i = 1; i < numBins; i++)
        {
            left.Clear();
            right.Clear();

            // Adds all triangles from bins 0 to (i - 1) into the left group
            for (int j = 0; j < i; j++)
                left.AddRange(bins[j]);

            // Adds all triangles from bins i to (numBins - 1) into the right group
            for (int j = i; j < numBins; j++)
                right.AddRange(bins[j]);

            // If either side has no triangles, skip evaluating this split — it’s not valid.
            if (left.Count == 0 || right.Count == 0)
                continue;

            float leftArea = GetSurfaceArea(ComputeBounds(left));
            float rightArea = GetSurfaceArea(ComputeBounds(right));
            float parentArea = GetSurfaceArea(ComputeBounds(tris));

            float cost = (leftArea * left.Count + rightArea * right.Count) / parentArea;

            if (cost < bestCost)
            {
                bestCost = cost;
                bestSplit = i;
            }
        }

        if (bestSplit == -1)
        {
            // Fallback to median split if SAH fails
            tris.Sort((a, b) => a.centroid[axis].CompareTo(b.centroid[axis]));
            int mid = tris.Count / 2;
            return (tris.GetRange(0, mid), tris.GetRange(mid, tris.Count - mid));
        }

        leftSplit.Clear();
        rightSplit.Clear();

        for (int x = 0; x < bestSplit; x++)
            leftSplit.AddRange(bins[x]);

        for (int x = bestSplit; x < numBins; x++)
            rightSplit.AddRange(bins[x]);

        return (leftSplit, rightSplit);
    }

    private Bounds ComputeBounds(List<TriangleInfo> tris)
    {
        Bounds b = tris[0].bounds;
        for (int i = 1; i < tris.Count; i++)
            b.Encapsulate(tris[i].bounds);
        return b;
    }

    private Bounds ComputeCentroidBounds(List<TriangleInfo> tris)
    {
        Bounds b = new Bounds(tris[0].centroid, Vector3.zero);
        for (int i = 1; i < tris.Count; i++)
            b.Encapsulate(tris[i].centroid);
        return b;
    }

    private int GetLongestAxis(Vector3 size)
    {
        if (size.x > size.y && size.x > size.z) return 0;
        if (size.y > size.z) return 1;
        return 2;
    }

    // Total surface area of the box is: 2 * (xy + yz + zx)
    private float GetSurfaceArea(Bounds b)
    {
        Vector3 size = b.size;
        return 2f * (size.x * size.y + size.y * size.z + size.z * size.x);
    }
}