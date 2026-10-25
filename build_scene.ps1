$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe"
$project = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR"
$log = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR\scene_build.log"

if (Test-Path $log) {
    Remove-Item $log -Force -ErrorAction SilentlyContinue
}

$lock = Join-Path $project "Temp\UnityLockfile"
if (Test-Path $lock) {
    Remove-Item $lock -Force -ErrorAction SilentlyContinue
}

Write-Host "Starting Unity batchmode to build MainScene..."
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $unity
$psi.Arguments = "-batchmode -quit -nographics -projectPath `"$project`" -executeMethod SmartClassroomAR.Editor.SceneSetup.BuildMainScene -logFile `"$log`""
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$proc = [System.Diagnostics.Process]::Start($psi)
$finished = $proc.WaitForExit(120000)

if (-not $finished) {
    Write-Error "TIMEOUT: Unity batchmode did not exit within 120 seconds."
    $proc.Kill()
    exit 2
}

$exitCode = $proc.ExitCode
Write-Host "Unity exited with code: $exitCode"

if (Test-Path $log) {
    Get-Content $log | Select-String -Pattern "(\[SceneSetup\]|Error|Exception)" | ForEach-Object { $_.Line }
}

exit $exitCode
