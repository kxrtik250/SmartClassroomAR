using System;
using UnityEngine;

namespace SmartClassroomAR.Visualization
{
    /// <summary>
    /// Provides an interactive 3D orbit, pan, and zoom camera controller.
    /// Supports mouse dragging, trackpad gestures, scroll wheel, touch, and keyboard navigation.
    /// Fully compatible with both Unity Editor Play Mode and WebGL browser canvas.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        [Header("Target & Center")]
        [Tooltip("The central point the camera focuses on. Shifted slightly to center building in available 3D viewport.")]
        [SerializeField] private Vector3 targetPosition = new Vector3(18.5f, 5.0f, 0.0f);

        [Header("Orbit Limits & Angles")]
        [SerializeField] private float distance = 68.0f;
        [SerializeField] private float minDistance = 12.0f;
        [SerializeField] private float maxDistance = 110.0f;
        [SerializeField] private float yaw = 34.0f;
        [SerializeField] private float pitch = 22.0f;
        [SerializeField] private float minPitch = 8.0f;
        [SerializeField] private float maxPitch = 85.0f;

        [Header("Sensitivities & Smoothing")]
        [SerializeField] private float orbitSensitivity = 3.5f;
        [SerializeField] private float zoomSensitivity = 8.0f;
        [SerializeField] private float panSensitivity = 0.5f;
        [SerializeField] private float smoothingTime = 0.14f;

        [Header("Subsystems")]
        [SerializeField] private BuildingVisualizer buildingVisualizer;

        public bool IsOrbitEnabled { get; set; } = true;

        public void SetBuildingVisualizer(BuildingVisualizer bVis) => buildingVisualizer = bVis;

        private Vector3 _currentTarget;
        private Vector3 _targetVelocity;
        private float _currentYaw;
        private float _currentPitch;
        private float _currentDistance;
        private Vector3 _lastMousePosition;

        private void Awake()
        {
            if (buildingVisualizer == null)
            {
                buildingVisualizer = FindObjectOfType<BuildingVisualizer>();
            }

            _currentTarget = targetPosition;
            _currentYaw = yaw;
            _currentPitch = pitch;
            _currentDistance = distance;
        }

        private void Start()
        {
            UpdateCameraTransform(true);
        }

        private void Update()
        {
            if (!IsOrbitEnabled) return;

            HandleInput();
            HandleKeyboardNavigation();
        }

        private void LateUpdate()
        {
            UpdateCameraTransform(false);
        }

        private bool _isDragging = false;
        private int _activeDragButton = -1;

        private bool IsPointerOverInteractiveUI()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleInput()
        {
            Vector3 mousePos = Input.mousePosition;

            // Check if any mouse button went down this frame
            bool mouseDown0 = Input.GetMouseButtonDown(0);
            bool mouseDown1 = Input.GetMouseButtonDown(1);
            bool mouseDown2 = Input.GetMouseButtonDown(2);

            if (mouseDown0 || mouseDown1 || mouseDown2)
            {
                if (!IsPointerOverInteractiveUI())
                {
                    _isDragging = true;
                    _activeDragButton = mouseDown0 ? 0 : (mouseDown1 ? 1 : 2);
                    _lastMousePosition = mousePos;
                }
            }

            // Check if mouse button released
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
            {
                _isDragging = false;
                _activeDragButton = -1;
            }

            // Zoom (Mouse Scroll Wheel or trackpad pinch) - only when NOT over interactive UI
            if (!IsPointerOverInteractiveUI())
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                {
                    distance -= scroll * zoomSensitivity * (distance * 0.25f);
                    distance = Mathf.Clamp(distance, minDistance, maxDistance);
                }
            }

            // Perform drag orbit / pan if drag is active
            if (_isDragging)
            {
                Vector3 delta = mousePos - _lastMousePosition;
                _lastMousePosition = mousePos;

                bool isAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

                // Orbit: Left Mouse Button Drag (without Alt) or Right Mouse Button Drag (without Alt)
                if ((Input.GetMouseButton(0) && !isAlt) || (Input.GetMouseButton(1) && !isAlt))
                {
                    yaw += delta.x * orbitSensitivity * 0.1f;
                    pitch -= delta.y * orbitSensitivity * 0.1f;
                    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                }
                // Pan: Middle Mouse Button Drag OR Alt + Left Drag OR Shift + Right Drag
                else if (Input.GetMouseButton(2) || (Input.GetMouseButton(0) && isAlt) || (Input.GetMouseButton(1) && isAlt))
                {
                    // Translate in camera plane
                    Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
                    Vector3 right = rotation * Vector3.right;
                    Vector3 up = rotation * Vector3.up;

                    float factor = panSensitivity * (distance * 0.02f);
                    targetPosition -= (right * delta.x + up * delta.y) * factor;
                }
            }
            else
            {
                _lastMousePosition = mousePos;
            }
        }

        private void HandleKeyboardNavigation()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null)
            {
                return;
            }

            // Floor focus hotkeys: 1 (Ground), 2 (First), 3 (Second)
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                FocusOnFloor(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                FocusOnFloor(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                FocusOnFloor(2);
            }

            // Reset camera hotkey: R
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetView();
            }

            // Keyboard panning: Arrow keys or WASD
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) > 0.05f || Mathf.Abs(v) > 0.05f)
            {
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
                Vector3 move = rotation * new Vector3(h, 0f, v).normalized;
                targetPosition += move * (15f * Time.deltaTime);
            }

            // Keyboard zoom: Q (zoom in) / E (zoom out)
            if (Input.GetKey(KeyCode.Q))
            {
                distance = Mathf.Clamp(distance - 15f * Time.deltaTime, minDistance, maxDistance);
            }
            if (Input.GetKey(KeyCode.E))
            {
                distance = Mathf.Clamp(distance + 15f * Time.deltaTime, minDistance, maxDistance);
            }
        }

        private void UpdateCameraTransform(bool instant)
        {
            if (instant)
            {
                _currentTarget = targetPosition;
                _currentYaw = yaw;
                _currentPitch = pitch;
                _currentDistance = distance;
            }
            else
            {
                _currentTarget = Vector3.SmoothDamp(_currentTarget, targetPosition, ref _targetVelocity, smoothingTime);
                _currentYaw = Mathf.Lerp(_currentYaw, yaw, Time.deltaTime * 18f);
                _currentPitch = Mathf.Lerp(_currentPitch, pitch, Time.deltaTime * 18f);
                _currentDistance = Mathf.Lerp(_currentDistance, distance, Time.deltaTime * 15f);
            }

            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -_currentDistance);
            transform.position = _currentTarget + offset;
            transform.LookAt(_currentTarget);
        }

        private bool _isSidebarCollapsed = false;
        public bool IsSidebarCollapsed => _isSidebarCollapsed;

        /// <summary>
        /// Adjusts camera framing based on sidebar expanded or collapsed state.
        /// When collapsed: centers building on screen (X=24m) and scales building larger.
        /// When expanded: centers building in the right viewport (X=19m) beside sidebar.
        /// </summary>
        public void SetSidebarCollapsed(bool collapsed)
        {
            if (_isSidebarCollapsed == collapsed) return;
            _isSidebarCollapsed = collapsed;

            float targetX = collapsed ? 24.0f : 18.5f;
            targetPosition.x = targetX;
            _currentTarget.x = targetX;

            float targetDist = collapsed ? 58.0f : 68.0f;
            distance = targetDist;
        }

        /// <summary>
        /// Smoothly centers the camera target on the given world position without jarring zooms.
        /// </summary>
        public void FocusOnPosition(Vector3 position)
        {
            float xShift = _isSidebarCollapsed ? 0f : -2.5f;
            float targetX = Mathf.Clamp(position.x + xShift, 10.0f, 38.0f);
            float targetY = position.y + 1.8f;
            float targetZ = Mathf.Clamp(position.z * 0.25f, -1.2f, 1.2f);

            targetPosition = new Vector3(targetX, targetY, targetZ);
            // Maintain comfortable overview distance so the building never jerks or zooms aggressively
            distance = Mathf.Clamp(distance, 48.0f, 62.0f);
        }

        /// <summary>
        /// Centers camera on the specified floor number (0 = Ground, 1 = First, 2 = Second).
        /// Highlights the active floor while dimming other floors for clear visual separation.
        /// </summary>
        public void FocusOnFloor(int floorNumber)
        {
            float floorY = Mathf.Clamp(floorNumber, 0, 2) * 4.0f;
            float targetX = _isSidebarCollapsed ? 24.0f : 18.5f;
            float targetDist = _isSidebarCollapsed ? 48.0f : 54.0f;

            targetPosition = new Vector3(targetX, floorY + 1.8f, 0.0f);
            pitch = 30.0f;
            yaw = 32.0f;
            distance = targetDist;

            if (buildingVisualizer != null)
            {
                buildingVisualizer.SetCutawayMode(true, floorNumber);
            }
        }

        /// <summary>
        /// Resets the camera to default overall building overview and restores all floor opacities.
        /// </summary>
        public void ResetView()
        {
            float targetX = _isSidebarCollapsed ? 24.0f : 18.5f;
            float targetDist = _isSidebarCollapsed ? 58.0f : 68.0f;

            targetPosition = new Vector3(targetX, 5.0f, 0.0f);
            distance = targetDist;
            yaw = 34.0f;
            pitch = 22.0f;

            if (buildingVisualizer != null)
            {
                buildingVisualizer.SetCutawayMode(false);
            }
        }

        /// <summary>
        /// Automatically frames the entire 3D building in the viewport.
        /// </summary>
        public void AutoFrameBuilding()
        {
            ResetView();
        }

        public void SetOrbitEnabled(bool enabled)
        {
            IsOrbitEnabled = enabled;
        }
    }
}
