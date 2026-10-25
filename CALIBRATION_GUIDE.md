# AR Coordinate Calibration Guide

## Overview

The Smart Classroom Navigation System uses two coordinate systems:

1. **Building Coordinate System** — The digital map coordinates defined in `cse_block_building_data.json`
2. **AR Session Coordinate System** — The coordinates provided by ARCore during the AR session

These two systems must be aligned (calibrated) for AR arrows to point in physically correct directions within the actual CSE block building.

## Current Status

> **⚠️ NOT CALIBRATED**: The current system uses placeholder building coordinates that have not been measured from the actual CSE block at Sathyabama Institute. AR navigation arrows are displayed in **demonstration mode**.

## How Calibration Works

The `CoordinateCalibrator` component transforms building coordinates to AR coordinates using:

```
AR_Position = (Building_Position + Offset) × Scale
```

Where:
- **Offset** is a Vector3 translation to align the building origin with the AR session origin
- **Scale** is a uniform scaling factor (usually 1.0 for meter-based coordinates)

## Step-by-Step Calibration Procedure

### Prerequisites
- Physical access to the CSE block building
- A measuring tape or laser distance meter
- The AR app running on a compatible Android device
- A printed or digital copy of the building floor plan

### Step 1: Measure Real Building Coordinates

1. Choose an **origin point** in the building (e.g., the center of the main entrance door)
2. Define axes:
   - **X-axis**: Along the main corridor (east-west or left-right)
   - **Y-axis**: Vertical (upward through floors)
   - **Z-axis**: Perpendicular to corridor (north-south or front-back)
3. Measure the position of at least **3 reference points** in meters:
   - The origin point (0, 0, 0)
   - A point at the far end of the ground floor corridor
   - A point at a known classroom door
4. For each additional floor:
   - Measure the floor height (typically 3-4 meters per floor)
   - Measure staircase entry/exit positions

### Step 2: Update Building Data

Edit `Assets/Data/cse_block_building_data.json`:
1. Update node coordinates to match your measurements
2. Update edge costs to match measured walking distances
3. Add or remove nodes to match the actual building layout
4. See [DATA_REPLACEMENT_GUIDE.md](DATA_REPLACEMENT_GUIDE.md) for detailed instructions

### Step 3: Collect AR Calibration Points

In the app:
1. Stand at your chosen building **origin point**
2. Start the AR session and let it stabilize (tracking status should show "Tracking")
3. Note the AR camera position displayed in the app (this is your first calibration point)
4. Walk to a **second reference point** (at least 10 meters away for accuracy)
5. Note the AR camera position at this point
6. Optionally, walk to a third point for better accuracy

### Step 4: Enter Calibration Data

Using the `CoordinateCalibrator` component:

```csharp
// In code or via the evaluation UI:
calibrator.AddCalibrationPoint(
    buildingPos: new Vector3(0, 0, 0),      // Building coordinates of origin
    arPos: arCameraPosition,                 // AR position at origin
    label: "Main Entrance"
);

calibrator.AddCalibrationPoint(
    buildingPos: new Vector3(40, 0, 0),     // Building coordinates of corridor end
    arPos: arCameraPositionAtCorridorEnd,   // AR position at corridor end
    label: "Corridor End"
);

calibrator.CalculateCalibration();
calibrator.SaveCalibration();
```

### Step 5: Verify Calibration

1. Navigate to a known destination
2. Check that AR arrows point in the correct physical direction
3. Check that the destination marker appears at or near the actual destination
4. If arrows are misaligned:
   - Recollect calibration points more carefully
   - Ensure AR tracking was stable during measurement
   - Try from different starting points

## Limitations

### AR Session Drift
- ARCore tracking may drift over time and distance
- Calibration accuracy decreases as you move farther from calibration points
- Recalibrate periodically or when tracking quality drops

### Single-Session Calibration
- Calibration is relative to a specific AR session
- When the app restarts, the AR coordinate system resets
- The saved calibration may need to be re-applied at the same physical starting point

### Floor Transitions
- AR tracking does not automatically detect which floor the user is on
- When navigating stairs, the user must carry the device smoothly
- ARCore may lose tracking on stairways due to rapid movement

### Accuracy Expectations
- With good calibration: ±1-2 meters accuracy for nearby navigation points
- Without calibration: AR arrows show general direction concept only
- Physical navigation accuracy depends on:
  - Quality of building coordinate measurements
  - ARCore tracking quality (affected by lighting, surface texture)
  - Time since calibration (drift accumulates)

## Demo Mode

For demonstration without physical calibration:
- The app uses identity transform (offset=0, scale=1)
- AR arrows are placed at building coordinate positions
- This shows the navigation concept but NOT accurate physical directions
- The app logs a warning: "Using demo calibration - not calibrated to physical building"

## Programmatic Calibration API

```csharp
// Access the calibrator
CoordinateCalibrator calibrator = GetComponent<CoordinateCalibrator>();

// Add measured points (minimum 2)
calibrator.AddCalibrationPoint(buildingPos, arPos, "Point Label");

// Calculate transform
calibrator.CalculateCalibration();

// Check status
if (calibrator.IsCalibrated)
{
    // Transform any building coordinate to AR space
    Vector3 arPosition = calibrator.TransformBuildingToAR(buildingPosition);
}

// Persist across sessions
calibrator.SaveCalibration();
calibrator.LoadCalibration();
```
