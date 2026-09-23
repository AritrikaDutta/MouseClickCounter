@echo off
cd /d "%~dp0"
if not exist "dist\MouseClickCounter.exe" call build.bat
if not exist "dist\MouseClickCounter.exe" (
  echo Build failed — cannot run.
  exit /b 1
)
start "" "%~dp0dist\MouseClickCounter.exe"
