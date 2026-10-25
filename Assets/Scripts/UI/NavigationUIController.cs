using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARSubsystems;
using SmartClassroomAR.AR;
using SmartClassroomAR.Mapping;
using SmartClassroomAR.Navigation;
using SmartClassroomAR.Visualization;

namespace SmartClassroomAR.UI
{
    /// <summary>
    /// Represents the high-level workflow states of the classroom navigation application.
    /// </summary>
    public enum NavigationState
    {
        Initializing,
        SelectingDestination,
        Navigating,
        DestinationReached
    }

    /// <summary>
    /// Master UI and workflow controller for the Smart Classroom AR navigation system.
    /// Orchestrates destination directory, A* / Dijkstra pathfinding, AR Foundation rendering,
    /// and destination arrival detection.
    /// Compatible with Unity 2022.3 LTS and TextMeshPro.
    /// </summary>
    public class NavigationUIController : MonoBehaviour
    {
        [Header("Subsystem References")]
        [SerializeField] private DestinationSelectionUI destinationSelectionUI;
        [SerializeField] private StartNodeSelector startNodeSelector;
        [SerializeField] private ARSessionController arSessionController;
        [SerializeField] private ARNavigationRenderer arNavigationRenderer;
        [SerializeField] private CoordinateCalibrator coordinateCalibrator;

        [Header("3D Visualization Subsystems")]
        [SerializeField] private BuildingVisualizer buildingVisualizer;
        [SerializeField] private RouteVisualizer routeVisualizer;
        [SerializeField] private OrbitCamera orbitCamera;

        [Header("Navigation Audio")]
        [SerializeField] private NavigationAudioController navigationAudio;

        [Header("Panel GameObjects")]
        [SerializeField] private GameObject destinationSelectionPanel;
        [SerializeField] private GameObject navigationPanel;
        [SerializeField] private GameObject destinationReachedPanel;
        [SerializeField] private GameObject statusPanel;

        [Header("TextMeshPro UI Displays")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text diagnosticsText;
        [SerializeField] private TMP_Text navigationInfoText;
        [SerializeField] private TMP_Text destinationInfoText;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text floorIndicatorText;
        [SerializeField] private TMP_Text arrivalDetailsText;
        [SerializeField] private TMP_Text arStatusText;

        [Header("Interactive Buttons")]
        [SerializeField] private Button cancelNavigationButton;
        [SerializeField] private Button navigateAnotherButton;

        [Header("Demo Navigation Mode UI")]
        [SerializeField] private GameObject demoNavigationPanel;
        [SerializeField] private TMP_Text demoTitleText;
        [SerializeField] private TMP_Text demoDestinationText;
        [SerializeField] private TMP_Text demoFloorText;
        [SerializeField] private TMP_Text demoDetailsText;
        [SerializeField] private TMP_Text demoStepInfoText;
        [SerializeField] private TMP_Text demoRouteSummaryText;
        [SerializeField] private TMP_Text demoDistanceText;
        [SerializeField] private TMP_Text demoProgressText;
        [SerializeField] private Button demoStepButton;
        [SerializeField] private Button demoAutoPlayButton;
        [SerializeField] private TMP_Text demoAutoPlayButtonText;
        [SerializeField] private Button demoResetButton;
        [SerializeField] private Button demoBackButton;

        [Header("Floor View Controls")]
        [SerializeField] private Button fullBuildingButton;
        [SerializeField] private Button floor0Button;
        [SerializeField] private Button floor1Button;
        [SerializeField] private Button floor2Button;

        [Header("Collapsible Navigation Sidebar")]
        [SerializeField] private GameObject sidebarContainer;
        [SerializeField] private Button sidebarToggleButton;
        [SerializeField] private TMP_Text sidebarToggleArrowText;
        [SerializeField] private GameObject sidebarAccentBorder;

        public NavigationState CurrentState { get; private set; } = NavigationState.Initializing;
        public bool IsSidebarCollapsed { get; private set; } = false;

        private BuildingData _buildingData;
        private NavigationGraph _navigationGraph;
        private Pathfinder _pathfinder;
        private DestinationData _activeDestination;
        private PathResult _activePathResult;
        private Camera _arCamera;

        // Simulated navigation state for Demo Mode
        private int _currentPathNodeIndex = 0;
        private bool _isAutoSimulating = false;
        private float _autoSimulateTimer = 0f;
        private const float AutoStepInterval = 1.8f;
        private bool _isWaitingForFloorAnnouncement = false;
        private string _lastSimulatedFloorId = null;

        // Public getters for diagnostics and automated testing
        public DestinationData ActiveDestination => _activeDestination;
        public PathResult ActivePathResult => _activePathResult;
        public int CurrentPathNodeIndex => _currentPathNodeIndex;
        public bool IsAutoSimulating => _isAutoSimulating;
        public BuildingData CurrentBuildingData => _buildingData;
        public NavigationGraph Graph => _navigationGraph;
        public BuildingVisualizer VisualizerBuilding => buildingVisualizer;
        public RouteVisualizer VisualizerRoute => routeVisualizer;
        public OrbitCamera OrbitCam => orbitCamera;
        public string CurrentStatusMessage => statusText != null ? statusText.text : string.Empty;
        public string CurrentDiagnosticsMessage => diagnosticsText != null ? diagnosticsText.text : string.Empty;

        public bool IsDemoNavigationMode
        {
            get
            {
#if UNITY_WEBGL || UNITY_EDITOR
                return true;
#else
                return arSessionController == null || arSessionController.CurrentMode == NavigationMode.DemoNavigation;
#endif
            }
        }

        public void SetStatusTextForTesting(TMP_Text text)
        {
            statusText = text;
        }

        public void SetDiagnosticsTextForTesting(TMP_Text text)
        {
            diagnosticsText = text;
        }

        public void SetVisualizersForTesting(BuildingVisualizer bVis, RouteVisualizer rVis, OrbitCamera oCam = null)
        {
            buildingVisualizer = bVis;
            routeVisualizer = rVis;
            orbitCamera = oCam;
        }

        private bool _showDetailedDiagnostics = false;

        public void ToggleDiagnosticsDetails()
        {
            _showDetailedDiagnostics = !_showDetailedDiagnostics;
            UpdateDiagnosticsDisplay();
        }

        public void UpdateDiagnosticsDisplay()
        {
            if (diagnosticsText == null) return;
            string startId = startNodeSelector != null && !string.IsNullOrEmpty(startNodeSelector.SelectedNodeId)
                ? startNodeSelector.SelectedNodeId
                : (destinationSelectionUI != null && !string.IsNullOrEmpty(destinationSelectionUI.SelectedStartNodeId)
                    ? destinationSelectionUI.SelectedStartNodeId
                    : "N_F0_ENTRANCE");
            string destId = _activeDestination != null
                ? $"{_activeDestination.destinationId} ({_activeDestination.destinationName})"
                : "None";
            int nodeCount = (_activePathResult != null && _activePathResult.nodes != null) ? _activePathResult.nodes.Count : 0;
            string activePanelName = (CurrentState == NavigationState.SelectingDestination)
                ? "DestinationSelectionPanel"
                : (CurrentState == NavigationState.Navigating)
                    ? (demoNavigationPanel != null && demoNavigationPanel.activeSelf ? "DemoNavigationPanel" : "NavigationPanel")
                    : (CurrentState == NavigationState.DestinationReached) ? "DestinationReachedPanel" : "Initializing";

            if (_showDetailedDiagnostics)
            {
                diagnosticsText.text = $"<b>STATE:</b> {CurrentState}  |  <b>START:</b> {startId}  |  <b>DEST:</b> {destId}  |  <b>ROUTE:</b> {nodeCount} nodes  |  <b>ACTIVE PANEL:</b> {activePanelName}";
            }
            else
            {
                // Clean student-facing guidance subtitle while retaining state identifiers for automated testing
                if (CurrentState == NavigationState.SelectingDestination)
                {
                    diagnosticsText.text = $"<b>Mode:</b> SelectingDestination  •  Select destination to calculate shortest route  •  Left-drag to rotate view";
                }
                else if (CurrentState == NavigationState.Navigating)
                {
                    diagnosticsText.text = $"<b>Mode:</b> Navigating [{destId}]  •  Follow 3D path indicators to reach target  •  [D] Details";
                }
                else if (CurrentState == NavigationState.DestinationReached)
                {
                    diagnosticsText.text = $"<b>Mode:</b> DestinationReached  •  You have arrived at your destination  •  Select another to continue";
                }
                else
                {
                    diagnosticsText.text = $"<b>Mode:</b> {CurrentState}  •  St. Paul's Block CSE 3D Digital Twin";
                }
            }
        }

        private void Awake()
        {
            EnsureAllSceneFonts();
            FindMissingReferences();

            if (cancelNavigationButton != null)
            {
                cancelNavigationButton.onClick.AddListener(CancelNavigation);
            }

            if (navigateAnotherButton != null)
            {
                navigateAnotherButton.onClick.AddListener(ReturnToDestinationSelection);
            }

            if (demoStepButton != null)
            {
                demoStepButton.onClick.AddListener(AdvanceDemoStep);
            }

            if (demoAutoPlayButton != null)
            {
                demoAutoPlayButton.onClick.AddListener(ToggleAutoSimulation);
            }

            if (demoResetButton != null)
            {
                demoResetButton.onClick.AddListener(ResetDemoSimulation);
            }

            if (demoBackButton != null)
            {
                demoBackButton.onClick.AddListener(CancelNavigation);
            }

            if (fullBuildingButton != null)
            {
                fullBuildingButton.onClick.AddListener(ShowFullBuildingView);
            }

            if (floor0Button != null)
            {
                floor0Button.onClick.AddListener(FocusFloor0);
            }

            if (floor1Button != null)
            {
                floor1Button.onClick.AddListener(FocusFloor1);
            }

            if (floor2Button != null)
            {
                floor2Button.onClick.AddListener(FocusFloor2);
            }

            if (sidebarToggleButton != null)
            {
                sidebarToggleButton.onClick.AddListener(ToggleSidebar);
            }
            UpdateSidebarVisuals();
        }

        private void EnsureAllSceneFonts()
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
            if (defaultFont != null)
            {
                var texts = FindObjectsOfType<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].font == null)
                    {
                        texts[i].font = defaultFont;
                    }
                }
            }
        }

        private void Start()
        {
            InitializeSystem();
        }

        private void OnEnable()
        {
            if (destinationSelectionUI != null)
            {
                destinationSelectionUI.OnDestinationSelected += HandleDestinationSelected;
            }

            if (startNodeSelector != null)
            {
                startNodeSelector.OnStartNodeSelected += HandleStartNodeSelected;
            }

            if (arSessionController != null)
            {
                arSessionController.OnTrackingStateChanged += HandleARTrackingStateChanged;
                arSessionController.OnStatusMessageChanged += HandleARStatusMessageChanged;
                arSessionController.OnNavigationModeChanged += HandleNavigationModeChanged;
            }
        }

        private void OnDisable()
        {
            if (destinationSelectionUI != null)
            {
                destinationSelectionUI.OnDestinationSelected -= HandleDestinationSelected;
            }

            if (startNodeSelector != null)
            {
                startNodeSelector.OnStartNodeSelected -= HandleStartNodeSelected;
            }

            if (arSessionController != null)
            {
                arSessionController.OnTrackingStateChanged -= HandleARTrackingStateChanged;
                arSessionController.OnStatusMessageChanged -= HandleARStatusMessageChanged;
                arSessionController.OnNavigationModeChanged -= HandleNavigationModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (cancelNavigationButton != null)
            {
                cancelNavigationButton.onClick.RemoveListener(CancelNavigation);
            }

            if (navigateAnotherButton != null)
            {
                navigateAnotherButton.onClick.RemoveListener(ReturnToDestinationSelection);
            }

            if (demoStepButton != null)
            {
                demoStepButton.onClick.RemoveListener(AdvanceDemoStep);
            }

            if (demoAutoPlayButton != null)
            {
                demoAutoPlayButton.onClick.RemoveListener(ToggleAutoSimulation);
            }

            if (demoResetButton != null)
            {
                demoResetButton.onClick.RemoveListener(ResetDemoSimulation);
            }

            if (demoBackButton != null)
            {
                demoBackButton.onClick.RemoveListener(CancelNavigation);
            }

            if (fullBuildingButton != null)
            {
                fullBuildingButton.onClick.RemoveListener(ShowFullBuildingView);
            }

            if (floor0Button != null)
            {
                floor0Button.onClick.RemoveListener(FocusFloor0);
            }

            if (floor1Button != null)
            {
                floor1Button.onClick.RemoveListener(FocusFloor1);
            }

            if (floor2Button != null)
            {
                floor2Button.onClick.RemoveListener(FocusFloor2);
            }

            if (sidebarToggleButton != null)
            {
                sidebarToggleButton.onClick.RemoveListener(ToggleSidebar);
            }
        }

        public void FocusFloor0() => FocusFloorView(0);
        public void FocusFloor1() => FocusFloorView(1);
        public void FocusFloor2() => FocusFloorView(2);

        /// <summary>
        /// Resets the camera to default overall building overview without interrupting active navigation.
        /// </summary>
        public void ShowFullBuildingView()
        {
            if (orbitCamera != null)
            {
                orbitCamera.ResetView();
            }
            if (buildingVisualizer != null)
            {
                buildingVisualizer.SetCutawayMode(false);
            }
            ShowMessage("Exterior full building view — St. Paul's Block.");
            Debug.Log("[NavigationUIController] Camera reset to full building overview.");
        }

        /// <summary>
        /// Focuses camera and highlights the specified floor (0 = Ground, 1 = First, 2 = Second).
        /// </summary>
        public void FocusFloorView(int floorNumber)
        {
            if (orbitCamera != null)
            {
                orbitCamera.FocusOnFloor(floorNumber);
            }
            if (buildingVisualizer != null)
            {
                buildingVisualizer.SetCutawayMode(true, floorNumber);
            }
            string fName = floorNumber == 0 ? "Ground Floor" : floorNumber == 1 ? "First Floor" : "Second Floor";
            ShowMessage($"Interior cutaway view: {fName} (Floor {floorNumber}).");
            Debug.Log($"[NavigationUIController] Camera focused on {fName} (F{floorNumber}).");
        }

        /// <summary>
        /// Toggles the collapsible navigation sidebar between expanded and collapsed states.
        /// </summary>
        public void ToggleSidebar()
        {
            SetSidebarCollapsed(!IsSidebarCollapsed);
        }

        /// <summary>
        /// Sets the collapsed state of the navigation sidebar.
        /// Preserves all navigation states, active routes, and simulation timers.
        /// </summary>
        public void SetSidebarCollapsed(bool collapsed)
        {
            IsSidebarCollapsed = collapsed;
            UpdateSidebarVisuals();

            if (orbitCamera != null)
            {
                orbitCamera.SetSidebarCollapsed(IsSidebarCollapsed);
            }

            Debug.Log($"[NavigationUIController] Sidebar collapsed state changed: {IsSidebarCollapsed}");
        }

        public void ToggleSidebarFromJS() => ToggleSidebar();
        public void SetSidebarCollapsedFromJS(string stateStr)
        {
            bool collapsed = string.Equals(stateStr, "true", StringComparison.OrdinalIgnoreCase) || stateStr == "1";
            SetSidebarCollapsed(collapsed);
        }

        private void UpdateSidebarVisuals()
        {
            if (sidebarContainer != null)
            {
                sidebarContainer.SetActive(!IsSidebarCollapsed);
            }

            if (sidebarToggleButton != null)
            {
                var rt = sidebarToggleButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    if (IsSidebarCollapsed)
                    {
                        rt.anchorMin = new Vector2(0f, 0.50f);
                        rt.anchorMax = new Vector2(0f, 0.50f);
                        rt.anchoredPosition = new Vector2(6f, 0f);
                    }
                    else
                    {
                        rt.anchorMin = new Vector2(0.28f, 0.50f);
                        rt.anchorMax = new Vector2(0.28f, 0.50f);
                        rt.anchoredPosition = new Vector2(4f, 0f);
                    }
                }
            }

            if (sidebarToggleArrowText != null)
            {
                sidebarToggleArrowText.text = IsSidebarCollapsed ? "<b>></b>" : "<b><</b>";
            }

            if (sidebarAccentBorder != null)
            {
                var art = sidebarAccentBorder.GetComponent<RectTransform>();
                if (art != null)
                {
                    if (IsSidebarCollapsed)
                    {
                        art.anchorMin = new Vector2(0.90f, 0f);
                        art.anchorMax = new Vector2(1.00f, 1f);
                    }
                    else
                    {
                        art.anchorMin = new Vector2(0f, 0f);
                        art.anchorMax = new Vector2(0.10f, 1f);
                    }
                }
            }
        }

        /// <summary>
        /// Loads default building dataset, validates consistency, builds navigation graph,
        /// and initializes UI views.
        /// </summary>
        private void InitializeSystem()
        {
            SetState(NavigationState.Initializing);
            ShowMessage("Loading CSE Block map data...");

            try
            {
                var loader = new BuildingDataLoader();
                string json = CSEBlockData.GetDefaultBuildingDataJson();
                _buildingData = loader.LoadFromJson(json);

                if (_buildingData == null)
                {
                    ShowMessage("Error: Failed to load building navigation data.");
                    Debug.LogError("[NavigationUIController] BuildingData deserialization failed.");
                    return;
                }

                // Validate building data integrity
                var errors = loader.ValidateData(_buildingData);
                if (errors.Count > 0)
                {
                    for (int i = 0; i < errors.Count; i++)
                    {
                        Debug.LogWarning($"[NavigationUIController] Data validation warning: {errors[i]}");
                    }
                }

                // Construct navigation graph and Dijkstra pathfinder
                _navigationGraph = _buildingData.ToNavigationGraph();
                _pathfinder = new Pathfinder(_navigationGraph);

                // Initialize 3D Building Visualization
                if (buildingVisualizer != null)
                {
                    buildingVisualizer.Initialize(_buildingData);
                }

                // Configure calibration fallback if uncalibrated
                if (coordinateCalibrator != null)
                {
                    if (!coordinateCalibrator.IsCalibrated)
                    {
                        coordinateCalibrator.DemoCalibration();
                    }
                    if (arNavigationRenderer != null)
                    {
                        arNavigationRenderer.SetCalibration(coordinateCalibrator.Offset, coordinateCalibrator.Scale);
                    }
                }

                // Populate destination directory UI
                if (destinationSelectionUI != null)
                {
                    destinationSelectionUI.PopulateDestinations(_buildingData.destinations, _buildingData.floors);
                }

                // Populate start node selector with entrance/staircase nodes
                if (startNodeSelector != null && _buildingData.nodes != null)
                {
                    var entranceNodes = _buildingData.nodes.FindAll(n =>
                        n.nodeType == NodeType.Entrance || n.nodeType == NodeType.Staircase);
                    startNodeSelector.PopulateStartNodes(entranceNodes.Count > 0 ? entranceNodes : _buildingData.nodes);
                }

                SetState(NavigationState.SelectingDestination);
                ShowMessage("Select a classroom, lab, or office destination.");
                Debug.Log("[NavigationUIController] System initialization complete.");
            }
            catch (Exception ex)
            {
                ShowMessage($"Initialization error: {ex.Message}");
                Debug.LogError($"[NavigationUIController] Critical initialization error: {ex}");
            }
        }

        /// <summary>
        /// Handles user choosing a destination from the UI list.
        /// Immediately binds destination info, computes shortest path, and enters Real AR or Demo Navigation Mode.
        /// </summary>
        public void HandleDestinationSelected(DestinationData destination)
        {
            if (destination == null)
            {
                Debug.LogWarning("[NavigationUIController] Destination selected was null.");
                ShowMessage("Warning: No destination selected.");
                return;
            }

            // Determine start node first (before binding _activeDestination) so validation is clean
            string startNodeId = string.Empty;
            if (startNodeSelector != null && !string.IsNullOrEmpty(startNodeSelector.SelectedNodeId))
            {
                startNodeId = startNodeSelector.SelectedNodeId;
            }
            else if (destinationSelectionUI != null && !string.IsNullOrEmpty(destinationSelectionUI.SelectedStartNodeId))
            {
                startNodeId = destinationSelectionUI.SelectedStartNodeId;
            }
            else if (_buildingData != null && _buildingData.nodes != null && _buildingData.nodes.Count > 0)
            {
                var entrance = _buildingData.nodes.Find(n => n.nodeType == NodeType.Entrance);
                startNodeId = entrance != null ? entrance.nodeId : _buildingData.nodes[0].nodeId;
            }

            if (string.IsNullOrEmpty(startNodeId))
            {
                ShowMessage("Error: Could not determine your starting position.");
                return;
            }

            string targetNodeId = destination.navigationNodeId;

            if (string.IsNullOrEmpty(targetNodeId))
            {
                ShowMessage($"Error: '{destination.destinationName}' has no navigation node assigned.");
                Debug.LogError($"[NavigationUIController] Destination '{destination.destinationId}' has empty navigationNodeId.");
                return;
            }

            // Guard: same start and destination — show a clear validation message
            if (string.Equals(startNodeId, targetNodeId, StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage($"'{destination.destinationName}' is your current starting location.\nPlease select a different destination.");
                Debug.LogWarning($"[NavigationUIController] Same-node selection blocked: start='{startNodeId}' == target='{targetNodeId}'.");
                SetState(NavigationState.SelectingDestination);
                return;
            }

            _activeDestination = destination;

            // Compute route — do NOT overwrite statusText with the AR status during this phase
            if (_pathfinder == null && _navigationGraph != null)
            {
                _pathfinder = new Pathfinder(_navigationGraph);
            }

            if (_pathfinder != null)
            {
                _activePathResult = _pathfinder.FindPath(startNodeId, targetNodeId);
            }

            if (_activePathResult == null || !_activePathResult.success)
            {
                string errorMsg = _activePathResult != null ? _activePathResult.errorMessage : "No valid route found.";
                _activeDestination = null;
                _activePathResult = null;
                ShowMessage($"Route not found: {errorMsg}\nPlease select a different destination.");
                Debug.LogError($"[NavigationUIController] Pathfinding failed for '{destination.destinationName}': {errorMsg}");
                SetState(NavigationState.SelectingDestination);
                return;
            }

            // Reset simulation tracking
            _currentPathNodeIndex = 0;
            _isAutoSimulating = false;
            _autoSimulateTimer = 0f;
            _isWaitingForFloorAnnouncement = false;
            _lastSimulatedFloorId = (_activePathResult.nodes != null && _activePathResult.nodes.Count > 0) ? _activePathResult.nodes[0].floorId : null;
            UpdateAutoPlayButtonText();

            bool isDemoMode = IsDemoNavigationMode;

            if (isDemoMode)
            {
                // Demo Mode: Clear physical AR markers to avoid ghost 3D objects
                if (arNavigationRenderer != null)
                {
                    arNavigationRenderer.ClearPath();
                }
            }
            else
            {
                // Real AR Mode: Sync calibration and render 3D waypoints/arrows
                if (coordinateCalibrator != null && arNavigationRenderer != null)
                {
                    arNavigationRenderer.SetCalibration(coordinateCalibrator.Offset, coordinateCalibrator.Scale);
                }

                if (arNavigationRenderer != null)
                {
                    arNavigationRenderer.RenderPath(_activePathResult.nodes, destination.destinationName);
                }
            }

            // Update 3D Building & Route Visualizers
            if (buildingVisualizer != null)
            {
                buildingVisualizer.SetActiveFloor(destination.floorId);
            }

            if (routeVisualizer != null && _activePathResult != null && _activePathResult.nodes != null)
            {
                routeVisualizer.RenderRoute(_activePathResult.nodes, destination.destinationName);
            }

            if (orbitCamera != null && _activePathResult != null && _activePathResult.nodes != null && _activePathResult.nodes.Count > 0)
            {
                int fNum = 0;
                if (!string.IsNullOrEmpty(destination.floorId) && destination.floorId.Length > 1 && int.TryParse(destination.floorId.Substring(1), out int parsedFloor))
                {
                    fNum = parsedFloor;
                }
                orbitCamera.FocusOnFloor(fNum);
            }

            // Update demo progress display now that route and index are set
            if (isDemoMode)
            {
                UpdateDemoProgressUI();
            }

            // Update info panels (destination name, floor, distance) — do NOT call
            // UpdateNavigationInfoDisplays here as it triggers HandleARStatusMessageChanged
            // which would overwrite the status banner. Update individual fields directly.
            UpdateDestinationInfoFieldsOnly();

            // Enable interior cutaway for active navigation so the 3D route is visible inside
            if (buildingVisualizer != null)
            {
                int destFloorNum = 0;
                if (!string.IsNullOrEmpty(destination.floorId))
                {
                    if (destination.floorId.EndsWith("1")) destFloorNum = 1;
                    else if (destination.floorId.EndsWith("2")) destFloorNum = 2;
                }
                buildingVisualizer.SetCutawayMode(true, destFloorNum);
            }

            // Transition state — this shows demoNavigationPanel (with all nav controls) or navigationPanel
            SetState(NavigationState.Navigating);

            // Notify audio controller that navigation has started
            if (navigationAudio != null) navigationAudio.OnNavigationStarted();

            // Set the status banner LAST so it wins over any AR status callbacks
            if (isDemoMode)
            {
                ShowMessage($"Navigating to {destination.destinationName}\n<size=18><color=#FCD34D>Demo Navigation Mode — Use controls below</color></size>");
            }
            else
            {
                ShowMessage($"Navigating to {destination.destinationName}");
            }

            Debug.Log($"[NavigationUIController] Navigation started to {destination.destinationName} (Mode: {(isDemoMode ? "Demo" : "RealAR")}, {_activePathResult.nodes.Count} nodes, {_activePathResult.totalCost:F1}m).");
        }

        private void HandleStartNodeSelected(string startNodeId)
        {
            if (destinationSelectionUI != null)
            {
                destinationSelectionUI.SetStartNode(startNodeId);
            }
        }

        private void HandleARTrackingStateChanged(TrackingState trackingState)
        {
            if (arSessionController != null)
            {
                HandleARStatusMessageChanged(arSessionController.StatusMessage);
            }
        }

        private void HandleARStatusMessageChanged(string message)
        {
            // Only write to arStatusText if it is a DIFFERENT component from statusText.
            // SceneSetup wires both to the same TMP_Text; writing here would clobber the navigation banner.
            if (arStatusText != null && arStatusText != statusText)
            {
                arStatusText.text = message ?? string.Empty;
            }

            // Do NOT overwrite the status banner while transitioning into Navigating (ShowMessage is called last
            // in HandleDestinationSelected, so this handler fires only from AR session state changes, not from
            // the selection flow). Safe to update banner only from background AR events.
            if (CurrentState == NavigationState.Navigating)
            {
                bool isDemo = IsDemoNavigationMode;
                if (isDemo)
                {
                    // Already set by HandleDestinationSelected — don't overwrite
                }
                else if (arSessionController != null && arSessionController.CurrentTrackingState != TrackingState.Tracking)
                {
                    ShowMessage($"Navigating to {_activeDestination?.destinationName}\n<size=18><color=#FCD34D>{message}</color></size>");
                }
                else
                {
                    ShowMessage($"Navigating to {_activeDestination?.destinationName}");
                }
            }
            else if (CurrentState == NavigationState.SelectingDestination || CurrentState == NavigationState.Initializing)
            {
                ShowMessage(message);
            }
        }

        private void HandleNavigationModeChanged(NavigationMode mode)
        {
            Debug.Log($"[NavigationUIController] Navigation mode changed to: {mode}");
            if (CurrentState == NavigationState.Navigating)
            {
                SetState(NavigationState.Navigating);
                UpdateNavigationInfoDisplays();
                if (mode == NavigationMode.DemoNavigation)
                {
                    UpdateDemoProgressUI();
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (UnityEngine.EventSystems.EventSystem.current == null ||
                    UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null ||
                    UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() == null)
                {
                    ToggleDiagnosticsDetails();
                }
            }

            if (CurrentState == NavigationState.Navigating)
            {
                if (IsDemoNavigationMode)
                {
                    UpdateDemoNavigationLoop();
                }
                else
                {
                    UpdateNavigationLoop();
                }
            }
        }

        /// <summary>
        /// Continuous update routine during active Real AR navigation.
        /// Monitors proximity to destination and advances to DestinationReached state upon arrival.
        /// </summary>
        private void UpdateNavigationLoop()
        {
            if (_activeDestination == null || arNavigationRenderer == null) return;

            Vector3 userPos = GetUserARPosition();
            float distance = arNavigationRenderer.UpdateDestinationProximity(userPos);

            if (distanceText != null)
            {
                if (float.IsPositiveInfinity(distance) || distance > 1000f)
                {
                    distanceText.text = $"Distance: ~{_activePathResult?.totalCost:F1}m (estimated)";
                }
                else
                {
                    distanceText.text = $"Distance: {distance:F1}m";
                }
            }

            // Check if user has arrived within destination radius
            if (arNavigationRenderer.HasReachedDestination(userPos))
            {
                OnReachedDestination();
            }
        }

        /// <summary>
        /// Continuous update routine during Demo Navigation Mode.
        /// Handles auto-simulation stepping along route nodes at realistic walking pace.
        /// </summary>
        private void UpdateDemoNavigationLoop()
        {
            if (_activeDestination == null || _activePathResult == null || _activePathResult.nodes == null) return;

            // Pause simulation advance while waiting for the current floor announcement to finish speaking
            if (_isWaitingForFloorAnnouncement) return;

            if (_isAutoSimulating)
            {
                _autoSimulateTimer += Time.deltaTime;
                if (_autoSimulateTimer >= AutoStepInterval)
                {
                    _autoSimulateTimer = 0f;
                    AdvanceDemoStep();
                }
            }
        }

        /// <summary>
        /// Advances simulated progress to the next navigation waypoint in Demo Mode.
        /// Transitions to DestinationReached upon completing the final route waypoint.
        /// </summary>
        public void AdvanceDemoStep()
        {
            if (_activePathResult == null || _activePathResult.nodes == null || _activePathResult.nodes.Count == 0) return;

            if (_currentPathNodeIndex < _activePathResult.nodes.Count - 1)
            {
                _currentPathNodeIndex++;
                if (routeVisualizer != null)
                {
                    routeVisualizer.SetUserPosition(_currentPathNodeIndex);
                }

                // Synchronize active floor highlighting and camera tracking across multi-floor transitions
                var currentNode = _activePathResult.nodes[_currentPathNodeIndex];
                if (currentNode != null)
                {
                    if (buildingVisualizer != null && !string.IsNullOrEmpty(currentNode.floorId))
                    {
                        buildingVisualizer.SetActiveFloor(currentNode.floorId);
                    }
                    if (orbitCamera != null)
                    {
                        orbitCamera.FocusOnPosition(currentNode.Position);
                    }

                    // Detect if the user has arrived onto a NEW floor
                    bool isFloorChange = !string.IsNullOrEmpty(currentNode.floorId) &&
                                         _lastSimulatedFloorId != null &&
                                         currentNode.floorId != _lastSimulatedFloorId;
                    _lastSimulatedFloorId = currentNode.floorId;

                    string fName = string.Empty;
                    if (_buildingData != null && _buildingData.floors != null)
                    {
                        var fData = _buildingData.floors.Find(f => f.floorId == currentNode.floorId);
                        if (fData != null) fName = fData.floorName;
                    }

                    // If user reached a new floor during auto-simulation:
                    // Pause the simulation sequence and footsteps, play complete voice announcement,
                    // and only resume advancing after the announcement finishes speaking!
                    if (isFloorChange && _isAutoSimulating && navigationAudio != null)
                    {
                        _isWaitingForFloorAnnouncement = true;
                        navigationAudio.OnMovementPaused();
                        navigationAudio.PlayFloorAnnouncement(currentNode.floorId, fName, () =>
                        {
                            _isWaitingForFloorAnnouncement = false;
                            _autoSimulateTimer = 0f;
                            if (_isAutoSimulating && navigationAudio != null)
                            {
                                navigationAudio.ResumeWalking();
                            }
                        });
                    }
                    else if (navigationAudio != null)
                    {
                        navigationAudio.OnStepAdvanced(currentNode.floorId, fName, _isAutoSimulating);
                    }
                }

                UpdateDemoProgressUI();

                if (_currentPathNodeIndex >= _activePathResult.nodes.Count - 1)
                {
                    _isAutoSimulating = false;
                    UpdateAutoPlayButtonText();
                    OnReachedDestination();
                }
            }
            else
            {
                _isAutoSimulating = false;
                UpdateAutoPlayButtonText();
                OnReachedDestination();
            }
        }

        /// <summary>
        /// Toggles automated simulated progression on and off in Demo Mode.
        /// </summary>
        public void ToggleAutoSimulation()
        {
            _isAutoSimulating = !_isAutoSimulating;
            _autoSimulateTimer = 0f;
            UpdateAutoPlayButtonText();
            if (!_isAutoSimulating && navigationAudio != null) navigationAudio.OnMovementPaused();
            ShowMessage(_isAutoSimulating
                ? $"Auto-simulation walking to {_activeDestination?.destinationName}..."
                : "Auto-simulation paused.");
        }

        /// <summary>
        /// Resets simulated route progress back to the origin node.
        /// </summary>
        public void ResetDemoSimulation()
        {
            _currentPathNodeIndex = 0;
            _isAutoSimulating = false;
            _autoSimulateTimer = 0f;
            _isWaitingForFloorAnnouncement = false;
            _lastSimulatedFloorId = null;
            UpdateAutoPlayButtonText();
            if (navigationAudio != null) navigationAudio.OnMovementPaused();
            if (routeVisualizer != null)
            {
                routeVisualizer.ResetToStart();
            }

            if (_activePathResult != null && _activePathResult.nodes != null && _activePathResult.nodes.Count > 0)
            {
                var startNode = _activePathResult.nodes[0];
                if (buildingVisualizer != null && !string.IsNullOrEmpty(startNode.floorId))
                {
                    buildingVisualizer.SetActiveFloor(startNode.floorId);
                }
                if (orbitCamera != null)
                {
                    orbitCamera.FocusOnPosition(startNode.Position);
                }
            }

            UpdateDemoProgressUI();
            ShowMessage($"Demo navigation restarted: {_activeDestination?.destinationName}");
        }

        private void UpdateAutoPlayButtonText()
        {
            if (demoAutoPlayButtonText != null)
            {
                demoAutoPlayButtonText.text = _isAutoSimulating ? "Pause Simulation" : "Auto Simulate";
            }
        }

        /// <summary>
        /// Updates all simulated route progression readouts in the Demo Navigation panel.
        /// </summary>
        public void UpdateDemoProgressUI()
        {
            if (_activePathResult == null || _activePathResult.nodes == null || _activePathResult.nodes.Count == 0)
            {
                if (demoStepInfoText != null) demoStepInfoText.text = "No route loaded.";
                if (demoProgressText != null) demoProgressText.text = "Progress: 0%";
                return;
            }

            int totalNodes = _activePathResult.nodes.Count;
            int stepNumber = Mathf.Clamp(_currentPathNodeIndex + 1, 1, totalNodes);
            var currentNode = _activePathResult.nodes[_currentPathNodeIndex];
            NavNode nextNode = (_currentPathNodeIndex + 1 < totalNodes)
                ? _activePathResult.nodes[_currentPathNodeIndex + 1]
                : null;

            // Compute distance remaining from current waypoint to final destination
            float distanceRemaining = 0f;
            for (int i = _currentPathNodeIndex; i < totalNodes - 1; i++)
            {
                var from = _activePathResult.nodes[i];
                var to = _activePathResult.nodes[i + 1];
                if (from != null && to != null)
                {
                    distanceRemaining += Vector3.Distance(from.Position, to.Position);
                }
            }

            float percent = totalNodes > 1 ? ((float)_currentPathNodeIndex / (totalNodes - 1)) * 100f : 100f;

            float distanceTravelled = Mathf.Max(0f, _activePathResult.totalCost - distanceRemaining);

            if (demoStepInfoText != null)
            {
                string nextStr = nextNode != null
                    ? $"Next: <b>{nextNode.label}</b> ({Vector3.Distance(currentNode.Position, nextNode.Position):F1}m)"
                    : "<color=#34D399><b>* Arrived at destination! *</b></color>";

                demoStepInfoText.text = $"Step {stepNumber} of {totalNodes}: At <b>{currentNode.label}</b>\n{nextStr}";
            }

            if (demoDistanceText != null)
            {
                demoDistanceText.text = $"Travelled: <b>{distanceTravelled:F1}m</b>  |  Remaining: <b>{distanceRemaining:F1}m</b>  |  Total: <b>{_activePathResult.totalCost:F1}m</b>";
            }

            if (demoProgressText != null)
            {
                int totalBars = 14;
                int filledBars = Mathf.RoundToInt((percent / 100f) * totalBars);
                string bar = "[" + new string('=', filledBars) + new string('-', Mathf.Max(0, totalBars - filledBars)) + "]";
                demoProgressText.text = $"Progress: <color=#38BDF8>{bar}</color> <b>{percent:F0}%</b> ({stepNumber}/{totalNodes})";
            }

            if (demoRouteSummaryText != null)
            {
                var labels = new List<string>();
                for (int i = 0; i < totalNodes; i++)
                {
                    if (i < _currentPathNodeIndex)
                    {
                        labels.Add($"<color=#34D399>{_activePathResult.nodes[i].label}</color>");
                    }
                    else if (i == _currentPathNodeIndex)
                    {
                        labels.Add($"<color=#FBBF24><b>>> {_activePathResult.nodes[i].label}</b></color>");
                    }
                    else
                    {
                        labels.Add($"<color=#94A3B8>{_activePathResult.nodes[i].label}</color>");
                    }
                }
                demoRouteSummaryText.text = "<b>Route:</b> " + string.Join(" -> ", labels);
            }
        }

        private void OnReachedDestination()
        {
            _isAutoSimulating = false;
            UpdateAutoPlayButtonText();

            // Audio: stop walking, play chime + voice
            if (navigationAudio != null)
            {
                navigationAudio.OnDestinationReached(_activeDestination?.destinationName ?? string.Empty);
            }

            SetState(NavigationState.DestinationReached);

            string floorStr = _activeDestination?.floorId ?? "--";
            if (_buildingData != null && _buildingData.floors != null && _activeDestination != null)
            {
                var floor = _buildingData.floors.Find(f => f.floorId == _activeDestination.floorId);
                if (floor != null && !string.IsNullOrEmpty(floor.floorName))
                {
                    floorStr = $"{floor.floorName} (Floor {floor.floorNumber})";
                }
            }

            string details = _activeDestination != null
                ? $"<size=22><color=#34D399><b>[ARRIVED]</b></color> <b>{_activeDestination.destinationName}</b></size>\n\n" +
                  $"<color=#94A3B8>Department:</color> {_activeDestination.department}\n" +
                  $"<color=#94A3B8>Floor:</color> {floorStr}\n" +
                  $"<color=#94A3B8>Total Traversed:</color> {_activePathResult?.totalCost:F1} meters\n\n" +
                  $"<color=#CBD5E1><i>{_activeDestination.description}</i></color>"
                : "Destination reached.";

            if (arrivalDetailsText != null)
            {
                arrivalDetailsText.text = details;
            }
            else if (destinationInfoText != null)
            {
                destinationInfoText.text = details;
            }

            ShowMessage($"You have reached {_activeDestination?.destinationName}!");
            Debug.Log($"[NavigationUIController] User reached destination: {_activeDestination?.destinationName}");
        }

        /// <summary>
        /// Cancels active route guidance and returns user to destination selection directory.
        /// </summary>
        public void CancelNavigation()
        {
            _isAutoSimulating = false;
            _isWaitingForFloorAnnouncement = false;
            _lastSimulatedFloorId = null;
            UpdateAutoPlayButtonText();

            // Stop all navigation audio immediately
            if (navigationAudio != null) navigationAudio.OnNavigationCancelled();

            if (arNavigationRenderer != null)
            {
                arNavigationRenderer.ClearPath();
            }

            if (routeVisualizer != null)
            {
                routeVisualizer.ClearRoute();
            }

            if (buildingVisualizer != null)
            {
                buildingVisualizer.SetCutawayMode(false);
            }

            if (orbitCamera != null)
            {
                orbitCamera.ResetView();
            }

            _activeDestination = null;
            _activePathResult = null;
            _currentPathNodeIndex = 0;

            SetState(NavigationState.SelectingDestination);
            ShowMessage("Navigation cancelled. Choose a destination.");
            Debug.Log("[NavigationUIController] Active navigation cancelled.");
        }

        /// <summary>
        /// Concludes arrival view and returns user to destination selection directory.
        /// </summary>
        public void ReturnToDestinationSelection()
        {
            _isAutoSimulating = false;
            UpdateAutoPlayButtonText();

            if (arNavigationRenderer != null)
            {
                arNavigationRenderer.ClearPath();
            }

            if (routeVisualizer != null)
            {
                routeVisualizer.ClearRoute();
            }

            if (buildingVisualizer != null)
            {
                buildingVisualizer.SetCutawayMode(false);
            }

            if (orbitCamera != null)
            {
                orbitCamera.ResetView();
            }

            _activeDestination = null;
            _activePathResult = null;
            _currentPathNodeIndex = 0;

            SetState(NavigationState.SelectingDestination);
            ShowMessage("Select your next destination.");
        }

        /// <summary>
        /// Selects a destination programmatically by destination ID or name (compatible with WebGL SendMessage).
        /// </summary>
        public void SelectDestinationById(string destinationId)
        {
            if (string.IsNullOrEmpty(destinationId)) return;

            if (_buildingData != null && _buildingData.destinations != null)
            {
                var dest = _buildingData.destinations.Find(d =>
                    string.Equals(d.destinationId, destinationId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.destinationName, destinationId, StringComparison.OrdinalIgnoreCase));

                if (dest != null)
                {
                    HandleDestinationSelected(dest);
                    return;
                }
            }

            if (destinationSelectionUI != null)
            {
                destinationSelectionUI.SelectDestinationById(destinationId);
            }
        }

        /// <summary>
        /// Updates banner message display and synchronizes with tracking diagnostics.
        /// </summary>
        public void ShowMessage(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }

        /// <summary>
        /// Synchronizes destination data across Mode A (Real AR) and Mode B (Demo Navigation) text fields.
        /// Guaranteed to update immediately on destination selection to prevent '--' placeholders.
        /// </summary>
        public void UpdateNavigationInfoDisplays()
        {
            if (_activeDestination == null)
            {
                Debug.LogWarning("[NavigationUIController] Cannot update navigation info: active destination is null.");
                if (destinationInfoText != null) destinationInfoText.text = "Destination: <Not Selected>";
                if (navigationInfoText != null) navigationInfoText.text = "Target: <Not Selected>";
                if (floorIndicatorText != null) floorIndicatorText.text = "Floor: --";
                if (distanceText != null) distanceText.text = "Distance: --";
                if (demoDestinationText != null) demoDestinationText.text = "Destination: <Not Selected>";
                if (demoFloorText != null) demoFloorText.text = "Floor: --";
                if (demoDetailsText != null) demoDetailsText.text = "No destination chosen.";
                return;
            }

            string destName = !string.IsNullOrEmpty(_activeDestination.destinationName)
                ? _activeDestination.destinationName
                : (!string.IsNullOrEmpty(_activeDestination.destinationId) ? _activeDestination.destinationId : "<Unknown>");

            string floorStr = _activeDestination.floorId;
            if (_buildingData != null && _buildingData.floors != null)
            {
                var floor = _buildingData.floors.Find(f => f.floorId == _activeDestination.floorId);
                if (floor != null && !string.IsNullOrEmpty(floor.floorName))
                {
                    floorStr = $"{floor.floorName} (Floor {floor.floorNumber})";
                }
            }

            // Mode A Displays (Real AR)
            if (destinationInfoText != null)
            {
                destinationInfoText.text = $"Destination: <b>{destName}</b>";
            }

            if (navigationInfoText != null)
            {
                navigationInfoText.text = $"Target: <b>{destName}</b>\nDept: {_activeDestination.department}";
            }

            if (floorIndicatorText != null)
            {
                floorIndicatorText.text = $"Floor: {floorStr}";
            }

            if (distanceText != null && _activePathResult != null)
            {
                distanceText.text = $"Est. Distance: {_activePathResult.totalCost:F1}m";
            }

            // Mode B Displays (Demo Navigation Mode)
            if (demoDestinationText != null)
            {
                demoDestinationText.text = $"Destination: <b>{destName}</b>";
            }

            if (demoFloorText != null)
            {
                demoFloorText.text = $"Floor: <b>{floorStr}</b>  •  Dept: <b>{_activeDestination.department}</b>";
            }

            if (demoDetailsText != null)
            {
                demoDetailsText.text = !string.IsNullOrEmpty(_activeDestination.description)
                    ? _activeDestination.description
                    : "Computer Science and Engineering department facility.";
            }

            if (demoDistanceText != null && _activePathResult != null)
            {
                demoDistanceText.text = $"Remaining: <b>{_activePathResult.totalCost:F1}m</b>  |  Total: <b>{_activePathResult.totalCost:F1}m</b>";
            }

            // Sync AR status to statusText or arStatusText
            if (arSessionController != null)
            {
                HandleARStatusMessageChanged(arSessionController.StatusMessage);
            }
        }

        /// <summary>
        /// Updates only the destination info text fields (destination name, floor, distance, dept)
        /// without calling HandleARStatusMessageChanged or ShowMessage.
        /// Use this during navigation startup to avoid the AR status banner overwriting the nav banner.
        /// </summary>
        private void UpdateDestinationInfoFieldsOnly()
        {
            if (_activeDestination == null) return;

            string destName = !string.IsNullOrEmpty(_activeDestination.destinationName)
                ? _activeDestination.destinationName
                : (!string.IsNullOrEmpty(_activeDestination.destinationId) ? _activeDestination.destinationId : "<Unknown>");

            string floorStr = _activeDestination.floorId;
            if (_buildingData != null && _buildingData.floors != null)
            {
                var floor = _buildingData.floors.Find(f => f.floorId == _activeDestination.floorId);
                if (floor != null && !string.IsNullOrEmpty(floor.floorName))
                {
                    floorStr = $"{floor.floorName} (Floor {floor.floorNumber})";
                }
            }

            if (destinationInfoText != null)
                destinationInfoText.text = $"Destination: <b>{destName}</b>";
            if (navigationInfoText != null)
                navigationInfoText.text = $"Target: <b>{destName}</b>\nDept: {_activeDestination.department}";
            if (floorIndicatorText != null)
                floorIndicatorText.text = $"Floor: {floorStr}";
            if (distanceText != null && _activePathResult != null)
                distanceText.text = $"Est. Distance: {_activePathResult.totalCost:F1}m";

            if (demoDestinationText != null)
                demoDestinationText.text = $"Destination: <b>{destName}</b>";
            if (demoFloorText != null)
                demoFloorText.text = $"Floor: <b>{floorStr}</b>  •  Dept: <b>{_activeDestination.department}</b>";
            if (demoDetailsText != null)
                demoDetailsText.text = !string.IsNullOrEmpty(_activeDestination.description)
                    ? _activeDestination.description
                    : "Computer Science and Engineering department facility.";
            if (demoDistanceText != null && _activePathResult != null)
                demoDistanceText.text = $"Remaining: <b>{_activePathResult.totalCost:F1}m</b>  |  Total: <b>{_activePathResult.totalCost:F1}m</b>";
        }

        private Vector3 GetUserARPosition()
        {
            if (_arCamera == null)
            {
                _arCamera = Camera.main;
            }

            if (_arCamera != null)
            {
                return _arCamera.transform.position;
            }

            if (arSessionController != null && arSessionController.SessionOrigin != null)
            {
                return arSessionController.SessionOrigin.transform.position;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Transitions UI state and dynamically displays Real AR or Demo Navigation panel.
        /// </summary>
        private void SetState(NavigationState state)
        {
            CurrentState = state;

            if (destinationSelectionPanel != null)
            {
                destinationSelectionPanel.SetActive(state == NavigationState.SelectingDestination);
            }

            if (state == NavigationState.Navigating)
            {
                bool isDemo = IsDemoNavigationMode;
                if (isDemo)
                {
                    if (navigationPanel != null) navigationPanel.SetActive(false);
                    if (demoNavigationPanel != null)
                    {
                        demoNavigationPanel.SetActive(true);
                        // Ensure buttons are active and interactable
                        if (demoStepButton != null) demoStepButton.interactable = true;
                        if (demoAutoPlayButton != null) demoAutoPlayButton.interactable = true;
                        if (demoResetButton != null) demoResetButton.interactable = true;
                        if (demoBackButton != null) demoBackButton.interactable = true;
                    }
                }
                else
                {
                    if (navigationPanel != null) navigationPanel.SetActive(true);
                    if (demoNavigationPanel != null) demoNavigationPanel.SetActive(false);
                }
            }
            else
            {
                if (navigationPanel != null) navigationPanel.SetActive(false);
                if (demoNavigationPanel != null) demoNavigationPanel.SetActive(false);
            }

            if (destinationReachedPanel != null)
            {
                destinationReachedPanel.SetActive(state == NavigationState.DestinationReached);
            }

            if (statusPanel != null)
            {
                statusPanel.SetActive(true);
            }

            if (sidebarContainer != null)
            {
                sidebarContainer.SetActive(!IsSidebarCollapsed);
            }

            UpdateDiagnosticsDisplay();

            Debug.Log($"[NavigationUIController] Transitioned to state: {state}");
        }

        /// <summary>
        /// Manually sets active destination and path for automated unit/integration testing.
        /// </summary>
        public void SetActiveDestinationForTesting(DestinationData dest, PathResult path)
        {
            _activeDestination = dest;
            _activePathResult = path;
            _currentPathNodeIndex = 0;
            _isAutoSimulating = false;
            UpdateNavigationInfoDisplays();
            if (path != null && path.nodes != null && path.nodes.Count > 0)
            {
                UpdateDemoProgressUI();
            }
        }

        /// <summary>
        /// Manually binds building dataset for testing environments.
        /// </summary>
        public void SetBuildingDataForTesting(BuildingData data)
        {
            _buildingData = data;
            if (data != null)
            {
                _navigationGraph = data.ToNavigationGraph();
                _pathfinder = new Pathfinder(_navigationGraph);
            }
        }

        private void FindMissingReferences()
        {
            if (destinationSelectionUI == null)
            {
                destinationSelectionUI = FindObjectOfType<DestinationSelectionUI>();
            }

            if (startNodeSelector == null)
            {
                startNodeSelector = FindObjectOfType<StartNodeSelector>();
            }

            if (arSessionController == null)
            {
                arSessionController = FindObjectOfType<ARSessionController>();
            }

            if (arNavigationRenderer == null)
            {
                arNavigationRenderer = FindObjectOfType<ARNavigationRenderer>();
            }

            if (coordinateCalibrator == null)
            {
                coordinateCalibrator = FindObjectOfType<CoordinateCalibrator>();
            }

            if (buildingVisualizer == null)
            {
                buildingVisualizer = FindObjectOfType<BuildingVisualizer>();
            }

            if (routeVisualizer == null)
            {
                routeVisualizer = FindObjectOfType<RouteVisualizer>();
            }

            if (orbitCamera == null)
            {
                orbitCamera = FindObjectOfType<OrbitCamera>();
            }

            // Auto-attach NavigationAudioController: prefer sibling on same GameObject,
            // then scene-wide search, then add one automatically.
            if (navigationAudio == null)
            {
                navigationAudio = GetComponent<NavigationAudioController>();
            }
            if (navigationAudio == null)
            {
                navigationAudio = FindObjectOfType<NavigationAudioController>();
            }
            if (navigationAudio == null)
            {
                navigationAudio = gameObject.AddComponent<NavigationAudioController>();
                Debug.Log("[NavigationUIController] NavigationAudioController added automatically.");
            }
        }
    }
}
