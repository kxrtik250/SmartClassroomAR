# Unity Setup and Android Build Guide

## Prerequisites

### Required Software
1. **Unity 2022.3.x LTS** (2022.3.51f1 recommended)
   - Download from: https://unity.com/download
   - Install via Unity Hub

2. **Android Build Support** (Unity module)
   - In Unity Hub → Installs → click gear icon on your Unity version → Add Modules
   - Check: **Android Build Support**
   - Check: **Android SDK & NDK Tools** (if not already installed separately)
   - Check: **OpenJDK** (if not already installed separately)

3. **Android SDK** (API 28+)
   - Usually installed with Unity's Android Build Support
   - Or install separately via Android Studio

4. **JDK 11+**
   - Usually provided by Unity's Android Build Support
   - Or install separately (e.g., Microsoft OpenJDK)

## Step 1: Open the Project

1. Open **Unity Hub**
2. Click **Add** → **Add project from disk**
3. Navigate to and select the `SmartClassroomAR` folder
4. Unity will detect it as a Unity 2022.3 project
5. Click to open the project

### First-Time Import
On first open, Unity will:
- Import all packages (AR Foundation, ARCore XR Plugin, TextMeshPro, etc.)
- Compile all scripts
- Import TextMeshPro essentials (accept the prompt if shown)

**This may take 5-15 minutes.** Wait for the progress bar to complete.

### If Package Errors Occur
1. Go to **Window → Package Manager**
2. Verify these packages are installed:
   - AR Foundation 5.1.5
   - ARCore XR Plugin 5.1.5
   - XR Plugin Management 4.4.0
   - TextMeshPro 3.0.9
3. If missing, click **+** → **Add package by name** and enter the package name

## Step 2: Verify Project Settings

### Player Settings
1. Go to **Edit → Project Settings → Player**
2. Switch to **Android** tab (robot icon)
3. Verify:
   - **Company Name**: SathyabamaCSE
   - **Product Name**: SmartClassroomAR
   - **Package Name**: com.sathyabamacse.smartclassroomnav
   - **Minimum API Level**: Android 9.0 (API 28)
   - **Target API Level**: Automatic (highest installed) or API 33+
   - **Scripting Backend**: IL2CPP (recommended) or Mono
   - **Target Architectures**: ARM64 (check), ARMv7 (optional)

### XR Plugin Management
1. Go to **Edit → Project Settings → XR Plugin Management**
2. Switch to **Android** tab
3. Check **ARCore** is enabled
4. If not visible, click **Install XR Plugin Management** first

### Quality Settings
1. Go to **Edit → Project Settings → Quality**
2. For Android, set the default quality level to **Medium** or **Low** for best AR performance

## Step 3: Configure the Scene

### Open Main Scene
1. Open `Assets/Scenes/MainScene.unity`
2. If this scene doesn't exist yet, create it:

### Required Scene Setup
The main scene needs these GameObjects:

1. **AR Session** (Add Component: `AR Session`)
   - Required for AR Foundation to function

2. **AR Session Origin** (or XR Origin for newer AR Foundation)
   - Add Component: `AR Session Origin` (or `XR Origin`)
   - Contains the AR Camera as a child
   - Add Component: `AR Plane Manager` (optional, for surface detection)

3. **AR Camera** (child of AR Session Origin)
   - Tag: `MainCamera`
   - Add Component: `AR Camera Manager`
   - Add Component: `AR Camera Background`
   - Add Component: `Tracked Pose Driver` (or `TrackedPoseDriver`)

4. **Navigation Manager** (Empty GameObject)
   - Add Component: `NavigationUIController`
   - Add Component: `ARSessionController`
   - Add Component: `ARNavigationRenderer`
   - Add Component: `CoordinateCalibrator`
   - Add Component: `ARPrefabSetup`

5. **Canvas** (UI Canvas)
   - Render Mode: **Screen Space - Overlay**
   - Canvas Scaler: **Scale With Screen Size**, Reference Resolution: 1080x1920
   - Contains all UI panels (destination selection, navigation info, etc.)

6. **EventSystem** (for UI input)

### Detailed UI Canvas Setup

Under the Canvas, create these panels:

```
Canvas
├── StatusPanel (top bar)
│   └── StatusText (TextMeshPro)
├── DestinationSelectionPanel
│   ├── TitleText ("Select Destination")
│   ├── SearchInputField (TMP InputField)
│   ├── StartNodeDropdown (TMP Dropdown)
│   ├── DestinationScrollView
│   │   └── Content (Vertical Layout Group)
│   │       └── [Destination buttons generated at runtime]
│   └── EvaluationButton
├── NavigationPanel (hidden by default)
│   ├── DestinationInfoText (TextMeshPro)
│   ├── FloorIndicatorText (TextMeshPro)
│   ├── DistanceText (TextMeshPro)
│   ├── NavigationInfoText (TextMeshPro)
│   └── CancelButton
├── DestinationReachedPanel (hidden by default)
│   ├── ConfirmationText ("Destination Reached!")
│   ├── DestinationDetailsText (TextMeshPro)
│   └── NavigateAnotherButton
└── EvaluationPanel (hidden by default)
    ├── SessionInfoText
    ├── TimerText
    ├── StartStopButton
    ├── FeedbackPanel
    │   ├── ArrowClaritySlider (1-5)
    │   ├── DestinationEaseSlider (1-5)
    │   ├── UsabilitySlider (1-5)
    │   ├── AccuracySlider (1-5)
    │   ├── RouteFollowingSlider (1-5)
    │   ├── CommentsInputField
    │   └── SubmitButton
    └── ExportButton
```

## Step 4: Wire Up References

In the Unity Inspector for **NavigationUIController**:
1. Drag the `DestinationSelectionUI` component to its field
2. Drag the `ARSessionController` component to its field
3. Drag the `ARNavigationRenderer` component to its field
4. Drag the `CoordinateCalibrator` component to its field
5. Drag each UI panel (destination selection, navigation, reached, status) to its field
6. Drag each TextMeshPro text component to its field

## Step 5: Build for Android

1. Go to **File → Build Settings**
2. Select **Android** platform
3. Click **Switch Platform** (if not already on Android)
4. Click **Add Open Scenes** to add MainScene
5. Click **Player Settings** and verify settings from Step 2
6. Connect an Android device via USB with:
   - Developer Options enabled
   - USB Debugging enabled
7. Click **Build And Run** or **Build** to generate APK

### Build Troubleshooting

| Issue | Solution |
|-------|----------|
| "No Android SDK found" | Edit → Preferences → External Tools → set Android SDK path |
| "JDK not found" | Edit → Preferences → External Tools → set JDK path |
| Gradle build failed | Check Logcat for details; ensure API levels match |
| AR Foundation errors | Verify XR Plugin Management has ARCore enabled |
| Script compilation errors | Check Console for specific errors; verify all scripts are present |

## Step 6: Test on Device

### First Launch
1. Install the APK on an ARCore-compatible Android device
2. Grant camera permission when prompted
3. The app will show the destination selection screen
4. Point the camera at a flat surface to initialize AR tracking

### Testing Steps
1. Select a starting position (defaults to Main Entrance)
2. Select a destination from the list
3. View the calculated route information
4. Observe AR arrows overlaid on camera view
5. Follow arrows toward destination
6. Verify destination confirmation appears

### Important Notes
- AR arrows use **demonstration coordinates** unless calibrated to the actual building
- The starting position is selected manually, not detected automatically
- Indoor tracking quality depends on lighting and surface features
- AR tracking may be limited in plain corridors with few visual features

## Step 7: Run Tests

### In Unity Editor
1. Open **Window → General → Test Runner**
2. Select **EditMode** or **PlayMode** tab
3. Click **Run All** to execute navigation tests

### In Play Mode
1. Create a test scene with a `TestRunner` component
2. Enter Play Mode
3. Check Console for test results

### Expected Test Output
```
========================================
NAVIGATION TEST SUITE
========================================
--- Category 1: Graph Validation ---
  PASS: Graph has nodes
  PASS: Graph has >= 25 nodes
  ...
--- Category 2: Single Floor Routes ---
  PASS: Entrance to Office: path found
  ...
========================================
TEST RESULTS: XX passed, 0 failed, XX total
========================================
```
