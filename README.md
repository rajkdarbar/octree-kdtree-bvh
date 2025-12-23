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

---

## 🌳 Tree Visualizations

Each row shows how different spatial acceleration structures subdivide space and geometry at increasing tree depths (0, 1, 2, and 8).


#### Octree — Uniform Spatial Subdivision (Depths: 0, 1, 2, 8)

<div align="left">
  <img src="Assets/Resources/octree-depth-0.png" width="180">
  <img src="Assets/Resources/octree-depth-1.png" width="165">
  <img src="Assets/Resources/octree-depth-2.png" width="162">
  <img src="Assets/Resources/octree-depth-8.png" width="186.5">
</div>


#### KD-Tree — Axis-Aligned Space Partitioning (Depths: 0, 1, 2, 8)

<div align="left">
  <img src="Assets/Resources/kdtree-depth-0.png" width="240">
  <img src="Assets/Resources/kdtree-depth-1.png" width="240">
  <img src="Assets/Resources/kdtree-depth-2.png" width="240">
  <img src="Assets/Resources/kdtree-depth-8.png" width="240">
</div>


#### BVH — Bounding Volume Hierarchy (Depths: 0, 1, 2, 8)

<div align="left">
  <img src="Assets/Resources/bvh-depth-0.png" width="240">
  <img src="Assets/Resources/bvh-depth-1.png" width="240">
  <img src="Assets/Resources/bvh-depth-2.png" width="240">
  <img src="Assets/Resources/bvh-depth-8.png" width="240">
</div>

---






