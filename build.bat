@echo off

.paket\paket.exe restore
if errorlevel 1 (
  echo "Paket restore failed"
  exit /b %errorlevel%
)

set encoding=utf-8
packages\build\FAKE\tools\FAKE.exe build.fsx %*

if errorlevel 1 (
  echo "Build failed"
  exit /b %errorlevel%
)