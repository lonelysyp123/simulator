@echo off
cd /d "%~dp0"
title EssSimulator (B/S)
echo Starting EssSimulator (B/S architecture)...
echo Web UI: http://localhost:5050
echo Modbus TCP ports: see appsettings.json -^> Simulator.Protocol
echo.
start "" http://localhost:5050
EssSimulator.exe
echo.
echo Program exited. Press any key to close.
pause >nul
