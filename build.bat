@echo off
setlocal
cd /d "%~dp0"

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Could not find the .NET Framework C# compiler ^(csc.exe^).
  echo Install .NET Framework 4.8 or build on a Windows PC.
  exit /b 1
)

if not exist "dist" mkdir dist

echo Building MouseClickCounter.exe ...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
  /out:dist\MouseClickCounter.exe ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Xml.dll ^
  src\*.cs

if errorlevel 1 (
  echo Build failed.
  exit /b 1
)

echo.
echo Built: dist\MouseClickCounter.exe
echo Data will be stored in: %%LOCALAPPDATA%%\MouseClickCounter\
echo.
exit /b 0
