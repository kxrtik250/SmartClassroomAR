using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartClassroomAR.AR
{
    /// <summary>
    /// Represents a recorded landmark with known building floorplan coordinates
    /// and corresponding measured AR Foundation session world coordinates.
    /// </summary>
    [Serializable]
    public class CalibrationPoint
    {
        public Vector3 buildingPosition;
        public Vector3 arPosition;
        public string label;

        public CalibrationPoint()
        {
            buildingPosition = Vector3.zero;
            arPosition = Vector3.zero;
            label = string.Empty;
        }

        public CalibrationPoint(Vector3 buildingPosition, Vector3 arPosition, string label)
        {
            this.buildingPosition = buildingPosition;
            this.arPosition = arPosition;
            this.label = label ?? string.Empty;
        }
    }

    /// <summary>
    /// Computes translation offset and scale factor between architectural building coordinates
    /// and Unity AR Foundation session coordinates.
    /// 
    /// PHYSICAL MEASUREMENT REQUIREMENT:
    /// In an actual physical building deployment, calibration cannot be guessed. A minimum of two
    /// surveyed reference landmarks (e.g., entrance doorway, corner pillar, elevator shaft door)
    /// must have their architectural (X, Y, Z) coordinates surveyed against floor blueprints, and
    /// their physical AR positions recorded when the AR device is stationed at those exact locations.
    /// </summary>
    public class CoordinateCalibrator : MonoBehaviour
    {
        private const string PrefsKeyOffsetX = "AR_Calibration_OffsetX";
        private const string PrefsKeyOffsetY = "AR_Calibration_OffsetY";
        private const string PrefsKeyOffsetZ = "AR_Calibration_OffsetZ";
        private const string PrefsKeyScale = "AR_Calibration_Scale";
        private const string PrefsKeyIsCalibrated = "AR_Calibration_IsCalibrated";

        [Header("Surveyed Calibration Landmarks")]
        [SerializeField] private List<CalibrationPoint> calibrationPoints = new List<CalibrationPoint>();

        [Header("Current Transform Parameters")]
        [SerializeField] private Vector3 offset = Vector3.zero;
        [SerializeField] private float scale = 1.0f;
        [SerializeField] private bool isCalibrated = false;

        public Vector3 Offset => offset;
        public float Scale => scale;
        public bool IsCalibrated => isCalibrated;
        public IReadOnlyList<CalibrationPoint> Points => calibrationPoints;

        private void Awake()
        {
            LoadCalibration();
        }

        /// <summary>
        /// Adds a physical calibration survey point.
        /// </summary>
        public void AddCalibrationPoint(Vector3 buildingPos, Vector3 arPos, string label)
        {
            calibrationPoints.Add(new CalibrationPoint(buildingPos, arPos, label));
            Debug.Log($"[CoordinateCalibrator] Added calibration point '{label}': Building={buildingPos}, AR={arPos}");
        }

        /// <summary>
        /// Calculates scale and translation offset from recorded calibration points.
        /// Requires at least 2 distinct points to determine scale.
        /// </summary>
        public bool CalculateCalibration()
        {
            if (calibrationPoints == null || calibrationPoints.Count < 2)
            {
                Debug.LogWarning("[CoordinateCalibrator] At least 2 calibration points are required to calculate AR transformation.");
                return false;
            }

            // Calculate centroids for building and AR positions
            Vector3 centroidBuilding = Vector3.zero;
            Vector3 centroidAR = Vector3.zero;

            for (int i = 0; i < calibrationPoints.Count; i++)
            {
                centroidBuilding += calibrationPoints[i].buildingPosition;
                centroidAR += calibrationPoints[i].arPosition;
            }

            centroidBuilding /= calibrationPoints.Count;
            centroidAR /= calibrationPoints.Count;

            // Compute scale by averaging distance ratios between all pairs
            float totalRatio = 0f;
            int validPairs = 0;

            for (int i = 0; i < calibrationPoints.Count; i++)
            {
                for (int j = i + 1; j < calibrationPoints.Count; j++)
                {
                    float distBuilding = Vector3.Distance(calibrationPoints[i].buildingPosition, calibrationPoints[j].buildingPosition);
                    float distAR = Vector3.Distance(calibrationPoints[i].arPosition, calibrationPoints[j].arPosition);

                    if (distBuilding > 0.001f)
                    {
                        totalRatio += (distAR / distBuilding);
                        validPairs++;
                    }
                }
            }

            if (validPairs > 0)
            {
                scale = totalRatio / validPairs;
            }
            else
            {
                scale = 1.0f;
                Debug.LogWarning("[CoordinateCalibrator] Unable to compute valid distance ratio between calibration points. Defaulting scale to 1.0.");
            }

            // Offset translates scaled building centroid to AR session centroid
            offset = centroidAR - (centroidBuilding * scale);
            isCalibrated = true;

            Debug.Log($"[CoordinateCalibrator] Calibration successful. Scale={scale:F4}, Offset={offset}. Verified across {validPairs} landmark pairs.");
            SaveCalibration();
            return true;
        }

        /// <summary>
        /// Transforms a building floorplan position into the AR session coordinate space.
        /// </summary>
        public Vector3 TransformBuildingToAR(Vector3 buildingPos)
        {
            return (buildingPos * scale) + offset;
        }

        /// <summary>
        /// Persists the active calibration parameters to PlayerPrefs.
        /// </summary>
        public void SaveCalibration()
        {
            PlayerPrefs.SetFloat(PrefsKeyOffsetX, offset.x);
            PlayerPrefs.SetFloat(PrefsKeyOffsetY, offset.y);
            PlayerPrefs.SetFloat(PrefsKeyOffsetZ, offset.z);
            PlayerPrefs.SetFloat(PrefsKeyScale, scale);
            PlayerPrefs.SetInt(PrefsKeyIsCalibrated, isCalibrated ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log("[CoordinateCalibrator] Calibration parameters saved to persistent storage.");
        }

        /// <summary>
        /// Restores saved calibration parameters from PlayerPrefs if available.
        /// </summary>
        public void LoadCalibration()
        {
            if (PlayerPrefs.HasKey(PrefsKeyIsCalibrated) && PlayerPrefs.GetInt(PrefsKeyIsCalibrated) == 1)
            {
                float ox = PlayerPrefs.GetFloat(PrefsKeyOffsetX, 0f);
                float oy = PlayerPrefs.GetFloat(PrefsKeyOffsetY, 0f);
                float oz = PlayerPrefs.GetFloat(PrefsKeyOffsetZ, 0f);
                offset = new Vector3(ox, oy, oz);
                scale = PlayerPrefs.GetFloat(PrefsKeyScale, 1.0f);
                isCalibrated = true;
                Debug.Log($"[CoordinateCalibrator] Loaded existing calibration: Scale={scale:F4}, Offset={offset}");
            }
            else
            {
                isCalibrated = false;
            }
        }

        /// <summary>
        /// Configures a 1:1 identity transformation (offset = 0, scale = 1) for desktop simulation and debugging.
        /// </summary>
        public void DemoCalibration()
        {
            offset = Vector3.zero;
            scale = 1.0f;
            isCalibrated = true;

            Debug.LogWarning("[CoordinateCalibrator] DEMO CALIBRATION APPLIED: Building coordinates mapped directly 1:1 to AR coordinates. " +
                             "NOTE: Real physical deployment strictly requires accurate on-site architectural measurements.");
        }
    }
}
