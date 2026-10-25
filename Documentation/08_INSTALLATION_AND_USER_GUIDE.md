# 8. Installation & User Guide

## 1. System Requirements

### WebGL Deployment (Evaluator & Demo Environment):
- **Operating System:** Windows 10/11, macOS 12+, or Ubuntu Linux 20.04+.
- **Web Browser:** Google Chrome 95+, Microsoft Edge 95+, Mozilla Firefox 90+, or Safari 15+ (WebGL 2.0 and WebAssembly enabled).
- **Runtime Environment:** Python 3.8+ (for local HTTP hosting) or Node.js.
- **Hardware:** Standard desktop or laptop with integrated or dedicated GPU supporting WebGL 2.0.

### Unity Development & Play Mode:
- **Unity Version:** Unity 2022.3.51f1 LTS.
- **Modules Installed:** WebGL Build Support, Android Build Support (OpenJDK, Android SDK & NDK tools).
- **Editor Packages:** TextMeshPro 3.0.9, Input System 1.7.0, AR Foundation 5.1.5, ARCore XR Plugin 5.1.5.

### Mobile Android AR Deployment:
- **Device:** ARCore-compatible Android smartphone running Android 9.0 (API Level 28) or higher.
- **Camera:** Standard RGB rear camera with autofocus.
- **Installation Tool:** Android Debug Bridge (ADB) or direct APK sideloading.

---

## 2. Launching & Running the WebGL Application

The fastest way to experience the SmartClassroomAR system is through the local HTTP WebGL server.

### Step 1: Launch Local HTTP Server
Open a terminal (PowerShell or Bash) in the project directory:
```powershell
# Option A: Using the provided PowerShell script
.\serve_webgl.ps1

# Option B: Directly via Python
python -m http.server 8080 --directory "Builds\WebGL\SmartClassroomAR_WebGL"
```

### Step 2: Open in Web Browser
Navigate to:
```
http://localhost:8080/index.html
```
The application will load the Unity WebAssembly runtime and immediately present the 3D CSE Building and the navigation interface.

---

## 3. WebGL User Interaction Guide

### 1. Viewport & Camera Navigation
- **Orbit Rotation:** Click and drag the **Left Mouse Button** anywhere on the 3D viewport to orbit around the building.
- **Panning:** Hold **Left Alt** and drag with the **Left Mouse Button** (or drag with the **Middle Mouse Button**) to translate the camera.
- **Zooming:** Rotate the **Mouse Scroll Wheel** to smoothly zoom in or out.
- **Floor Isolation Hotkeys:**
  - Press **Key 1** (or Numpad 1): Focuses directly on the **Ground Floor ($Y=0\text{m}$)**.
  - Press **Key 2** (or Numpad 2): Focuses directly on the **First Floor ($Y=4\text{m}$)**.
  - Press **Key 3** (or Numpad 3): Focuses directly on the **Second Floor ($Y=8\text{m}$)**.
- **Reset View:** Press **Key R** to restore the default isometric viewpoint.

### 2. Searching & Selecting a Destination
1. **Starting Point Selector:** The top dropdown defaults to `Main Entrance`. To simulate navigation from a stairwell landing, select `Ground Floor Staircase`, `First Floor Staircase`, or `Second Floor Staircase`.
2. **Real-Time Search Bar:** Type into the `Search destinations...` input field (e.g., `"Lab"` or `"Office"`). The directory list filters instantly.
3. **Directory Browsing:** Browse facilities grouped under **Ground Floor (F0)**, **First Floor (F1)**, and **Second Floor (F2)**.
4. **Selecting Destination:** Click any facility button (e.g., `Room 101` or `CSE Department Office`). The system calculates the shortest path in $< 0.5\text{ ms}$, highlights the 3D route polyline, and switches to the navigation control panel.

### 3. Navigation Controls
Once a destination is active, the left panel presents four controls:
- **Step Forward:** Manually advances the user avatar to the next corridor waypoint. The progress bar updates, remaining distance decreases, and the completed route segments turn from cyan to green.
- **Auto Simulate:** Automatically advances the user marker along the route at $1.2\text{s}$ intervals.
- **Reset Route:** Repositions the user avatar back to the origin ($0\%$).
- **Back / Change:** Cancels the active route and returns to the directory browser.

### 4. Destination Arrival Card
Upon reaching the final node, a compact arrival dialog appears:
- Displays a green checkmark badge.
- Shows destination name, floor identifier, department, and total distance traversed.
- Click **"Navigate to Another"** to select a new destination.

---

## 4. Unity Editor Play Mode Guide

To inspect or modify the application in the Unity Editor:
1. Launch **Unity Hub** and open the project at:
   `C:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR`
2. In the Project window, open `Assets/Scenes/MainScene.unity`.
3. Press the **Play** button at the top of the Editor.
4. The system boots directly into Demo Navigation Mode on frame 0.
5. All mouse, keyboard, and UI controls function identically to the WebGL build.

---

## 5. Android Mobile APK Sideloading

The verified Android build is pre-compiled and located at:
`Builds/Android/SmartClassroomAR_CompatibilityFix.apk`

To install via USB debugging:
```powershell
adb install -r "Builds\Android\SmartClassroomAR_CompatibilityFix.apk"
```
Or transfer the `.apk` file directly to your smartphone via Google Drive or USB file transfer and install using Android's Files app.
