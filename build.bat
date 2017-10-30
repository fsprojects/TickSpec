@echo off
cls

.paket\paket.bootstrapper.exe
if errorlevel 1 (
  exit /b %errorlevel%
)

.paket\paket.exe restore
if errorlevel 1 (
  exit /b %errorlevel%
)

SET NuGetTool=packages\NuGet.CommandLine\tools\NuGet.exe
SET EnableNuGetPackageRestore=true

msbuild TickSpec.sln /p:Configuration=Release
cd Nuget\dotNet
..\..\%NuGetTool% pack TickSpec.nuspec
cd ..\..

IF NOT "%1" == "SkipSilverlight" (
	msbuild TickSpec.Silverlight5.sln /p:Configuration=Release
	cd Nuget\Silverlight
	..\..\%NuGetTool% pack TickSpec.Silverlight.nuspec
	cd ..\..
)