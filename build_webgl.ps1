$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe"
$project = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR"
$log = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR\webgl_build.log"

if (Test-Path $log) {
    Remove-Item $log -Force -ErrorAction SilentlyContinue
}

$lock = Join-Path $project "Temp\UnityLockfile"
if (Test-Path $lock) {
    Remove-Item $lock -Force -ErrorAction SilentlyContinue
}

Write-Host "Starting WebGL build for SmartClassroomAR..."
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $unity
$psi.Arguments = "-batchmode -quit -nographics -projectPath `"$project`" -buildTarget WebGL -executeMethod SmartClassroomAR.Editor.SceneSetup.BuildWebGL -logFile `"$log`""
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$proc = [System.Diagnostics.Process]::Start($psi)
$finished = $proc.WaitForExit(900000)

if (-not $finished) {
    Write-Error "TIMEOUT: WebGL build did not finish within 15 minutes."
    $proc.Kill()
    exit 2
}

$exitCode = $proc.ExitCode
Write-Host "Unity exited with code: $exitCode"

if (Test-Path $log) {
    Get-Content $log | Select-String -Pattern "(\[SceneSetup\]|WebGL Build|Error|Exception)" | ForEach-Object { $_.Line }
}

exit $exitCode
