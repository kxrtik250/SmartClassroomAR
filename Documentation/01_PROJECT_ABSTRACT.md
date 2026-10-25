# SMART CLASSROOM NAVIGATION SYSTEM USING AUGMENTED REALITY
## Academic Capstone Project — Final Technical Report
**Department of Computer Science and Engineering**  
**Project Repository:** `SmartClassroomAR`  
**Target Environments:** Unity WebGL 2022.3.51f1 LTS (Desktop/Web) & Android ARCore Mobile App  

---

### Abstract

Indoor navigation within large-scale multi-story academic buildings remains a pervasive challenge due to the physical attenuation and multipath degradation of Global Positioning System (GPS) satellite signals when penetrating reinforced concrete and metal structures. Consequently, traditional outdoor mapping systems fail entirely within institutional indoor complexes, resulting in student and visitor disorientation, scheduled class delays, and logistical inefficiencies.

This project, **“SMART CLASSROOM NAVIGATION SYSTEM USING AUGMENTED REALITY,”** presents an end-to-end, cross-platform indoor navigation solution developed in Unity 2022.3.51f1 LTS. The system bridges mathematical graph algorithms, 3D architectural digital-twin visualization, and augmented reality tracking:

1. **Topological Navigation Graph & Shortest-Path Engine:** The building layout is modeled as a weighted directed/undirected graph $G = (V, E)$ comprising distinct semantic node types (entrances, hallway waypoints, staircases, classrooms, laboratories, and departmental offices) and weighted edges reflecting physical Euclidean distances. An optimized implementation of Dijkstra's algorithm executes deterministic pathfinding in sub-millisecond time ($< 0.5\text{ ms}$ for 30+ nodes), seamlessly computing optimal multi-floor routes traversing vertical stairwells and horizontal corridors.
2. **Interactive 3D Digital-Twin Visualization (WebGL & Unity Editor):** A complete three-story digital twin of the Computer Science and Engineering (CSE) Block (Ground Floor $Y=0\text{m}$, First Floor $Y=4\text{m}$, Second Floor $Y=8\text{m}$) is procedurally constructed and rendered in real-time at 60 FPS in desktop web browsers through WebGL and WebAssembly. A split-pane layout combines a responsive left-hand navigation sidebar (with start-point selection, real-time destination search filtering, and categorized directory) with an interactive 3D architectural viewport featuring orbital rotation, panning, smooth zooming, and single-key floor isolation (Keys 1, 2, 3).
3. **Step-by-Step Route Simulation & Arrival Detection:** Users can preview routes through dynamic 3D visualizers, featuring glowing animated waypoints, floor-adaptive trajectory polylines, an animated user avatar beacon, incremental "Step Forward" manual advance, automated route simulation, and arrival cards upon reaching the target destination.
4. **Physical Mobile AR Architecture (Android ARCore):** For physical on-site deployment, the system integrates AR Foundation and Google ARCore. A coordinate calibrator maps physical landmark anchors to digital navigation nodes, enabling camera-passthrough directional arrow overlays and spatial destination markers.
5. **Quality Assurance & Verification:** The codebase is validated by 362 automated unit and integration tests (100% pass rate) running in headless Unity batchmode, alongside headless Chrome DevTools Protocol (CDP) automated browser test suites verifying same-start guards, single-floor paths, multi-floor vertical stair navigation, and route cancellation.

---

### Keywords
`Augmented Reality (AR)`, `Indoor Navigation`, `Graph Theory`, `Dijkstra's Algorithm`, `Unity WebGL`, `ARCore`, `Digital Twin`, `Spatial Computing`, `Human-Computer Interaction (HCI)`.

---

### Project Metadata
| Attribute | Specification |
| :--- | :--- |
| **Project Title** | Smart Classroom Navigation System Using Augmented Reality |
| **Unity Version** | Unity 2022.3.51f1 LTS |
| **Render Pipeline** | Built-in Render Pipeline (optimized for WebGL 2.0 & OpenGLES3) |
| **Input System** | Unity Input System package (`com.unity.inputsystem`) & uGUI |
| **AR Framework** | Unity AR Foundation 5.1.5 & ARCore XR Plugin 5.1.5 |
| **Web Build Size** | ~33.3 MB (Gzip/Brotli compressed, zero external assets) |
| **Test Suite** | 362 Automated Unit & Integration Tests (100% Pass) |
| **Target Browsers** | Google Chrome, Mozilla Firefox, Microsoft Edge, Safari 15+ |
| **Mobile Platform** | Android 9.0+ (API Level 28 to 33, ARCore compatible) |
