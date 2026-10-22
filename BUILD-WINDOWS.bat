@echo off
setlocal
rem Echo Crypt - export a Windows test build
rem Point GODOT at your Godot .NET console exe (the one ending in _console.exe)
set "GODOT=%USERPROFILE%\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"

if not exist "%GODOT%" (
  echo Godot not found at:
  echo   %GODOT%
  echo Edit the GODOT line at the top of BUILD-WINDOWS.bat, then run it again.
  pause
  exit /b 1
)

cd /d "%~dp0"
if not exist builds\windows mkdir builds\windows

echo Exporting debug build...
"%GODOT%" --headless --path . --export-debug "Windows Desktop" builds\windows\EchoCrypt.exe
if errorlevel 1 (
  echo.
  echo Export failed. Most common fix: open the project in the Godot editor once,
  echo then Editor ^> Manage Export Templates ^> Download and Install.
  pause
  exit /b 1
)

echo.
echo Done: builds\windows\EchoCrypt.exe
start "" builds\windows
pause
