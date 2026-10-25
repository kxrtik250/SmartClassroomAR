# Data Replacement Guide

## How to Replace Placeholder Data with Real CSE Block Measurements

The navigation system uses a JSON data file (`Assets/Data/cse_block_building_data.json`) that currently contains **placeholder data**. This guide explains how to replace it with actual measurements from the CSE block at Sathyabama Institute of Science and Technology.

---

## Step 1: Survey the Building

### Equipment Needed
- Measuring tape (30m+) or laser distance meter
- Graph paper or tablet for sketching
- Camera for reference photos
- Pen/pencil

### What to Measure

For each floor:
1. **Overall dimensions**: Length and width of the building
2. **Corridor layout**: Length, width, and position of each corridor
3. **Room positions**: Door position along the corridor for each room
4. **Room identifiers**: Actual room numbers/names on doors
5. **Staircase positions**: Location and width of each staircase
6. **Entrance positions**: Location of each entrance/exit
7. **Floor height**: Distance between floors (staircase height)

### Coordinate System
Choose a consistent coordinate system:
- **Origin (0, 0, 0)**: Place at the main entrance door
- **X-axis**: Along the main corridor (positive = away from entrance)
- **Y-axis**: Vertical (positive = upward; each floor adds ~3-4 meters)
- **Z-axis**: Perpendicular to corridor (positive = toward rooms)
- **Units**: Meters

---

## Step 2: Draw the Navigation Graph

### Nodes
Place navigation nodes at:
- Each room entrance/door
- Corridor intersections
- Corridor endpoints
- Staircase landings (one per floor)
- Building entrances
- Every point where a user needs to make a directional decision

### Naming Convention
Use consistent node IDs:
```
N_{FloorId}_{Description}
```
Examples:
- `N_F0_ENTRANCE` — Ground floor main entrance
- `N_F0_CORR1` — Ground floor corridor point 1
- `N_F1_ROOM101` — First floor Room 101 entrance
- `N_F1_STAIR_DOWN` — First floor staircase (going down)

### Edges
Connect nodes with edges representing walkable paths:
- Corridor segments between adjacent nodes
- Door connections from corridor to room
- Staircase connections between floors
- Measure or estimate walking distance for each edge

---

## Step 3: Edit the JSON Data File

Open `Assets/Data/cse_block_building_data.json` and update each section.

### Floors Section
```json
"floors": [
  {
    "floorId": "F0",
    "floorName": "Ground Floor",
    "floorNumber": 0,
    "description": "Actual ground floor description"
  },
  ...
]
```
- Add or remove floors to match the actual building
- Use sequential floor numbers (0, 1, 2, ...)

### Destinations Section
```json
"destinations": [
  {
    "destinationId": "DEST_ROOM101",
    "destinationName": "Room 101 - Data Structures Lab",
    "department": "Computer Science and Engineering",
    "floorId": "F1",
    "navigationNodeId": "N_F1_ROOM101",
    "destinationType": "Laboratory",
    "description": "Actual room description"
  },
  ...
]
```
- `destinationId`: Unique ID starting with `DEST_`
- `destinationName`: The actual name/number on the room
- `navigationNodeId`: Must match an existing node ID
- `destinationType`: One of: `Classroom`, `Laboratory`, `FacultyRoom`, `Entrance`, `Staircase`, `Corridor`, `Other`

### Nodes Section
```json
"nodes": [
  {
    "nodeId": "N_F1_ROOM101",
    "floorId": "F1",
    "x": 15.5,
    "y": 4.0,
    "z": 7.2,
    "label": "Room 101",
    "nodeType": "Destination"
  },
  ...
]
```
- Use measured coordinates in meters
- `y` should reflect the floor height (e.g., 0 for GF, 4 for 1F, 8 for 2F)
- `nodeType`: One of: `Waypoint`, `Destination`, `Staircase`, `Entrance`, `Corridor`

### Edges Section
```json
"edges": [
  {
    "edgeId": "E001",
    "fromNodeId": "N_F0_ENTRANCE",
    "toNodeId": "N_F0_CORR1",
    "cost": 5.0,
    "isBidirectional": true,
    "edgeType": "Corridor"
  },
  ...
]
```
- `cost`: Walking distance in meters (measure or estimate)
- `isBidirectional`: `true` for corridors and staircases, `false` for one-way paths
- `edgeType`: One of: `Corridor`, `Staircase`, `Entrance`, `Door`
- **Staircase edges** connect nodes on different floors (e.g., `N_F0_STAIR_UP` → `N_F1_STAIR_DOWN`)

---

## Step 4: Validate the Data

After editing, the app automatically validates the data on startup. Check the console/log for:

- ❌ **"Edge references non-existent node"** — Fix the `fromNodeId` or `toNodeId`
- ❌ **"Destination references non-existent node"** — Fix the `navigationNodeId`
- ❌ **"Floor not found"** — Fix the `floorId` reference
- ❌ **"Zero or negative edge cost"** — Fix the edge `cost` value
- ❌ **"Unreachable node"** — Add edges to connect the isolated node

All validation errors must be fixed before navigation will work correctly.

---

## Step 5: Test the Updated Data

1. Save the JSON file
2. Restart the app or reload data in Unity Editor
3. Run `NavigationTests.cs` to verify graph integrity
4. Test pathfinding between various destinations
5. Verify all destinations appear in the selection UI
6. Test multi-floor routes that use staircase connections

---

## Example: Adding a New Room

To add "Room 103" on the first floor:

1. **Add a node** at the room's door position:
```json
{
  "nodeId": "N_F1_ROOM103",
  "floorId": "F1",
  "x": 30.0,
  "y": 4.0,
  "z": 7.0,
  "label": "Room 103",
  "nodeType": "Destination"
}
```

2. **Add a destination** record:
```json
{
  "destinationId": "DEST_ROOM103",
  "destinationName": "Room 103",
  "department": "Computer Science and Engineering",
  "floorId": "F1",
  "navigationNodeId": "N_F1_ROOM103",
  "destinationType": "Classroom",
  "description": "First floor classroom"
}
```

3. **Add an edge** connecting it to the nearest corridor node:
```json
{
  "edgeId": "E_NEW_001",
  "fromNodeId": "N_F1_CORR2",
  "toNodeId": "N_F1_ROOM103",
  "cost": 7.5,
  "isBidirectional": true,
  "edgeType": "Door"
}
```

---

## Common Mistakes to Avoid

| Mistake | Consequence |
|---------|------------|
| Duplicate node IDs | Validation error, unpredictable routing |
| Missing edge to a room node | Room becomes unreachable |
| Wrong floor ID on a node | Multi-floor routing fails |
| Zero-cost edges | Validation error |
| Non-matching navigation node IDs | Destination cannot be located in graph |
| Forgetting staircase edges between floors | Cross-floor navigation fails |
| Using incorrect units (cm instead of m) | AR arrows placed at wrong scale |

---

## Alternative: Using CSEBlockData.cs

Instead of editing the JSON file, you can also modify `Assets/Scripts/Mapping/CSEBlockData.cs` which contains the same data as a hardcoded JSON string. This is useful for:
- Keeping data embedded in the build
- Avoiding file-loading issues on Android
- Version-controlling data changes with code

Update the `GetDefaultBuildingDataJson()` method with your measured data, following the same JSON structure.
