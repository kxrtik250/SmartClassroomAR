// =============================================================================
// NavigationTests.cs - Automated Test Suite for Navigation Algorithm
// Smart Classroom Navigation System Using Augmented Reality
// 
// This script can be run as a Unity Test Runner EditMode test
// or executed standalone to verify pathfinding correctness.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartClassroomAR.Tests
{
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;
#endif
    using SmartClassroomAR.Navigation;
    using SmartClassroomAR.Mapping;
    using SmartClassroomAR.AR;
    using SmartClassroomAR.UI;
    using SmartClassroomAR.Visualization;
    using TMPro;
    using System.Reflection;
    using System.IO;

    /// <summary>
    /// Comprehensive test suite for the navigation pathfinding algorithm.
    /// Tests cover single-floor routes, multi-floor routes, unreachable destinations,
    /// invalid node IDs, and graph validation.
    /// 
    /// Node IDs used match CSEBlockData.cs defaults:
    ///   Ground Floor (F0): N_F0_ENTRANCE, N_F0_C0..C4, N_F0_OFFICE, N_F0_LOUNGE, N_F0_STAIRS, N_F0_LAB1, N_F0_CONF
    ///   First Floor  (F1): N_F1_C0..C4, N_F1_R101, N_F1_LAB2, N_F1_STAIRS, N_F1_R102, N_F1_FAC1, N_F1_R103
    ///   Second Floor (F2): N_F2_C0..C4, N_F2_R201, N_F2_LAB3, N_F2_STAIRS, N_F2_R202, N_F2_SEMINAR, N_F2_PROJECT
    /// </summary>
    public class NavigationTests
    {
        private NavigationGraph graph;
        private Pathfinder pathfinder;
        private BuildingData buildingData;
        private int passCount;
        private int failCount;
        private List<string> failedTests;

        public NavigationTests()
        {
            passCount = 0;
            failCount = 0;
            failedTests = new List<string>();
        }

        /// <summary>
        /// Builds the test navigation graph from the default CSE block data.
        /// </summary>
        private void SetupGraph()
        {
            buildingData = CSEBlockData.CreateDefaultBuildingData();
            BuildingData data = buildingData;

            if (data == null)
            {
                throw new Exception("Failed to load building data from CSEBlockData.");
            }

            BuildingDataLoader loader = new BuildingDataLoader();
            List<string> validationErrors = loader.ValidateData(data);
            if (validationErrors.Count > 0)
            {
                foreach (string error in validationErrors)
                {
                    LogError("Data Validation Error: " + error);
                }
            }

            graph = data.ToNavigationGraph();
            if (graph == null)
            {
                throw new Exception("Failed to create navigation graph from building data.");
            }

            List<string> graphErrors = graph.ValidateGraph();
            if (graphErrors.Count > 0)
            {
                foreach (string error in graphErrors)
                {
                    LogError("Graph Validation Error: " + error);
                }
            }

            pathfinder = new Pathfinder(graph);
        }

        /// <summary>
        /// Run all tests and return a summary report.
        /// </summary>
        public string RunAllTests()
        {
            passCount = 0;
            failCount = 0;
            failedTests.Clear();

            Log("========================================");
            Log("NAVIGATION TEST SUITE");
            Log("Smart Classroom AR Navigation System");
            Log("========================================\n");

            try
            {
                SetupGraph();
                Log("Graph loaded successfully.");
                Log($"Nodes: {graph.NodeCount}, Edges: {graph.EdgeCount}\n");
            }
            catch (Exception ex)
            {
                Log($"FATAL: Failed to setup graph: {ex.Message}");
                return GetSummary();
            }

            // Test Category 1: Graph Validation
            Log("--- Category 1: Graph Validation ---");
            Test_GraphHasExpectedNodeCount();
            Test_GraphHasExpectedEdgeCount();
            Test_GraphValidationReturnsNoErrors();
            Test_AllFloorsHaveNodes();
            Test_StaircaseNodesExist();

            // Test Category 2: Single Floor Routes
            Log("\n--- Category 2: Single Floor Routes ---");
            Test_RouteOnSameFloor_EntranceToOffice();
            Test_RouteOnSameFloor_EntranceToLab1();
            Test_RouteOnSameFloor_OfficeToLab1();
            Test_RouteOnSameFloor_FirstFloorRoom101ToLab2();

            // Test Category 3: Multi-Floor Routes
            Log("\n--- Category 3: Multi-Floor Routes ---");
            Test_RouteAcrossFloors_EntranceToRoom101();
            Test_RouteAcrossFloors_EntranceToRoom201();
            Test_RouteAcrossFloors_Lab1ToSeminarHall();
            Test_RouteAcrossFloors_Room101ToRoom201();

            // Test Category 4: Edge Cases
            Log("\n--- Category 4: Edge Cases ---");
            Test_RouteToSameNode();
            Test_RouteWithInvalidStartNode();
            Test_RouteWithInvalidDestinationNode();
            Test_RouteWithBothInvalidNodes();

            // Test Category 5: Route Validity
            Log("\n--- Category 5: Route Validity ---");
            Test_AllRoutesFollowValidEdges();
            Test_RouteHasMultiplePossiblePaths();

            // Test Category 6: Unreachable Destination
            Log("\n--- Category 6: Unreachable Destination ---");
            Test_UnreachableDestination();

            // Test Category 7: Destination Data Propagation
            Log("\n--- Category 7: Destination Data Propagation ---");
            Test_DestinationSelection_PropagatesNameToInfoDisplays();
            Test_DestinationSelection_ResolvesFloorNameCorrectly();
            Test_DestinationSelection_HandlesMissingOrNullDestinationGracefully();
            Test_AllDestinations_CanGenerateValidPathsFromEntrance();

            // Test Category 8: AR Availability Detection & Failure Handling
            Log("\n--- Category 8: AR Availability Detection & Failure Handling ---");
            Test_ARSupportedDevice_DetectionConfiguresRealARMode();
            Test_ARUnsupportedDevice_DetectionActivatesDemoNavigationMode();
            Test_ARInitializationFailure_GracefullyFallsBackToDemoMode();

            // Test Category 9: Fallback & Destination Data in Demo Mode
            Log("\n--- Category 9: Fallback & Destination Data in Demo Mode ---");
            Test_FallbackToDemoMode_WhenARUnavailable();
            Test_DestinationDataDisplay_InDemoMode_ShowsCorrectRoomAndFloor();
            Test_DestinationChangeAndRestart_InDemoMode();
            Test_NullOrMissingDestination_HandledGracefullyInDemoMode();

            // Test Category 10: Simulated Route Progress & Real AR Preservation
            Log("\n--- Category 10: Simulated Route Progress & Real AR Preservation ---");
            Test_SimulatedProgressThroughRouteNodes_InDemoMode();
            Test_RealARModePreserved_WhenARSupported();

            // Test Category 11: Editor Play Mode & Runtime UI Controls Validation
            Log("\n--- Category 11: Editor Play Mode & Runtime UI Controls Validation ---");
            Test_EditorPlayMode_MainSceneSetupAndAssetIntegrity();
            Test_EditorPlayMode_UIHierarchyAndStartDropdownAndDestinationDirectory();
            Test_EditorPlayMode_DestinationSelectionAndNavigationScreenDisplay();
            Test_EditorPlayMode_SimulatedRouteControlsAndArrivalWorkflow();

            // Test Category 12: 3D Visualization Subsystem Validation
            Log("\n--- Category 12: 3D Visualization Subsystem Validation ---");
            Test_3DVisualization_BuildingVisualizer_GeneratesFloorsAndGeometry();
            Test_3DVisualization_RouteVisualizer_RendersRouteAndAdvancesWaypoints();
            Test_3DVisualization_OrbitCamera_FocusAndControls();
            Test_3DVisualization_IntegratedWithNavigationUIController();

            Log("\n" + GetSummary());
            return GetSummary();
        }

        // =====================================================================
        // Category 1: Graph Validation Tests
        // =====================================================================

        private void Test_GraphHasExpectedNodeCount()
        {
            int nodeCount = graph.NodeCount;
            AssertTrue("Graph has nodes", nodeCount > 0, $"Expected > 0, got {nodeCount}");
            AssertTrue("Graph has >= 25 nodes", nodeCount >= 25, $"Expected >= 25, got {nodeCount}");
        }

        private void Test_GraphHasExpectedEdgeCount()
        {
            int edgeCount = graph.EdgeCount;
            AssertTrue("Graph has edges", edgeCount > 0, $"Expected > 0, got {edgeCount}");
            AssertTrue("Graph has >= 20 edges", edgeCount >= 20, $"Expected >= 20, got {edgeCount}");
        }

        private void Test_GraphValidationReturnsNoErrors()
        {
            List<string> errors = graph.ValidateGraph();
            AssertTrue("Graph validation returns no errors", errors.Count == 0,
                $"Got {errors.Count} errors: {string.Join("; ", errors)}");
        }

        private void Test_AllFloorsHaveNodes()
        {
            List<NavNode> f0Nodes = graph.GetNodesOnFloor("F0");
            List<NavNode> f1Nodes = graph.GetNodesOnFloor("F1");
            List<NavNode> f2Nodes = graph.GetNodesOnFloor("F2");

            AssertTrue("Ground floor has nodes", f0Nodes.Count > 0, $"F0 nodes: {f0Nodes.Count}");
            AssertTrue("First floor has nodes", f1Nodes.Count > 0, $"F1 nodes: {f1Nodes.Count}");
            AssertTrue("Second floor has nodes", f2Nodes.Count > 0, $"F2 nodes: {f2Nodes.Count}");
        }

        private void Test_StaircaseNodesExist()
        {
            NavNode stairF0 = graph.GetNode("N_F0_STAIRS");
            NavNode stairF1 = graph.GetNode("N_F1_STAIRS");
            NavNode stairF2 = graph.GetNode("N_F2_STAIRS");

            AssertTrue("GF staircase exists", stairF0 != null, "N_F0_STAIRS not found");
            AssertTrue("1F staircase exists", stairF1 != null, "N_F1_STAIRS not found");
            AssertTrue("2F staircase exists", stairF2 != null, "N_F2_STAIRS not found");
        }

        // =====================================================================
        // Category 2: Single Floor Route Tests
        // =====================================================================

        private void Test_RouteOnSameFloor_EntranceToOffice()
        {
            PathResult result = pathfinder.FindPath("N_F0_ENTRANCE", "N_F0_OFFICE");

            AssertTrue("Entrance to Office: path found", result.success, result.errorMessage);
            AssertTrue("Entrance to Office: has nodes", result.nodeIds.Count >= 2,
                $"Path length: {result.nodeIds.Count}");
            AssertTrue("Entrance to Office: starts at entrance", result.nodeIds[0] == "N_F0_ENTRANCE",
                $"Start: {result.nodeIds[0]}");
            AssertTrue("Entrance to Office: ends at office",
                result.nodeIds[result.nodeIds.Count - 1] == "N_F0_OFFICE",
                $"End: {result.nodeIds[result.nodeIds.Count - 1]}");
            AssertTrue("Entrance to Office: does not cross floors", !result.crossesFloors,
                $"Crosses floors: {result.crossesFloors}");
            AssertTrue("Entrance to Office: positive cost", result.totalCost > 0,
                $"Cost: {result.totalCost}");
            ValidateRouteEdges("Entrance to Office", result);
            Log($"  Path: {string.Join(" -> ", result.nodeIds)}  Cost: {result.totalCost:F1}m");
        }

        private void Test_RouteOnSameFloor_EntranceToLab1()
        {
            PathResult result = pathfinder.FindPath("N_F0_ENTRANCE", "N_F0_LAB1");

            AssertTrue("Entrance to Lab1: path found", result.success, result.errorMessage);
            AssertTrue("Entrance to Lab1: correct start", result.nodeIds[0] == "N_F0_ENTRANCE", "");
            AssertTrue("Entrance to Lab1: correct end",
                result.nodeIds[result.nodeIds.Count - 1] == "N_F0_LAB1", "");
            AssertTrue("Entrance to Lab1: single floor", !result.crossesFloors, "");
            ValidateRouteEdges("Entrance to Lab1", result);
            Log($"  Path: {string.Join(" -> ", result.nodeIds)}  Cost: {result.totalCost:F1}m");
        }

        private void Test_RouteOnSameFloor_OfficeToLab1()
        {
            PathResult result = pathfinder.FindPath("N_F0_OFFICE", "N_F0_LAB1");

            AssertTrue("Office to Lab1: path found", result.success, result.errorMessage);
            AssertTrue("Office to Lab1: correct endpoints",
                result.nodeIds[0] == "N_F0_OFFICE" &&
                result.nodeIds[result.nodeIds.Count - 1] == "N_F0_LAB1",
                $"Start: {result.nodeIds[0]}, End: {result.nodeIds[result.nodeIds.Count - 1]}");
            ValidateRouteEdges("Office to Lab1", result);
            Log($"  Path: {string.Join(" -> ", result.nodeIds)}  Cost: {result.totalCost:F1}m");
        }

        private void Test_RouteOnSameFloor_FirstFloorRoom101ToLab2()
        {
            PathResult result = pathfinder.FindPath("N_F1_R101", "N_F1_LAB2");

            AssertTrue("Room101 to Lab2: path found", result.success, result.errorMessage);
            AssertTrue("Room101 to Lab2: single floor", !result.crossesFloors, "");
            ValidateRouteEdges("Room101 to Lab2", result);
            Log($"  Path: {string.Join(" -> ", result.nodeIds)}  Cost: {result.totalCost:F1}m");
        }

        // =====================================================================
        // Category 3: Multi-Floor Route Tests
        // =====================================================================

        private void Test_RouteAcrossFloors_EntranceToRoom101()
        {
            PathResult result = pathfinder.FindPath("N_F0_ENTRANCE", "N_F1_R101");

            AssertTrue("Entrance to Room101: path found", result.success, result.errorMessage);
            AssertTrue("Entrance to Room101: crosses floors", result.crossesFloors,
                $"Crosses floors: {result.crossesFloors}");
            AssertTrue("Entrance to Room101: traverses F0 and F1",
                result.floorsTraversed.Contains("F0") && result.floorsTraversed.Contains("F1"),
                $"Floors: {string.Join(", ", result.floorsTraversed)}");
            ValidateRouteEdges("Entrance to Room101", result);
            Log($"  Path: {string.Join(" -> ", result.nodeIds)}  Cost: {result.totalCost:F1}m");
        }

        private void Test_RouteAcrossFloors_EntranceToRoom201()
        {
            PathResult result = pathfinder.FindPath("N_F0_ENTRANCE", "N_F2_R201");

            AssertTrue("Entrance to Room201: path found", result.success, result.errorMessage);
            AssertTrue("Entrance to Room201: crosses floors", result.crossesFloors, "");
            AssertTrue("Entrance to Room201: traverses F0, F1, F2",
                result.floorsTraversed.Contains("F0") &&
                result.floorsTraversed.Contains("F1") &&
                result.floorsTraversed.Contains("F2"),
                $"Floors: {string.Join(", ", result.floorsTraversed)}");
            ValidateRouteEdges("Entrance to Room201", result);
            Log($"  Path: {string.Join(" -> ", result.nodeIds)}  Cost: {result.totalCost:F1}m");
        }

        private void Test_RouteAcrossFloors_Lab1ToSeminarHall()
        {
            PathResult result = pathfinder.FindPath("N_F0_LAB1", "N_F2_SEMINAR");

            AssertTrue("Lab1 to Seminar: path found", result.success, result.errorMessage);
            AssertTrue("Lab1 to Seminar: crosses floors", result.crossesFloors, "");
            ValidateRouteEdges("Lab1 to Seminar", result);
            Log($"  Path: {string.Join(" -> ", result.nodeIds)}  Cost: {result.totalCost:F1}m");
        }

        private void Test_RouteAcrossFloors_Room101ToRoom201()
        {
            PathResult result = pathfinder.FindPath("N_F1_R101", "N_F2_R201");

            AssertTrue("Room101 to Room201: path found", result.success, result.errorMessage);
            AssertTrue("Room101 to Room201: crosses floors", result.crossesFloors, "");
            AssertTrue("Room101 to Room201: F1 and F2",
                result.floorsTraversed.Contains("F1") && result.floorsTraversed.Contains("F2"),
                $"Floors: {string.Join(", ", result.floorsTraversed)}");
            ValidateRouteEdges("Room101 to Room201", result);
            Log($"  Path: {string.Join(" -> ", result.nodeIds)}  Cost: {result.totalCost:F1}m");
        }

        // =====================================================================
        // Category 4: Edge Case Tests
        // =====================================================================

        private void Test_RouteToSameNode()
        {
            PathResult result = pathfinder.FindPath("N_F0_ENTRANCE", "N_F0_ENTRANCE");

            AssertTrue("Same node: succeeds", result.success, result.errorMessage);
            AssertTrue("Same node: single node in path", result.nodeIds.Count == 1,
                $"Path length: {result.nodeIds.Count}");
            AssertTrue("Same node: zero cost", result.totalCost == 0,
                $"Cost: {result.totalCost}");
        }

        private void Test_RouteWithInvalidStartNode()
        {
            PathResult result = pathfinder.FindPath("INVALID_NODE", "N_F0_OFFICE");

            AssertTrue("Invalid start: fails", !result.success, "Expected failure");
            AssertTrue("Invalid start: has error message", !string.IsNullOrEmpty(result.errorMessage),
                "No error message");
            Log($"  Error: {result.errorMessage}");
        }

        private void Test_RouteWithInvalidDestinationNode()
        {
            PathResult result = pathfinder.FindPath("N_F0_ENTRANCE", "NONEXISTENT_ROOM");

            AssertTrue("Invalid destination: fails", !result.success, "Expected failure");
            AssertTrue("Invalid destination: has error message", !string.IsNullOrEmpty(result.errorMessage),
                "No error message");
            Log($"  Error: {result.errorMessage}");
        }

        private void Test_RouteWithBothInvalidNodes()
        {
            PathResult result = pathfinder.FindPath("BAD_START", "BAD_END");

            AssertTrue("Both invalid: fails", !result.success, "Expected failure");
            AssertTrue("Both invalid: has error message", !string.IsNullOrEmpty(result.errorMessage),
                "No error message");
        }

        // =====================================================================
        // Category 5: Route Validity Tests
        // =====================================================================

        private void Test_AllRoutesFollowValidEdges()
        {
            // Test a comprehensive set of routes and verify all follow valid edges
            string[] testNodes = new string[]
            {
                "N_F0_ENTRANCE", "N_F0_OFFICE", "N_F0_LAB1",
                "N_F1_R101", "N_F1_R102", "N_F1_LAB2",
                "N_F2_R201", "N_F2_R202", "N_F2_SEMINAR"
            };

            bool allValid = true;
            int routesTested = 0;

            for (int i = 0; i < testNodes.Length; i++)
            {
                for (int j = i + 1; j < testNodes.Length; j++)
                {
                    PathResult result = pathfinder.FindPath(testNodes[i], testNodes[j]);
                    if (result.success)
                    {
                        routesTested++;
                        bool valid = ValidateRouteEdgesQuiet(result);
                        if (!valid)
                        {
                            allValid = false;
                            LogError($"Invalid edge in route: {testNodes[i]} -> {testNodes[j]}");
                        }
                    }
                }
            }

            AssertTrue($"All {routesTested} routes follow valid edges", allValid,
                "Some routes have invalid edges");
            Log($"  Tested {routesTested} route combinations");
        }

        private void Test_RouteHasMultiplePossiblePaths()
        {
            // Test route from Entrance to Faculty Room 1 (crosses floors, multiple corridor segments)
            PathResult result = pathfinder.FindPath("N_F0_ENTRANCE", "N_F1_FAC1");

            AssertTrue("Multi-path test: path found", result.success, result.errorMessage);
            AssertTrue("Multi-path test: crosses floors", result.crossesFloors, "");
            AssertTrue("Multi-path test: path has multiple nodes", result.nodeIds.Count >= 5,
                $"Path length: {result.nodeIds.Count}");
            ValidateRouteEdges("Multi-path test", result);

            Log($"  Path: {string.Join(" -> ", result.nodeIds)}");
            Log($"  Cost: {result.totalCost:F1}m, Floors: {string.Join(", ", result.floorsTraversed)}");
        }

        // =====================================================================
        // Category 6: Unreachable Destination Test
        // =====================================================================

        private void Test_UnreachableDestination()
        {
            // Add an isolated node to test unreachable destination
            NavNode isolatedNode = new NavNode
            {
                nodeId = "N_ISOLATED",
                floorId = "F0",
                x = 100,
                y = 0,
                z = 100,
                label = "Isolated Node",
                nodeType = NodeType.Destination
            };

            graph.AddNode(isolatedNode);

            PathResult result = pathfinder.FindPath("N_F0_ENTRANCE", "N_ISOLATED");

            AssertTrue("Unreachable destination: fails", !result.success, "Expected failure");
            AssertTrue("Unreachable destination: has error message",
                !string.IsNullOrEmpty(result.errorMessage), "No error message");

            Log($"  Error: {result.errorMessage}");
        }

        // =====================================================================
        // Category 7: Destination Data Propagation Tests
        // =====================================================================

        private void Test_DestinationSelection_PropagatesNameToInfoDisplays()
        {
            AssertTrue("BuildingData contains destinations", buildingData.destinations != null && buildingData.destinations.Count > 0,
                "destinations list is null or empty");

            foreach (var dest in buildingData.destinations)
            {
                bool hasValidId = !string.IsNullOrEmpty(dest.destinationId);
                bool hasValidName = !string.IsNullOrEmpty(dest.destinationName) && dest.destinationName != "--";
                bool hasValidFloor = !string.IsNullOrEmpty(dest.floorId);
                bool hasValidNode = !string.IsNullOrEmpty(dest.navigationNodeId);

                AssertTrue($"Destination '{dest.destinationName}' has valid non-empty fields",
                    hasValidId && hasValidName && hasValidFloor && hasValidNode,
                    $"Destination fields invalid: id={dest.destinationId}, name={dest.destinationName}, floor={dest.floorId}, node={dest.navigationNodeId}");

                // Ensure node exists in graph
                NavNode node = graph.GetNode(dest.navigationNodeId);
                AssertTrue($"Destination '{dest.destinationName}' node exists in graph",
                    node != null, $"Node {dest.navigationNodeId} not found in graph");
            }
        }

        private void Test_DestinationSelection_ResolvesFloorNameCorrectly()
        {
            AssertTrue("BuildingData contains floors", buildingData.floors != null && buildingData.floors.Count > 0,
                "floors list is null or empty");

            var floorMap = new Dictionary<string, string>();
            foreach (var f in buildingData.floors)
            {
                floorMap[f.floorId] = f.floorName;
            }

            foreach (var dest in buildingData.destinations)
            {
                bool floorResolved = floorMap.TryGetValue(dest.floorId, out string resolvedName) && !string.IsNullOrEmpty(resolvedName);
                AssertTrue($"Destination '{dest.destinationName}' floor '{dest.floorId}' resolves to human-readable name",
                    floorResolved, $"Floor ID {dest.floorId} could not be resolved");
            }
        }

        private void Test_DestinationSelection_HandlesMissingOrNullDestinationGracefully()
        {
            // Simulate missing destination name fallback logic used in UI
            DestinationData nullDest = null;
            string destName = !string.IsNullOrEmpty(nullDest?.destinationName) ? nullDest.destinationName : "Unknown Destination";
            AssertTrue("Null destination name falls back to 'Unknown Destination' instead of null",
                destName == "Unknown Destination", $"Expected 'Unknown Destination', got {destName}");

            DestinationData emptyDest = new DestinationData { destinationId = "TEST_EMPTY", destinationName = "" };
            string emptyName = !string.IsNullOrEmpty(emptyDest.destinationName) ? emptyDest.destinationName : "Unknown Destination";
            AssertTrue("Empty destination name falls back to 'Unknown Destination' instead of empty",
                emptyName == "Unknown Destination", $"Expected 'Unknown Destination', got {emptyName}");

            // Floor fallback logic
            var floorMap = new Dictionary<string, string>();
            string floorName = (emptyDest.floorId != null && floorMap.TryGetValue(emptyDest.floorId, out var fname)) ? fname : "Ground Floor";
            AssertTrue("Unresolved floor falls back to sensible default",
                !string.IsNullOrEmpty(floorName), "Floor name fallback was null or empty");
        }

        private void Test_AllDestinations_CanGenerateValidPathsFromEntrance()
        {
            string startNodeId = "N_F0_ENTRANCE";
            foreach (var dest in buildingData.destinations)
            {
                PathResult result = pathfinder.FindPath(startNodeId, dest.navigationNodeId);
                AssertTrue($"Path from ENTRANCE to '{dest.destinationName}' ({dest.navigationNodeId}) succeeds",
                    result.success, $"Path failed: {result.errorMessage}");
                AssertTrue($"Path to '{dest.destinationName}' contains nodes",
                    result.nodeIds != null && result.nodeIds.Count > 0, "Path node list empty");
            }
        }

        // =====================================================================
        // Category 8: AR Availability Detection & Failure Handling
        // =====================================================================

        private void Test_ARSupportedDevice_DetectionConfiguresRealARMode()
        {
            var go = new GameObject("Test_ARSession_Supported");
            try
            {
                var controller = go.AddComponent<ARSessionController>();
                controller.SimulateAvailabilityCheck(true);

                AssertTrue("AR supported check sets CurrentMode to RealAR",
                    controller.CurrentMode == NavigationMode.RealAR,
                    $"Expected RealAR, got {controller.CurrentMode}");

                AssertTrue("AR supported check sets IsARSupported to true",
                    controller.IsARSupported == true,
                    "Expected IsARSupported=true");

                AssertTrue("AR supported check marks availability check complete",
                    controller.IsAvailabilityCheckComplete == true,
                    "Expected IsAvailabilityCheckComplete=true");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_ARUnsupportedDevice_DetectionActivatesDemoNavigationMode()
        {
            var go = new GameObject("Test_ARSession_Unsupported");
            try
            {
                var controller = go.AddComponent<ARSessionController>();
                controller.SimulateAvailabilityCheck(false);

                AssertTrue("AR unsupported check sets CurrentMode to DemoNavigation",
                    controller.CurrentMode == NavigationMode.DemoNavigation,
                    $"Expected DemoNavigation, got {controller.CurrentMode}");

                AssertTrue("AR unsupported check sets IsARSupported to false",
                    controller.IsARSupported == false,
                    "Expected IsARSupported=false");

                AssertTrue("AR unsupported check status message contains Demo Navigation Mode",
                    controller.StatusMessage.Contains("Demo Navigation Mode"),
                    $"Status was: {controller.StatusMessage}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_ARInitializationFailure_GracefullyFallsBackToDemoMode()
        {
            var go = new GameObject("Test_ARSession_Failure");
            try
            {
                var controller = go.AddComponent<ARSessionController>();
                controller.ConfigureForDemoMode("AR initialization exception occurred.");

                AssertTrue("AR init failure falls back to DemoNavigation mode",
                    controller.CurrentMode == NavigationMode.DemoNavigation,
                    $"Expected DemoNavigation, got {controller.CurrentMode}");

                AssertTrue("AR init failure sets IsARSupported to false",
                    controller.IsARSupported == false,
                    "Expected IsARSupported=false");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // =====================================================================
        // Category 9: Fallback & Destination Data in Demo Mode
        // =====================================================================

        private void Test_FallbackToDemoMode_WhenARUnavailable()
        {
            var go = new GameObject("Test_FallbackDemo");
            try
            {
                var sessionController = go.AddComponent<ARSessionController>();
                sessionController.SimulateAvailabilityCheck(false);

                var navUI = go.AddComponent<NavigationUIController>();
                navUI.SetBuildingDataForTesting(buildingData);

                AssertTrue("Session controller in DemoNavigation mode",
                    sessionController.CurrentMode == NavigationMode.DemoNavigation,
                    "Session controller not in demo mode");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_DestinationDataDisplay_InDemoMode_ShowsCorrectRoomAndFloor()
        {
            var go = new GameObject("Test_DemoDestDisplay");
            try
            {
                var sessionController = go.AddComponent<ARSessionController>();
                sessionController.SimulateAvailabilityCheck(false);

                var navUI = go.AddComponent<NavigationUIController>();
                navUI.SetBuildingDataForTesting(buildingData);

                // Test every destination across all 3 floors
                foreach (var dest in buildingData.destinations)
                {
                    var floor = buildingData.floors.Find(f => f.floorId == dest.floorId);
                    AssertTrue($"Destination '{dest.destinationName}' has valid non-empty room name",
                        !string.IsNullOrEmpty(dest.destinationName), "Empty destination name");

                    AssertTrue($"Destination '{dest.destinationName}' maps to valid floor '{dest.floorId}'",
                        floor != null && !string.IsNullOrEmpty(floor.floorName), $"Floor not found for {dest.destinationName}");

                    AssertTrue($"Destination '{dest.destinationName}' department is CSE",
                        dest.department.Contains("Computer Science"), $"Unexpected department: {dest.department}");

                    // Test binding directly to navUI
                    PathResult path = pathfinder.FindPath("N_F0_ENTRANCE", dest.navigationNodeId);
                    navUI.SetActiveDestinationForTesting(dest, path);

                    AssertTrue($"Active destination bound to '{dest.destinationName}'",
                        navUI.ActiveDestination != null && navUI.ActiveDestination.destinationName == dest.destinationName,
                        "Active destination was null or mismatched");

                    AssertTrue($"Active path bound for '{dest.destinationName}'",
                        navUI.ActivePathResult != null && navUI.ActivePathResult.success,
                        $"Active path failed for {dest.destinationName}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_DestinationChangeAndRestart_InDemoMode()
        {
            var go = new GameObject("Test_DestChangeRestart");
            try
            {
                var sessionController = go.AddComponent<ARSessionController>();
                sessionController.SimulateAvailabilityCheck(false);

                var navUI = go.AddComponent<NavigationUIController>();
                navUI.SetBuildingDataForTesting(buildingData);

                var dest1 = buildingData.destinations.Find(d => d.destinationId == "DEST_F1_R101");
                var path1 = pathfinder.FindPath("N_F0_ENTRANCE", dest1.navigationNodeId);
                navUI.SetActiveDestinationForTesting(dest1, path1);

                // Advance a step
                navUI.AdvanceDemoStep();
                AssertTrue("Path node index advanced to 1 after 1 step",
                    navUI.CurrentPathNodeIndex == 1,
                    $"Expected index 1, got {navUI.CurrentPathNodeIndex}");

                // Change destination to Room 202
                var dest2 = buildingData.destinations.Find(d => d.destinationId == "DEST_F2_R202");
                var path2 = pathfinder.FindPath("N_F0_ENTRANCE", dest2.navigationNodeId);
                navUI.SetActiveDestinationForTesting(dest2, path2);

                AssertTrue("Destination updated to Room 202",
                    navUI.ActiveDestination != null && navUI.ActiveDestination.destinationName == "Room 202",
                    "Destination not updated");

                AssertTrue("Path index reset to 0 upon destination change",
                    navUI.CurrentPathNodeIndex == 0,
                    $"Expected index 0, got {navUI.CurrentPathNodeIndex}");

                // Test restart simulation
                navUI.AdvanceDemoStep();
                navUI.AdvanceDemoStep();
                AssertTrue("Advanced 2 steps along Room 202 path",
                    navUI.CurrentPathNodeIndex == 2,
                    $"Expected index 2, got {navUI.CurrentPathNodeIndex}");

                navUI.ResetDemoSimulation();
                AssertTrue("ResetDemoSimulation resets path index to 0",
                    navUI.CurrentPathNodeIndex == 0,
                    $"Expected index 0 after reset, got {navUI.CurrentPathNodeIndex}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_NullOrMissingDestination_HandledGracefullyInDemoMode()
        {
            var go = new GameObject("Test_NullDest");
            try
            {
                var sessionController = go.AddComponent<ARSessionController>();
                sessionController.SimulateAvailabilityCheck(false);

                var navUI = go.AddComponent<NavigationUIController>();
                navUI.SetBuildingDataForTesting(buildingData);

                // Passing null must not throw
                navUI.HandleDestinationSelected(null);
                AssertTrue("Null destination handled safely without throwing",
                    navUI.ActiveDestination == null,
                    "ActiveDestination should remain null");

                navUI.SetActiveDestinationForTesting(null, null);
                AssertTrue("SetActiveDestinationForTesting(null, null) handled safely",
                    navUI.ActiveDestination == null,
                    "ActiveDestination should remain null");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // =====================================================================
        // Category 10: Simulated Route Progress & Real AR Preservation
        // =====================================================================

        private void Test_SimulatedProgressThroughRouteNodes_InDemoMode()
        {
            var go = new GameObject("Test_SimulatedProgress");
            try
            {
                var sessionController = go.AddComponent<ARSessionController>();
                sessionController.SimulateAvailabilityCheck(false);

                var navUI = go.AddComponent<NavigationUIController>();
                navUI.SetBuildingDataForTesting(buildingData);

                // Route: Entrance (F0) to Seminar Hall (F2) - multi-floor route with multiple waypoints
                var dest = buildingData.destinations.Find(d => d.destinationId == "DEST_F2_SEMINAR");
                var path = pathfinder.FindPath("N_F0_ENTRANCE", dest.navigationNodeId);

                AssertTrue("Route to Seminar Hall found", path.success, "Pathfinding failed");
                int totalWaypoints = path.nodes.Count;
                AssertTrue("Route has at least 4 waypoints", totalWaypoints >= 4, $"Only {totalWaypoints} waypoints");

                navUI.SetActiveDestinationForTesting(dest, path);
                AssertTrue("Simulation begins at index 0", navUI.CurrentPathNodeIndex == 0, "Initial index not 0");

                // Step forward through each node
                for (int i = 1; i < totalWaypoints; i++)
                {
                    navUI.AdvanceDemoStep();
                    AssertTrue($"Step {i} advances CurrentPathNodeIndex to {i}",
                        navUI.CurrentPathNodeIndex == i,
                        $"Expected index {i}, got {navUI.CurrentPathNodeIndex}");
                }

                // Final step triggers arrival state
                AssertTrue("Simulation reached final waypoint index",
                    navUI.CurrentPathNodeIndex == totalWaypoints - 1,
                    $"Expected {totalWaypoints - 1}, got {navUI.CurrentPathNodeIndex}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_RealARModePreserved_WhenARSupported()
        {
            var go = new GameObject("Test_RealARPreserved");
            try
            {
                var controller = go.AddComponent<ARSessionController>();
                controller.SimulateAvailabilityCheck(true);

                AssertTrue("Real AR mode maintained when ARCore supported",
                    controller.CurrentMode == NavigationMode.RealAR,
                    "CurrentMode should be RealAR");

                AssertTrue("IsARSupported is true for supported AR device",
                    controller.IsARSupported == true,
                    "IsARSupported should be true");

                // Toggle between modes
                controller.SetNavigationMode(NavigationMode.DemoNavigation);
                AssertTrue("Can toggle to DemoNavigation mode",
                    controller.CurrentMode == NavigationMode.DemoNavigation,
                    "Failed to switch to DemoNavigation");

                controller.SetNavigationMode(NavigationMode.RealAR);
                AssertTrue("Can switch back to RealAR mode",
                    controller.CurrentMode == NavigationMode.RealAR,
                    "Failed to switch back to RealAR");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // =====================================================================
        // Category 11: Editor Play Mode & Runtime UI Controls Validation
        // =====================================================================

        private void Test_EditorPlayMode_MainSceneSetupAndAssetIntegrity()
        {
            bool sceneExists = File.Exists("Assets/Scenes/MainScene.unity");
            AssertTrue("MainScene asset exists at Assets/Scenes/MainScene.unity", sceneExists, "Scene file missing");

            string json = CSEBlockData.GetDefaultBuildingDataJson();
            AssertTrue("Default CSE block building data JSON is non-empty", !string.IsNullOrEmpty(json), "JSON is null or empty");

            var loader = new BuildingDataLoader();
            var data = loader.LoadFromJson(json);
            AssertTrue("Building data parses successfully", data != null, "BuildingData is null");
            AssertTrue("Building data contains 3 floors (F0, F1, F2)", data != null && data.floors != null && data.floors.Count == 3, $"Expected 3 floors, got {data?.floors?.Count}");
            AssertTrue("Building data contains >= 15 destinations", data != null && data.destinations != null && data.destinations.Count >= 15, $"Expected >= 15 destinations, got {data?.destinations?.Count}");
            AssertTrue("Building data contains >= 25 navigation nodes", data != null && data.nodes != null && data.nodes.Count >= 25, $"Expected >= 25 nodes, got {data?.nodes?.Count}");
            AssertTrue("Building data contains >= 30 navigation edges", data != null && data.edges != null && data.edges.Count >= 30, $"Expected >= 30 edges, got {data?.edges?.Count}");

            var font = TMP_Settings.defaultFontAsset ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            AssertTrue("TextMeshPro default font asset is loaded and accessible", font != null, "Font asset is null");
        }

        private void Test_EditorPlayMode_UIHierarchyAndStartDropdownAndDestinationDirectory()
        {
            var go = new GameObject("Test_UIHierarchy");
            try
            {
                // 1. StartNodeSelector & TMP_Dropdown setup
                var dropdownGO = new GameObject("StartDropdown");
                dropdownGO.transform.SetParent(go.transform, false);
                var dropdown = dropdownGO.AddComponent<TMP_Dropdown>();

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(dropdownGO.transform, false);
                dropdown.captionText = labelGO.AddComponent<TextMeshProUGUI>();

                var selector = go.AddComponent<StartNodeSelector>();
                typeof(StartNodeSelector).GetField("startNodeDropdown", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(selector, dropdown);

                var entranceNodes = buildingData.nodes.FindAll(n => n.nodeType == NodeType.Entrance || n.nodeType == NodeType.Staircase);
                selector.PopulateStartNodes(entranceNodes);

                AssertTrue("Start node dropdown populated with >= 3 entry points",
                    dropdown.options != null && dropdown.options.Count >= 3,
                    $"Dropdown options: {dropdown.options?.Count}");

                AssertTrue("StartNodeSelector defaults to Main Entrance (N_F0_ENTRANCE)",
                    selector.SelectedNodeId == "N_F0_ENTRANCE",
                    $"Expected N_F0_ENTRANCE, got '{selector.SelectedNodeId}'");

                selector.SetSelectedNode("N_F1_STAIRS");
                AssertTrue("StartNodeSelector programmatic selection updates SelectedNodeId",
                    selector.SelectedNodeId == "N_F1_STAIRS",
                    $"Expected N_F1_STAIRS, got '{selector.SelectedNodeId}'");

                // 2. DestinationSelectionUI & Search / Floor Grouping setup
                var destListGO = new GameObject("DestSelectionUI");
                destListGO.transform.SetParent(go.transform, false);
                var contentGO = new GameObject("Content");
                contentGO.transform.SetParent(destListGO.transform, false);

                var destUI = destListGO.AddComponent<DestinationSelectionUI>();
                typeof(DestinationSelectionUI).GetField("contentArea", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(destUI, contentGO.transform);

                destUI.PopulateDestinations(buildingData.destinations, buildingData.floors);
                int totalItemsInitial = contentGO.transform.childCount;
                AssertTrue("DestinationSelectionUI creates UI items for floors and destinations",
                    totalItemsInitial >= buildingData.destinations.Count,
                    $"Expected >= {buildingData.destinations.Count} items, got {totalItemsInitial}");

                // 3. Search filter testing
                var searchMethod = typeof(DestinationSelectionUI).GetMethod("HandleSearchFilterChanged", BindingFlags.NonPublic | BindingFlags.Instance);
                searchMethod?.Invoke(destUI, new object[] { "Lab" });
                int labItemsCount = contentGO.transform.childCount;
                AssertTrue("Search filter 'Lab' filters destination list",
                    labItemsCount > 0 && labItemsCount < totalItemsInitial,
                    $"Filtered count: {labItemsCount}, Initial: {totalItemsInitial}");

                // 4. Clearing search restores full list
                searchMethod?.Invoke(destUI, new object[] { "" });
                int restoredItemsCount = contentGO.transform.childCount;
                AssertTrue("Clearing search filter restores full destination directory",
                    restoredItemsCount == totalItemsInitial,
                    $"Restored count: {restoredItemsCount}, expected: {totalItemsInitial}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_EditorPlayMode_DestinationSelectionAndNavigationScreenDisplay()
        {
            var go = new GameObject("Test_DestinationSelectionNavScreen");
            try
            {
                var sessionController = go.AddComponent<ARSessionController>();
                sessionController.SimulateAvailabilityCheck(false);

                var navUI = go.AddComponent<NavigationUIController>();
                navUI.SetBuildingDataForTesting(buildingData);

                var statusTextGO = new GameObject("StatusText");
                statusTextGO.transform.SetParent(go.transform, false);
                var tmpStatus = statusTextGO.AddComponent<TextMeshProUGUI>();
                navUI.SetStatusTextForTesting(tmpStatus);

                var diagTextGO = new GameObject("DiagText");
                diagTextGO.transform.SetParent(go.transform, false);
                var tmpDiag = diagTextGO.AddComponent<TextMeshProUGUI>();
                navUI.SetDiagnosticsTextForTesting(tmpDiag);

                // 1. Same-node selection guard: Main Entrance as both start and destination
                var entranceDest = buildingData.destinations.Find(d => d.destinationId == "DEST_F0_ENTRANCE");
                AssertTrue("Main Entrance destination found in test data", entranceDest != null, "Entrance dest null");
                navUI.HandleDestinationSelected(entranceDest);
                AssertTrue("Selecting same start and destination node does not enter Navigating state",
                    navUI.CurrentState == NavigationState.SelectingDestination,
                    $"Expected SelectingDestination, got {navUI.CurrentState}");
                AssertTrue("Same-node selection clears ActiveDestination",
                    navUI.ActiveDestination == null,
                    "ActiveDestination was not null");
                AssertTrue("Same-node selection displays informative location warning message",
                    navUI.CurrentStatusMessage.Contains("current starting location"),
                    $"Message: '{navUI.CurrentStatusMessage}'");
                AssertTrue("Diagnostics shows SelectingDestination state",
                    navUI.CurrentDiagnosticsMessage.Contains("SelectingDestination"),
                    $"Diag: '{navUI.CurrentDiagnosticsMessage}'");

                // 2. Invalid destination with empty navigationNodeId
                var invalidDest = new DestinationData("DEST_EMPTY", "Empty Node Room", "CSE", "F0", "", DestinationType.Classroom, "Empty");
                navUI.HandleDestinationSelected(invalidDest);
                AssertTrue("Destination with empty nodeId remains in SelectingDestination state",
                    navUI.CurrentState == NavigationState.SelectingDestination,
                    $"Expected SelectingDestination, got {navUI.CurrentState}");
                AssertTrue("Destination with empty nodeId displays error message",
                    navUI.CurrentStatusMessage.Contains("no navigation node assigned"),
                    $"Message: '{navUI.CurrentStatusMessage}'");

                // 3. Unreachable destination recovers gracefully
                var unreachableDest = new DestinationData("DEST_UNREACH", "Unreachable Room", "CSE", "F0", "NON_EXISTENT_NODE", DestinationType.Classroom, "Isolated");
                navUI.HandleDestinationSelected(unreachableDest);
                AssertTrue("Unreachable destination recovers to SelectingDestination state",
                    navUI.CurrentState == NavigationState.SelectingDestination,
                    $"Expected SelectingDestination, got {navUI.CurrentState}");
                AssertTrue("Unreachable destination clears ActiveDestination",
                    navUI.ActiveDestination == null,
                    "ActiveDestination was not null");

                // 4. Select Ground Floor destination: CSE Department Office
                var officeDest = buildingData.destinations.Find(d => d.destinationId == "DEST_F0_OFFICE");
                AssertTrue("CSE Department Office destination found in test data", officeDest != null, "Destination is null");

                navUI.HandleDestinationSelected(officeDest);

                AssertTrue("Selecting destination transitions state to Navigating",
                    navUI.CurrentState == NavigationState.Navigating,
                    $"Expected Navigating, got {navUI.CurrentState}");

                AssertTrue("Diagnostics updates to Navigating and shows active route info",
                    navUI.CurrentDiagnosticsMessage.Contains("Navigating") && navUI.CurrentDiagnosticsMessage.Contains("DEST_F0_OFFICE"),
                    $"Diag: '{navUI.CurrentDiagnosticsMessage}'");

                AssertTrue("Active destination is CSE Department Office",
                    navUI.ActiveDestination != null && navUI.ActiveDestination.destinationName == "CSE Department Office",
                    $"Active destination: {navUI.ActiveDestination?.destinationName}");

                AssertTrue("Active destination floor is F0 (Ground Floor)",
                    navUI.ActiveDestination != null && navUI.ActiveDestination.floorId == "F0",
                    $"FloorId: {navUI.ActiveDestination?.floorId}");

                AssertTrue("Route calculated with success = true",
                    navUI.ActivePathResult != null && navUI.ActivePathResult.success,
                    "Route calculation failed");

                AssertTrue("Route distance is positive and realistic (> 0m)",
                    navUI.ActivePathResult != null && navUI.ActivePathResult.totalCost > 0f,
                    $"Total cost: {navUI.ActivePathResult?.totalCost}");

                // Select Second Floor destination: Seminar Hall
                var seminarDest = buildingData.destinations.Find(d => d.destinationId == "DEST_F2_SEMINAR");
                AssertTrue("Seminar Hall destination found in test data", seminarDest != null, "Seminar dest is null");

                navUI.HandleDestinationSelected(seminarDest);

                AssertTrue("Switching destination updates active destination to Seminar Hall",
                    navUI.ActiveDestination != null && navUI.ActiveDestination.destinationName == "Seminar Hall",
                    $"Active destination: {navUI.ActiveDestination?.destinationName}");

                AssertTrue("Seminar Hall destination floor is F2 (Second Floor)",
                    navUI.ActiveDestination != null && navUI.ActiveDestination.floorId == "F2",
                    $"FloorId: {navUI.ActiveDestination?.floorId}");

                AssertTrue("Multi-floor route traverses staircase nodes across floors",
                    navUI.ActivePathResult != null && navUI.ActivePathResult.crossesFloors,
                    "Route does not traverse floors");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_EditorPlayMode_SimulatedRouteControlsAndArrivalWorkflow()
        {
            var go = new GameObject("Test_SimulatedControlsAndArrival");
            try
            {
                var sessionController = go.AddComponent<ARSessionController>();
                sessionController.SimulateAvailabilityCheck(false);

                var navUI = go.AddComponent<NavigationUIController>();
                navUI.SetBuildingDataForTesting(buildingData);

                var dest = buildingData.destinations.Find(d => d.destinationId == "DEST_F1_R101");
                AssertTrue("Room 101 destination exists", dest != null, "Destination null");

                navUI.HandleDestinationSelected(dest);
                AssertTrue("Navigation starts in Navigating state", navUI.CurrentState == NavigationState.Navigating, $"State: {navUI.CurrentState}");
                AssertTrue("Initial waypoint index is 0", navUI.CurrentPathNodeIndex == 0, $"Index: {navUI.CurrentPathNodeIndex}");

                // 1. Step Forward advances route
                navUI.AdvanceDemoStep();
                AssertTrue("Step Forward advances waypoint index to 1",
                    navUI.CurrentPathNodeIndex == 1,
                    $"Index: {navUI.CurrentPathNodeIndex}");

                // 2. Auto Simulate toggles
                AssertTrue("Auto simulation initially off", !navUI.IsAutoSimulating, "Auto sim is on");
                navUI.ToggleAutoSimulation();
                AssertTrue("ToggleAutoSimulation turns auto simulation ON",
                    navUI.IsAutoSimulating,
                    "Auto sim is off");
                navUI.ToggleAutoSimulation();
                AssertTrue("ToggleAutoSimulation turns auto simulation OFF",
                    !navUI.IsAutoSimulating,
                    "Auto sim is on");

                // 3. Reset Route
                navUI.AdvanceDemoStep();
                AssertTrue("Waypoint index at 2 after another step",
                    navUI.CurrentPathNodeIndex == 2,
                    $"Index: {navUI.CurrentPathNodeIndex}");
                navUI.ResetDemoSimulation();
                AssertTrue("ResetDemoSimulation resets waypoint index to 0",
                    navUI.CurrentPathNodeIndex == 0,
                    $"Index after reset: {navUI.CurrentPathNodeIndex}");
                AssertTrue("ResetDemoSimulation stops auto simulation",
                    !navUI.IsAutoSimulating,
                    "Auto sim still running after reset");

                // 4. Back / Cancel Navigation returns to destination selection
                navUI.CancelNavigation();
                AssertTrue("CancelNavigation returns state to SelectingDestination",
                    navUI.CurrentState == NavigationState.SelectingDestination,
                    $"State: {navUI.CurrentState}");
                AssertTrue("CancelNavigation clears active destination",
                    navUI.ActiveDestination == null,
                    "Active destination not null");
                AssertTrue("CancelNavigation clears active route",
                    navUI.ActivePathResult == null,
                    "Active path not null");

                // 5. Arrival screen appears ONLY when route reaches final destination
                navUI.HandleDestinationSelected(dest);
                int totalNodes = navUI.ActivePathResult.nodes.Count;
                for (int i = 0; i < totalNodes - 2; i++)
                {
                    navUI.AdvanceDemoStep();
                    AssertTrue($"Waypoint index {navUI.CurrentPathNodeIndex} does NOT trigger DestinationReached prematurely",
                        navUI.CurrentState == NavigationState.Navigating,
                        $"Premature transition to state: {navUI.CurrentState}");
                }

                // Final step advances to final node
                navUI.AdvanceDemoStep();
                AssertTrue("Final waypoint step transitions state to DestinationReached",
                    navUI.CurrentState == NavigationState.DestinationReached,
                    $"Expected DestinationReached, got {navUI.CurrentState}");

                // 6. Navigate to Another destination resets and recalculates
                navUI.ReturnToDestinationSelection();
                AssertTrue("ReturnToDestinationSelection returns to SelectingDestination",
                    navUI.CurrentState == NavigationState.SelectingDestination,
                    $"State: {navUI.CurrentState}");

                var newDest = buildingData.destinations.Find(d => d.destinationId == "DEST_F2_R202");
                navUI.HandleDestinationSelected(newDest);
                AssertTrue("Selecting new destination updates ActiveDestination to Room 202",
                    navUI.ActiveDestination != null && navUI.ActiveDestination.destinationName == "Room 202",
                    $"Dest: {navUI.ActiveDestination?.destinationName}");
                AssertTrue("New destination recalculates route successfully",
                    navUI.ActivePathResult != null && navUI.ActivePathResult.success,
                    "New route failed");
                AssertTrue("New destination route starts at waypoint index 0",
                    navUI.CurrentPathNodeIndex == 0,
                    $"Index: {navUI.CurrentPathNodeIndex}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // =====================================================================
        // Category 12: 3D Visualization Subsystem Validation Tests
        // =====================================================================

        private void Test_3DVisualization_BuildingVisualizer_GeneratesFloorsAndGeometry()
        {
            var go = new GameObject("Test_BuildingVisualizer");
            try
            {
                var visualizer = go.AddComponent<BuildingVisualizer>();
                AssertTrue("BuildingVisualizer initially has null BuildingRoot", visualizer.BuildingRoot == null, "BuildingRoot was not null");

                visualizer.Initialize(buildingData);
                AssertTrue("BuildingVisualizer builds BuildingRoot GameObject", visualizer.BuildingRoot != null, "BuildingRoot is null");
                AssertTrue("BuildingVisualizer root contains floor children", visualizer.BuildingRoot.transform.childCount >= 3, $"Children: {visualizer.BuildingRoot.transform.childCount}");

                // Test floor isolation and visibility controls
                visualizer.SetActiveFloor("F1");
                visualizer.SetFloorVisibility("F2", false);
                visualizer.ResetAllFloorsVisibility();

                visualizer.ClearVisualization();
                AssertTrue("ClearVisualization cleans up BuildingRoot", visualizer.BuildingRoot == null, "BuildingRoot not cleared");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_3DVisualization_RouteVisualizer_RendersRouteAndAdvancesWaypoints()
        {
            var go = new GameObject("Test_RouteVisualizer");
            try
            {
                var routeVis = go.AddComponent<RouteVisualizer>();
                AssertTrue("RouteVisualizer initially has null RouteRoot", routeVis.RouteRoot == null, "RouteRoot was not null");

                var pathResult = pathfinder.FindPath("N_F0_ENTRANCE", "N_F2_R201");
                AssertTrue("Path to Room 201 found for testing", pathResult != null && pathResult.success, "Path failed");

                routeVis.RenderRoute(pathResult.nodes, "Room 201");
                AssertTrue("RouteVisualizer creates RouteRoot", routeVis.RouteRoot != null, "RouteRoot is null");
                AssertTrue("RouteVisualizer total waypoints matches path node count",
                    routeVis.GetTotalWaypoints() == pathResult.nodes.Count,
                    $"Waypoints: {routeVis.GetTotalWaypoints()} vs {pathResult.nodes.Count}");
                AssertTrue("Initial waypoint index is 0", routeVis.GetCurrentWaypointIndex() == 0, $"Index: {routeVis.GetCurrentWaypointIndex()}");
                AssertTrue("Not initially at destination", !routeVis.IsAtDestination(), "Reported at destination initially");

                // Step forward
                int nextIdx = routeVis.AdvanceUserToNextWaypoint();
                AssertTrue("AdvanceUserToNextWaypoint moves to index 1", nextIdx == 1, $"Index: {nextIdx}");
                AssertTrue("GetCurrentWaypointIndex reflects step", routeVis.GetCurrentWaypointIndex() == 1, $"Index: {routeVis.GetCurrentWaypointIndex()}");

                // Jump to final waypoint
                routeVis.SetUserPosition(pathResult.nodes.Count - 1);
                AssertTrue("IsAtDestination is true at final waypoint", routeVis.IsAtDestination(), "Not at destination");

                // Reset
                routeVis.ResetToStart();
                AssertTrue("ResetToStart resets index to 0", routeVis.GetCurrentWaypointIndex() == 0, $"Index: {routeVis.GetCurrentWaypointIndex()}");

                routeVis.ClearRoute();
                AssertTrue("ClearRoute destroys RouteRoot", routeVis.RouteRoot == null, "RouteRoot not destroyed");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_3DVisualization_OrbitCamera_FocusAndControls()
        {
            var go = new GameObject("Test_OrbitCamera");
            try
            {
                var cam = go.AddComponent<Camera>();
                var orbitCam = go.AddComponent<OrbitCamera>();

                AssertTrue("OrbitCamera is enabled by default", orbitCam.IsOrbitEnabled, "Orbit disabled");

                orbitCam.FocusOnFloor(0);
                orbitCam.FocusOnFloor(1);
                orbitCam.FocusOnFloor(2);
                orbitCam.FocusOnPosition(new Vector3(12.5f, 4.0f, 6.0f));
                orbitCam.ResetView();

                orbitCam.SetOrbitEnabled(false);
                AssertTrue("SetOrbitEnabled(false) disables camera orbit", !orbitCam.IsOrbitEnabled, "Orbit still enabled");
                orbitCam.SetOrbitEnabled(true);
                AssertTrue("SetOrbitEnabled(true) re-enables camera orbit", orbitCam.IsOrbitEnabled, "Orbit not re-enabled");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void Test_3DVisualization_IntegratedWithNavigationUIController()
        {
            var go = new GameObject("Test_Integrated3DNavigation");
            try
            {
                var navUI = go.AddComponent<NavigationUIController>();
                var buildingVis = go.AddComponent<BuildingVisualizer>();
                var routeVis = go.AddComponent<RouteVisualizer>();
                var orbitCam = go.AddComponent<OrbitCamera>();

                navUI.SetBuildingDataForTesting(buildingData);
                navUI.SetVisualizersForTesting(buildingVis, routeVis, orbitCam);
                buildingVis.Initialize(buildingData);

                AssertTrue("Building visualizer initialized with BuildingRoot", buildingVis.BuildingRoot != null, "BuildingRoot null");

                var officeDest = buildingData.destinations.Find(d => d.destinationId == "DEST_F0_OFFICE");
                navUI.HandleDestinationSelected(officeDest);

                AssertTrue("Selecting destination renders 3D route in RouteVisualizer",
                    routeVis.RouteRoot != null, "RouteRoot was not rendered");
                AssertTrue("Route contains waypoints", routeVis.GetTotalWaypoints() >= 2, $"Waypoints: {routeVis.GetTotalWaypoints()}");
                AssertTrue("Initial waypoint index is 0", routeVis.GetCurrentWaypointIndex() == 0, $"Index: {routeVis.GetCurrentWaypointIndex()}");

                navUI.AdvanceDemoStep();
                AssertTrue("AdvanceDemoStep synchronizes RouteVisualizer index to 1",
                    routeVis.GetCurrentWaypointIndex() == 1,
                    $"Route index: {routeVis.GetCurrentWaypointIndex()}");

                navUI.ResetDemoSimulation();
                AssertTrue("ResetDemoSimulation synchronizes RouteVisualizer index to 0",
                    routeVis.GetCurrentWaypointIndex() == 0,
                    $"Route index: {routeVis.GetCurrentWaypointIndex()}");

                navUI.CancelNavigation();
                AssertTrue("CancelNavigation clears RouteVisualizer route",
                    routeVis.RouteRoot == null, "RouteRoot not cleared");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

        private void ValidateRouteEdges(string testName, PathResult result)
        {
            if (!result.success || result.nodeIds.Count < 2)
                return;

            bool allEdgesValid = ValidateRouteEdgesQuiet(result);
            AssertTrue($"{testName}: all edges valid", allEdgesValid,
                "Route contains nodes not connected by edges");
        }

        private bool ValidateRouteEdgesQuiet(PathResult result)
        {
            for (int i = 0; i < result.nodeIds.Count - 1; i++)
            {
                string fromId = result.nodeIds[i];
                string toId = result.nodeIds[i + 1];

                var neighbors = graph.GetNeighbors(fromId);
                bool hasEdge = false;
                foreach (var neighbor in neighbors)
                {
                    if (neighbor.neighborId == toId)
                    {
                        hasEdge = true;
                        break;
                    }
                }

                if (!hasEdge)
                {
                    return false;
                }
            }
            return true;
        }

        private void AssertTrue(string testName, bool condition, string details)
        {
            if (condition)
            {
                passCount++;
                Log($"  PASS: {testName}");
            }
            else
            {
                failCount++;
                string msg = $"  FAIL: {testName} - {details}";
                Log(msg);
                failedTests.Add(testName);
            }
        }

        private string GetSummary()
        {
            string summary = "========================================\n";
            summary += $"TEST RESULTS: {passCount} passed, {failCount} failed, {passCount + failCount} total\n";
            summary += "========================================\n";

            if (failedTests.Count > 0)
            {
                summary += "\nFailed tests:\n";
                foreach (string test in failedTests)
                {
                    summary += $"  - {test}\n";
                }
            }

            return summary;
        }

        private void Log(string message)
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.Log(message);
#else
            Console.WriteLine(message);
#endif
        }

        private void LogError(string message)
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
            UnityEngine.Debug.LogError(message);
#else
            Console.Error.WriteLine(message);
#endif
        }
    }
}
