/*

KD-Tree Variants Overview

1️⃣ ObjectKDTree  (a.k.a. centroid-based KD-Tree)
    - Build the overall bounding box for the current node.
    - Find the *longest axis* of that box.
    - Compute the split plane at the *midpoint* of that axis.
    - For each triangle, compare its centroid’s coordinate 
      on that axis with the split plane:
        • centroid[axis] < split → goes to left
        • centroid[axis] ≥ split → goes to right
    - No triangle duplication (each triangle belongs to one side).
    - Fast to build; partitions based on centroid positions only.   

2️⃣ SpatialKDTree (a.k.a. spatial or classical KD-Tree)
    - Compute the node’s bounding box and find its *longest axis*.
    - Split space at the *midpoint* of that axis (split plane).
    - For each triangle, check its own bounding box extents:
        • If its [min, max] range overlaps the split plane → add to both sides.
        • If it lies fully on one side → add to that side only.
    - Triangles may be *duplicated* when spanning the plane.
    - Produces tight, spatially accurate partitions with minimal overlap.
    - Ideal for ray tracing and precise spatial queries.

*/

using System.Collections.Generic;
using UnityEngine;

public class KDTreeManager : MonoBehaviour
{
    public GameObject _3DModel;

    public enum KDTreeMode { Spatial, Object }
    [Header("KDTree Settings")]
    public KDTreeMode buildMode = KDTreeMode.Spatial;

    public int kdTreeDepth;
    private int clampedMaxDepthToDraw;

    [Range(0, 32)]
    public int userMaxDepthToDraw = 5;

    public bool showKDTree = true;

    private MeshFilter mf;
    private List<TriangleInfo> triangleList = new();

    private IKDTree kdTree;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;

    private Color nodeColor = Color.yellow;

    void Start()
    {
        CacheTransformState();

        mf = _3DModel.GetComponentInChildren<MeshFilter>();

        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("No MeshFilter or mesh found on this GameObject.");
            return;
        }

        triangleList = ExtractTriangles(mf);

        if (buildMode == KDTreeMode.Spatial)
            kdTree = new SpatialKDTree(triangleList);
        else
            kdTree = new ObjectKDTree(triangleList);

        kdTreeDepth = ComputeDepth(kdTree.RootNode);
        clampedMaxDepthToDraw = Mathf.Clamp(userMaxDepthToDraw, 0, kdTreeDepth);
    }

    void Update()
    {
        Transform t = _3DModel.transform;

        if (t.position != lastPosition || t.rotation != lastRotation || t.localScale != lastScale)
        {
            triangleList = ExtractTriangles(mf);

            if (buildMode == KDTreeMode.Spatial)
                kdTree = new SpatialKDTree(triangleList);
            else
                kdTree = new ObjectKDTree(triangleList);

            kdTreeDepth = ComputeDepth(kdTree.RootNode);
            clampedMaxDepthToDraw = Mathf.Clamp(userMaxDepthToDraw, 0, kdTreeDepth);

            CacheTransformState();
        }

        clampedMaxDepthToDraw = Mathf.Clamp(userMaxDepthToDraw, 0, kdTreeDepth);
    }

    void OnDrawGizmos()
    {
        if (showKDTree && kdTree != null && kdTree.RootNode != null)
        {
            Gizmos.color = nodeColor;
            DrawKDNode(kdTree.RootNode, 0);
        }
    }

    void DrawKDNode(KDNode node, int depth)
    {
        if (node == null || depth > clampedMaxDepthToDraw) return;

        Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);

        if (!node.IsLeaf)
        {
            DrawKDNode(node.left, depth + 1);
            DrawKDNode(node.right, depth + 1);
        }
    }

    List<TriangleInfo> ExtractTriangles(MeshFilter mf)
    {
        Mesh mesh = mf.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.triangles;

        var tris = new List<TriangleInfo>();
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 v0 = mf.transform.TransformPoint(vertices[indices[i]]);
            Vector3 v1 = mf.transform.TransformPoint(vertices[indices[i + 1]]);
            Vector3 v2 = mf.transform.TransformPoint(vertices[indices[i + 2]]);

            tris.Add(new TriangleInfo(v0, v1, v2));
        }
        return tris;
    }

    int ComputeDepth(KDNode node)
    {
        if (node == null || node.IsLeaf) return 0;

        int depth;
        int leftDepth = ComputeDepth(node.left);
        int rightDepth = ComputeDepth(node.right);
        depth = Mathf.Max(leftDepth, rightDepth);

        return 1 + depth;
    }

    void CacheTransformState()
    {
        lastPosition = _3DModel.transform.position;
        lastRotation = _3DModel.transform.rotation;
        lastScale = _3DModel.transform.localScale;
    }
}
