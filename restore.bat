@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion
title RaiDrive Ad Remover - Restore

REM ============================================================
REM  Restore RaiDrive to the version that was there before.
REM  Double-click to run. Will ask for admin rights.
REM
REM  ASCII-only on purpose (see install.bat header).
REM ============================================================

set "SCRIPT=%~dp0"
set "TARGET=C:\Program Files\OpenBoxLab\RaiDrive Mount\RaiDrive.Mount.exe"

net session >nul 2>&1
if %errorLevel% neq 0 (
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

cd /d "%SCRIPT%"
echo.
echo ============================================
echo   Restore RaiDrive to original
echo ============================================
echo.

if not exist "%SCRIPT%backup" (
    echo  [ERROR] backup folder not found.
    echo          This patch was probably not installed
    echo          with install.bat, or backup was deleted.
    echo.
    pause
    exit /b 1
)

set "LATEST="
for /f "delims=" %%F in ('dir /b /o-d "%SCRIPT%backup\RaiDrive.Mount.exe.*.bak" 2^>nul') do (
    if not defined LATEST set "LATEST=%%F"
)

if not defined LATEST (
    echo  [ERROR] No backup file found in backup folder.
    echo.
    pause
    exit /b 1
)

echo  Found backup: !LATEST!
echo.
set /p CONFIRM=Restore now? (y/N) 
if /i not "!CONFIRM!"=="y" (
    echo.
    echo  Cancelled.
    pause
    exit /b 0
)

echo.
echo  Closing RaiDrive...
taskkill /F /IM "RaiDrive.Mount.exe" >nul 2>&1
taskkill /F /IM "RaiDrive.NotifyHelper.exe" >nul 2>&1
net stop "RaiDriveMountService" >nul 2>&1
timeout /t 2 /nobreak >nul

echo  Restoring...
copy /y "%SCRIPT%backup\!LATEST!" "%TARGET%" >nul
if errorlevel 1 (
    echo.
    echo  [ERROR] Restore failed. Please reboot and retry.
    echo.
    pause
    exit /b 1
)

echo.
echo ============================================
echo   Restored to original version.
echo ============================================
echo.
pause
