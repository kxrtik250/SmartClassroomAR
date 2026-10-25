# Testing Protocol

## Smart Classroom Navigation System Using Augmented Reality
### Evaluation Testing Protocol

This document defines the testing protocol for evaluating the Smart Classroom Navigation System, covering all six evaluation metrics from the research paper.

---

## 1. Evaluation Metrics

| # | Metric | What is Measured | How it is Measured |
|---|--------|-----------------|-------------------|
| 1 | Navigation Accuracy | Whether the calculated route leads to the correct destination | Compare expected destination with actual arrival point |
| 2 | Destination Identification | Whether the app correctly identifies and displays destination information | Verify displayed name, floor, department match the selected destination |
| 3 | Route Following | Whether a user can follow the AR navigation from start to destination | Observe participant following AR arrows; record if they reach the destination |
| 4 | AR Tracking Stability | How stable the AR display remains during navigation | Record tracking state changes; note visual glitches or lost tracking |
| 5 | Response Time | Time from destination selection to first AR arrow display | Measure with stopwatch or app timer |
| 6 | User Satisfaction | Subjective user experience rating | Post-navigation questionnaire with 1-5 ratings |

---

## 2. Test Environment Requirements

### Software Tests (No Physical Building Required)
- [ ] Unity Editor installed and project opens without errors
- [ ] All scripts compile successfully
- [ ] Navigation graph loads from JSON without validation errors
- [ ] Pathfinding tests pass (run `NavigationTests.cs`)
- [ ] UI can be tested in Unity Editor Play Mode

### Device Tests (Requires Android Device)
- [ ] ARCore-compatible Android device (API 28+)
- [ ] Camera permission granted
- [ ] App installed and launches correctly
- [ ] AR session initializes (tracking state reaches "Tracking")

### Physical Building Tests (Requires CSE Block Access)
- [ ] AR coordinate calibration completed (see CALIBRATION_GUIDE.md)
- [ ] Building data replaced with actual measurements (see DATA_REPLACEMENT_GUIDE.md)
- [ ] Test destinations are physically accessible

---

## 3. Software Test Procedures

### Test 3.1: Navigation Graph Validation
**Objective**: Verify the building data model is correct and complete.

**Procedure**:
1. Run the app or test runner
2. Check console/log for validation errors
3. Verify: All nodes referenced by edges exist
4. Verify: All destinations have valid navigation node references
5. Verify: All floors have at least one node
6. Verify: The graph is connected (no isolated nodes)

**Record**: Number of validation errors found and details.

### Test 3.2: Pathfinding Accuracy
**Objective**: Verify Dijkstra's algorithm produces correct shortest paths.

**Procedure**:
1. Run `NavigationTests.cs` (via Unity Test Runner or standalone)
2. Record pass/fail for each test category:
   - Single-floor routes
   - Multi-floor routes
   - Invalid node handling
   - Unreachable destination handling
   - Edge validity of all routes

**Record**: Test results summary (pass count, fail count, details of failures).

### Test 3.3: Destination Selection UI
**Objective**: Verify the destination list displays correctly and selection works.

**Procedure**:
1. Launch app in Play Mode
2. Verify all destinations appear grouped by floor
3. Verify floor headers display correct floor names
4. Select each destination and verify navigation initiates
5. Test the search/filter function
6. Test the start node selector

**Record**: Any missing destinations, UI issues, or selection failures.

### Test 3.4: Route Information Display
**Objective**: Verify navigation information is displayed correctly.

**Procedure**:
1. Select a destination and initiate navigation
2. Verify displayed information includes:
   - Destination name
   - Department
   - Floor
   - Estimated distance
3. Verify cancel button returns to destination selection
4. Verify destination reached panel shows correct information

**Record**: Any incorrect or missing information.

---

## 4. Device Test Procedures

### Test 4.1: AR Session Initialization
**Objective**: Verify AR session starts and reaches tracking state.

**Procedure**:
1. Install and launch app on Android device
2. Grant camera permission if prompted
3. Point camera at a well-lit surface with visual features
4. Wait for tracking state indicator

**Record**:
- Time to reach "Tracking" state: ___ seconds
- Tracking quality observed: Good / Limited / Poor
- Any error messages displayed

### Test 4.2: AR Arrow Rendering
**Objective**: Verify AR navigation objects are visible and correctly oriented.

**Procedure**:
1. Select a destination and initiate navigation
2. Observe AR arrows through the camera view
3. Check:
   - Arrows are visible on screen
   - Arrows appear at reasonable positions
   - Arrow direction seems logical relative to path
   - Markers appear at waypoints
   - Destination marker is distinguishable
4. Rotate the device and verify arrows maintain position in AR space

**Record**:
- Arrow visibility: Yes / No
- Arrow positioning: Correct / Approximate / Incorrect
- Marker visibility: Yes / No
- Note: Without physical calibration, arrows show demonstration positions only

### Test 4.3: AR Tracking Stability
**Objective**: Measure AR tracking consistency during movement.

**Procedure**:
1. Start navigation to a destination
2. Walk slowly while viewing the AR display
3. Record:
   - Number of tracking state changes (Tracking → Limited → Tracking)
   - Duration of any "Limited" or "Not Tracking" periods
   - Visual glitches (arrows jumping, flickering, disappearing)
4. Test in different lighting conditions if possible

**Record**:
- Tracking interruptions: ___ times
- Average recovery time: ___ seconds
- Overall stability: Excellent / Good / Fair / Poor

---

## 5. Physical Navigation Test Procedures

> **⚠️ PREREQUISITE**: These tests require physical access to the CSE block AND completed AR coordinate calibration. Results are only meaningful if calibration has been performed.

### Test 5.1: Single-Floor Navigation
**Objective**: Test navigation between two locations on the same floor.

**Procedure**:
1. Stand at the designated starting position (e.g., Main Entrance)
2. Select a same-floor destination (e.g., CSE Lab 1)
3. Follow AR arrows through the corridor
4. Record arrival at destination or failure point
5. Repeat for at least 3 different same-floor destinations

**Record per attempt**:
| Field | Value |
|-------|-------|
| Participant ID | P_XXX |
| Start Node | |
| Destination | |
| Navigation Completed | Yes / No |
| Correct Destination Reached | Yes / No |
| Time Taken (seconds) | |
| Tracking Quality | Good / Limited / Poor |
| Wrong Turns Taken | |
| Notes | |

### Test 5.2: Multi-Floor Navigation
**Objective**: Test navigation between different floors.

**Procedure**:
1. Start from ground floor (Main Entrance)
2. Select a destination on a different floor (e.g., Room 201 on 2nd floor)
3. Follow AR arrows to staircase
4. Climb stairs while maintaining AR view
5. Follow AR arrows to destination on target floor
6. Record arrival or failure point

**Record**: Same as Test 5.1, plus:
- Tracking maintained through staircase: Yes / No
- Floor transition smooth: Yes / No

### Test 5.3: Response Time Measurement
**Objective**: Measure time from destination selection to AR display.

**Procedure**:
1. Prepare stopwatch
2. Select a destination (start timer on tap)
3. Stop timer when first AR arrow appears
4. Repeat 5 times with different destinations

**Record**:
| Attempt | Destination | Response Time (sec) |
|---------|------------|-------------------|
| 1 | | |
| 2 | | |
| 3 | | |
| 4 | | |
| 5 | | |
| **Average** | | |

---

## 6. User Satisfaction Survey

### Questionnaire
Administer this questionnaire to each participant after completing their navigation test.

**Instructions to participant**: Rate each aspect from 1 (Poor) to 5 (Excellent).

| # | Question | Rating (1-5) |
|---|----------|-------------|
| 1 | How clear and visible were the AR navigation arrows? | |
| 2 | How easy was it to select your destination from the list? | |
| 3 | How would you rate the overall usability of the app? | |
| 4 | How accurate was the navigation guidance? | |
| 5 | How easy was it to follow the indicated route? | |

**Open-ended questions**:
- What did you like most about the navigation system?
- What was the most difficult part of using the app?
- What improvements would you suggest?
- Any additional comments?

### Using the In-App Evaluation
The app includes an evaluation mode accessible from the main screen:
1. Tap the **Evaluation** button
2. Enter a participant ID (anonymous, e.g., "P001")
3. Tap **Start Session** before the participant begins navigation
4. Tap **Stop Session** when navigation is complete
5. Fill in the feedback sliders and submit
6. Tap **Export** to save all evaluation data as JSON

---

## 7. Data Recording

### Session Data Format
Each test session is recorded with:
```json
{
  "sessionId": "SESSION_001",
  "participantId": "P_001",
  "startTime": "2024-XX-XXTXX:XX:XX",
  "endTime": "2024-XX-XXTXX:XX:XX",
  "startNodeId": "N_F0_ENTRANCE",
  "destinationId": "DEST_ROOM201",
  "navigationCompleted": true,
  "correctDestinationReached": true,
  "navigationTimeSeconds": 0,
  "arTrackingQuality": "",
  "responseTimeSeconds": 0,
  "notes": ""
}
```

### Feedback Data Format
```json
{
  "sessionId": "SESSION_001",
  "arrowClarityRating": 0,
  "destinationSelectionEaseRating": 0,
  "overallUsabilityRating": 0,
  "navigationAccuracyRating": 0,
  "routeFollowingEaseRating": 0,
  "additionalComments": ""
}
```

> **IMPORTANT**: All data fields above are templates. Do NOT fill in values until actual tests are conducted. No fabricated results.

---

## 8. Analysis Guide

After collecting data from actual test sessions:

1. **Navigation Accuracy**: (Correct arrivals / Total attempts) × 100%
2. **Destination Identification**: (Correct IDs displayed / Total navigations) × 100%
3. **Route Following Success**: (Completed navigations / Total attempts) × 100%
4. **AR Tracking Stability**: Average duration of stable tracking / Total navigation time
5. **Response Time**: Average time from selection to first AR arrow display
6. **User Satisfaction**: Average rating for each questionnaire item

Report each metric with:
- The measured value
- The number of observations
- Any notable outliers or issues
- Conditions that may have affected the result

Do NOT report "expected" or "target" values as measured results.
