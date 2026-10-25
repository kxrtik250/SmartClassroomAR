# Smart Classroom Navigation System Using Augmented Reality

A Unity-based multi-platform indoor navigation application and 3D digital-twin system for the Computer Science and Engineering (CSE) block at Sathyabama Institute of Science and Technology, Chennai.

---

### Quick Launch — Interactive WebGL Live Demo
Experience the application directly in your web browser:
1. Run the local HTTP server in PowerShell:
   ```powershell
   .\serve_webgl.ps1
   ```
2. Open your web browser at:
   ```
   http://localhost:8080/index.html
   ```
3. Use the left directory panel to search and select classrooms, or use mouse and keyboard hotkeys (Keys 1, 2, 3) to orbit and inspect the 3D building twin.

---

## Academic Documentation Package
Comprehensive college project documentation is located in [`Documentation/`](file:///c:/Users/s%20karthik%20sriram/Desktop/projects/ar%20vr%20classroom/SmartClassroomAR/Documentation):
- [`01_PROJECT_ABSTRACT.md`](file:///c:/Users/s%20karthik%20sriram/Desktop/projects/ar%20vr%20classroom/SmartClassroomAR/Documentation/01_PROJECT_ABSTRACT.md): Executive summary & project metadata
- [`02_PROBLEM_STATEMENT_AND_OBJECTIVES.md`](file:///c:/Users/s%20karthik%20sriram/Desktop/projects/ar%20vr%20classroom/SmartClassroomAR/Documentation/02_PROBLEM_STATEMENT_AND_OBJECTIVES.md): GPS attenuation & indoor navigation challenges
- [`03_SYSTEM_ARCHITECTURE.md`](file:///c:/Users/s%20karthik%20sriram/Desktop/projects/ar%20vr%20classroom/SmartClassroomAR/Documentation/03_SYSTEM_ARCHITECTURE.md): 5-tier architecture & finite state machine
- [`04_NAVIGATION_GRAPH_AND_ALGORITHMS.md`](file:///c:/Users/s%20karthik%20sriram/Desktop/projects/ar%20vr%20classroom/SmartClassroomAR/Documentation/04_NAVIGATION_GRAPH_AND_ALGORITHMS.md): Dijkstra pathfinding & node directory
- [`05_UNITY_COMPONENTS_AND_SCRIPTS.md`](file:///c:/Users/s%20karthik%20sriram/Desktop/projects/ar%20vr%20classroom/SmartClassroomAR/Documentation/05_UNITY_COMPONENTS_AND_SCRIPTS.md): Script APIs, events & inspector references
- [`06_WEBGL_DEPLOYMENT_AND_AR_ANALYSIS.md`](file:///c:/Users/s%20karthik%20sriram/Desktop/projects/ar%20vr%20classroom/SmartClassroomAR/Documentation/06_WEBGL_DEPLOYMENT_AND_AR_ANALYSIS.md): WebGL 3D simulation vs Android ARCore
- [`07_TEST_CASES_AND_VERIFICATION_EVIDENCE.md`](file:///c:/Users/s%20karthik%20sriram/Desktop/projects/ar%20vr%20classroom/SmartClassroomAR/Documentation/07_TEST_CASES_AND_VERIFICATION_EVIDENCE.md): 362 unit tests & 5 browser CDP tests
- [`08_INSTALLATION_AND_USER_GUIDE.md`](file:///c:/Users/s%20karthik%20sriram/Desktop/projects/ar%20vr%20classroom/SmartClassroomAR/Documentation/08_INSTALLATION_AND_USER_GUIDE.md): WebGL, Editor & Android installation
- [`09_COLLEGE_VIVA_AND_DEMO_GUIDE.md`](file:///c:/Users/s%20karthik%20sriram/Desktop/projects/ar%20vr%20classroom/SmartClassroomAR/Documentation/09_COLLEGE_VIVA_AND_DEMO_GUIDE.md): 5-minute presentation script & 30 viva Q&As

---

## Project Overview

This system helps students and visitors navigate to classrooms, laboratories, faculty rooms, and other facilities within the CSE block using Augmented Reality and 3D digital-twin visualization. Users select a destination from the app interface and receive step-by-step route guidance (arrows, markers, polylines, and labels) overlaid on the 3D model or real-world camera view.

### Key Features
- **Multi-floor navigation** across 3 floors of the CSE block (Ground, First, Second)
- **Dijkstra's pathfinding algorithm** with $< 0.5\text{ ms}$ optimal route calculation
- **WebGL 2.0 Browser Deployment** with responsive split-screen sidebar and interactive 3D viewport
- **Step Forward & Auto Simulation** with waypoint tracking, distance estimation, and arrival card
- **Orbit Camera Controller** with mouse orbit, Alt-pan, scroll zoom, and Keys 1, 2, 3 floor isolation
- **AR arrow indicators** displayed through Android phone camera via ARCore
- **Automated Verification:** 362 automated unit tests passing (100% pass rate)

## Technology Stack

| Component | Technology | Version |
|---|---|---|
| Engine | Unity | 2022.3.51f1 LTS |
| Language | C# | .NET Standard 2.1 |
| Web Runtime | WebGL 2.0 / WebAssembly | Emscripten / IL2CPP |
| AR Framework | AR Foundation | 5.1.5 |
| AR Provider | ARCore XR Plugin | 5.1.5 |
| XR Management | XR Plugin Management | 4.4.0 |
| UI & Typography | TextMeshPro + Unity uGUI | 3.0.9 |
| Target Platforms | WebGL 2.0 (Desktop) & Android | API 28+ |

## Project Structure

```
SmartClassroomAR/
├── Assets/
│   ├── Scenes/
│   │   └── MainScene.unity          # Primary AR navigation scene
│   ├── Scripts/
│   │   ├── Navigation/
│   │   │   ├── NavNode.cs           # Navigation graph node
│   │   │   ├── NavEdge.cs           # Navigation graph edge
│   │   │   ├── NavigationGraph.cs   # Graph data structure with validation
│   │   │   └── Pathfinder.cs        # Dijkstra's algorithm implementation
│   │   ├── Mapping/
│   │   │   ├── FloorData.cs         # Floor information data model
│   │   │   ├── DestinationData.cs   # Classroom/facility data model
│   │   │   ├── BuildingDataLoader.cs# JSON data loader and validator
│   │   │   └── CSEBlockData.cs      # Default placeholder CSE block data
│   │   ├── AR/
│   │   │   ├── ARSessionController.cs    # AR session and tracking management
│   │   │   ├── ARNavigationRenderer.cs   # AR arrow/marker rendering
│   │   │   ├── CoordinateCalibrator.cs   # Building-to-AR coordinate mapping
│   │   │   └── ARPrefabSetup.cs          # Default AR prefab creation
│   │   ├── UI/
│   │   │   ├── DestinationSelectionUI.cs # Destination list and selection
│   │   │   ├── NavigationUIController.cs # Main UI state controller
│   │   │   └── StartNodeSelector.cs      # Starting position selector
│   │   └── Evaluation/
│   │       ├── EvaluationManager.cs      # Test session recording
│   │       └── EvaluationUI.cs           # Evaluation questionnaire UI
│   ├── Prefabs/                      # AR arrow, marker prefabs
│   ├── Materials/                    # Materials for AR objects
│   ├── Data/
│   │   ├── cse_block_building_data.json      # Placeholder CSE block data
│   │   └── evaluation_results_template.json  # Empty evaluation template
│   └── Tests/
│       ├── NavigationTests.cs        # Pathfinding test suite
│       └── TestRunner.cs             # Test execution entry point
├── Packages/
│   └── manifest.json                 # Unity package dependencies
├── ProjectSettings/
│   ├── ProjectSettings.asset         # Unity project settings
│   ├── ProjectVersion.txt            # Unity version
│   └── ...                           # Other Unity settings
├── README.md                         # This file
├── SETUP_GUIDE.md                    # Unity setup and build instructions
├── CALIBRATION_GUIDE.md              # AR coordinate calibration guide
├── TESTING_PROTOCOL.md               # Evaluation testing protocol
├── DATA_REPLACEMENT_GUIDE.md         # Guide for real building data
└── IMPLEMENTATION_STATUS.md          # Current implementation status
```

## Requirements

### Development
- Unity 2022.3.x LTS (2022.3.51f1 recommended)
- Android Build Support module for Unity
- Android SDK (API 28+)
- JDK 11+

### Runtime (Test Device)
- Android device with ARCore support
- Android 9.0 (API 28) or higher
- Camera permission

## Quick Start

1. **Open in Unity**: Open Unity Hub → Add Project → Select `SmartClassroomAR` folder
2. **Wait for packages**: Unity will import AR Foundation, ARCore, and other packages
3. **Open scene**: `Assets/Scenes/MainScene.unity`
4. **Build for Android**: File → Build Settings → Android → Build
5. **Install on device**: Transfer APK to ARCore-compatible Android device
6. **Test**: Launch app, select a destination, view AR arrows

See [SETUP_GUIDE.md](SETUP_GUIDE.md) for detailed instructions.

## Architecture

### Data Flow
```
CSE Block JSON Data
    ↓
BuildingDataLoader (validates & parses)
    ↓
BuildingData (floors, destinations, nodes, edges)
    ↓
NavigationGraph (graph data structure)
    ↓
Pathfinder (Dijkstra's algorithm)
    ↓
PathResult (ordered nodes, cost, floor info)
    ↓
ARNavigationRenderer (3D arrows & markers)
    ↓
AR Camera Display (user sees AR guidance)
```

### Module Responsibilities

| Module | Responsibility |
|--------|---------------|
| **Mapping** | Building data models, JSON loading, CSE block data |
| **Navigation** | Graph structure, Dijkstra's pathfinding, path validation |
| **AR** | AR session, tracking, coordinate calibration, 3D rendering |
| **UI** | Destination selection, navigation status, destination info |
| **Evaluation** | Test session recording, user feedback collection |

### Key Design Decisions
- **Local data only**: All building data is stored as JSON, no external backend
- **Separable pathfinding**: Route calculation is testable without AR runtime
- **Calibration-aware**: Coordinate mapping between building and AR is explicit
- **Placeholder-safe**: Sample data is clearly marked and easily replaceable

## Important Notices

### Placeholder Data
The CSE block building data (`cse_block_building_data.json`) contains **placeholder coordinates, room numbers, and layouts**. These are NOT verified measurements of the actual Sathyabama CSE block. See [DATA_REPLACEMENT_GUIDE.md](DATA_REPLACEMENT_GUIDE.md) for how to replace with real data.

### AR Coordinate Calibration
The system requires physical calibration to align AR coordinates with building coordinates. Without calibration, AR arrows are displayed in **demonstration mode** and do not accurately indicate real-world directions. See [CALIBRATION_GUIDE.md](CALIBRATION_GUIDE.md).

### No Indoor Positioning
This system does not implement GPS, Wi-Fi, or Bluetooth-based indoor positioning. The user must manually select their starting position. The system then provides relative direction guidance.

## Testing

### Automated Tests
Run `NavigationTests.cs` from Unity Test Runner or as a standalone console application:
- Graph validation (structure, connectivity, references)
- Single-floor routing (entrance to office, lab to lab)
- Multi-floor routing (ground floor to second floor)
- Invalid node handling
- Unreachable destination detection
- Route edge validation

### Manual Testing
See [TESTING_PROTOCOL.md](TESTING_PROTOCOL.md) for the complete evaluation protocol.

## Authors

Based on the research paper "Smart Classroom Navigation System Using Augmented Reality" by:
- R Kavya, M Lyka Israth, Krothapalli Mokshagna, Kovvuru Pujitha
- Under guidance of Srideivanai Nagarajan, Assistant Professor
- Department of Computer Science and Engineering
- Sathyabama Institute of Science and Technology, Chennai, Tamil Nadu, India

## License

This project is developed for academic and educational purposes at Sathyabama Institute of Science and Technology.
