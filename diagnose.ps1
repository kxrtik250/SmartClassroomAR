$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe"
$project = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR"
$log = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR\diag_run.log"

if (Test-Path $log) {
    Remove-Item $log -Force -ErrorAction SilentlyContinue
}
$lock = Join-Path $project "Temp\UnityLockfile"
if (Test-Path $lock) {
    Remove-Item $lock -Force -ErrorAction SilentlyContinue
}

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $unity
$psi.Arguments = "-batchmode -quit -nographics -projectPath `"$project`" -executeMethod SmartClassroomAR.Editor.SceneSetup.DiagnoseSceneAndClicks -logFile `"$log`""
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$proc = [System.Diagnostics.Process]::Start($psi)
$proc.WaitForExit(60000)

if (Test-Path $log) {
    Get-Content $log | Select-String -Pattern "DIAG" | ForEach-Object { $_.Line }
} else {
    Write-Error "No log generated"
}
