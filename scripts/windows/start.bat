@echo off
cd /d "%~dp0"
title EssSimulator
echo Starting EssSimulator...
echo.
EssSimulator.exe
echo.
echo Program exited. Press any key to close.
pause >nul
