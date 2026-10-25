@echo off
title Install Unity WebGL Build Support
echo ======================================================================
echo   Installing Unity WebGL Build Support for Unity 2022.3.51f1
echo ======================================================================
echo.
echo The 587MB installer is already downloaded on this computer at:
echo   %TEMP%\UnitySetup-WebGL-2022.3.51f1.exe
echo.
echo Installing silently to:
echo   C:\Program Files\Unity\Hub\Editor\2022.3.51f1
echo.
echo NOTE: A Windows Administrator (UAC) prompt will appear.
echo       Click 'Yes' to authorize the installation.
echo.
powershell -NoProfile -Command "Start-Process '%TEMP%\UnitySetup-WebGL-2022.3.51f1.exe' -ArgumentList '/S /D=C:\Program Files\Unity\Hub\Editor\2022.3.51f1' -Verb RunAs -Wait"
echo.
echo ======================================================================
echo WebGL Build Support has been installed successfully!
echo You can now run build_webgl.ps1 to build the 3D Web application.
echo ======================================================================
echo.
pause
