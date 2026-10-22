@echo off
setlocal
rem Echo Crypt - export a Windows test build
rem Point GODOT at your Godot .NET console exe (the one ending in _console.exe)
set "GODOT=%USERPROFILE%\Downloads\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
set "TEMPLATES=%APPDATA%\Godot\export_templates\4.7.2.stable.mono"

if not exist "%GODOT%" (
  echo Godot not found at:
  echo   %GODOT%
  echo Edit the GODOT line at the top of BUILD-WINDOWS.bat, then run it again.
  pause
  exit /b 1
)

if not exist "%TEMPLATES%\windows_debug_x86_64.exe" (
  echo Export templates are missing.
  echo Open the project in the Godot editor, then:
  echo   Editor ^> Manage Export Templates ^> Download and Install
  pause
  exit /b 1
)

cd /d "%~dp0"

if exist "echo-crypt\project.godot" (
  echo A second copy of the project is inside this folder: echo-crypt
  echo Move or delete that folder, then run this again.
  pause
  exit /b 1
)

echo Building C# code...
dotnet build -c Debug
if errorlevel 1 (
  echo.
  echo C# build failed. Open the project in the Godot editor once so it can
  echo upgrade to 4.7.2, press Build, then run this again.
  pause
  exit /b 1
)

if not exist builds\windows mkdir builds\windows

echo Exporting debug build...
"%GODOT%" --headless --path . --export-debug "Windows Desktop" builds\windows\EchoCrypt.exe
if errorlevel 1 (
  echo.
  echo Export failed. See the messages above.
  pause
  exit /b 1
)

echo.
echo Done: builds\windows\EchoCrypt.exe
start "" builds\windows
pause
