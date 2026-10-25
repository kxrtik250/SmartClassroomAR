$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe"
$project = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR"
$sceneLog = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR\unity_scene_regen_compat.log"
$apkLog = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR\unity_apk_compat.log"

# Clean stale lockfile if present
$lock = Join-Path $project "Temp\UnityLockfile"
if (Test-Path $lock) {
    Remove-Item $lock -Force -ErrorAction SilentlyContinue
}

# 1. Regenerate MainScene
Write-Host "--- Step 1: Regenerating MainScene with all current UI controllers and DemoNavigationPanel ---"
$psiScene = New-Object System.Diagnostics.ProcessStartInfo
$psiScene.FileName = $unity
$psiScene.Arguments = "-batchmode -quit -nographics -projectPath `"$project`" -executeMethod SmartClassroomAR.Editor.SceneSetup.BuildMainScene -logFile `"$sceneLog`""
$psiScene.UseShellExecute = $false
$psiScene.CreateNoWindow = $true

$procScene = [System.Diagnostics.Process]::Start($psiScene)
$finishedScene = $procScene.WaitForExit(120000)

if (-not $finishedScene) {
    Write-Error "TIMEOUT: Scene generation did not complete within 120 seconds."
    $procScene.Kill()
    exit 1
}

Write-Host "Scene generation exited with code: $($procScene.ExitCode)"
if ($procScene.ExitCode -ne 0) {
    Write-Host "=== SCENE GENERATION ERRORS ==="
    Get-Content $sceneLog | Select-String -Pattern "(error CS|Scripts have compiler errors|Fatal Error|FATAL)" | ForEach-Object { $_.Line }
    exit $procScene.ExitCode
}

# Clean stale lockfile if present between steps
if (Test-Path $lock) {
    Remove-Item $lock -Force -ErrorAction SilentlyContinue
}

# 2. Build Android APK with Compatibility Fix (Target SDK 34, Min SDK 28)
Write-Host "--- Step 2: Building Android APK (SmartClassroomAR_CompatibilityFix.apk with Target SDK 34) ---"
$psiApk = New-Object System.Diagnostics.ProcessStartInfo
$psiApk.FileName = $unity
$psiApk.Arguments = "-batchmode -quit -nographics -projectPath `"$project`" -executeMethod SmartClassroomAR.Editor.SceneSetup.BuildAndroidApkCompatibilityFix -logFile `"$apkLog`""
$psiApk.UseShellExecute = $false
$psiApk.CreateNoWindow = $true

$procApk = [System.Diagnostics.Process]::Start($psiApk)
$finishedApk = $procApk.WaitForExit(600000) # 10 minute timeout for full APK build

if (-not $finishedApk) {
    Write-Error "TIMEOUT: Android APK build did not complete within 600 seconds."
    $procApk.Kill()
    exit 2
}

Write-Host "APK build exited with code: $($procApk.ExitCode)"
$apkPath = Join-Path $project "Builds\Android\SmartClassroomAR_CompatibilityFix.apk"
if (Test-Path $apkPath) {
    $apk = Get-Item $apkPath
    Write-Host "=== BUILD SUCCESSFUL ==="
    Write-Host "APK Location: $($apk.FullName)"
    Write-Host "APK Size: $([math]::Round($apk.Length / 1MB, 2)) MB ($($apk.Length) bytes)"
    Write-Host "Last Modified: $($apk.LastWriteTime)"
} else {
    Write-Error "APK file was not created at $apkPath"
    if (Test-Path $apkLog) {
        Get-Content $apkLog | Select-String -Pattern "(error|failed|exception)" -Context 0,2 | Select-Object -Last 20
    }
}

exit $procApk.ExitCode
