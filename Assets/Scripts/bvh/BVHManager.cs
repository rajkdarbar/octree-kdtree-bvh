using System.Collections.Generic;
using UnityEngine;

public class BVHManager : MonoBehaviour
{
    public GameObject _3DModel;
    private Color nodeColor;

    private MeshFilter mf;
    private List<TriangleInfo> triangleList = new();
    private List<TriangleInfo> tris;

    private MedianSplitBVH medianBVH;
    private HybridSplitBVH hybridBVH;

    [Header("BVH Tree Depth Info")]
    public int medianBVHDepth;
    public int hybridBVHDepth;

    [Header("Visualization Settings")]
    public bool showMedianBVH = false;
    public bool showHybridBVH = false;

    [Range(0, 32)]
    public int userMaxDepthToDraw = 10;
    private int clampedMaxDepthToDraw;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;


    void Start()
    {
        // Cache initial transform state
        lastPosition = _3DModel.transform.position;
        lastRotation = _3DModel.transform.rotation;
        lastScale = _3DModel.transform.localScale;

        mf = _3DModel.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("No MeshFilter or mesh found on this GameObject.");
            return;
        }

        tris = ExtractTriangles(mf);

        medianBVH = new MedianSplitBVH(tris); // build BVH tree using median split
        hybridBVH = new HybridSplitBVH(tris); // build BVH tree combining median split and Surface Area Heuristic (SAH)

        medianBVHDepth = ComputeDepth(medianBVH.root);
        hybridBVHDepth = ComputeDepth(hybridBVH.root);

        UpdateMaxTreeDepth();
    }

    void Update()
    {
        Transform t = _3DModel.transform;

        Vector3 currentPos = t.position;
        Quaternion currentRot = t.rotation;
        Vector3 currentScale = t.localScale;

        bool hasTransformChanged = currentPos != lastPosition || currentRot != lastRotation || currentScale != lastScale;

        if (hasTransformChanged)
        {
            if (!IsUniformScale(currentScale))
            {
                tris = ExtractTriangles(mf);
                medianBVH = new MedianSplitBVH(tris);
                hybridBVH = new HybridSplitBVH(tris);
            }
            else
            {
                UpdateTrianglePositions(tris, mf);
                medianBVH.Refit(medianBVH.root);
                hybridBVH.Refit(hybridBVH.root);
            }

            medianBVHDepth = ComputeDepth(medianBVH.root);
            hybridBVHDepth = ComputeDepth(hybridBVH.root);

            UpdateMaxTreeDepth();

            // Cache current transform
            lastPosition = currentPos;
            lastRotation = currentRot;
            lastScale = currentScale;
        }
    }

    bool IsUniformScale(Vector3 scale)
    {
        bool xyEqual = Mathf.Approximately(scale.x, scale.y);
        bool yzEqual = Mathf.Approximately(scale.y, scale.z);

        return xyEqual && yzEqual;
    }

    List<TriangleInfo> ExtractTriangles(MeshFilter mf)
    {
        Mesh mesh = mf.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.triangles;

        triangleList.Clear();
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 v0 = mf.transform.TransformPoint(vertices[indices[i]]);
            Vector3 v1 = mf.transform.TransformPoint(vertices[indices[i + 1]]);
            Vector3 v2 = mf.transform.TransformPoint(vertices[indices[i + 2]]);

            TriangleInfo tri = new TriangleInfo(v0, v1, v2);
            triangleList.Add(tri);
        }

        return triangleList;
    }

    void UpdateTrianglePositions(List<TriangleInfo> tris, MeshFilter mf)
    {
        Vector3[] vertices = mf.sharedMesh.vertices;
        int[] indices = mf.sharedMesh.triangles;

        for (int i = 0; i < tris.Count; i++)
        {
            Vector3 v0 = mf.transform.TransformPoint(vertices[indices[i * 3]]);
            Vector3 v1 = mf.transform.TransformPoint(vertices[indices[i * 3 + 1]]);
            Vector3 v2 = mf.transform.TransformPoint(vertices[indices[i * 3 + 2]]);

            tris[i].v0 = v0;
            tris[i].v1 = v1;
            tris[i].v2 = v2;
        }
    }


    void OnValidate()
    {
        if (showHybridBVH && showMedianBVH)
            showMedianBVH = false; // only one stays true      

        UpdateMaxTreeDepth();
    }

    void OnDrawGizmos()
    {
        if (showMedianBVH && medianBVH != null && medianBVH.root != null)
        {
            nodeColor = Color.cyan;
            DrawBVHNode(medianBVH.root, 0);
        }

        if (showHybridBVH && hybridBVH != null && hybridBVH.root != null)
        {
            nodeColor = Color.yellow;
            DrawBVHNode(hybridBVH.root, 0);
        }
    }

    void DrawBVHNode(BVHNode node, int depth)
    {
        if (node == null || depth > clampedMaxDepthToDraw)
            return;

        Gizmos.color = nodeColor;
        Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);

        if (!node.IsLeaf)
        {
            if (node.left != null)
                DrawBVHNode(node.left, depth + 1);

            if (node.right != null)
                DrawBVHNode(node.right, depth + 1);
        }
    }


    int ComputeDepth(BVHNode node)
    {
        if (node == null || node.IsLeaf)
            return 0;

        return 1 + Mathf.Max(ComputeDepth(node.left), ComputeDepth(node.right));
    }


    void UpdateMaxTreeDepth()
    {
        int visibleDepth = showHybridBVH ? hybridBVHDepth : medianBVHDepth;
        clampedMaxDepthToDraw = Mathf.Clamp(userMaxDepthToDraw, 0, visibleDepth);
    }
}
