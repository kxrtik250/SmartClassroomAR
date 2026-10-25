# 2. Problem Statement & Objectives

## 1. Problem Statement

Modern university campuses, corporate headquarters, and medical facilities encompass complex, multi-story architectural infrastructures with intricate corridor layouts, multiple stairwells, and dozens of specialized laboratories, classrooms, and faculty offices. Navigating these environments poses significant difficulties for incoming students, faculty, conference attendees, and emergency responders.

### The Inadequacy of Satellite-Based Positioning (GPS) Indoors
Outdoor navigation relies universally on Global Positioning System (GPS) signals transmitted from Earth-orbiting satellites. However, GPS is fundamentally unviable for indoor environments due to:
1. **Signal Attenuation & Line-of-Sight Loss:** Satellite signals operate in the microwave L-band (1.1 to 1.6 GHz), which is severely attenuated or completely blocked by reinforced concrete roofs, steel rebar, and thick masonry walls.
2. **Multipath Reflection:** Signals bounce off walls, floors, and metal furniture, generating false phase delays that reduce positioning accuracy from several meters to tens of meters, rendering it useless for room-level distinction.
3. **Absence of Altitude / Floor Resolution:** GPS $Z$-axis (vertical elevation) error is typically 2 to 3 times greater than horizontal error, making it impossible to distinguish between the Ground Floor ($Y=0\text{m}$), First Floor ($Y=4\text{m}$), or Second Floor ($Y=8\text{m}$).

### Existing Indoor Signage Limitations
- **Static Wall Maps:** Often oriented inconsistently relative to the user's facing direction, requiring mental coordinate rotation.
- **Physical Room Placards:** Difficult to locate from a distance and provide no routing guidance along convoluted corridor junctions.
- **High Deployment Cost of Beacons:** Traditional indoor RF solutions (BLE beacons, Ultra-Wideband anchors, Wi-Fi fingerprinting) require expensive hardware installation, frequent battery replacements, and periodic radio mapping calibration.

---

## 2. Project Objectives

The primary aim of this project is to develop and validate a lightweight, zero-hardware-infrastructure, cross-platform indoor navigation application that delivers precise shortest-path guidance through a multi-floor digital twin and augmented reality overlays.

### Specific Technical Objectives:
1. **Mathematical Graph Modeling of Campus Facilities:**
   - Formalize the 3D architectural floorplan of the Computer Science and Engineering (CSE) building as a topological graph $G = (V, E)$.
   - Define exact Cartesian coordinates $(X, Y, Z)$ for all rooms, corridor intersections, entrance portals, and vertical staircase nodes.
2. **Deterministic Shortest-Path Engine:**
   - Implement an efficient, garbage-collection-free Dijkstra algorithm.
   - Support seamless single-floor and multi-floor path computation, validating all traversals across stairwells with non-negative physical metric costs.
   - Implement defensive validation guards against identical start-and-destination selections, isolated nodes, and malformed queries.
3. **High-Performance 3D Digital-Twin Visualization (WebGL & Unity Editor):**
   - Procedurally render the CSE building geometry (floor plates, corridors, room volumes, stairwells, and billboarded 3D typographic labels).
   - Implement an orbital camera system supporting mouse orbit, Alt-pan, smooth zoom, and keyboard hotkeys (Keys 1, 2, 3 for floor focusing, 'R' for reset).
   - Ensure constant 60 FPS performance in standard web browsers without requiring specialized plugins or GPU hardware.
4. **Interactive Simulation & Waypoint Tracking:**
   - Provide an intuitive split-view UI (left 28% sidebar for destination search/directory; right 72% for 3D spatial viewport).
   - Render continuous 3D path polylines with distinguished completed (green) and remaining (cyan) segments.
   - Provide step-by-step waypoint manual advancement ("Step Forward"), continuous auto-simulation, route reset, and arrival detection.
5. **Physical Mobile AR Architecture (Android ARCore):**
   - Provide camera-based directional navigation on ARCore-supported Android devices.
   - Establish a 2-point coordinate calibration system linking physical real-world landmarks to digital building coordinates.
6. **Automated Verification & Quality Assurance:**
   - Establish 100% test pass rate across 360+ automated unit tests and integration tests in Unity batchmode.
   - Execute end-to-end browser automation suites via Chrome DevTools Protocol (CDP) to guarantee bulletproof reliability for college viva demonstrations.

---

## 3. Scope and Delimitations

### In-Scope:
- Procedural 3D modeling and rendering of a 3-floor CSE Block (Ground Floor, First Floor, Second Floor).
- Real-time Dijkstra graph pathfinding across 30+ nodes and 35+ bidirectional edges.
- Comprehensive WebGL desktop browser execution via local or remote HTTP hosting.
- Identical simulation logic in Unity Editor Play Mode for development and inspection.
- Android APK deployment with ARCore camera tracking and fallback simulation mode.
- Real-time search filtering, floor grouping, and error handling.

### Out-of-Scope (Delimitations):
- **Desktop WebGL Camera AR:** Standard desktop browsers lack physical ARCore depth sensors and camera tracking. As documented in Section 6, the WebGL build operates in simulated 3D digital-twin mode. Physical camera AR is exclusive to mobile Android builds equipped with ARCore.
- **Dynamic Obstacle Avoidance:** Navigation paths utilize pre-mapped architectural corridors and stairwells; real-time dynamic obstacle avoidance (e.g., temporary furniture obstructions) is not modeled.
