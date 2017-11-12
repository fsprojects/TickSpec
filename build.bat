@echo off

.paket\paket.bootstrapper.exe
if errorlevel 1 (
  echo "Bootstrapper failed"
  exit /b %errorlevel%
)

.paket\paket.exe restore
if errorlevel 1 (
  echo "Paket restore failed"
  exit /b %errorlevel%
)

SET NuGetTool=%~dp0packages\NuGet.CommandLine\tools\NuGet.exe

msbuild TickSpec.sln /p:Configuration=Release /m

if errorlevel 1 (
  echo "Build failed"
  exit /b %errorlevel%
)

if %1.==. (
    set version="1.0.1.1"
) else (
    set version="%1"
)

.paket\paket.exe pack packed_nugets --version %version% --symbols

pushd %~dp0packed_nugets
%NuGetTool% pack ..\Nuget\NUnit\TickSpec.NUnit.nuspec -Version %version%
%NuGetTool% pack ..\Nuget\xUnit\TickSpec.xUnit.nuspec -Version %version%
popd