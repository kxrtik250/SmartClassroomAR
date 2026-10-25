using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SmartClassroomAR.Navigation;

namespace SmartClassroomAR.AR
{
    /// <summary>
    /// Renders AR navigation waypoints, directional arrows, and destination markers
    /// in AR space corresponding to a calculated route.
    /// 
    /// CALIBRATION NOTICE:
    /// Coordinate mapping from architectural building coordinates to AR Foundation world coordinates
    /// relies on physical calibration (offset and scale). In an actual physical deployment,
    /// calibration parameters must be derived from precise surveyed site measurements
    /// using CoordinateCalibrator before rendering.
    /// </summary>
    public class ARNavigationRenderer : MonoBehaviour
    {
        [Header("AR Prefab References")]
        [Tooltip("Prefab representing a directional arrow placed between path nodes.")]
        [SerializeField] private GameObject arrowPrefab;

        [Tooltip("Prefab representing an intermediate waypoint or turn node.")]
        [SerializeField] private GameObject markerPrefab;

        [Tooltip("Prefab representing the final destination point with a label.")]
        [SerializeField] private GameObject destinationMarkerPrefab;

        [Header("AR Origin Reference")]
        [Tooltip("Transform of the AR Session Origin / XR Origin.")]
        [SerializeField] private Transform arSessionOrigin;

        [Header("Coordinate Calibration Settings")]
        [Tooltip("Offset applied to convert building coordinates to AR space.")]
        [SerializeField] private Vector3 coordinateOffset = Vector3.zero;

        [Tooltip("Scale factor applied to convert building coordinates to AR space.")]
        [SerializeField] private float coordinateScale = 1.0f;

        [Header("Proximity Settings")]
        [Tooltip("Distance in meters within which the user is considered to have arrived at the destination.")]
        public float DestinationReachThreshold = 2.0f;

        private readonly List<GameObject> _instantiatedObjects = new List<GameObject>();
        private Vector3 _currentDestinationArPosition = Vector3.positiveInfinity;
        private bool _hasActivePath = false;
        private ARPrefabSetup _fallbackPrefabSetup;

        private void Awake()
        {
            _fallbackPrefabSetup = GetComponent<ARPrefabSetup>();
            if (_fallbackPrefabSetup == null)
            {
                _fallbackPrefabSetup = gameObject.AddComponent<ARPrefabSetup>();
            }
        }

        private void OnDestroy()
        {
            ClearPath();
        }

        /// <summary>
        /// Updates the translation offset and scale factor used to map building coordinates to AR coordinates.
        /// </summary>
        public void SetCalibration(Vector3 offset, float scale)
        {
            coordinateOffset = offset;
            coordinateScale = Mathf.Max(0.001f, scale);
            Debug.Log($"[ARNavigationRenderer] Calibration updated: Offset={coordinateOffset}, Scale={coordinateScale}");
        }

        /// <summary>
        /// Transforms building blueprint coordinates into AR Foundation session world coordinates.
        /// </summary>
        public Vector3 BuildingToARPosition(Vector3 buildingPos)
        {
            Vector3 calibratedLocalPos = (buildingPos * coordinateScale) + coordinateOffset;
            if (arSessionOrigin != null)
            {
                return arSessionOrigin.TransformPoint(calibratedLocalPos);
            }
            return calibratedLocalPos;
        }

        /// <summary>
        /// Instantiates AR visualization objects along the specified path nodes.
        /// </summary>
        public void RenderPath(List<NavNode> pathNodes, string destinationName)
        {
            ClearPath();

            if (pathNodes == null || pathNodes.Count == 0)
            {
                Debug.LogWarning("[ARNavigationRenderer] RenderPath called with null or empty path nodes.");
                return;
            }

            EnsurePrefabsAvailable();

            // 1. Place waypoint markers for all nodes except the last
            for (int i = 0; i < pathNodes.Count - 1; i++)
            {
                var node = pathNodes[i];
                if (node == null) continue;

                Vector3 arPos = BuildingToARPosition(node.Position);
                if (markerPrefab != null)
                {
                    var markerObj = Instantiate(markerPrefab, arPos, Quaternion.identity, transform);
                    markerObj.name = $"Waypoint_{i}_{node.nodeId}";
                    _instantiatedObjects.Add(markerObj);
                }
            }

            // 2. Place directional arrows between consecutive pairs of nodes
            for (int i = 0; i < pathNodes.Count - 1; i++)
            {
                var current = pathNodes[i];
                var next = pathNodes[i + 1];
                if (current == null || next == null) continue;

                Vector3 currentArPos = BuildingToARPosition(current.Position);
                Vector3 nextArPos = BuildingToARPosition(next.Position);

                Vector3 midpoint = (currentArPos + nextArPos) * 0.5f;
                Vector3 direction = nextArPos - currentArPos;

                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    if (arrowPrefab != null)
                    {
                        var arrowObj = Instantiate(arrowPrefab, midpoint, rotation, transform);
                        arrowObj.name = $"Arrow_{i}_to_{i + 1}";
                        _instantiatedObjects.Add(arrowObj);
                    }
                }
            }

            // 3. Place destination marker at final node
            var finalNode = pathNodes[pathNodes.Count - 1];
            if (finalNode != null)
            {
                _currentDestinationArPosition = BuildingToARPosition(finalNode.Position);
                _hasActivePath = true;

                if (destinationMarkerPrefab != null)
                {
                    var destObj = Instantiate(destinationMarkerPrefab, _currentDestinationArPosition, Quaternion.identity, transform);
                    destObj.name = $"Destination_{finalNode.nodeId}";

                    // Configure destination label text
                    var textComp = destObj.GetComponentInChildren<TMP_Text>();
                    if (textComp != null)
                    {
                        textComp.text = string.IsNullOrEmpty(destinationName) ? finalNode.label : destinationName;
                    }
                    else
                    {
                        var standardText = destObj.GetComponentInChildren<TextMesh>();
                        if (standardText != null)
                        {
                            standardText.text = string.IsNullOrEmpty(destinationName) ? finalNode.label : destinationName;
                        }
                    }

                    _instantiatedObjects.Add(destObj);
                }
            }

            Debug.Log($"[ARNavigationRenderer] Rendered navigation path with {pathNodes.Count} nodes and {_instantiatedObjects.Count} AR markers.");
        }

        /// <summary>
        /// Clears all active AR navigation objects and resets destination tracking.
        /// </summary>
        public void ClearPath()
        {
            for (int i = 0; i < _instantiatedObjects.Count; i++)
            {
                if (_instantiatedObjects[i] != null)
                {
                    Destroy(_instantiatedObjects[i]);
                }
            }
            _instantiatedObjects.Clear();
            _currentDestinationArPosition = Vector3.positiveInfinity;
            _hasActivePath = false;
        }

        /// <summary>
        /// Calculates Euclidean distance from user's current AR camera position to the destination.
        /// </summary>
        public float UpdateDestinationProximity(Vector3 userPosition)
        {
            if (!_hasActivePath || float.IsPositiveInfinity(_currentDestinationArPosition.x))
            {
                return float.MaxValue;
            }

            return Vector3.Distance(userPosition, _currentDestinationArPosition);
        }

        /// <summary>
        /// Checks whether the user is within the reach threshold of the destination.
        /// </summary>
        public bool HasReachedDestination(Vector3 userPosition)
        {
            if (!_hasActivePath) return false;
            return UpdateDestinationProximity(userPosition) <= DestinationReachThreshold;
        }

        /// <summary>
        /// Ensures fallback prefabs are instantiated if serialized fields are not set.
        /// </summary>
        private void EnsurePrefabsAvailable()
        {
            if (arrowPrefab == null && _fallbackPrefabSetup != null)
            {
                arrowPrefab = _fallbackPrefabSetup.CreateDefaultArrowPrefab();
            }
            if (markerPrefab == null && _fallbackPrefabSetup != null)
            {
                markerPrefab = _fallbackPrefabSetup.CreateDefaultMarkerPrefab();
            }
            if (destinationMarkerPrefab == null && _fallbackPrefabSetup != null)
            {
                destinationMarkerPrefab = _fallbackPrefabSetup.CreateDefaultDestinationMarkerPrefab();
            }
        }
    }
}
