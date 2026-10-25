# 5. Unity Components & Script Architecture

## 1. Component Responsibilities & Mapping

The project codebase is strictly organized across four domain namespaces:
- `SmartClassroomAR.Mapping`: Graph data models and JSON deserialization.
- `SmartClassroomAR.Navigation`: Dijkstra pathfinding engine and graph topology validation.
- `SmartClassroomAR.Visualization`: 3D procedural architecture, route line rendering, and camera controls.
- `SmartClassroomAR.UI`: uGUI responsive panels, dropdown selectors, and state coordination.
- `SmartClassroomAR.AR`: Hardware abstraction, tracking lifecycle, and coordinate calibration.

---

## 2. Comprehensive Script Reference

### 1. `NavigationUIController.cs` (`SmartClassroomAR.UI`)
- **Type:** `MonoBehaviour` (Attached to `UIRoot/Canvas`)
- **Primary Responsibility:** Master state machine controlling navigation lifecycle, user actions, path computation requests, and UI panel visibility.
- **Key Public API:**
  - `SelectDestination(DestinationData destination)`: Validates start vs destination; calls `Pathfinder.FindPath()`; renders 3D route; transitions to `Navigating` state.
  - `SelectDestinationById(string destinationId)`: Programmatic selection method for browser automation and scripting.
  - `AdvanceDemoStep()`: Increments the user position marker along the computed route.
  - `ToggleAutoSimulation()`: Starts/stops coroutine that steps forward every 1.2 seconds.
  - `ResetDemoSimulation()`: Resets user position marker to start waypoint ($0\%$).
  - `CancelNavigation()`: Clears active 3D route and returns to `SelectingDestination`.
  - `ReturnToDestinationSelection()`: Invoked from Arrival card to select another destination.
- **Inspector Wiring:**
  - `destinationSelectionPanel`: Left-hand scrollable directory panel.
  - `demoNavigationPanel`: Control panel containing **Step Forward**, **Auto Simulate**, **Reset Route**, and **Back / Change**.
  - `destinationReachedPanel`: Arrival confirmation card.
  - `statusText`, `navigationInfoText`, `distanceText`, `floorIndicatorText`: TextMeshPro labels.

### 2. `DestinationSelectionUI.cs` (`SmartClassroomAR.UI`)
- **Type:** `MonoBehaviour` (Attached to `DestinationSelectionPanel`)
- **Primary Responsibility:** Manages destination list generation, category grouping by floor, and real-time substring search filtering.
- **Key Public API:**
  - `PopulateDestinations(List<DestinationData> destinations, List<FloorData> floors)`: Groups destinations by floor and instantiates headers and interactive buttons.
  - `SetSearchFilter(string query)`: Filters buttons dynamically by matching query against `destinationName`, `department`, or `description`.
  - `SetStartNode(string startNodeId)`: Sets active origin node.
  - `SelectDestinationById(string id)`: Directly invokes click listener for a specific destination.
  - `event Action<DestinationData> OnDestinationSelected`: Event fired when user clicks a destination item.

### 3. `StartNodeSelector.cs` (`SmartClassroomAR.UI`)
- **Type:** `MonoBehaviour` (Attached to `StartNodeSelector`)
- **Primary Responsibility:** Populates and manages the origin dropdown.
- **Key Public API:**
  - `PopulateStartNodes(List<NavNode> nodes)`: Filters entrance and staircase nodes to build dropdown options.
  - `SelectNodeById(string nodeId)`: Programmatically selects an origin node.
  - `event Action<string> OnStartNodeSelected`: Event fired when dropdown value changes.

### 4. `BuildingVisualizer.cs` (`SmartClassroomAR.Visualization`)
- **Type:** `MonoBehaviour` (Attached to `BuildingVisualizer`)
- **Primary Responsibility:** Procedurally generates the 3D CSE building digital twin.
- **Architectural Generation:**
  - `CreateFloorSlab()`: Generates floor plates ($60\text{m} \times 20\text{m} \times 0.25\text{m}$) at $Y = 0\text{m}, 4\text{m}, 8\text{m}$.
  - `CreateCorridorSegment()`: Extrudes central hallway running along $X$-axis.
  - `CreateRoom()`: Generates room volumetric boxes ($8\text{m} \times 3\text{m} \times 5\text{m}$) colored semantically:
    - *Classrooms:* Azure blue (`#3A7BD5`)
    - *Laboratories:* Deep cyan (`#2196F3`)
    - *Offices:* Emerald green (`#4CAF50`)
    - *Stairwells:* Amber orange (`#FFA000`)
  - `CreateFloorLevelSign()`: Renders structural signpost column and 3D badge on each floor.
  - `CreateStaircaseRamps()`: Constructs physical inclined tread geometry connecting floors.
  - `CreateRoomLabel()`: Spawns billboarded TextMeshPro 3D text facing the active camera.
- **Key Methods:**
  - `Initialize(BuildingData data)`: Builds the entire 3D model hierarchy.
  - `SetActiveFloor(string floorId)`: Highlights the target floor while adjusting opacity of non-active floors.

### 5. `RouteVisualizer.cs` (`SmartClassroomAR.Visualization`)
- **Type:** `MonoBehaviour` (Attached to `RouteVisualizer`)
- **Primary Responsibility:** Renders 3D navigation paths, waypoint markers, and user position indicator.
- **Visual Features:**
  - `LineRenderer` path ribbon with dual styling: completed steps rendered in bright emerald (`#00E676`), upcoming route in cyan (`#00E5FF`).
  - Animated pulsing start marker (green sphere) and destination marker (red beacon with vertical bobbing sine-wave animation).
  - Staircase transition markers (rotated amber diamonds).
  - User avatar pin: Composed of ground halo pad, upright pedestal stem, glowing beacon orb, and point light.
- **Key Methods:**
  - `RenderRoute(List<NavNode> nodes, string destinationName)`: Creates line geometry and markers.
  - `AdvanceUserToNextWaypoint()`: Advances user marker to the next waypoint along the polyline.
  - `ResetToStart()`: Repositions user marker back to node index 0.
  - `ClearRoute()`: Destroys all route GameObjects and cleans up materials.

### 6. `OrbitCamera.cs` (`SmartClassroomAR.Visualization`)
- **Type:** `MonoBehaviour` (Attached to `Main Camera`)
- **Primary Responsibility:** Provides orbit, pan, and zoom controls optimized for desktop WebGL and Editor.
- **Key Features:**
  - Left-click drag: Spherical orbit around building center ($X=22\text{m}, Y=4.2\text{m}, Z=0\text{m}$).
  - Alt + Left-click or Middle-click drag: Camera plane panning.
  - Mouse scroll wheel: Distance zoom clamped between $8\text{m}$ and $100\text{m}$.
  - Floor hotkeys: **Key 1** (Ground Floor $Y=0\text{m}$), **Key 2** (First Floor $Y=4\text{m}$), **Key 3** (Second Floor $Y=8\text{m}$).
  - View reset: **Key R** resets camera to isometric overview ($26.5\text{m}$ distance, $35^\circ$ yaw, $26^\circ$ pitch).
  - UI Raycast suppression: Prevents camera movement when mouse hovers over UI buttons or input fields.

### 7. `ARSessionController.cs` (`SmartClassroomAR.AR`)
- **Type:** `MonoBehaviour` (Attached to `ARSession`)
- **Primary Responsibility:** Monitors AR tracking state (`None`, `Limited`, `Tracking`).
- **Platform Parity (`#if UNITY_WEBGL || UNITY_EDITOR`):**
  - Instantly initializes in Demo Navigation Mode on frame 0, bypassing mobile hardware timeouts.
  - On Android mobile devices, automatically launches ARCore tracking session.

### 8. `SceneSetup.cs` (`SmartClassroomAR.Editor`)
- **Type:** Editor Automation Script
- **Primary Responsibility:** Headless batchmode scene generator (`build_scene.ps1`) and WebGL compiler (`build_webgl.ps1`).
- **Safety Configurations:**
  - Injects `InputSystemUIInputModule.AssignDefaultActions()` to ensure button clickability under the new Input System.
  - Generates `_ShaderKeeper` GameObject with pre-referenced materials to prevent WebGL shader stripping.
  - Sets `scrollSensitivity = 35f` and `raycastTarget = true` on ScrollRect.
  - Injects `window.unityInstance = unityInstance;` into `index.html`.
