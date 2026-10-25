using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SmartClassroomAR.AR
{
    /// <summary>
    /// Supported navigation modes: Real AR with camera tracking or Demo Navigation Mode for non-AR devices.
    /// </summary>
    public enum NavigationMode
    {
        RealAR,
        DemoNavigation
    }

    /// <summary>
    /// Controls the AR session lifecycle, monitors tracking state changes,
    /// verifies AR device capability on startup, and exposes tracking status.
    /// Implements graceful fallback to Demo Navigation Mode on non-ARCore devices.
    /// Compatible with Unity 2022.3 LTS and AR Foundation 5.x.
    /// </summary>
    public class ARSessionController : MonoBehaviour
    {
        [Header("AR Foundation References")]
        [Tooltip("Reference to the scene ARSession component.")]
        [SerializeField] private ARSession arSession;

        [Tooltip("Reference to the scene ARSessionOrigin component.")]
        [SerializeField] private ARSessionOrigin arSessionOrigin;

        /// <summary>
        /// Event invoked whenever AR tracking state transitions between NotTracking, Limited, or Tracking.
        /// </summary>
        public event Action<TrackingState> OnTrackingStateChanged;

        /// <summary>
        /// Event invoked whenever the detailed AR diagnostic status message changes.
        /// </summary>
        public event Action<string> OnStatusMessageChanged;

        /// <summary>
        /// Event invoked when navigation mode transitions between RealAR and DemoNavigation.
        /// </summary>
        public event Action<NavigationMode> OnNavigationModeChanged;

        /// <summary>
        /// Event invoked when AR availability determination completes.
        /// </summary>
        public event Action<bool> OnARAvailabilityDetermined;

        /// <summary>
        /// Current active navigation mode (RealAR or DemoNavigation).
        /// </summary>
        public NavigationMode CurrentMode { get; private set; } = NavigationMode.RealAR;

        /// <summary>
        /// True if the hardware device supports ARCore and AR session can run.
        /// </summary>
        public bool IsARSupported { get; private set; } = false;

        /// <summary>
        /// True when the runtime AR availability check has completed.
        /// </summary>
        public bool IsAvailabilityCheckComplete { get; private set; } = false;

        /// <summary>
        /// Current tracking state of the AR device session.
        /// </summary>
        public TrackingState CurrentTrackingState { get; private set; } = TrackingState.None;

        /// <summary>
        /// True if the AR session is actively tracking the environment.
        /// </summary>
        public bool IsTracking => CurrentTrackingState == TrackingState.Tracking;

        /// <summary>
        /// Human-readable diagnostic message describing the current AR session status.
        /// </summary>
        public string StatusMessage { get; private set; } = "Initializing AR session...";

        /// <summary>
        /// True if device camera permission has been confirmed.
        /// </summary>
        public bool HasCameraPermission
        {
            get
            {
#if UNITY_ANDROID
                return UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera);
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// Returns the current raw AR Foundation session state.
        /// </summary>
        public ARSessionState SessionState => ARSession.state;

        /// <summary>
        /// Public access to ARSession component.
        /// </summary>
        public ARSession Session => arSession;

        /// <summary>
        /// Public access to ARSessionOrigin component.
        /// </summary>
        public ARSessionOrigin SessionOrigin => arSessionOrigin;

        private Coroutine _availabilityCheckCoroutine;

        private void Awake()
        {
            if (arSession == null)
            {
                arSession = FindObjectOfType<ARSession>();
            }

            if (arSessionOrigin == null)
            {
                arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
            }

            _availabilityCheckCoroutine = StartCoroutine(CheckARAvailabilityRoutine());
        }

        private void OnEnable()
        {
            ARSession.stateChanged += HandleARSessionStateChanged;
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= HandleARSessionStateChanged;
        }

        private void OnDestroy()
        {
            if (_availabilityCheckCoroutine != null)
            {
                StopCoroutine(_availabilityCheckCoroutine);
                _availabilityCheckCoroutine = null;
            }
            ARSession.stateChanged -= HandleARSessionStateChanged;
        }

        /// <summary>
        /// Checks runtime hardware capability, ARCore services, and camera permissions.
        /// Automatically falls back to Demo Navigation Mode on unsupported devices.
        /// </summary>
        private IEnumerator CheckARAvailabilityRoutine()
        {
#if UNITY_WEBGL || UNITY_EDITOR
            ConfigureForDemoMode("WebGL/Editor 3D Interactive Navigation Mode.");
            yield break;
#else
            SetStatusMessage("Checking AR subsystem compatibility...");

            // Check ARCore hardware availability with a safety timeout
            IEnumerator availabilityCheck = null;
            try
            {
                availabilityCheck = ARSession.CheckAvailability();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ARSessionController] CheckAvailability threw: {ex.Message}. Falling back to Demo Mode.");
                ConfigureForDemoMode("AR initialization exception. Demo Navigation Mode active.");
                yield break;
            }

            if (availabilityCheck != null)
            {
                float timeout = 4.0f;
                while (availabilityCheck.MoveNext() && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // Evaluate ARCore availability state
            if (ARSession.state == ARSessionState.Unsupported)
            {
                Debug.LogWarning("[ARSessionController] Device hardware does not support ARCore. Activating Demo Navigation Mode.");
                ConfigureForDemoMode("AR unavailable — Demo Navigation Mode.");
                yield break;
            }
            else if (ARSession.state == ARSessionState.NeedsInstall)
            {
                Debug.LogWarning("[ARSessionController] Google Play Services for AR (ARCore) not installed. Activating Demo Navigation Mode.");
                ConfigureForDemoMode("ARCore not installed — Demo Navigation Mode.");
                yield break;
            }
            else if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            {
                // Timeout or undetermined subsystem in Editor/emulator
                Debug.LogWarning("[ARSessionController] AR availability check timed out or subsystem undetermined. Activating Demo Navigation Mode.");
                ConfigureForDemoMode("AR unavailable — Demo Navigation Mode.");
                yield break;
            }

            // If hardware supports ARCore, check and request camera permission
            SetStatusMessage("Checking camera permission...");

#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                Debug.Log("[ARSessionController] Requesting Android CAMERA permission...");
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);

                float permTimeout = 4.0f;
                while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera) && permTimeout > 0f)
                {
                    permTimeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                string permMsg = "Camera permission denied — Demo Navigation Mode active.";
                Debug.LogWarning("[ARSessionController] " + permMsg);
                ConfigureForDemoMode(permMsg);
                yield break;
            }
            else
            {
                Debug.Log("[ARSessionController] Camera permission granted.");
            }
#endif

            // ARCore supported and permissions granted
            ConfigureForRealAR("ARCore supported. Starting camera tracking session...");
#endif
        }

        /// <summary>
        /// Configures the controller for Demo Navigation Mode (non-ARCore fallback).
        /// Disables native ARSession and camera background to eliminate black screens.
        /// </summary>
        public void ConfigureForDemoMode(string message = "AR unavailable — Demo Navigation Mode.")
        {
            CurrentMode = NavigationMode.DemoNavigation;
            IsARSupported = false;
            IsAvailabilityCheckComplete = true;

            // Disable ARSession to prevent native ARCore loop errors on unsupported devices
            if (arSession != null)
            {
                arSession.enabled = false;
            }

            // Disable ARCameraBackground to prevent black render quads
            var camBackground = FindObjectOfType<ARCameraBackground>();
            if (camBackground != null)
            {
                camBackground.enabled = false;
            }

            // Set main camera to a clean solid daylight azure sky background
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.50f, 0.68f, 0.86f, 1f); // Daylight azure sky #80ADE0
            }

            UpdateTrackingState(TrackingState.None, message);
            OnARAvailabilityDetermined?.Invoke(false);
            OnNavigationModeChanged?.Invoke(CurrentMode);
            Debug.Log($"[ARSessionController] Activated Demo Navigation Mode. {message}");
        }

        /// <summary>
        /// Configures the controller for Real AR Navigation on supported devices.
        /// </summary>
        public void ConfigureForRealAR(string message = "ARCore supported. Camera tracking active.")
        {
            CurrentMode = NavigationMode.RealAR;
            IsARSupported = true;
            IsAvailabilityCheckComplete = true;

            if (arSession != null)
            {
                arSession.enabled = true;
            }

            var camBackground = FindObjectOfType<ARCameraBackground>();
            if (camBackground != null)
            {
                camBackground.enabled = true;
            }

            UpdateTrackingState(TrackingState.Limited, message);
            OnARAvailabilityDetermined?.Invoke(true);
            OnNavigationModeChanged?.Invoke(CurrentMode);
            Debug.Log($"[ARSessionController] Activated Real AR Navigation Mode. {message}");
        }

        /// <summary>
        /// Switches navigation mode dynamically (e.g. for testing, toggle, or fallback).
        /// </summary>
        public void SetNavigationMode(NavigationMode mode)
        {
            if (mode == NavigationMode.DemoNavigation)
            {
                ConfigureForDemoMode("Switched to Demo Navigation Mode.");
            }
            else
            {
                ConfigureForRealAR("Switched to Real AR Navigation Mode.");
            }
        }

        /// <summary>
        /// Simulates the outcome of an availability check for unit and integration testing.
        /// </summary>
        public void SimulateAvailabilityCheck(bool supported)
        {
            if (supported)
            {
                ConfigureForRealAR("Simulated ARCore supported.");
            }
            else
            {
                ConfigureForDemoMode("AR unavailable — Demo Navigation Mode.");
            }
        }

        /// <summary>
        /// Callback triggered by AR Foundation when session state transitions.
        /// </summary>
        private void HandleARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            if (CurrentMode == NavigationMode.DemoNavigation)
            {
                return;
            }

            TrackingState calculatedState = TrackingState.None;
            string message = string.Empty;

            switch (args.state)
            {
                case ARSessionState.None:
                    calculatedState = TrackingState.None;
                    message = "AR subsystem idle.";
                    break;

                case ARSessionState.Unsupported:
                    calculatedState = TrackingState.None;
                    ConfigureForDemoMode("Device does not support ARCore. Demo Navigation Mode active.");
                    return;

                case ARSessionState.CheckingAvailability:
                    calculatedState = TrackingState.None;
                    message = "Checking device AR availability...";
                    break;

                case ARSessionState.NeedsInstall:
                case ARSessionState.Installing:
                    calculatedState = TrackingState.None;
                    ConfigureForDemoMode("ARCore not installed. Demo Navigation Mode active.");
                    return;

                case ARSessionState.Ready:
                    calculatedState = TrackingState.Limited;
                    message = "AR subsystem ready. Starting camera stream...";
                    break;

                case ARSessionState.SessionInitializing:
                    calculatedState = TrackingState.Limited;
                    message = "Initializing camera feed... Point phone at floor or corridor.";
                    break;

                case ARSessionState.SessionTracking:
                    calculatedState = TrackingState.Tracking;
                    message = "AR tracking active.";
                    break;

                default:
                    calculatedState = TrackingState.None;
                    message = $"AR session state: {args.state}";
                    break;
            }

            if (calculatedState != TrackingState.Tracking && ARSession.notTrackingReason != NotTrackingReason.None)
            {
                switch (ARSession.notTrackingReason)
                {
                    case NotTrackingReason.Initializing:
                        message += " (Initializing sensors...)";
                        break;
                    case NotTrackingReason.ExcessiveMotion:
                        message += " (Moving too fast - slow down)";
                        break;
                    case NotTrackingReason.InsufficientFeatures:
                        message += " (Point at textured floor/wall)";
                        break;
                    case NotTrackingReason.InsufficientLight:
                        message += " (Low lighting conditions)";
                        break;
                    case NotTrackingReason.Relocalizing:
                        message += " (Relocalizing...)";
                        break;
                    default:
                        message += $" ({ARSession.notTrackingReason})";
                        break;
                }
            }

            UpdateTrackingState(calculatedState, message);
        }

        private void SetStatusMessage(string message)
        {
            StatusMessage = message;
            OnStatusMessageChanged?.Invoke(message);
        }

        /// <summary>
        /// Updates the internal tracking state and status message, firing events if state changed.
        /// </summary>
        private void UpdateTrackingState(TrackingState newState, string message)
        {
            StatusMessage = message;
            OnStatusMessageChanged?.Invoke(message);

            if (CurrentTrackingState != newState)
            {
                CurrentTrackingState = newState;
                Debug.Log($"[ARSessionController] Tracking state changed to: {newState}. {message}");
                OnTrackingStateChanged?.Invoke(newState);
            }
        }

        /// <summary>
        /// Resets the current AR session tracking data and re-establishes world origin.
        /// </summary>
        public void ResetSession()
        {
            if (CurrentMode == NavigationMode.DemoNavigation)
            {
                UpdateTrackingState(TrackingState.None, "Demo navigation active.");
                return;
            }

            if (arSession != null)
            {
                Debug.Log("[ARSessionController] Resetting AR session tracking origin.");
                arSession.Reset();
                UpdateTrackingState(TrackingState.Limited, "AR session reset. Re-establishing world origin...");
            }
            else
            {
                Debug.LogWarning("[ARSessionController] Cannot reset AR session: ARSession reference is null.");
            }
        }
    }
}
