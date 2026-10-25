$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe"
$project = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR"
$log = "c:\Users\s karthik sriram\Desktop\projects\ar vr classroom\SmartClassroomAR\unity_apk_rebuild.log"

Write-Host "Starting Unity Android APK build..."
$proc = Start-Process -FilePath $unity -ArgumentList "-batchmode", "-quit", "-nographics", "-projectPath", "`"$project`"", "-executeMethod", "SmartClassroomAR.Editor.SceneSetup.BuildAndroidApk", "-logFile", "`"$log`"" -Wait -PassThru
Write-Host "Unity exited with code: $($proc.ExitCode)"
if (Test-Path "Builds/Android/SmartClassroomAR.apk") {
    $apk = Get-Item "Builds/Android/SmartClassroomAR.apk"
    Write-Host "APK rebuilt successfully: $($apk.Length) bytes, LastWriteTime: $($apk.LastWriteTime)"
} else {
    Write-Error "APK file not found!"
}
