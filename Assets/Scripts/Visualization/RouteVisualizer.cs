using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SmartClassroomAR.Navigation;

namespace SmartClassroomAR.Visualization
{
    /// <summary>
    /// Renders and animates the 3D navigation route in the building model.
    /// Manages the 3D route line, waypoint beacons, pulsing destination pin, and moving user marker.
    /// Synchronizes directly with Pathfinder output and NavigationUIController step/simulation events.
    /// </summary>
    public class RouteVisualizer : MonoBehaviour
    {
        [Header("Route Settings")]
        [SerializeField] private float lineWidth = 0.35f;
        [SerializeField] private float lineElevation = 0.42f;
        [SerializeField] private Color routeColorRemaining = new Color(0.0f, 0.85f, 1f, 1f); // Electric Cyan
        [SerializeField] private Color routeColorCompleted = new Color(0.15f, 1f, 0.35f, 1f); // Vibrant Neon Green

        [Header("User Marker")]
        [SerializeField] private Color userMarkerColor = new Color(1f, 0.82f, 0.15f, 1f); // Glowing Amber-Gold

        public GameObject RouteRoot { get; private set; }

        private readonly List<NavNode> _currentPath = new List<NavNode>();
        private readonly List<GameObject> _waypointMarkers = new List<GameObject>();
        private readonly List<Transform> _floatingLabels = new List<Transform>();
        private readonly List<Material> _runtimeMaterials = new List<Material>();

        private LineRenderer _remainingLine;
        private LineRenderer _completedLine;
        private GameObject _userMarker;
        private GameObject _destinationMarker;
        private Vector3 _destMarkerBasePos;
        private Vector3 _userTargetPos;
        private bool _isMovingUser = false;
        private int _currentWaypointIndex = 0;
        private string _destinationName = string.Empty;

        // Visual animation parameters
        private float _animationTimer = 0f;

        private void Update()
        {
            _animationTimer += Time.deltaTime;

            // Bobbing & pulsing animation for destination marker
            if (_destinationMarker != null)
            {
                float bobOffset = Mathf.Sin(_animationTimer * 3.5f) * 0.25f;
                _destinationMarker.transform.position = _destMarkerBasePos + new Vector3(0f, bobOffset, 0f);

                float pulseScale = 1.0f + Mathf.Sin(_animationTimer * 5f) * 0.12f;
                _destinationMarker.transform.localScale = new Vector3(pulseScale, pulseScale, pulseScale);
            }

            // Subtle rotation for user marker and smooth position interpolation
            if (_userMarker != null)
            {
                _userMarker.transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);

                if (_isMovingUser)
                {
                    _userMarker.transform.position = Vector3.MoveTowards(_userMarker.transform.position, _userTargetPos, 14f * Time.deltaTime);
                    if (Vector3.Distance(_userMarker.transform.position, _userTargetPos) < 0.01f)
                    {
                        _userMarker.transform.position = _userTargetPos;
                        _isMovingUser = false;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            // Billboard all floating route labels to face the active camera
            if (Camera.main != null)
            {
                for (int i = _floatingLabels.Count - 1; i >= 0; i--)
                {
                    if (_floatingLabels[i] != null)
                    {
                        _floatingLabels[i].forward = Camera.main.transform.forward;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            ClearRoute();
            foreach (var mat in _runtimeMaterials)
            {
                if (mat != null) Destroy(mat);
            }
            _runtimeMaterials.Clear();
        }

        /// <summary>
        /// Clears all route geometry and markers.
        /// </summary>
        public void ClearRoute()
        {
            if (RouteRoot != null)
            {
                Destroy(RouteRoot);
                RouteRoot = null;
            }

            _waypointMarkers.Clear();
            _floatingLabels.Clear();
            _currentPath.Clear();
            _remainingLine = null;
            _completedLine = null;
            _userMarker = null;
            _destinationMarker = null;
            _isMovingUser = false;
            _currentWaypointIndex = 0;
            _destinationName = string.Empty;
        }

        /// <summary>
        /// Renders the complete 3D route given a list of path nodes.
        /// </summary>
        public void RenderRoute(List<NavNode> pathNodes, string destinationName = "Destination")
        {
            ClearRoute();

            if (pathNodes == null || pathNodes.Count == 0)
            {
                Debug.LogWarning("[RouteVisualizer] RenderRoute called with null or empty path nodes.");
                return;
            }

            _currentPath.AddRange(pathNodes);
            _destinationName = destinationName;
            _currentWaypointIndex = 0;

            RouteRoot = new GameObject("RouteVisuals_3D");
            RouteRoot.transform.SetParent(transform, false);

            // 1. Create Line Renderers (Completed and Remaining)
            CreateLineRenderers();

            // 2. Create Waypoint Markers
            CreateWaypointMarkers();

            // 3. Create User Marker
            CreateUserMarker();

            // 4. Update route line coordinates
            UpdateRouteLines();

            Debug.Log($"[RouteVisualizer] Rendered route with {_currentPath.Count} waypoints to '{_destinationName}'.");
        }

        private Shader GetSafeShader(string preferredName = "Sprites/Default")
        {
            Shader s = null;
            if (!string.IsNullOrEmpty(preferredName))
            {
                s = Shader.Find(preferredName);
            }
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Diffuse");
            if (s == null) s = Shader.Find("Mobile/Diffuse");
            if (s == null) s = Shader.Find("UI/Default");
            if (s == null) s = Shader.Find("Hidden/InternalErrorShader");

            if (s == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var rend = temp.GetComponent<MeshRenderer>();
                if (rend != null && rend.sharedMaterial != null)
                {
                    s = rend.sharedMaterial.shader;
                }
                Destroy(temp);
            }
            return s;
        }

        private void CreateLineRenderers()
        {
            Shader lineShader = GetSafeShader("Sprites/Default");

            // Remaining route line
            var remObj = new GameObject("RouteLine_Remaining");
            remObj.transform.SetParent(RouteRoot.transform, false);
            _remainingLine = remObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(_remainingLine, lineShader, routeColorRemaining);

            // Completed route line
            var compObj = new GameObject("RouteLine_Completed");
            compObj.transform.SetParent(RouteRoot.transform, false);
            _completedLine = compObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(_completedLine, lineShader, routeColorCompleted);
        }

        private void ConfigureLineRenderer(LineRenderer lr, Shader shader, Color color)
        {
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.numCapVertices = 5;
            lr.numCornerVertices = 5;

            var mat = CreateMaterial(shader, color);
            lr.sharedMaterial = mat;
        }

        private void CreateWaypointMarkers()
        {
            Shader standardShader = GetSafeShader("Standard");

            for (int i = 0; i < _currentPath.Count; i++)
            {
                var node = _currentPath[i];
                Vector3 nodePos = new Vector3(node.x, node.y + lineElevation, node.z);

                if (i == 0)
                {
                    // Start Marker (Vibrant Green Beacon)
                    var startMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    startMarker.name = $"StartMarker_{node.nodeId}";
                    startMarker.transform.SetParent(RouteRoot.transform, false);
                    startMarker.transform.position = nodePos;
                    startMarker.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

                    var mat = CreateMaterial(standardShader, new Color(0.12f, 0.95f, 0.42f, 1f));
                    ApplyMaterial(startMarker, mat);
                    _waypointMarkers.Add(startMarker);

                    // Start Label
                    CreateFloatingText(startMarker.transform, "START POINT", new Vector3(0f, 1.25f, 0f), new Color(0.2f, 1f, 0.5f, 1f));
                }
                else if (i == _currentPath.Count - 1)
                {
                    // Destination Marker (Vibrant Red Beacon Pin)
                    _destinationMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    _destinationMarker.name = $"DestinationMarker_{node.nodeId}";
                    _destinationMarker.transform.SetParent(RouteRoot.transform, false);
                    _destMarkerBasePos = nodePos + new Vector3(0f, 0.95f, 0f);
                    _destinationMarker.transform.position = _destMarkerBasePos;
                    _destinationMarker.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

                    var mat = CreateMaterial(standardShader, new Color(1f, 0.22f, 0.22f, 1f));
                    ApplyMaterial(_destinationMarker, mat);
                    _waypointMarkers.Add(_destinationMarker);

                    // Destination Label
                    CreateFloatingText(_destinationMarker.transform, _destinationName, new Vector3(0f, 1.25f, 0f), Color.white, 2.0f, 0.68f);
                }
                else if (node.nodeType == NodeType.Staircase)
                {
                    // Staircase Marker (Warm Amber Diamond)
                    var stairMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stairMarker.name = $"StairWaypoint_{node.nodeId}";
                    stairMarker.transform.SetParent(RouteRoot.transform, false);
                    stairMarker.transform.position = nodePos;
                    stairMarker.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
                    stairMarker.transform.localScale = new Vector3(0.68f, 0.68f, 0.68f);

                    var mat = CreateMaterial(standardShader, new Color(1f, 0.65f, 0.12f, 1f));
                    ApplyMaterial(stairMarker, mat);
                    _waypointMarkers.Add(stairMarker);

                    CreateFloatingText(stairMarker.transform, "STAIRS", new Vector3(0f, 0.85f, 0f), new Color(1f, 0.85f, 0.3f, 1f), 1.6f, 0.50f);
                }
                else
                {
                    // Intermediate Waypoint Marker (Glowing Cyan Sphere)
                    var wpMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    wpMarker.name = $"Waypoint_{node.nodeId}";
                    wpMarker.transform.SetParent(RouteRoot.transform, false);
                    wpMarker.transform.position = nodePos;
                    wpMarker.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);

                    var mat = CreateMaterial(standardShader, new Color(0.15f, 0.85f, 1f, 0.95f));
                    ApplyMaterial(wpMarker, mat);
                    _waypointMarkers.Add(wpMarker);
                }
            }
        }

        private void CreateUserMarker()
        {
            if (_currentPath.Count == 0) return;

            Shader standardShader = GetSafeShader("Standard");

            _userMarker = new GameObject("UserPositionMarker");
            _userMarker.transform.SetParent(RouteRoot.transform, false);

            var startNode = _currentPath[0];
            Vector3 startPos = new Vector3(startNode.x, startNode.y + lineElevation, startNode.z);
            _userMarker.transform.position = startPos;

            // Ground Footprint Halo Pad (square beacon pad)
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ring.name = "UserGroundHalo";
            ring.transform.SetParent(_userMarker.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            ring.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);
            var ringMat = CreateMaterial(standardShader, new Color(1f, 0.85f, 0.2f, 0.85f));
            ApplyMaterial(ring, ringMat);
            var ringCol = ring.GetComponent<Collider>();
            if (ringCol != null) Destroy(ringCol);

            // Upright avatar pedestal column
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stem.name = "UserStem";
            stem.transform.SetParent(_userMarker.transform, false);
            stem.transform.localPosition = new Vector3(0f, 0.40f, 0f);
            stem.transform.localScale = new Vector3(0.25f, 0.70f, 0.25f);
            var mat = CreateMaterial(standardShader, userMarkerColor);
            ApplyMaterial(stem, mat);
            var stemCol = stem.GetComponent<Collider>();
            if (stemCol != null) Destroy(stemCol);

            // Glowing avatar head / beacon orb
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "UserHead";
            head.transform.SetParent(_userMarker.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.90f, 0f);
            head.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            ApplyMaterial(head, mat);
            var headCol = head.GetComponent<Collider>();
            if (headCol != null) Destroy(headCol);

            // Warm glow point light
            var lightObj = new GameObject("UserGlowLight");
            lightObj.transform.SetParent(_userMarker.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            var pl = lightObj.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = userMarkerColor;
            pl.range = 5.0f;
            pl.intensity = 2.5f;

            // "YOU" Label
            CreateFloatingText(_userMarker.transform, "YOU", new Vector3(0f, 1.40f, 0f), new Color(1f, 0.9f, 0.2f, 1f), 1.8f, 0.55f);
        }

        private void CreateFloatingText(Transform parent, string text, Vector3 localOffset, Color color, float fontSize = 2.0f, float badgeHeight = 0.65f)
        {
            var textObj = new GameObject("FloatingLabel");
            textObj.transform.SetParent(parent, false);
            textObj.transform.localPosition = localOffset;

            // Compact sleek badge backing
            var badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "RouteBadgeBacking";
            badge.transform.SetParent(textObj.transform, false);
            badge.transform.localPosition = new Vector3(0f, 0f, 0.04f);
            float badgeWidth = Mathf.Clamp(text.Length * 0.22f + 1.0f, 2.6f, 5.8f);
            badge.transform.localScale = new Vector3(badgeWidth, badgeHeight, 0.04f);

            Shader shader = GetSafeShader("Standard");
            var badgeMat = CreateMaterial(shader, new Color(0.04f, 0.06f, 0.12f, 0.92f));
            ApplyMaterial(badge, badgeMat);
            var col = badge.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = $"<b>{text}</b>";
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.outlineWidth = 0.24f;
            tmp.outlineColor = Color.black;
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(badgeWidth - 0.2f, badgeHeight);

            _floatingLabels.Add(textObj.transform);
        }

        private Material CreateMaterial(Shader shader, Color color)
        {
            if (shader == null)
            {
                shader = GetSafeShader("Standard");
            }

            Material mat = null;
            if (shader != null)
            {
                try
                {
                    mat = new Material(shader);
                }
                catch (Exception)
                {
                    mat = null;
                }
            }

            if (mat == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var rend = temp.GetComponent<MeshRenderer>();
                if (rend != null && rend.sharedMaterial != null)
                {
                    mat = new Material(rend.sharedMaterial);
                }
                Destroy(temp);
            }

            if (mat != null)
            {
                if (mat.HasProperty("_Color")) mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                _runtimeMaterials.Add(mat);
            }
            return mat;
        }

        private void ApplyMaterial(GameObject go, Material mat)
        {
            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null) rend.sharedMaterial = mat;
        }

        /// <summary>
        /// Updates the LineRenderer coordinate buffers based on current user waypoint position.
        /// </summary>
        private void UpdateRouteLines()
        {
            if (_currentPath.Count == 0) return;

            // Completed Segment: from index 0 to _currentWaypointIndex
            if (_completedLine != null)
            {
                int compCount = _currentWaypointIndex + 1;
                if (compCount > 1)
                {
                    _completedLine.positionCount = compCount;
                    for (int i = 0; i < compCount; i++)
                    {
                        var n = _currentPath[i];
                        _completedLine.SetPosition(i, new Vector3(n.x, n.y + lineElevation, n.z));
                    }
                }
                else
                {
                    _completedLine.positionCount = 0;
                }
            }

            // Remaining Segment: from _currentWaypointIndex to end
            if (_remainingLine != null)
            {
                int remCount = _currentPath.Count - _currentWaypointIndex;
                if (remCount > 0)
                {
                    _remainingLine.positionCount = remCount;
                    for (int i = 0; i < remCount; i++)
                    {
                        var n = _currentPath[_currentWaypointIndex + i];
                        _remainingLine.SetPosition(i, new Vector3(n.x, n.y + lineElevation, n.z));
                    }
                }
                else
                {
                    _remainingLine.positionCount = 0;
                }
            }
        }

        /// <summary>
        /// Moves user marker directly to the specified waypoint index.
        /// </summary>
        public void SetUserPosition(int waypointIndex)
        {
            if (_currentPath.Count == 0) return;

            _currentWaypointIndex = Mathf.Clamp(waypointIndex, 0, _currentPath.Count - 1);
            var targetNode = _currentPath[_currentWaypointIndex];
            Vector3 targetPos = new Vector3(targetNode.x, targetNode.y + lineElevation, targetNode.z);

            if (_userMarker != null)
            {
                _userTargetPos = targetPos;
                // Snap if initializing or far away, otherwise interpolate smoothly
                if (waypointIndex == 0 && Vector3.Distance(_userMarker.transform.position, targetPos) > 5f)
                {
                    _userMarker.transform.position = targetPos;
                    _isMovingUser = false;
                }
                else
                {
                    _isMovingUser = true;
                }
            }

            UpdateRouteLines();
        }

        /// <summary>
        /// Advances the user marker by one waypoint. Returns the new waypoint index.
        /// </summary>
        public int AdvanceUserToNextWaypoint()
        {
            if (_currentWaypointIndex < _currentPath.Count - 1)
            {
                SetUserPosition(_currentWaypointIndex + 1);
            }
            return _currentWaypointIndex;
        }

        /// <summary>
        /// Resets the user marker back to the initial start waypoint.
        /// </summary>
        public void ResetToStart()
        {
            if (_currentPath.Count == 0) return;
            _currentWaypointIndex = 0;
            var targetNode = _currentPath[0];
            Vector3 targetPos = new Vector3(targetNode.x, targetNode.y + lineElevation, targetNode.z);

            if (_userMarker != null)
            {
                _userMarker.transform.position = targetPos;
                _userTargetPos = targetPos;
                _isMovingUser = false;
            }

            UpdateRouteLines();
        }

        public int GetCurrentWaypointIndex() => _currentWaypointIndex;
        public int GetTotalWaypoints() => _currentPath.Count;
        public bool IsAtDestination() => _currentPath.Count > 0 && _currentWaypointIndex >= _currentPath.Count - 1;

        public NavNode GetCurrentNode()
        {
            if (_currentPath.Count == 0 || _currentWaypointIndex < 0 || _currentWaypointIndex >= _currentPath.Count)
                return null;
            return _currentPath[_currentWaypointIndex];
        }

        public NavNode GetNextNode()
        {
            int nextIdx = _currentWaypointIndex + 1;
            if (_currentPath.Count == 0 || nextIdx >= _currentPath.Count)
                return null;
            return _currentPath[nextIdx];
        }
    }
}
