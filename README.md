# Smart Classroom Navigation System Using Augmented Reality

A Unity-based multi-platform indoor navigation application and 3D digital twin for the Computer Science and Engineering (CSE) block at Sathyabama Institute of Science and Technology, Chennai.

## Project Overview

SmartClassroomAR helps students and visitors navigate classrooms, laboratories, faculty rooms, and other facilities using an interactive 3D building model and an Android AR navigation interface.

Users select a destination and receive route guidance through visual markers, arrows, polylines, and labels. The project supports browser-based 3D visualization and an Android AR demonstration.

> **Important:** The building coordinates and room layout currently use placeholder data. Physical AR alignment requires calibration, and the system does not automatically determine the user's indoor location.

## Key Features

- **Multi-floor navigation:** Ground, first, and second floors.
- **Pathfinding:** Dijkstra's algorithm for route calculation.
- **Interactive 3D digital twin:** Explore the building with orbit, pan, zoom, and floor-isolation controls.
- **WebGL deployment:** Browser-based 3D visualization.
- **Android AR:** ARCore-based arrows and markers.
- **Route simulation:** Step-forward navigation, waypoint tracking, distance estimation, and arrival feedback.
- **Testing:** Automated navigation tests and browser-based verification.

## Technology Stack

| Component | Technology |
|---|---|
| Game engine | Unity 2022.3.51f1 LTS |
| Programming language | C# |
| Web platform | WebGL 2.0 / WebAssembly |
| AR framework | AR Foundation 5.1.5 |
| AR provider | ARCore XR Plugin 5.1.5 |
| XR management | XR Plugin Management 4.4.0 |
| UI | TextMeshPro and Unity uGUI |
| Data | JSON |

## Architecture

The application separates building data, route computation, visualization, and user interaction.

1. **Mapping:** Loads building floors, destinations, nodes, and edges from JSON.
2. **Navigation:** Represents the building as a graph and calculates routes using Dijkstra's algorithm.
3. **Visualization:** Renders the 3D building, route lines, markers, and navigation indicators.
4. **AR:** Manages AR sessions, tracking, coordinate calibration, and AR guidance.
5. **UI and evaluation:** Handles destination selection, navigation status, testing, and feedback.

## Project Structure

```text
SmartClassroomAR/
├── Assets/
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── AR/
│   │   ├── Audio/
│   │   ├── Evaluation/
│   │   ├── Mapping/
│   │   ├── Navigation/
│   │   ├── UI/
│   │   └── Visualization/
│   ├── Data/
│   ├── Prefabs/
│   ├── Materials/
│   └── Tests/
├── Packages/
├── ProjectSettings/
├── Documentation/
├── README.md
├── SETUP_GUIDE.md
├── CALIBRATION_GUIDE.md
└── TESTING_PROTOCOL.md
```

## Getting Started

### Requirements

- Unity 2022.3 LTS (2022.3.51f1 recommended)
- Android Build Support for Android builds
- Android SDK and JDK as required by the Unity installation
- An ARCore-compatible Android device for AR testing

### Open in Unity

1. Clone or download this repository.
2. Open Unity Hub.
3. Select **Add Project** and choose the cloned `SmartClassroomAR` directory.
4. Wait for Unity to import assets and resolve packages.
5. Open `Assets/Scenes/MainScene.unity`.

### Run the WebGL Demo Locally

After generating the WebGL build:

1. Open PowerShell in the project directory.
2. Run:

   ```powershell
   .\serve_webgl.ps1
   ```

3. Open `http://localhost:8080/index.html` in your browser.

The WebGL build is generated output and is not included in the source repository by default.

### Build for Android

1. Open **File → Build Settings** in Unity.
2. Select Android.
3. Configure the required XR settings and verify ARCore support.
4. Build the APK and install it on a compatible device.

See `SETUP_GUIDE.md` for detailed instructions.

## Documentation

The `Documentation/` directory contains the academic project documentation:

1. Project abstract
2. Problem statement and objectives
3. System architecture
4. Navigation graph and algorithms
5. Unity components and scripts
6. WebGL deployment and AR analysis
7. Test cases and verification evidence
8. Installation and user guide
9. College viva and demo guide

Additional guides cover AR coordinate calibration, building data replacement, and testing procedures.

## Limitations

- **Placeholder building data:** Room numbers, coordinates, and layouts are not verified measurements of the actual CSE block.
- **Manual starting location:** Users must select their starting position; automatic indoor positioning is not implemented.
- **AR calibration:** Physical alignment requires calibration. Without it, AR guidance is for demonstration rather than reliable real-world navigation.
- **WebGL versus AR:** The browser build provides 3D visualization; Android AR uses the device camera and ARCore.

## Testing

The project includes navigation tests for graph validation, single-floor and multi-floor routing, invalid nodes, unreachable destinations, and route-edge validation.

Consult `TESTING_PROTOCOL.md` and the project's verification evidence for test procedures and results.

## License and Academic Use

Developed as an academic project at Sathyabama Institute of Science and Technology. Refer to the repository owner for reuse and distribution permissions.
