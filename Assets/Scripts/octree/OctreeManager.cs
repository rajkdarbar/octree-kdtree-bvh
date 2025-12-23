using System.Collections.Generic;
using UnityEngine;

public class OctreeManager : MonoBehaviour
{
    [Header("Target Mesh Object")]
    public GameObject targetObject;

    [Header("Visualization Settings")]
    public bool showOctree = true;
    [Range(0, 8)] public int userMaxDepthToDraw = 4;
    private int clampedMaxDepthToDraw;

    private MeshFilter mf;
    private List<TriangleInfo> triangleList = new();
    private Octree octree;
    private int octreeDepth;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;

    private Color nodeColor = Color.cyan;

    void Start()
    {
        CacheTransformState();

        mf = targetObject.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("No MeshFilter or mesh found on this GameObject.");
            return;
        }

        triangleList = ExtractTriangles(mf);
        octree = new Octree(triangleList);
        octreeDepth = ComputeDepth(octree.root);
        clampedMaxDepthToDraw = Mathf.Clamp(userMaxDepthToDraw, 0, octreeDepth);
    }

    void Update()
    {
        Transform t = targetObject.transform;

        // Rebuild if the object moves, rotates, or scales
        if (t.position != lastPosition || t.rotation != lastRotation || t.localScale != lastScale)
        {
            triangleList = ExtractTriangles(mf);
            octree = new Octree(triangleList);
            octreeDepth = ComputeDepth(octree.root);
            clampedMaxDepthToDraw = Mathf.Clamp(userMaxDepthToDraw, 0, octreeDepth);

            CacheTransformState();
        }

        clampedMaxDepthToDraw = Mathf.Clamp(userMaxDepthToDraw, 0, octreeDepth);
    }

    void OnDrawGizmos()
    {
        if (showOctree && octree.root != null)
        {
            Gizmos.color = nodeColor;
            DrawNode(octree.root, 0);
        }
    }

    void DrawNode(OctreeNode node, int depth)
    {
        if (node == null || depth > clampedMaxDepthToDraw) return;

        Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);

        if (!node.IsLeaf && node.children != null)
        {
            foreach (var child in node.children)
                DrawNode(child, depth + 1);
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

    int ComputeDepth(OctreeNode node)
    {
        if (node == null || node.IsLeaf) return 0;

        int depth = 0;
        foreach (var child in node.children)
            depth = Mathf.Max(depth, ComputeDepth(child));

        return 1 + depth;
    }

    void CacheTransformState()
    {
        lastPosition = targetObject.transform.position;
        lastRotation = targetObject.transform.rotation;
        lastScale = targetObject.transform.localScale;
    }
}
