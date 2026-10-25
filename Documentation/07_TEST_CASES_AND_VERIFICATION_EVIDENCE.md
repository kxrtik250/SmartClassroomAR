# 7. Test Cases & Verification Evidence

## 1. Testing Methodology & Verification Protocol

The **SmartClassroomAR** project adheres to a dual verification methodology:
1. **Automated Unit & Integration Test Suite:** Executed in headless Unity batchmode via NUnit test runner (`run_tests.ps1`). Tests validate graph construction, pathfinding optimality, priority queue mechanics, UI state transitions, and visualizer lifecycle.
2. **Headless Browser CDP Automation Suite:** Executed against the live WebGL HTTP server (`http://localhost:8080`) using Chrome DevTools Protocol (`test_all_destinations.mjs`). Interacts directly with WebGL DOM elements, canvas clicks, and WebAssembly functions to verify user workflows.

---

## 2. Unity Test Runner Results (362 / 362 Tests Passing)

### Test Suite Execution Summary:
- **Total Tests Executed:** 362
- **Tests Passed:** 362 (100.0%)
- **Tests Failed:** 0
- **Execution Environment:** Unity 2022.3.51f1 Batchmode `-runTests`

### Test Suite Category Breakdown:
| Test Category | Test Count | Pass Rate | Key Validations |
| :--- | :---: | :---: | :--- |
| **Graph Topology & Data** | 84 | 100% | Validates bidirectional edges, non-negative weights, valid floor references, and absence of disconnected components. |
| **Pathfinding Engine (Dijkstra)** | 112 | 100% | Tests single-floor shortest paths, multi-floor stair crossings, start/destination validation, priority queue order, and total distance accuracy. |
| **UI State Machine & Controllers** | 96 | 100% | Tests state transitions (`SelectingDestination` $\rightarrow$ `Navigating` $\rightarrow$ `DestinationReached`), search filtering, and dropdown selection. |
| **Spatial Visualization Lifecycle** | 70 | 100% | Tests `BuildingVisualizer` root creation, `RouteVisualizer` line segment updates, user waypoint stepping, and camera orbit controls. |

---

## 3. Chrome DevTools Protocol (CDP) Browser Verification Suite

The WebGL build at `http://localhost:8080` was subjected to a rigorous 5-stage automated browser test suite simulating physical human mouse clicks and programmatic API interactions.

### Browser Test Matrix & Results:

| Test ID | Test Scenario | Expected Outcome | Observed Result | Status |
| :---: | :--- | :--- | :--- | :---: |
| **T-01** | **Same-Start Validation Guard**<br>Origin: `Main Entrance`<br>Selected: `Main Entrance` | Block transition to `Navigating`; display validation warning: *"Selected destination is your current location."* Keep directory open. | System blocked transition. Status updated: *"Selected destination is your current location."* Directory remained active. | **PASS** |
| **T-02** | **Ground Floor Navigation**<br>Origin: `Main Entrance`<br>Selected: `CSE Dept Office` | Calculate 4-node, $21.5\text{m}$ route. Transition to `Navigating`. Test manual **Step Forward** until arrival card displays. | Route rendered (4 nodes, $21.5\text{m}$). User stepped from waypoint 1 to 4. Arrival card displayed. Returned to directory. | **PASS** |
| **T-03** | **Multi-Floor Transition (F0 $\rightarrow$ F1)**<br>Origin: `Main Entrance`<br>Selected: `Room 101` | Calculate 9-node, $62.5\text{m}$ route across stairwell. Test **Step Forward**, **Reset Route** ($0\%$), and **Auto Simulate** to arrival. | Route traversed F0 corridor, staircase, and F1 hallway. Reset returned user to start. Auto-simulate guided user to arrival. | **PASS** |
| **T-04** | **3-Level Multi-Floor Route (F0 $\rightarrow$ F2)**<br>Origin: `Main Entrance`<br>Selected: `Room 202` | Calculate 10-node, $68.5\text{m}$ route. Traverse Ground, First, and Second floors. Auto-simulate to arrival. | Route traversed across both stair flights to Room 202. Arrival card confirmed arrival at Room 202. | **PASS** |
| **T-05** | **Route Cancellation & Dynamic Switching**<br>Origin: `Main Entrance`<br>Selected: `Project Lab` (F2) | Calculate 11-node, $81.0\text{m}$ route. Click **Back / Change** button to cancel navigation and return to directory. | Navigation cancelled cleanly. 3D route cleared. Directory restored with all destinations clickable. | **PASS** |

---

## 4. Verification Evidence & Artifact References

All test stages generated timestamped visual evidence verifying UI state, 3D line rendering, and panel transitions:
1. `verify_01_same_start_guard.png`: Demonstrates defense against selecting the starting node as destination.
2. `verify_02_office_navigating.png`: Demonstrates active navigation mode with Ground Floor Office route.
3. `verify_02_office_arrived.png`: Demonstrates arrival detection and arrival card presentation.
4. `verify_03_room101_navigating.png`: Demonstrates First Floor multi-floor route traversing the stairwell.
5. `verify_03_room101_arrived.png`: Demonstrates completion of Room 101 route.
6. `verify_04_room202_navigating.png`: Demonstrates 3-story route to Second Floor Room 202.
7. `verify_04_room202_arrived.png`: Demonstrates arrival confirmation at Room 202.
8. `verify_05_projectlab_navigating.png`: Demonstrates navigation to Project Lab ($81.0\text{m}$).
9. `verify_05_projectlab_cancelled.png`: Demonstrates clean cancellation and directory restoration upon clicking **Back / Change**.
