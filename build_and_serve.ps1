$webglPath = "C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Data\PlaybackEngines\WebGLSupport"

if (-not (Test-Path $webglPath)) {
    Write-Host "======================================================================" -ForegroundColor Yellow
    Write-Host "  WebGL Build Support is not installed yet!" -ForegroundColor Red
    Write-Host "======================================================================" -ForegroundColor Yellow
    Write-Host "The 587MB installer has already been downloaded to:" -ForegroundColor Cyan
    Write-Host "  $env:TEMP\UnitySetup-WebGL-2022.3.51f1.exe"
    Write-Host ""
    Write-Host "To install it with 1 click:" -ForegroundColor Green
    Write-Host "  Run: .\Install_WebGL_Support.bat" -ForegroundColor White
    Write-Host "  (Click 'Yes' on the administrator prompt that appears)"
    Write-Host ""
    Write-Host "Or install via Unity Hub:" -ForegroundColor Green
    Write-Host "  Unity Hub -> Installs -> Unity 2022.3.51f1 -> Add modules -> WebGL Build Support"
    Write-Host "======================================================================" -ForegroundColor Yellow
    exit 1
}

Write-Host "WebGL Support detected. Building WebGL application..." -ForegroundColor Green
& powershell -ExecutionPolicy Bypass -File .\build_webgl.ps1

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build complete! Starting local HTTP server and browser..." -ForegroundColor Green
    & powershell -ExecutionPolicy Bypass -File .\serve_webgl.ps1
} else {
    Write-Error "WebGL build failed. Please check webgl_build.log for details."
}
