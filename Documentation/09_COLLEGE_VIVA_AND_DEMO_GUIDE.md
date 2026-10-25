# 9. College Demonstration Script & Technical Viva Guide

## Part 1: Live Demonstration Presentation Script (5-Minute Walkthrough)

When presenting this project to external examiners and evaluation committee members, follow this structured, confident walkthrough.

### Phase 1: Problem Context & Architectural Overview (Minute 0:00 - 1:00)
> *"Respected examiners, our capstone project is titled **Smart Classroom Navigation System Using Augmented Reality**. As campuses expand into multi-story complexes, outdoor GPS fails completely due to concrete signal attenuation and zero vertical resolution.*
> *To solve this without installing costly BLE beacons or radio sensors, we engineered a lightweight, cross-platform indoor navigation solution. It models the facility as a mathematical 3D graph, calculates deterministic shortest paths using Dijkstra's algorithm in sub-millisecond time, and presents both an interactive 3D digital-twin WebGL client for web exploration and an ARCore-enabled mobile app for camera-based AR tracking."*

### Phase 2: WebGL Interface & 3D Building Overview (Minute 1:00 - 2:00)
1. **Show the Web Browser:**
   - Display `http://localhost:8080/index.html`.
   - Point out the clean split-screen layout: the left 28% sidebar provides directory navigation, while the right 72% viewport displays the procedural 3D CSE Building.
2. **Demonstrate 3D Camera Controls:**
   - Drag with Left Mouse Button to orbit: *"The examiner can view all three floors from any angle."*
   - Press **Key 1**, **Key 2**, **Key 3**: *"With single keystrokes, the camera smoothly isolates the Ground Floor, First Floor, or Second Floor."*
   - Press **Key R**: *"Key R immediately restores the isometric overview."*

### Phase 3: Defensive Validation & Single-Floor Routing (Minute 2:00 - 3:00)
1. **Demonstrate Same-Start Guard:**
   - With `Main Entrance` as starting point, click `Main Entrance` in the directory.
   - Point out the yellow validation alert: *"Notice our defensive programming: selecting one's current location blocks path computation and displays: 'Selected destination is your current location.' The system remains stable."*
2. **Demonstrate Single-Floor Routing (CSE Department Office):**
   - Click `CSE Department Office`.
   - Point out the instant route generation: *"In under 0.5 milliseconds, the Dijkstra engine computes the 4-node, 21.5-meter path. The 3D viewport renders the glowing path polyline with an animated user avatar at the origin."*
   - Click **Step Forward** three times: *"With each step, the user marker advances along corridor waypoints, the completed segment turns green, and the progress bar tracks percentage completion."*
   - Show the Arrival Card: *"Upon reaching the destination, an arrival card confirms the destination name, department, and floor."*
   - Click **"Navigate to Another"**.

### Phase 4: Multi-Floor Vertical Stair Traversal (Minute 3:00 - 4:00)
1. **Demonstrate First Floor Transition (Room 101):**
   - Click `Room 101`.
   - Highlight the stairwell navigation: *"Here the destination is on the First Floor. Observe how the polyline guides the user horizontally along Ground Floor waypoints, ascends the physical staircase at coordinate (25, 0, 5), traverses the landing, and enters Room 101."*
   - Click **Auto Simulate**: *"The auto-simulation automatically steps the user through the entire multi-floor journey."*
2. **Demonstrate Route Reset & Cancellation:**
   - Click **Reset Route**: *"The avatar instantly snaps back to 0% without recalculation overhead."*
   - Click **Back / Change**: *"Navigation cancels cleanly and restores the full directory."*

### Phase 5: Technical Defense & Verification Summary (Minute 4:00 - 5:00)
> *"Our system is verified by 362 automated unit and integration tests passing with a 100% success rate in Unity batchmode, along with headless Chrome DevTools Protocol automation testing every browser interaction. We are ready for technical viva questions."*

---

## Part 2: 30 Technical Viva Voce Questions & Model Answers

### Category A: Graph Theory & Algorithms

#### Q1: Why did you choose Dijkstra's algorithm over A* (A-Star)?
**Model Answer:**  
*"Dijkstra's algorithm guarantees the mathematically optimal shortest path on graphs with non-negative edge weights without requiring an admissible heuristic. For our 30-node campus building graph, Dijkstra executes in under 0.2 milliseconds—essentially instantaneous. While A* uses a Euclidean distance heuristic $h(n)$ to bias exploration toward the target, in a multi-story building with vertical staircases, an unweighted straight-line 3D Euclidean distance heuristic can underestimate horizontal hallway detours and produce suboptimal exploration across floor boundaries. Given our graph size and performance profile, Dijkstra provides guaranteed optimality with zero heuristic tuning overhead."*

#### Q2: What is the computational complexity of your pathfinding implementation?
**Model Answer:**  
*"Our Dijkstra implementation uses a priority queue. Its time complexity is $O((|V| + |E|) \log |V|)$, where $|V|$ is the number of vertices (30) and $|E|$ is the number of edges (35). The space complexity is $O(|V| + |E|)$ to store adjacency lists, distance tables, and predecessor pointers. Memory allocation during pathfinding is minimized to eliminate garbage collection pauses."*

#### Q3: How do you handle multi-floor transitions in the graph?
**Model Answer:**  
*"Floors are not treated as isolated 2D planes. Instead, our navigation graph is a unified 3D coordinate graph. Stairwell landings on adjacent floors are connected by explicit vertical edges (e.g., `N_F0_STAIR` to `N_F1_STAIR_DN`). The edge cost represents the actual traversal effort (7.0 meters, combining vertical elevation and stair tread run). When a user selects a destination on another floor, Dijkstra naturally discovers the staircase edge as the lowest-cost path."*

#### Q4: How does the system prevent infinite loops or graph cycles?
**Model Answer:**  
*"Dijkstra's algorithm inherently prevents cycles because all edge weights are strictly positive ($w > 0$) and every visited node is closed in a `visited` set. Furthermore, our `NavigationGraph.ValidateGraph()` method runs during initialization to assert that no zero or negative edge weights exist and that all edges reference valid nodes."*

#### Q5: What happens if a destination node is physically disconnected from the start node?
**Model Answer:**  
*"If an isolated node exists or an edge is broken, Dijkstra terminates with `dist[destination] == infinity`. The `Pathfinder.FindPath()` method intercepts this condition, sets `PathResult.success = false`, and returns an informative error message: 'Destination unreachable from selected start point.' The UI safely catches this and maintains the directory state."*

#### Q6: Can the graph be updated dynamically if a hallway is blocked for maintenance?
**Model Answer:**  
*"Yes. Because the `NavigationGraph` stores edges in a dynamic list, we can temporarily set an edge's weight to infinity ($\infty$) or remove it via `RemoveEdge(edgeId)`. Re-running `Pathfinder.FindPath()` will immediately route users through alternate corridors or secondary staircases without restarting the application."*

---

### Category B: Augmented Reality, Computer Vision & Tracking

#### Q7: Why does the WebGL build run in 3D simulation mode rather than using the webcam for AR?
**Model Answer:**  
*"Standard desktop web browsers lack mobile ARCore/ARKit capabilities: specifically, hardware-synchronized IMU sensors, time-of-flight depth cameras, and hardware-accelerated Visual Inertial Odometry (VIO) SLAM pipelines. A stationary laptop webcam cannot track 6-DoF spatial movement through building corridors. Therefore, the WebGL build functions as an interactive 3D digital-twin simulation, while camera-passthrough AR is deployed to mobile devices via Android ARCore."*

#### Q8: How does ARCore achieve camera tracking without GPS?
**Model Answer:**  
*"ARCore uses Visual Inertial Odometry (VIO). It detects distinct optical feature points (corners, texture gradients) in consecutive camera frames and tracks their 2D displacement. Concurrently, it samples the device's internal inertial measurement unit (accelerometer and gyroscope) at high frequency. By fusing optical flow with inertial readings through an Extended Kalman Filter, ARCore estimates the camera's 6-Degrees-of-Freedom pose (position and orientation) in real time."*

#### Q9: How do you align digital building coordinates with the physical world?
**Model Answer:**  
*"Through our `CoordinateCalibrator` system. Digital coordinates $(X_b, Y_b, Z_b)$ and physical AR tracking space $(X_{ar}, Y_{ar}, Z_{ar})$ have different origins and orientations. We perform 2-point landmark calibration at the building entrance: surveying two physical reference markers (e.g., the entrance doorframe and corridor threshold). This computes an affine transformation matrix $[R | T]$ such that $\mathbf{P}_{\text{AR}} = \mathbf{R} \cdot \mathbf{P}_{\text{Building}} + \mathbf{T}$."*

#### Q10: What is 'scale drift' in AR, and how does your system handle it?
**Model Answer:**  
*"Scale drift occurs in monocular SLAM when the visual tracking system gradually accumulates error in its metric distance estimate over long trajectories. In our mobile system, ARCore minimizes drift by integrating accelerometer data and detecting ground planes. Additionally, traversing known staircase waypoints can trigger recalibration against localized QR code markers or spatial anchors."*

#### Q11: How do you determine that a user has arrived at their destination in AR?
**Model Answer:**  
*"In `ARNavigationRenderer`, we implement continuous Euclidean distance checks between the camera's current AR position $\mathbf{P}_{\text{user}}$ and the destination node $\mathbf{P}_{\text{dest}}$. When $\|\mathbf{P}_{\text{user}} - \mathbf{P}_{\text{dest}}\| \le 2.0\text{ meters}$, the system triggers arrival, plays a confirmation cue, and presents the destination info card."*

---

### Category C: Unity Engine Architecture & WebGL

#### Q12: How does Unity compile C# code to run in a web browser?
**Model Answer:**  
*"Unity uses IL2CPP (Intermediate Language to C++). During WebGL compilation, Unity converts compiled C# .NET assemblies into optimized C++ source code. Then, using the open-source Emscripten compiler toolchain, that C++ is compiled into WebAssembly (WASM)—a compact binary instruction format executed by the browser's JavaScript virtual machine at near-native speed."*

#### Q13: Why did you use Unity's Built-in Render Pipeline instead of URP or HDRP?
**Model Answer:**  
*"The Built-in Render Pipeline offers maximum compatibility and the smallest binary footprint for WebGL 2.0 and mobile OpenGLES3. URP and HDRP introduce significant package overhead, increasing WebGL download sizes by tens of megabytes and requiring compute shaders that are not universally supported across all mobile and embedded browsers. The Built-in Pipeline ensures 60 FPS performance even on low-end hardware."*

#### Q14: What is shader stripping, and how did you prevent missing materials in WebGL?
**Model Answer:**  
*"During WebGL build compilation, Unity's build pipeline inspects scenes and aggressively strips out shader variants deemed 'unused' to minimize binary size. However, because our building slabs, room boxes, and route polylines are generated procedurally at runtime via `Shader.Find()`, Unity's build stripper might remove them. We solved this by creating a `_ShaderKeeper` GameObject in the scene that holds persistent serialized material references to `Standard`, `Sprites/Default`, and `TextMeshPro` shaders, preventing them from being stripped."*

#### Q15: Why did you replace Cylinder and Capsule primitives with Cubes and Spheres in your procedural visualizers?
**Model Answer:**  
*"When Unity's `GameObject.CreatePrimitive(PrimitiveType.Cylinder)` or `PrimitiveType.Capsule` is called in WebGL where 3D physics components are stripped, Unity logs a runtime warning: `Can't add component because class 'CapsuleCollider' doesn't exist!` By replacing signposts and pillars with sleek rectangular architectural `Cube` columns and the user avatar with a composite `Cube` pedestal and `Sphere` beacon orb, we eliminated collider warnings entirely and improved modern architectural aesthetics."*

#### Q16: How did you bridge the new Unity Input System with the WebGL Canvas?
**Model Answer:**  
*"Unity's new Input System package requires an `InputSystemUIInputModule` on the scene `EventSystem`. In our automated `SceneSetup.cs` script, we programmatically call `InputSystemUIInputModule.AssignDefaultActions()`. This wires standard mouse clicks, touches, and keyboard events to the uGUI Canvas, ensuring all buttons and scrollbars respond immediately in desktop browsers."*

---

### Category D: Indoor Positioning Systems (IPS) vs Outdoor GPS

#### Q17: Why can't indoor navigation simply use Google Maps?
**Model Answer:**  
*"Google Maps depends on satellite GPS trilateration, which requires unobstructed line-of-sight to at least 4 satellites. Concrete ceilings and steel rebar attenuate satellite signals by 20 to 30 dB. Furthermore, Google Maps lacks interior floorplan CAD data, multi-floor vertical room connectivity, and room-level accuracy for individual campus buildings."*

#### Q18: What are the trade-offs between your zero-infrastructure approach and BLE (Bluetooth Low Energy) beacon triangulation?
**Model Answer:**  
*"BLE beacon systems require installing dozens of battery-powered transmitters across hallways, requiring ongoing hardware maintenance, battery replacements, and labor-intensive RSSI radio fingerprinting. Our graph-based digital-twin and visual AR approach requires zero hardware infrastructure—the building geometry and navigation logic reside entirely within software, drastically reducing deployment and operational costs."*

#### Q19: How accurate is your system compared to Wi-Fi RSSI fingerprinting?
**Model Answer:**  
*"Wi-Fi fingerprinting suffers from environmental noise: fluctuating human foot traffic, open doors, and electrical interference cause signal fluctuations that degrade accuracy to 3–5 meters, often misidentifying adjacent rooms. Our topological graph routes along exact architectural corridors, providing millimeter-level graphical precision in the digital twin and decimeter-level visual placement in AR."*

---

### Category E: Software Engineering, Testing & Edge Cases

#### Q20: What defensive programming guards did you implement for user inputs?
**Model Answer:**  
*"1. **Same-Start Guard:** If a user selects their current location as destination, pathfinding is blocked, and an alert explains: 'Selected destination is your current location.'  
2. **Missing Node Guard:** If an invalid or unmapped destination is queried, the system displays an error and recovers to directory browsing.  
3. **UI Raycast Suppression:** In `OrbitCamera`, we check `EventSystem.current.IsPointerOverGameObject()`. When users click or scroll over the left directory panel, 3D camera orbit and zooming are suspended to prevent accidental camera motion."*

#### Q21: How did you test the system thoroughly without manual clicking every time?
**Model Answer:**  
*"We implemented a two-tier test strategy:  
1. **Headless Unity NUnit Suite:** 362 automated tests executing via `run_tests.ps1` in batchmode covering graph validation, Dijkstra correctness, and state machine transitions.  
2. **Chrome DevTools Protocol (CDP) Suite:** An automated Node.js test script (`test_all_destinations.mjs`) that connects to the live browser via CDP, executes mouse clicks on buttons, steps through routes, triggers auto-simulation, and captures timestamped verification screenshots."*

#### Q22: How is building data stored and maintained?
**Model Answer:**  
*"Building data is structured as standard JSON adhering to our `BuildingData` schema. It cleanly separates architectural configuration from engine code. If new classrooms or wings are added, an administrator simply updates the JSON file or connects a REST API endpoint; the `BuildingDataLoader` parses it at runtime and regenerates the 3D twin and navigation graph automatically without recompiling the codebase."*

#### Q23: What are the main limitations of the current implementation?
**Model Answer:**  
*"1. In the WebGL browser, navigation is simulated in the 3D digital twin rather than physical camera AR due to desktop hardware limitations.  
2. The current navigation graph assumes fixed, pre-surveyed paths and does not model real-time temporary physical obstructions (such as renovation barricades).  
3. Initial AR coordinate calibration requires physical alignment at known entrance landmark anchors."*

#### Q24: What future enhancements would you recommend?
**Model Answer:**  
*"1. Integration of WebXR Device API on AR-capable mobile web browsers to enable browser-based AR without installing native APKs.  
2. Visual QR code scanning at entrance thresholds for instantaneous, zero-calibration spatial anchoring.  
3. Multi-user real-time synchronization via WebSockets to enable indoor location sharing among students."*

---

### Summary Checklist for Viva Voce

| Topic | Key Terminology to Mention |
| :--- | :--- |
| **Pathfinding** | Dijkstra's algorithm, priority queue, non-negative weights, time complexity $O((V+E)\log V)$, multi-floor staircase edges. |
| **Indoor Positioning** | GPS microwave attenuation, multipath reflection, zero-infrastructure approach, building digital twin. |
| **Augmented Reality** | Google ARCore, Visual Inertial Odometry (VIO), 6-DoF tracking, affine coordinate transformation, landmark anchors. |
| **Unity Architecture** | IL2CPP, WebAssembly (WASM), WebGL 2.0, Built-in Render Pipeline, uGUI EventSystem, `_ShaderKeeper`. |
| **Quality & Reliability** | 362 automated unit tests (100% pass rate), CDP browser test automation, defensive same-start validation. |
