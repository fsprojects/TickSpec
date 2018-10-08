@echo off
cls

SET TOOL_PATH=.fake

IF NOT EXIST "%TOOL_PATH%\fake.exe" (
  dotnet tool install fake-cli --tool-path ./%TOOL_PATH%
)

dotnet restore TickSpec.sln

"%TOOL_PATH%/fake.exe" build %*

if errorlevel 1 (
  echo "Build failed"
  exit /b %errorlevel%
)