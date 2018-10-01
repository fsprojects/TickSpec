@echo off
cls

dotnet restore build.proj

set encoding=utf-8
dotnet fake build %*

if errorlevel 1 (
  echo "Build failed"
  exit /b %errorlevel%
)