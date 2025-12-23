# 🧩 Spatial Acceleration Structures in Computer Graphics

This project implements core spatial acceleration structures commonly used in computer graphics, built entirely from scratch in Unity for learning and experimentation.
Each structure is constructed on triangle meshes, visualized in real time using Unity Gizmos.

The repository includes:
- **Octree** — uniform spatial subdivision of 3D space
- **KD-Tree**
  - Object-based (centroid split)
  - Spatial (space-partitioning with triangle duplication)
- **BVH (Bounding Volume Hierarchy)**
  - Median split BVH
  - Hybrid BVH using Surface Area Heuristic (SAH)
