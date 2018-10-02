@echo off
cls

.paket\paket.exe restore
if errorlevel 1 (
  echo "Paket restore failed"
  exit /b %errorlevel%
)

dotnet restore build.proj

set encoding=utf-8
dotnet fake build %*

if errorlevel 1 (
  echo "Build failed"
  exit /b %errorlevel%
)