$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe"
$project = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR"
$log = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR\test_run.log"

# Clean previous log
if (Test-Path $log) {
    Remove-Item $log -Force -ErrorAction SilentlyContinue
}

# Clean stale lockfile if present
$lock = Join-Path $project "Temp\UnityLockfile"
if (Test-Path $lock) {
    Remove-Item $lock -Force -ErrorAction SilentlyContinue
}

Write-Host "Starting Unity batchmode for NavigationTests..."
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $unity
$psi.Arguments = "-batchmode -quit -nographics -projectPath `"$project`" -executeMethod SmartClassroomAR.Editor.SceneSetup.RunNavigationTests -logFile `"$log`""
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$proc = [System.Diagnostics.Process]::Start($psi)
$finished = $proc.WaitForExit(120000) # 120 second timeout

if (-not $finished) {
    Write-Error "TIMEOUT: Unity batchmode did not exit within 120 seconds. Terminating process."
    $proc.Kill()
    exit 2
}

$exitCode = $proc.ExitCode
Write-Host "Unity exited with code: $exitCode"

if (Test-Path $log) {
    if ($exitCode -eq 0) {
        Write-Host "=== TEST SUITE RESULTS ==="
        Get-Content $log | Select-String -Pattern "(PASS:|FAIL:|TEST RESULTS:)" | ForEach-Object { $_.Line }
    } else {
        Write-Host "=== COMPILATION OR EXECUTION ERRORS ==="
        Get-Content $log | Select-String -Pattern "(error CS|Scripts have compiler errors|Fatal Error|FATAL)" | ForEach-Object { $_.Line }
    }
} else {
    Write-Error "Log file $log was not generated."
}

exit $exitCode
