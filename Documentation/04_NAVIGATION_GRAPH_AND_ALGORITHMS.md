# 4. Navigation Graph & Algorithms

## 1. Graph Formulation

The campus indoor environment is modeled mathematically as a weighted directed/undirected graph:

$$G = (V, E)$$

Where:
- $V = \{v_1, v_2, \dots, v_n\}$ represents the set of navigation vertices (nodes), where each vertex $v_i = (\text{id}, \text{floorId}, x, y, z, \text{label}, \text{type})$.
- $E = \{e_1, e_2, \dots, e_m\}$ represents the set of edges connecting accessible physical corridors, stairwells, and room portals, where each edge $e = (u, v, w)$ connects nodes $u, v \in V$ with non-negative weight $w \ge 0$.

### Coordinate Reference System
All spatial coordinates are expressed in meters using a right-handed Cartesian coordinate system:
- **$X$-Axis (Corridor Length):** Runs horizontally along the main building corridor from West ($X = 0\text{m}$) to East ($X = 50\text{m}$).
- **$Y$-Axis (Floor Elevation):** Represents vertical elevation:
  - Ground Floor (F0): $Y = 0.0\text{m}$
  - First Floor (F1): $Y = 4.0\text{m}$
  - Second Floor (F2): $Y = 8.0\text{m}$
- **$Z$-Axis (Room Offset):** Perpendicular depth across the building footprint:
  - North Rooms: $Z = +6.0\text{m}$
  - Central Corridor: $Z = 0.0\text{m}$
  - South Rooms: $Z = -6.0\text{m}$

---

## 2. Vertex & Edge Taxonomy

### Vertex Types (`NodeType`):
| NodeType | Visual Representation | Architectural Purpose | Example Nodes |
| :--- | :--- | :--- | :--- |
| **Entrance** | Emerald Cube & Pillar | Primary building egress / access portals | `N_F0_ENTRANCE` |
| **Destination** | Cyan / Blue Room Volumes | Classrooms, laboratories, faculty offices | `N_F0_OFFICE`, `N_F1_R101`, `N_F2_R202` |
| **Staircase** | Amber Diamond / Ramp Glyphs | Vertical transit hubs linking floors | `N_F0_STAIR`, `N_F1_STAIR_UP`, `N_F2_STAIR` |
| **Waypoint** | Light Gray Nodes | Corridor centerlines & pathway intersections | `N_F0_CORR_0`, `N_F0_CORR_12`, `N_F0_CORR_25` |

### Edge Types (`EdgeType`):
1. **Corridor Edges:** Longitudinal connections along hallways (typical weight = $12.5\text{m}$).
2. **Doorway Edges:** Perpendicular hallway spurs connecting corridor waypoints to room interiors (weight = $6.0\text{m}$).
3. **Staircase Edges:** Vertical interconnects between floors (weight = $7.0\text{m}$, representing vertical elevation plus horizontal stair run).

---

## 3. Shortest-Path Algorithm (Dijkstra)

Pathfinding between any start node $s \in V$ and destination $d \in V$ is calculated via Dijkstra's algorithm.

### Algorithmic Formulation

```
Algorithm: DijkstraShortestPath(G, startNodeId, endNodeId)
Input: Graph G = (V, E), startNodeId s, destinationNodeId d
Output: PathResult (success, nodeIds, totalCost, crossesFloors, floorsTraversed)

1.  If s == d:
2.      Return Error("Start and destination are identical.")
3.  If s ∉ V or d ∉ V:
4.      Return Error("Invalid start or destination node ID.")

5.  Initialize dist[v] = ∞ for all v ∈ V
6.  Initialize prev[v] = null for all v ∈ V
7.  Initialize visited = ∅
8.  dist[s] = 0

9.  Initialize PriorityQueue Q ordered by dist
10. Insert (0, s) into Q

11. While Q is not empty:
12.     Extract node u with minimum dist[u] from Q
13.     If u == d: Break (Destination found)
14.     If u ∈ visited: Continue
15.     Add u to visited

16.     For each neighbor (v, cost, edge) of u in G:
17.         If v ∉ visited:
18.             alt = dist[u] + cost
19.             If alt < dist[v]:
20.                 dist[v] = alt
21.                 prev[v] = u
22.                 Insert or Update (alt, v) in Q

23. If dist[d] == ∞:
24.     Return Error("Destination unreachable from selected start point.")

25. Reconstruct path by tracing prev pointers from d backwards to s
26. Reverse reconstructed path: path = [s, ..., d]
27. Compute floorsTraversed = Distinct floor IDs along path
28. crossesFloors = (count of distinct floors > 1)
29. Return PathResult(success=True, nodes=path, cost=dist[d], crossesFloors, floorsTraversed)
```

### Time and Space Complexity
- **Time Complexity:** Utilizing a binary heap / balanced search tree priority queue, the algorithm executes in $O((|V| + |E|) \log |V|)$. For the 30-node CSE graph, execution completes in **$< 0.2\text{ ms}$**, eliminating frame drops.
- **Space Complexity:** $O(|V| + |E|)$ to store the graph adjacency lists, distance dictionary, and predecessor mappings.

---

## 4. Multi-Floor Navigation Analysis

Multi-floor traversal is solved directly within the unified 3D graph. Staircases are modeled as physical vertical edges connecting floor landings:
- `N_F0_STAIR` $(25.0, 0.0, 5.0) \longleftrightarrow$ `N_F1_STAIR_DN` $(25.0, 4.0, 5.0)$ (Cost = $7.0\text{m}$)
- `N_F1_STAIR_UP` $(25.0, 4.0, 5.0) \longleftrightarrow$ `N_F2_STAIR` $(25.0, 8.0, 5.0)$ (Cost = $7.0\text{m}$)

When a user on the Ground Floor selects a Second Floor classroom (e.g., Room 202):
1. The pathfinder routes horizontally along Ground Floor corridor waypoints to the stairwell.
2. It ascends the staircase edge from F0 to F1, transitions across the First Floor landing, and ascends to F2.
3. It exits onto the Second Floor corridor and navigates into Room 202.
4. `PathResult.crossesFloors` is flagged `true`, and `floorsTraversed` registers `["F0", "F1", "F2"]`.

---

## 5. Complete Facility Directory & Node Mapping

| Facility Name | Destination ID | Node ID | Floor | Coordinates $(X, Y, Z)$ | Facility Type |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Main Entrance** | `DEST_F0_ENTRANCE` | `N_F0_ENTRANCE` | F0 | $(-3.0, 0.0, 0.0)$ | Entrance |
| **CSE Department Office** | `DEST_F0_OFFICE` | `N_F0_OFFICE` | F0 | $(12.5, 0.0, 6.0)$ | FacultyRoom |
| **Student Lounge** | `DEST_F0_LOUNGE` | `N_F0_LOUNGE` | F0 | $(12.5, 0.0, -6.0)$ | Other |
| **Ground Floor Staircase** | `DEST_F0_STAIR` | `N_F0_STAIR` | F0 | $(25.0, 0.0, 5.0)$ | Staircase |
| **CSE Lab 1** | `DEST_F0_LAB1` | `N_F0_LAB1` | F0 | $(37.5, 0.0, -6.0)$ | Laboratory |
| **Conference Room** | `DEST_F0_CONF` | `N_F0_CONF` | F0 | $(37.5, 0.0, 6.0)$ | Other |
| **Tutorial Room 103** | `DEST_F1_TUT103` | `N_F1_TUT103` | F1 | $(0.0, 4.0, 6.0)$ | Classroom |
| **Room 101** | `DEST_F1_R101` | `N_F1_R101` | F1 | $(12.5, 4.0, 6.0)$ | Classroom |
| **CSE Lab 2** | `DEST_F1_LAB2` | `N_F1_LAB2` | F1 | $(12.5, 4.0, -6.0)$ | Laboratory |
| **First Floor Staircase** | `DEST_F1_STAIR` | `N_F1_STAIR_UP` | F1 | $(25.0, 4.0, 5.0)$ | Staircase |
| **Room 102** | `DEST_F1_R102` | `N_F1_R102` | F1 | $(37.5, 4.0, 6.0)$ | Classroom |
| **Faculty Room 1** | `DEST_F1_FAC1` | `N_F1_FAC1` | F1 | $(37.5, 4.0, -6.0)$ | FacultyRoom |
| **Project Lab** | `DEST_F2_PROJ` | `N_F2_PROJ` | F2 | $(0.0, 8.0, 6.0)$ | Laboratory |
| **Room 201** | `DEST_F2_R201` | `N_F2_R201` | F2 | $(12.5, 8.0, 6.0)$ | Classroom |
| **CSE Lab 3** | `DEST_F2_LAB3` | `N_F2_LAB3` | F2 | $(12.5, 8.0, -6.0)$ | Laboratory |
| **Second Floor Staircase** | `DEST_F2_STAIR` | `N_F2_STAIR` | F2 | $(25.0, 8.0, 5.0)$ | Staircase |
| **Room 202** | `DEST_F2_R202` | `N_F2_R202` | F2 | $(37.5, 8.0, 6.0)$ | Classroom |
| **Seminar Hall** | `DEST_F2_SEMINAR` | `N_F2_SEMINAR` | F2 | $(37.5, 8.0, -6.0)$ | Classroom |
