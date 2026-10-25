param(
    [int]$Port = 8080
)

$buildDir = Join-Path $PSScriptRoot "Builds\WebGL\SmartClassroomAR_WebGL"

if (-not (Test-Path (Join-Path $buildDir "index.html"))) {
    Write-Error "WebGL build or index.html not found. Run build_webgl.ps1 first."
    exit 1
}

Write-Host "SmartClassroomAR WebGL Server" -ForegroundColor Cyan
Write-Host "Build directory: $buildDir"
Write-Host "URL: http://localhost:$Port"

# Open the browser after starting the server.
Start-Process "http://localhost:$Port"

python -m http.server $Port --directory $buildDir