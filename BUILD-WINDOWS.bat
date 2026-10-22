@echo off
setlocal
rem Echo Crypt - export a Windows test build
rem Leave GODOT empty to search for Godot automatically,
rem or paste the full path to your Godot .NET console exe between the quotes.
set "GODOT="

if not defined GODOT (
  echo Looking for Godot 4.7 .NET...
  for %%D in ("%USERPROFILE%\Downloads" "%USERPROFILE%\Desktop" "%USERPROFILE%\Documents" "%OneDrive%\Desktop" "%OneDrive%\Documents" "C:\Godot" "%ProgramFiles%\Godot" "%LOCALAPPDATA%\Programs") do (
    if not defined GODOT if exist "%%~D" (
      for /f "delims=" %%G in ('where /r "%%~D" "Godot_v4.7*mono_win64_console.exe" 2^>nul') do if not defined GODOT set "GODOT=%%G"
    )
  )
)

if defined GODOT echo Using Godot: %GODOT%
set "TEMPLATES=%APPDATA%\Godot\export_templates\4.7.2.stable.mono"

if not defined GODOT set "GODOT=not found"
if not exist "%GODOT%" (
  echo Could not find Godot 4.7 .NET.
  echo Find the file ending in _console.exe in your Godot folder, right-click it,
  echo choose "Copy as path", and paste it into the GODOT line at the top of this file.
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
