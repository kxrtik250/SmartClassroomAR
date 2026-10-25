using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using SmartClassroomAR.AR;
using SmartClassroomAR.UI;
using SmartClassroomAR.Evaluation;
using SmartClassroomAR.Visualization;

namespace SmartClassroomAR.Editor
{
    /// <summary>
    /// Editor utility for constructing and configuring the complete AR navigation MainScene.
    /// Can be executed from the Unity Editor menu or via batchmode command line:
    /// -executeMethod SmartClassroomAR.Editor.SceneSetup.BuildMainScene
    /// </summary>
    public static class SceneSetup
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("SmartClassroomAR/Configure XR and Graphics", priority = 5)]
        public static void ConfigureXRAndGraphics()
        {
            Debug.Log("[SceneSetup] Configuring Android Graphics APIs (OpenGLES3 only for ARCore Built-in Render Pipeline)...");
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new GraphicsDeviceType[] {
                GraphicsDeviceType.OpenGLES3
            });

            Debug.Log("[SceneSetup] Configuring XR General Settings for Android (Assigning ARCoreLoader)...");
            XRGeneralSettingsPerBuildTarget generalSettings = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out generalSettings);
            if (generalSettings == null)
            {
                var assets = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
                if (assets.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(assets[0]);
                    generalSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                }
            }
            if (generalSettings == null)
            {
                generalSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                if (!Directory.Exists("Assets/XR")) Directory.CreateDirectory("Assets/XR");
                AssetDatabase.CreateAsset(generalSettings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, generalSettings, true);
                AssetDatabase.SaveAssets();
            }

            if (!generalSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            {
                generalSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            }
            if (!generalSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            {
                generalSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            }
            var manager = generalSettings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            if (manager != null)
            {
                if (!XRPackageMetadataStore.IsLoaderAssigned("UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android))
                {
                    bool assigned = XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android);
                    Debug.Log($"[SceneSetup] ARCoreLoader assigned to Android: {assigned}");
                }
                else
                {
                    Debug.Log("[SceneSetup] ARCoreLoader is already assigned to Android.");
                }
            }

            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, generalSettings, true);
            EditorUtility.SetDirty(generalSettings);
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneSetup] XR and Graphics configuration completed successfully.");
        }

        [MenuItem("SmartClassroomAR/Build Android APK", priority = 20)]
        public static void BuildAndroidApk()
        {
            Debug.Log("[SceneSetup] Configuring XR and graphics before Android APK build...");
            ConfigureXRAndGraphics();

            Debug.Log("[SceneSetup] Starting Android APK build...");
            string outputDir = "Builds/Android";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            string apkPath = Path.Combine(outputDir, "SmartClassroomAR.apk");

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[SceneSetup] Android Build succeeded: {summary.totalSize} bytes at {apkPath}");
            }
            else
            {
                Debug.LogError($"[SceneSetup] Android Build failed with result: {summary.result}. Total errors: {summary.totalErrors}");
            }
        }

        [MenuItem("SmartClassroomAR/Build Android APK (Demo Fallback)", priority = 21)]
        public static void BuildAndroidApkDemoFallback()
        {
            Debug.Log("[SceneSetup] Configuring XR and graphics before Android APK build (Demo Fallback)...");
            ConfigureXRAndGraphics();

            Debug.Log("[SceneSetup] Starting Android APK build for SmartClassroomAR_DemoFallback.apk...");
            string outputDir = "Builds/Android";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            string apkPath = Path.Combine(outputDir, "SmartClassroomAR_DemoFallback.apk");

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[SceneSetup] Android Build (Demo Fallback) succeeded: {summary.totalSize} bytes at {apkPath}");
            }
            else
            {
                Debug.LogError($"[SceneSetup] Android Build (Demo Fallback) failed with result: {summary.result}. Total errors: {summary.totalErrors}");
            }
        }

        [MenuItem("SmartClassroomAR/Configure Android Compatibility Settings", priority = 6)]
        public static void ConfigureAndroidCompatibility()
        {
            Debug.Log("[SceneSetup] Configuring Android Compatibility Settings (Min API 28, Target API 34)...");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
            PlayerSettings.bundleVersion = "1.0.1";
            PlayerSettings.Android.bundleVersionCode = 2;
            Debug.Log($"[SceneSetup] Configured: MinSDK={PlayerSettings.Android.minSdkVersion}, TargetSDK={PlayerSettings.Android.targetSdkVersion}, Version={PlayerSettings.bundleVersion} (Code: {PlayerSettings.Android.bundleVersionCode})");
        }

        [MenuItem("SmartClassroomAR/Build Android APK (Compatibility Fix)", priority = 22)]
        public static void BuildAndroidApkCompatibilityFix()
        {
            Debug.Log("[SceneSetup] Configuring XR, graphics, and Android target API level 34...");
            ConfigureXRAndGraphics();
            ConfigureAndroidCompatibility();

            Debug.Log("[SceneSetup] Starting Android APK build for SmartClassroomAR_CompatibilityFix.apk...");
            string outputDir = "Builds/Android";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            string apkPath = Path.Combine(outputDir, "SmartClassroomAR_CompatibilityFix.apk");

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[SceneSetup] Android Build (Compatibility Fix) succeeded: {summary.totalSize} bytes at {apkPath}");
            }
            else
            {
                Debug.LogError($"[SceneSetup] Android Build (Compatibility Fix) failed with result: {summary.result}. Total errors: {summary.totalErrors}");
            }
        }

        [MenuItem("SmartClassroomAR/Build WebGL", priority = 25)]
        public static void BuildWebGL()
        {
            Debug.Log("[SceneSetup] Starting automated WebGL application build...");
            string outputDir = "Builds/WebGL/SmartClassroomAR_WebGL";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[SceneSetup] WebGL Build succeeded: {summary.totalSize} bytes at {outputDir}");
                PostProcessWebGlOutput(outputDir);
            }
            else
            {
                Debug.LogError($"[SceneSetup] WebGL Build failed with result: {summary.result}. Total errors: {summary.totalErrors}");
            }
        }

        private static void PostProcessWebGlOutput(string outputDir)
        {
            try
            {
                string indexPath = Path.Combine(outputDir, "index.html");
                if (File.Exists(indexPath))
                {
                    string html = File.ReadAllText(indexPath);
                    html = html.Replace("canvas.style.width = \"1280px\";", "canvas.style.width = \"100%\";");
                    html = html.Replace("canvas.style.height = \"720px\";", "canvas.style.height = \"100%\";");
                    html = html.Replace("canvas.style.width = \"1920px\";", "canvas.style.width = \"100%\";");
                    html = html.Replace("canvas.style.height = \"1080px\";", "canvas.style.height = \"100%\";");
                    html = html.Replace("}).then((unityInstance) => {", "}).then((unityInstance) => { window.unityInstance = unityInstance;");
                    File.WriteAllText(indexPath, html);
                }

                string cssPath = Path.Combine(outputDir, "TemplateData", "style.css");
                if (File.Exists(cssPath))
                {
                    // Premium dark navy design system
                    string responsiveCss = @"
/* ── SmartClassroomAR Premium WebGL Theme ── */
*,*::before,*::after{box-sizing:border-box}
html,body{width:100%;height:100%;padding:0;margin:0;overflow:hidden;
  background-color:#080c14;
  font-family:'Inter',-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;
  -webkit-font-smoothing:antialiased;-moz-osx-font-smoothing:grayscale;color:#e2e8f0}
#unity-container{width:100%;height:100%;position:absolute;left:0;top:0}
#unity-container.unity-desktop{width:100%;height:100%;left:0;top:0;transform:none}
#unity-container.unity-mobile{position:fixed;width:100%;height:100%}
#unity-canvas{width:100%!important;height:100%!important;display:block;background:#080c14}
.unity-mobile #unity-canvas{width:100%;height:100%}
#unity-loading-bar{position:absolute;left:50%;top:50%;transform:translate(-50%,-50%);display:none;z-index:20}
#unity-logo{width:154px;height:130px;background:url('unity-logo-dark.png') no-repeat center}
#unity-progress-bar-empty{width:220px;height:6px;margin-top:16px;border-radius:3px;background:rgba(30,41,59,0.8);border:1px solid rgba(51,65,85,0.5)}
#unity-progress-bar-full{height:6px;border-radius:3px;background:linear-gradient(90deg,#0ea5e9,#38bdf8);box-shadow:0 0 12px rgba(14,165,233,0.4);transition:width 0.3s ease}
#unity-footer{position:absolute;bottom:0;left:0;right:0;z-index:10;
  background:linear-gradient(180deg,transparent,rgba(8,12,20,0.85));
  padding:12px 20px;display:flex;align-items:center;justify-content:flex-end;gap:12px}
.unity-mobile #unity-footer{display:none}
#unity-webgl-logo{width:80px;height:24px;background:url('webgl-logo.png') no-repeat center;background-size:contain;opacity:0.5}
#unity-build-title{line-height:24px;font-size:12px;color:#64748b;letter-spacing:0.02em}
#unity-fullscreen-button{cursor:pointer;width:28px;height:28px;background:url('fullscreen-button.png') no-repeat center;background-size:contain;opacity:0.6;transition:opacity 0.2s}
#unity-fullscreen-button:hover{opacity:1}
#unity-warning{position:absolute;left:50%;top:5%;transform:translate(-50%);background:#1e293b;color:#f1f5f9;padding:16px 24px;border-radius:8px;border:1px solid #334155;display:none;font-size:14px}
";
                    File.WriteAllText(cssPath, responsiveCss);
                }
                Debug.Log("[SceneSetup] Post-processed WebGL HTML and CSS for responsive desktop presentation.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SceneSetup] Note: WebGL post-process warning: {ex.Message}");
            }
        }

        [MenuItem("SmartClassroomAR/Run Navigation Tests", priority = 30)]
        public static void RunNavigationTests()
        {
            Debug.Log("[SceneSetup] Running navigation unit and integration tests...");
            var tests = new SmartClassroomAR.Tests.NavigationTests();
            string results = tests.RunAllTests();
            Debug.Log($"[NavigationTestsResult]\n{results}");
        }

        [MenuItem("SmartClassroomAR/Diagnose Scene and Clicks", priority = 35)]
        public static void DiagnoseSceneAndClicks()
        {
            Debug.Log("=== DIAGNOSTIC: INSPECTING MAINSCENE AND CLICK WORKFLOW ===");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var navManager = GameObject.Find("Navigation System");
            if (navManager == null)
            {
                Debug.LogError("DIAG: 'Navigation System' not found in MainScene!");
                return;
            }
            var navUI = navManager.GetComponent<NavigationUIController>();
            var session = navManager.GetComponent<ARSessionController>();
            var canvas = GameObject.Find("Canvas");
            var eventSystem = GameObject.Find("EventSystem");
            Debug.Log($"DIAG: navUI found: {navUI != null}, session found: {session != null}, canvas found: {canvas != null}, eventSystem found: {eventSystem != null}");

            if (eventSystem != null)
            {
                var comps = eventSystem.GetComponents<Component>();
                foreach (var c in comps)
                {
                    Debug.Log($"DIAG: EventSystem component: {c.GetType().FullName}");
                }
            }

            var awakeMethod = typeof(NavigationUIController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            var onEnableMethod = typeof(NavigationUIController).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            var startMethod = typeof(NavigationUIController).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

            awakeMethod?.Invoke(navUI, null);
            onEnableMethod?.Invoke(navUI, null);
            startMethod?.Invoke(navUI, null);

            Debug.Log($"DIAG: Post-Start navUI State: {navUI.CurrentState}, Diagnostics: '{navUI.CurrentDiagnosticsMessage}'");

            var destUI = UnityEngine.Object.FindObjectOfType<DestinationSelectionUI>();
            Debug.Log($"DIAG: destUI found: {destUI != null}");

            var buttons = UnityEngine.Object.FindObjectsOfType<Button>(true);
            Debug.Log($"DIAG: Total Button components in scene: {buttons.Length}");
            foreach (var b in buttons)
            {
                var txt = b.GetComponentInChildren<TMP_Text>();
                string bName = txt != null ? txt.text.Replace("\n", " ") : b.gameObject.name;
                Debug.Log($"DIAG: Button '{b.gameObject.name}' (parent: {b.transform.parent?.name}, activeInHierarchy: {b.gameObject.activeInHierarchy}, interactable: {b.interactable}, label: '{bName}')");
            }

            Button r101Btn = null;
            foreach (var b in buttons)
            {
                var txt = b.GetComponentInChildren<TMP_Text>();
                if (txt != null && txt.text.Contains("Room 101"))
                {
                    r101Btn = b;
                    break;
                }
            }

            if (r101Btn != null)
            {
                Debug.Log($"DIAG: Found Room 101 button! Invoking onClick...");
                r101Btn.onClick.Invoke();
                Debug.Log($"DIAG: Post-Click navUI State: {navUI.CurrentState}, ActiveDest: '{navUI.ActiveDestination?.destinationName}', Diagnostics: '{navUI.CurrentDiagnosticsMessage}'");
            }
            else
            {
                Debug.LogError("DIAG: Room 101 button NOT found among buttons!");
            }
        }

        [MenuItem("SmartClassroomAR/Generate Main Scene", priority = 10)]
        public static void BuildMainScene()
        {
            Debug.Log("[SceneSetup] Starting automated generation of MainScene...");
            ConfigureXRAndGraphics();

            // Load default TMP Font Asset
            var defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (defaultFont == null)
            {
                defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
            if (defaultFont != null)
            {
                Debug.Log($"[SceneSetup] Loaded default font asset: {defaultFont.name}");
            }
            else
            {
                Debug.LogWarning("[SceneSetup] Default font asset could not be loaded from disk or resources!");
            }

            // Ensure directory exists
            string dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Create new empty scene
            // Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Sunlight Rig (Warm Key + Sky Fill) aligned with Celestial Sun position
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.18f;
            light.color = new Color(1.0f, 0.98f, 0.92f);
            lightObj.transform.rotation = Quaternion.Euler(46.5f, -52.2f, 0f);

            var fillLightObj = new GameObject("Fill Light");
            var fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.36f;
            fillLight.color = new Color(0.74f, 0.84f, 0.96f);
            fillLightObj.transform.rotation = Quaternion.Euler(-25f, 130f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.50f, 0.54f, 0.58f);

            // Atmospheric Fog for Campus Horizon Depth
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.92f, 1f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 260f;

            // 2. AR Session
            var arSessionObj = new GameObject("AR Session");
            var arSession = arSessionObj.AddComponent<ARSession>();
            arSession.attemptUpdate = true;
            arSession.matchFrameRate = true;
            arSessionObj.AddComponent<ARInputManager>();

            // 3. AR Session Origin & AR Camera
            var originObj = new GameObject("AR Session Origin");
            var arOrigin = originObj.AddComponent<ARSessionOrigin>();

            var cameraObj = new GameObject("AR Camera");
            cameraObj.transform.SetParent(originObj.transform, false);
            cameraObj.tag = "MainCamera";

            var cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.50f, 0.68f, 0.86f, 1f); // Daylight azure sky #80ADE0
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 350f;
            cam.fieldOfView = 50f; // Balanced architectural isometric field of view

            cameraObj.AddComponent<AudioListener>();
            cameraObj.AddComponent<ARCameraManager>();
            cameraObj.AddComponent<ARCameraBackground>();

            // Configure TrackedPoseDriver for handheld AR pose tracking
            var trackedPoseDriver = cameraObj.AddComponent<TrackedPoseDriver>();
            var positionAction = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
            positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");
            var rotationAction = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
            rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
            trackedPoseDriver.positionInput = new InputActionProperty(positionAction);
            trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);

            // Set camera on origin
            var arOriginSerialized = new SerializedObject(arOrigin);
            var camProp = arOriginSerialized.FindProperty("m_Camera");
            if (camProp != null)
            {
                camProp.objectReferenceValue = cam;
                arOriginSerialized.ApplyModifiedProperties();
            }

            // 4. Navigation System (Managers)
            var navManagerObj = new GameObject("Navigation System");
            var navUIController = navManagerObj.AddComponent<NavigationUIController>();
            var sessionController = navManagerObj.AddComponent<ARSessionController>();
            var navRenderer = navManagerObj.AddComponent<ARNavigationRenderer>();
            var calibrator = navManagerObj.AddComponent<CoordinateCalibrator>();
            var prefabSetup = navManagerObj.AddComponent<ARPrefabSetup>();
            var buildingVis = navManagerObj.AddComponent<BuildingVisualizer>();
            var routeVis = navManagerObj.AddComponent<RouteVisualizer>();
            var orbitCam = cameraObj.AddComponent<OrbitCamera>();
            orbitCam.SetBuildingVisualizer(buildingVis);

            // Wire session controller references
            var sessionSerialized = new SerializedObject(sessionController);
            sessionSerialized.FindProperty("arSession").objectReferenceValue = arSession;
            sessionSerialized.FindProperty("arSessionOrigin").objectReferenceValue = arOrigin;
            sessionSerialized.ApplyModifiedProperties();

            // Wire renderer origin reference
            var rendererSerialized = new SerializedObject(navRenderer);
            rendererSerialized.FindProperty("arSessionOrigin").objectReferenceValue = originObj.transform;
            rendererSerialized.ApplyModifiedProperties();

            // 5. Canvas UI Setup (Configured for Desktop Landscape 1920x1080)
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 5A. Top Status Panel & Professional Project Branding Header
            var statusPanelObj = CreateUIPanel(canvasObj.transform, "StatusPanel", new Vector2(0f, 0.915f), new Vector2(1f, 1f));
            SetPanelBackground(statusPanelObj, new Color(0.035f, 0.055f, 0.095f, 0.98f));

            // Left Branding Section (Permanent project identity)
            var brandContainer = CreateUIPanel(statusPanelObj.transform, "HeaderBrand", new Vector2(0.015f, 0.05f), new Vector2(0.36f, 0.95f));
            SetPanelBackground(brandContainer, Color.clear);

            var projectTitleText = CreateTextMeshPro(brandContainer.transform, "ProjectTitle", "SMART CLASSROOM AR", 20, TextAlignmentOptions.Left, defaultFont);
            projectTitleText.fontStyle = FontStyles.Bold;
            projectTitleText.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Sky blue #38BDF8
            projectTitleText.rectTransform.anchorMin = new Vector2(0f, 0.46f);
            projectTitleText.rectTransform.anchorMax = new Vector2(1f, 0.98f);
            projectTitleText.rectTransform.offsetMin = Vector2.zero;
            projectTitleText.rectTransform.offsetMax = Vector2.zero;

            var projectSubText = CreateTextMeshPro(brandContainer.transform, "ProjectSubtitle", "Sathyabama Institute of Science and Technology  |  St. Paul's Block — Computer Science & Engineering", 12, TextAlignmentOptions.Left, defaultFont);
            projectSubText.color = new Color(0.58f, 0.65f, 0.75f, 1f); // Slate #94A3B8
            projectSubText.rectTransform.anchorMin = new Vector2(0f, 0.04f);
            projectSubText.rectTransform.anchorMax = new Vector2(1f, 0.46f);
            projectSubText.rectTransform.offsetMin = Vector2.zero;
            projectSubText.rectTransform.offsetMax = Vector2.zero;

            // Center Dynamic Status & Guidance Area
            var statusText = CreateTextMeshPro(statusPanelObj.transform, "StatusText", "Select a destination to begin 3D route guidance", 16, TextAlignmentOptions.Center, defaultFont);
            statusText.fontStyle = FontStyles.Bold;
            statusText.color = new Color(0.94f, 0.96f, 1f, 1f);
            statusText.rectTransform.anchorMin = new Vector2(0.37f, 0.44f);
            statusText.rectTransform.anchorMax = new Vector2(0.76f, 0.96f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;

            var diagText = CreateTextMeshPro(statusPanelObj.transform, "DiagnosticsText", "<b>Mode:</b> SelectingDestination  •  Select any destination from the directory to begin 3D guidance  •  Left-drag to rotate view", 12, TextAlignmentOptions.Center, defaultFont);
            diagText.color = new Color(0.55f, 0.65f, 0.78f, 1f);
            diagText.rectTransform.anchorMin = new Vector2(0.37f, 0.04f);
            diagText.rectTransform.anchorMax = new Vector2(0.76f, 0.44f);
            diagText.rectTransform.offsetMin = Vector2.zero;
            diagText.rectTransform.offsetMax = Vector2.zero;

            // Right Status Indicator Pill
            var statusPillObj = CreateUIPanel(statusPanelObj.transform, "StatusPill", new Vector2(0.77f, 0.20f), new Vector2(0.985f, 0.80f));
            SetPanelBackground(statusPillObj, new Color(0.08f, 0.15f, 0.26f, 0.95f));
            var pillText = CreateTextMeshPro(statusPillObj.transform, "StatusPillText", "[●] 3D NAVIGATION ACTIVE", 13, TextAlignmentOptions.Center, defaultFont);
            pillText.fontStyle = FontStyles.Bold;
            pillText.color = new Color(0.20f, 0.83f, 0.60f, 1f); // Vibrant Emerald #34D399
            FitToParent(pillText.rectTransform, 4f, 2f);

            // Bottom Divider Line
            var dividerObj = CreateUIPanel(statusPanelObj.transform, "HeaderDivider", new Vector2(0f, 0f), new Vector2(1f, 0.025f));
            SetPanelBackground(dividerObj, new Color(0.18f, 0.35f, 0.55f, 0.5f));

            // 5B. Navigation Sidebar Container (Non-raycast container managing collapsible sidebar visibility)
            var sidebarContainer = CreateUIPanel(canvasObj.transform, "SidebarContainer", Vector2.zero, Vector2.one);
            // NOTE: Do NOT add an Image or call SetPanelBackground here! A RectTransform without Graphic components
            // never blocks raycasts, allowing full mouse orbit and dragging in the 3D viewport.

            // Destination Selection Panel (Left Sidebar: 26.5% width, leaving ~72% open for 3D building)
            var destPanelObj = CreateUIPanel(sidebarContainer.transform, "DestinationSelectionPanel", new Vector2(0.015f, 0.02f), new Vector2(0.28f, 0.895f));
            SetPanelBackground(destPanelObj, new Color(0.06f, 0.08f, 0.13f, 0.96f));

            var destUI = destPanelObj.AddComponent<DestinationSelectionUI>();
            var startSelector = destPanelObj.AddComponent<StartNodeSelector>();

            // Title
            var titleText = CreateTextMeshPro(destPanelObj.transform, "HeaderTitle", "CAMPUS DIRECTORY", 20, TextAlignmentOptions.Center, defaultFont);
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Vibrant sky blue
            titleText.rectTransform.anchorMin = new Vector2(0.04f, 0.935f);
            titleText.rectTransform.anchorMax = new Vector2(0.96f, 0.985f);
            titleText.rectTransform.offsetMin = Vector2.zero;
            titleText.rectTransform.offsetMax = Vector2.zero;

            // Start Position Label & Dropdown
            var startLabel = CreateTextMeshPro(destPanelObj.transform, "StartLabel", "Starting Point:", 14, TextAlignmentOptions.Left, defaultFont);
            startLabel.color = new Color(0.58f, 0.64f, 0.75f, 1f);
            startLabel.rectTransform.anchorMin = new Vector2(0.04f, 0.88f);
            startLabel.rectTransform.anchorMax = new Vector2(0.96f, 0.925f);
            startLabel.rectTransform.offsetMin = Vector2.zero;
            startLabel.rectTransform.offsetMax = Vector2.zero;

            var dropdownObj = CreateDropdown(destPanelObj.transform, "StartNodeDropdown", new Vector2(0.04f, 0.81f), new Vector2(0.96f, 0.875f), defaultFont);
            var startDropdown = dropdownObj.GetComponent<TMP_Dropdown>();

            // Search Bar
            var searchObj = CreateInputField(destPanelObj.transform, "SearchInputField", new Vector2(0.04f, 0.73f), new Vector2(0.96f, 0.795f), "Search classrooms, labs...", defaultFont);
            var searchInputField = searchObj.GetComponent<TMP_InputField>();

            // ScrollView for destination list
            var scrollObj = CreateScrollView(destPanelObj.transform, "DestinationScrollView", new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.715f));
            var scrollRect = scrollObj.GetComponent<ScrollRect>();
            var scrollContent = scrollRect.content;

            // Wire DestinationSelectionUI
            var destUISerialized = new SerializedObject(destUI);
            destUISerialized.FindProperty("contentArea").objectReferenceValue = scrollContent;
            destUISerialized.FindProperty("searchInputField").objectReferenceValue = searchInputField;
            destUISerialized.ApplyModifiedProperties();

            // Wire StartNodeSelector
            var startSelectorSerialized = new SerializedObject(startSelector);
            startSelectorSerialized.FindProperty("startNodeDropdown").objectReferenceValue = startDropdown;
            startSelectorSerialized.ApplyModifiedProperties();

            // 5C. Active Navigation Panel (AR Mode docked in sidebar)
            var navPanelObj = CreateUIPanel(sidebarContainer.transform, "NavigationPanel", new Vector2(0.015f, 0.02f), new Vector2(0.28f, 0.45f));
            SetPanelBackground(navPanelObj, new Color(0.06f, 0.08f, 0.13f, 0.96f));

            var destInfoText = CreateTextMeshPro(navPanelObj.transform, "DestinationInfoText", "Destination: --", 22, TextAlignmentOptions.Left, defaultFont);
            destInfoText.rectTransform.anchorMin = new Vector2(0.05f, 0.68f);
            destInfoText.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            destInfoText.rectTransform.offsetMin = Vector2.zero;
            destInfoText.rectTransform.offsetMax = Vector2.zero;

            var floorText = CreateTextMeshPro(navPanelObj.transform, "FloorIndicatorText", "Floor: --", 17, TextAlignmentOptions.Left, defaultFont);
            floorText.rectTransform.anchorMin = new Vector2(0.05f, 0.48f);
            floorText.rectTransform.anchorMax = new Vector2(0.95f, 0.68f);
            floorText.rectTransform.offsetMin = Vector2.zero;
            floorText.rectTransform.offsetMax = Vector2.zero;

            var distText = CreateTextMeshPro(navPanelObj.transform, "DistanceText", "Distance: --", 18, TextAlignmentOptions.Left, defaultFont);
            distText.rectTransform.anchorMin = new Vector2(0.05f, 0.28f);
            distText.rectTransform.anchorMax = new Vector2(0.95f, 0.48f);
            distText.rectTransform.offsetMin = Vector2.zero;
            distText.rectTransform.offsetMax = Vector2.zero;

            var cancelBtnObj = CreateButton(navPanelObj.transform, "CancelButton", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.24f), "Cancel Navigation", new Color(0.85f, 0.20f, 0.20f), defaultFont);
            var cancelBtn = cancelBtnObj.GetComponent<Button>();

            navPanelObj.SetActive(false); // Hidden initially

            // 5C2. Demo Navigation Mode Panel (Docked in left sidebar)
            var demoNavPanelObj = CreateUIPanel(sidebarContainer.transform, "DemoNavigationPanel", new Vector2(0.015f, 0.02f), new Vector2(0.28f, 0.895f));
            SetPanelBackground(demoNavPanelObj, new Color(0.06f, 0.08f, 0.13f, 0.96f));

            var demoTitle = CreateTextMeshPro(demoNavPanelObj.transform, "DemoTitleText", "ACTIVE NAVIGATION", 20, TextAlignmentOptions.Center, defaultFont);
            demoTitle.fontStyle = FontStyles.Bold;
            demoTitle.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Sky blue
            demoTitle.rectTransform.anchorMin = new Vector2(0.04f, 0.935f);
            demoTitle.rectTransform.anchorMax = new Vector2(0.96f, 0.985f);
            demoTitle.rectTransform.offsetMin = Vector2.zero;
            demoTitle.rectTransform.offsetMax = Vector2.zero;

            var demoDestText = CreateTextMeshPro(demoNavPanelObj.transform, "DemoDestinationText", "Destination: --", 22, TextAlignmentOptions.Left, defaultFont);
            demoDestText.fontStyle = FontStyles.Bold;
            demoDestText.color = new Color(0.95f, 0.97f, 1f, 1f);
            demoDestText.rectTransform.anchorMin = new Vector2(0.05f, 0.855f);
            demoDestText.rectTransform.anchorMax = new Vector2(0.95f, 0.93f);
            demoDestText.rectTransform.offsetMin = Vector2.zero;
            demoDestText.rectTransform.offsetMax = Vector2.zero;

            var demoFloorText = CreateTextMeshPro(demoNavPanelObj.transform, "DemoFloorText", "Floor: --", 16, TextAlignmentOptions.Left, defaultFont);
            demoFloorText.color = new Color(0.58f, 0.64f, 0.72f, 1f);
            demoFloorText.rectTransform.anchorMin = new Vector2(0.05f, 0.795f);
            demoFloorText.rectTransform.anchorMax = new Vector2(0.95f, 0.85f);
            demoFloorText.rectTransform.offsetMin = Vector2.zero;
            demoFloorText.rectTransform.offsetMax = Vector2.zero;

            var demoDetailsText = CreateTextMeshPro(demoNavPanelObj.transform, "DemoDetailsText", "Facility description...", 14, TextAlignmentOptions.Left, defaultFont);
            demoDetailsText.color = new Color(0.78f, 0.83f, 0.90f, 1f);
            demoDetailsText.rectTransform.anchorMin = new Vector2(0.05f, 0.715f);
            demoDetailsText.rectTransform.anchorMax = new Vector2(0.95f, 0.79f);
            demoDetailsText.rectTransform.offsetMin = Vector2.zero;
            demoDetailsText.rectTransform.offsetMax = Vector2.zero;

            var demoStepInfoText = CreateTextMeshPro(demoNavPanelObj.transform, "DemoStepInfoText", "Step 1 of --: Loading route...", 16, TextAlignmentOptions.Left, defaultFont);
            demoStepInfoText.color = new Color(0.22f, 0.74f, 0.97f, 1f); // sky blue
            demoStepInfoText.rectTransform.anchorMin = new Vector2(0.05f, 0.58f);
            demoStepInfoText.rectTransform.anchorMax = new Vector2(0.95f, 0.70f);
            demoStepInfoText.rectTransform.offsetMin = Vector2.zero;
            demoStepInfoText.rectTransform.offsetMax = Vector2.zero;

            var demoDistText = CreateTextMeshPro(demoNavPanelObj.transform, "DemoDistanceText", "Distance: --", 15, TextAlignmentOptions.Left, defaultFont);
            demoDistText.rectTransform.anchorMin = new Vector2(0.05f, 0.515f);
            demoDistText.rectTransform.anchorMax = new Vector2(0.95f, 0.575f);
            demoDistText.rectTransform.offsetMin = Vector2.zero;
            demoDistText.rectTransform.offsetMax = Vector2.zero;

            var demoProgressText = CreateTextMeshPro(demoNavPanelObj.transform, "DemoProgressText", "Progress: 0%", 15, TextAlignmentOptions.Left, defaultFont);
            demoProgressText.color = new Color(0.20f, 0.83f, 0.60f, 1f); // emerald
            demoProgressText.rectTransform.anchorMin = new Vector2(0.05f, 0.445f);
            demoProgressText.rectTransform.anchorMax = new Vector2(0.95f, 0.51f);
            demoProgressText.rectTransform.offsetMin = Vector2.zero;
            demoProgressText.rectTransform.offsetMax = Vector2.zero;

            var demoRouteSummaryText = CreateTextMeshPro(demoNavPanelObj.transform, "DemoRouteSummaryText", "Route: ...", 13, TextAlignmentOptions.Left, defaultFont);
            demoRouteSummaryText.color = new Color(0.58f, 0.64f, 0.72f, 1f);
            demoRouteSummaryText.rectTransform.anchorMin = new Vector2(0.05f, 0.22f);
            demoRouteSummaryText.rectTransform.anchorMax = new Vector2(0.95f, 0.435f);
            demoRouteSummaryText.rectTransform.offsetMin = Vector2.zero;
            demoRouteSummaryText.rectTransform.offsetMax = Vector2.zero;

            var demoStepBtnObj = CreateButton(demoNavPanelObj.transform, "DemoStepButton", new Vector2(0.05f, 0.12f), new Vector2(0.48f, 0.195f), "Step Forward", new Color(0.02f, 0.52f, 0.85f), defaultFont);
            var demoAutoBtnObj = CreateButton(demoNavPanelObj.transform, "DemoAutoPlayButton", new Vector2(0.52f, 0.12f), new Vector2(0.95f, 0.195f), "Auto Simulate", new Color(0.39f, 0.40f, 0.95f), defaultFont);
            var demoResetBtnObj = CreateButton(demoNavPanelObj.transform, "DemoResetButton", new Vector2(0.05f, 0.035f), new Vector2(0.48f, 0.11f), "Reset Route", new Color(0.22f, 0.27f, 0.35f), defaultFont);
            var demoBackBtnObj = CreateButton(demoNavPanelObj.transform, "DemoBackButton", new Vector2(0.52f, 0.035f), new Vector2(0.95f, 0.11f), "Back / Change", new Color(0.85f, 0.20f, 0.20f), defaultFont);

            demoNavPanelObj.SetActive(false); // Hidden initially

            // 5D. Destination Reached Panel (Docked in left sidebar)
            var reachedPanelObj = CreateUIPanel(sidebarContainer.transform, "DestinationReachedPanel", new Vector2(0.015f, 0.12f), new Vector2(0.28f, 0.88f));
            SetPanelBackground(reachedPanelObj, new Color(0.04f, 0.14f, 0.09f, 0.98f));

            var reachedTitle = CreateTextMeshPro(reachedPanelObj.transform, "ReachedTitle", "You Have Arrived!", 26, TextAlignmentOptions.Center, defaultFont);
            reachedTitle.fontStyle = FontStyles.Bold;
            reachedTitle.color = new Color(0.20f, 0.83f, 0.60f, 1f);
            reachedTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.80f);
            reachedTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            reachedTitle.rectTransform.offsetMin = Vector2.zero;
            reachedTitle.rectTransform.offsetMax = Vector2.zero;

            var reachedDetails = CreateTextMeshPro(reachedPanelObj.transform, "DestinationDetailsText", "Classroom details...", 17, TextAlignmentOptions.Center, defaultFont);
            reachedDetails.color = new Color(0.90f, 0.94f, 0.98f, 1f);
            reachedDetails.rectTransform.anchorMin = new Vector2(0.05f, 0.22f);
            reachedDetails.rectTransform.anchorMax = new Vector2(0.95f, 0.78f);
            reachedDetails.rectTransform.offsetMin = Vector2.zero;
            reachedDetails.rectTransform.offsetMax = Vector2.zero;

            var anotherBtnObj = CreateButton(reachedPanelObj.transform, "NavigateAnotherButton", new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.18f), "Select Another Destination", new Color(0.05f, 0.60f, 0.38f), defaultFont);
            var anotherBtn = anotherBtnObj.GetComponent<Button>();

            reachedPanelObj.SetActive(false); // Hidden initially

            // 5D2. Collapsible Navigation Sidebar Toggle Button (Mounted on Canvas, docks to edge of sidebar)
            var sidebarToggleBtnObj = new GameObject("SidebarToggleButton");
            sidebarToggleBtnObj.transform.SetParent(canvasObj.transform, false);
            var toggleImg = sidebarToggleBtnObj.AddComponent<Image>();
            toggleImg.color = new Color(0.07f, 0.12f, 0.20f, 0.95f); // Deep navy button
            toggleImg.raycastTarget = true;

            var sidebarToggleBtn = sidebarToggleBtnObj.AddComponent<Button>();
            sidebarToggleBtn.targetGraphic = toggleImg;

            var toggleColors = sidebarToggleBtn.colors;
            toggleColors.normalColor = new Color(0.07f, 0.12f, 0.20f, 0.95f);
            toggleColors.highlightedColor = new Color(0.14f, 0.26f, 0.44f, 1f);
            toggleColors.pressedColor = new Color(0.04f, 0.08f, 0.15f, 1f);
            toggleColors.selectedColor = toggleColors.normalColor;
            sidebarToggleBtn.colors = toggleColors;

            var toggleRt = sidebarToggleBtnObj.GetComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(0.28f, 0.50f);
            toggleRt.anchorMax = new Vector2(0.28f, 0.50f);
            toggleRt.pivot = new Vector2(0f, 0.5f);
            toggleRt.anchoredPosition = new Vector2(4f, 0f);
            toggleRt.sizeDelta = new Vector2(44f, 80f);

            // Left decorative accent bar on toggle button (non-raycast)
            var sidebarAccentBorder = CreateUIPanel(sidebarToggleBtnObj.transform, "SidebarAccentBorder", new Vector2(0f, 0f), new Vector2(0.12f, 1f));
            SetPanelBackground(sidebarAccentBorder, new Color(0.22f, 0.74f, 0.97f, 0.95f)); // Sky blue accent
            var borderImg = sidebarAccentBorder.GetComponent<Image>();
            if (borderImg != null) borderImg.raycastTarget = false;

            // Toggle arrow icon (non-raycast, standard ASCII)
            var sidebarToggleArrowText = CreateTextMeshPro(sidebarToggleBtnObj.transform, "ToggleArrowText", "<b><</b>", 22, TextAlignmentOptions.Center, defaultFont);
            sidebarToggleArrowText.color = new Color(0.92f, 0.96f, 1f, 1f);
            sidebarToggleArrowText.raycastTarget = false;
            FitToParent(sidebarToggleArrowText.rectTransform, 0f, 0f);

            // 5E. Evaluation Panel
            var evalPanelObj = CreateUIPanel(canvasObj.transform, "EvaluationPanel", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.92f));
            SetPanelBackground(evalPanelObj, new Color(0.15f, 0.15f, 0.2f, 0.98f));
            evalPanelObj.AddComponent<EvaluationUI>();
            evalPanelObj.SetActive(false); // Hidden by default, toggled for tests

            // 5F. Interactive Floor Controls Bar (Floating at top-right of 3D Viewport)
            var floorBarObj = CreateUIPanel(canvasObj.transform, "FloorControlsBar", new Vector2(0.66f, 0.842f), new Vector2(0.985f, 0.895f));
            SetPanelBackground(floorBarObj, new Color(0.05f, 0.08f, 0.14f, 0.88f));

            var fullBtnObj = CreateButton(floorBarObj.transform, "FullBuildingButton", new Vector2(0.01f, 0.10f), new Vector2(0.24f, 0.90f), "Full Building", new Color(0.10f, 0.16f, 0.26f), defaultFont, 14f);
            var f0BtnObj = CreateButton(floorBarObj.transform, "Floor0Button", new Vector2(0.26f, 0.10f), new Vector2(0.49f, 0.90f), "F0 Ground", new Color(0.10f, 0.16f, 0.26f), defaultFont, 14f);
            var f1BtnObj = CreateButton(floorBarObj.transform, "Floor1Button", new Vector2(0.51f, 0.10f), new Vector2(0.74f, 0.90f), "F1 First", new Color(0.10f, 0.16f, 0.26f), defaultFont, 14f);
            var f2BtnObj = CreateButton(floorBarObj.transform, "Floor2Button", new Vector2(0.76f, 0.10f), new Vector2(0.99f, 0.90f), "F2 Second", new Color(0.10f, 0.16f, 0.26f), defaultFont, 14f);

            // 6. Connect Master Controller References
            var navSerialized = new SerializedObject(navUIController);
            navSerialized.FindProperty("destinationSelectionUI").objectReferenceValue = destUI;
            navSerialized.FindProperty("startNodeSelector").objectReferenceValue = startSelector;
            navSerialized.FindProperty("arSessionController").objectReferenceValue = sessionController;
            navSerialized.FindProperty("arNavigationRenderer").objectReferenceValue = navRenderer;
            navSerialized.FindProperty("coordinateCalibrator").objectReferenceValue = calibrator;
            navSerialized.FindProperty("buildingVisualizer").objectReferenceValue = buildingVis;
            navSerialized.FindProperty("routeVisualizer").objectReferenceValue = routeVis;
            navSerialized.FindProperty("orbitCamera").objectReferenceValue = orbitCam;

            navSerialized.FindProperty("destinationSelectionPanel").objectReferenceValue = destPanelObj;
            navSerialized.FindProperty("navigationPanel").objectReferenceValue = navPanelObj;
            navSerialized.FindProperty("destinationReachedPanel").objectReferenceValue = reachedPanelObj;
            navSerialized.FindProperty("statusPanel").objectReferenceValue = statusPanelObj;

            navSerialized.FindProperty("statusText").objectReferenceValue = statusText;
            navSerialized.FindProperty("diagnosticsText").objectReferenceValue = diagText;
            navSerialized.FindProperty("destinationInfoText").objectReferenceValue = destInfoText;
            navSerialized.FindProperty("distanceText").objectReferenceValue = distText;
            navSerialized.FindProperty("floorIndicatorText").objectReferenceValue = floorText;
            navSerialized.FindProperty("arrivalDetailsText").objectReferenceValue = reachedDetails;
            navSerialized.FindProperty("arStatusText").objectReferenceValue = statusText;

            navSerialized.FindProperty("cancelNavigationButton").objectReferenceValue = cancelBtn;
            navSerialized.FindProperty("navigateAnotherButton").objectReferenceValue = anotherBtn;

            // Wire Floor View Control buttons
            navSerialized.FindProperty("fullBuildingButton").objectReferenceValue = fullBtnObj.GetComponent<Button>();
            navSerialized.FindProperty("floor0Button").objectReferenceValue = f0BtnObj.GetComponent<Button>();
            navSerialized.FindProperty("floor1Button").objectReferenceValue = f1BtnObj.GetComponent<Button>();
            navSerialized.FindProperty("floor2Button").objectReferenceValue = f2BtnObj.GetComponent<Button>();

            // Wire Demo Navigation Panel references
            navSerialized.FindProperty("demoNavigationPanel").objectReferenceValue = demoNavPanelObj;
            navSerialized.FindProperty("demoTitleText").objectReferenceValue = demoTitle;
            navSerialized.FindProperty("demoDestinationText").objectReferenceValue = demoDestText;
            navSerialized.FindProperty("demoFloorText").objectReferenceValue = demoFloorText;
            navSerialized.FindProperty("demoDetailsText").objectReferenceValue = demoDetailsText;
            navSerialized.FindProperty("demoStepInfoText").objectReferenceValue = demoStepInfoText;
            navSerialized.FindProperty("demoRouteSummaryText").objectReferenceValue = demoRouteSummaryText;
            navSerialized.FindProperty("demoDistanceText").objectReferenceValue = demoDistText;
            navSerialized.FindProperty("demoProgressText").objectReferenceValue = demoProgressText;

            navSerialized.FindProperty("demoStepButton").objectReferenceValue = demoStepBtnObj.GetComponent<Button>();
            navSerialized.FindProperty("demoAutoPlayButton").objectReferenceValue = demoAutoBtnObj.GetComponent<Button>();
            navSerialized.FindProperty("demoAutoPlayButtonText").objectReferenceValue = demoAutoBtnObj.GetComponentInChildren<TMP_Text>();
            navSerialized.FindProperty("demoResetButton").objectReferenceValue = demoResetBtnObj.GetComponent<Button>();
            navSerialized.FindProperty("demoBackButton").objectReferenceValue = demoBackBtnObj.GetComponent<Button>();

            // Wire Collapsible Navigation Sidebar
            navSerialized.FindProperty("sidebarContainer").objectReferenceValue = sidebarContainer;
            navSerialized.FindProperty("sidebarToggleButton").objectReferenceValue = sidebarToggleBtn;
            navSerialized.FindProperty("sidebarToggleArrowText").objectReferenceValue = sidebarToggleArrowText;
            navSerialized.FindProperty("sidebarAccentBorder").objectReferenceValue = sidebarAccentBorder;

            navSerialized.ApplyModifiedProperties();

            // 7. Event System (InputSystemUIInputModule for unified WebGL, Desktop, and Touch Pointer Events)
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputModule = eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            // Ensure every TMP_Text in scene has defaultFont assigned
            if (defaultFont != null)
            {
                var allTexts = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
                int fontAssignedCount = 0;
                foreach (var t in allTexts)
                {
                    if (t.font == null)
                    {
                        t.font = defaultFont;
                        EditorUtility.SetDirty(t);
                        fontAssignedCount++;
                    }
                }
                Debug.Log($"[SceneSetup] Verified all scene text components ({allTexts.Length} total). Assigned fallback font to {fontAssignedCount} texts.");
            }

            // 8. Shader Keeper (Guarantees Standard, Sprites, and Unlit shaders are included in player builds)
            var shaderKeeperObj = new GameObject("_ShaderKeeper");
            var keeperRend = shaderKeeperObj.AddComponent<MeshRenderer>();
            var sStandard = Shader.Find("Standard");
            var sSprites = Shader.Find("Sprites/Default");
            var sUnlit = Shader.Find("Unlit/Color");
            var keeperMats = new List<Material>();
            if (sStandard != null) keeperMats.Add(new Material(sStandard));
            if (sSprites != null) keeperMats.Add(new Material(sSprites));
            if (sUnlit != null) keeperMats.Add(new Material(sUnlit));
            keeperRend.sharedMaterials = keeperMats.ToArray();
            keeperRend.enabled = false;

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SceneSetup] Scene saved successfully to: {ScenePath}");

            // Configure EditorBuildSettings to include MainScene
            var editorBuildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = editorBuildScenes;
            Debug.Log($"[SceneSetup] EditorBuildSettings configured with {ScenePath} as build index 0.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SceneSetup] MainScene generation completed successfully.");
        }

        // =====================================================================
        // UI Helper Methods
        // =====================================================================

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private static void SetPanelBackground(GameObject panel, Color color)
        {
            var img = panel.AddComponent<Image>();
            img.color = color;
        }

        private static TMP_Text CreateTextMeshPro(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, TMP_FontAsset font = null)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                tmp.font = font;
            }
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static void FitToParent(RectTransform rect, float paddingX, float paddingY)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(paddingX, paddingY);
            rect.offsetMax = new Vector2(-paddingX, -paddingY);
        }

        private static GameObject CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string buttonText, Color bgColor, TMP_FontAsset font = null, float fontSize = 20f)
        {
            var btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.22f);
            colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
            colors.selectedColor = Color.Lerp(bgColor, Color.white, 0.15f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var txt = CreateTextMeshPro(btnObj.transform, "ButtonText", buttonText, fontSize, TextAlignmentOptions.Center, font);
            txt.fontStyle = FontStyles.Bold;
            txt.raycastTarget = false;
            FitToParent(txt.rectTransform, 4f, 2f);

            return btnObj;
        }

        private static GameObject CreateInputField(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string placeholder, TMP_FontAsset font = null)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.14f, 0.22f, 1f);

            var input = obj.AddComponent<TMP_InputField>();

            // Viewport & Text component
            var textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            FitToParent(textRect, 15f, 5f);

            var tmpText = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmpText.font = font;
            tmpText.fontSize = 20;
            tmpText.color = Color.white;

            // Placeholder component
            var phObj = new GameObject("Placeholder", typeof(RectTransform));
            phObj.transform.SetParent(obj.transform, false);
            var phRect = phObj.GetComponent<RectTransform>();
            FitToParent(phRect, 15f, 5f);

            var phText = phObj.AddComponent<TextMeshProUGUI>();
            if (font != null) phText.font = font;
            phText.text = placeholder;
            phText.fontSize = 20;
            phText.fontStyle = FontStyles.Italic;
            phText.color = new Color(0.55f, 0.65f, 0.78f, 0.8f);

            input.textViewport = textRect;
            input.textComponent = tmpText;
            input.placeholder = phText;
            if (font != null) input.fontAsset = font;

            return obj;
        }

        private static GameObject CreateDropdown(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font = null)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.14f, 0.22f, 1f);

            var dropdown = obj.AddComponent<TMP_Dropdown>();

            // Caption text
            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(obj.transform, false);
            var caption = labelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) caption.font = font;
            caption.text = "Select Starting Point...";
            caption.fontSize = 20;
            caption.color = Color.white;
            caption.alignment = TextAlignmentOptions.Left;
            FitToParent(caption.rectTransform, 15f, 5f);
            caption.rectTransform.offsetMax = new Vector2(-45f, -5f);

            // Arrow icon
            var arrowObj = new GameObject("Arrow", typeof(RectTransform));
            arrowObj.transform.SetParent(obj.transform, false);
            var arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(40f, 0f);
            arrowRect.offsetMin = new Vector2(-40f, 0f);
            arrowRect.offsetMax = Vector2.zero;
            var arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
            if (font != null) arrowText.font = font;
            arrowText.text = "\u25BC";
            arrowText.fontSize = 16;
            arrowText.color = new Color(0.6f, 0.7f, 0.85f, 1f);
            arrowText.alignment = TextAlignmentOptions.Center;

            // Template hierarchy (required by TMP_Dropdown to open popup options)
            var templateObj = new GameObject("Template", typeof(RectTransform));
            templateObj.transform.SetParent(obj.transform, false);
            var templateRect = templateObj.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.offsetMin = Vector2.zero;
            templateRect.offsetMax = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0f, 320f);

            var templateImg = templateObj.AddComponent<Image>();
            templateImg.color = new Color(0.08f, 0.11f, 0.18f, 0.98f);

            var scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Viewport
            var vpObj = new GameObject("Viewport", typeof(RectTransform));
            vpObj.transform.SetParent(templateObj.transform, false);
            var vpRect = vpObj.GetComponent<RectTransform>();
            FitToParent(vpRect, 2f, 2f);
            var vpImg = vpObj.AddComponent<Image>();
            vpImg.color = Color.white;
            var mask = vpObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            var contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(vpObj.transform, false);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 60f);

            // Item prototype
            var itemObj = new GameObject("Item", typeof(RectTransform));
            itemObj.transform.SetParent(contentObj.transform, false);
            var itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 60f);

            var itemToggle = itemObj.AddComponent<Toggle>();

            var itemBg = new GameObject("Item Background", typeof(RectTransform));
            itemBg.transform.SetParent(itemObj.transform, false);
            FitToParent(itemBg.GetComponent<RectTransform>(), 0f, 0f);
            var itemBgImg = itemBg.AddComponent<Image>();
            itemBgImg.color = new Color(0.12f, 0.17f, 0.26f, 0.95f);

            var itemCheck = new GameObject("Item Checkmark", typeof(RectTransform));
            itemCheck.transform.SetParent(itemObj.transform, false);
            var checkRect = itemCheck.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.sizeDelta = new Vector2(28f, 28f);
            checkRect.anchoredPosition = new Vector2(20f, 0f);
            var checkImg = itemCheck.AddComponent<Image>();
            checkImg.color = new Color(0.2f, 0.83f, 0.60f, 1f);

            var itemLabelObj = new GameObject("Item Label", typeof(RectTransform));
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            var itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
            FitToParent(itemLabelRect, 45f, 5f);
            var itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
            if (font != null) itemLabel.font = font;
            itemLabel.fontSize = 22;
            itemLabel.color = Color.white;
            itemLabel.alignment = TextAlignmentOptions.Left;

            itemToggle.targetGraphic = itemBgImg;
            itemToggle.graphic = checkImg;
            itemToggle.isOn = false;

            scrollRect.viewport = vpRect;
            scrollRect.content = contentRect;

            dropdown.template = templateRect;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;

            // Template must be inactive by default
            templateObj.SetActive(false);

            return obj;
        }

        private static GameObject CreateScrollView(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var scrollObj = new GameObject(name, typeof(RectTransform));
            scrollObj.transform.SetParent(parent, false);
            var rect = scrollObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var scrollImage = scrollObj.AddComponent<Image>();
            scrollImage.color = new Color(0.05f, 0.07f, 0.1f, 0.8f);
            scrollImage.raycastTarget = true;

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 35f;

            // Viewport
            var viewportObj = new GameObject("Viewport", typeof(RectTransform));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var vpRect = viewportObj.GetComponent<RectTransform>();
            FitToParent(vpRect, 5f, 5f);
            var vpImg = viewportObj.AddComponent<Image>();
            vpImg.color = Color.white;
            vpImg.raycastTarget = false;
            var mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            var contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 800f);

            var layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(6, 6, 6, 6);

            var fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRect;
            scrollRect.content = contentRect;

            return scrollObj;
        }
    }
}
