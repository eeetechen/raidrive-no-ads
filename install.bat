@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion
title RaiDrive Ad Remover - Install

REM ============================================================
REM  Double-click this file. No commands to type.
REM  It will ask for admin rights (UAC -> click Yes).
REM
REM  NOTE: This file is intentionally ASCII-only.
REM  cmd.exe parses .bat in the system ANSI codepage (GBK on
REM  Chinese Windows), so non-ASCII comments break parsing.
REM ============================================================

set "SCRIPT=%~dp0"
set "TARGET=C:\Program Files\OpenBoxLab\RaiDrive Mount\RaiDrive.Mount.exe"
set "EXPECTED=0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa"

REM ---------- auto elevate ----------
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo  Administrator rights are required.
    echo  A UAC prompt will appear - please click Yes.
    echo.
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

cd /d "%SCRIPT%"
echo.
echo ============================================
echo   RaiDrive Ad Remover
echo ============================================
echo.

REM ---------- locate install dir ----------
if not exist "%TARGET%" (
    echo  [ERROR] RaiDrive not found:
    echo          %TARGET%
    echo.
    echo  Please install RaiDrive Mount 2026.10.0 first.
    echo.
    pause
    exit /b 1
)

REM ---------- locate patched exe ----------
set "PATCHED="
for %%F in ("%SCRIPT%RaiDrive.Mount.noads.v4.exe" "%SCRIPT%bin\RaiDrive.Mount.noads.v4.exe" "%SCRIPT%..\bin\RaiDrive.Mount.noads.v4.exe") do (
    if exist "%%~fF" set "PATCHED=%%~fF"
)
if "%PATCHED%"=="" (
    echo  [ERROR] RaiDrive.Mount.noads.v4.exe not found.
    echo.
    echo  Put the .exe and this .bat in the SAME folder.
    echo.
    pause
    exit /b 1
)

echo  Target : %TARGET%
echo  Patch  : %PATCHED%
echo.

REM ---------- step 1: verify patch file ----------
echo  [1/5] Verifying patch file...
call :GetHash "%PATCHED%"
if /i not "!HASH!"=="%EXPECTED%" (
    echo.
    echo  [ERROR] Patch file is corrupted or incomplete.
    echo          Please download RaiDrive.Mount.noads.v4.exe again.
    echo.
    pause
    exit /b 1
)
echo         OK

REM ---------- step 2: already installed? ----------
echo  [2/5] Checking current version...
call :GetHash "%TARGET%"
if /i "!HASH!"=="%EXPECTED%" (
    echo.
    echo  Already patched. Nothing to do.
    echo.
    pause
    exit /b 0
)

REM ---------- step 3: stop process ----------
echo  [3/5] Closing RaiDrive...
taskkill /F /IM "RaiDrive.Mount.exe" >nul 2>&1
taskkill /F /IM "RaiDrive.NotifyHelper.exe" >nul 2>&1
net stop "RaiDriveMountService" >nul 2>&1
timeout /t 2 /nobreak >nul
echo         Closed

REM ---------- step 4: backup ----------
echo  [4/5] Backing up original...
if not exist "%SCRIPT%backup" mkdir "%SCRIPT%backup"
set "STAMP=%DATE:~0,4%%DATE:~5,2%%DATE:~8,2%-%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%"
set "STAMP=!STAMP: =0!"
set "BAK=%SCRIPT%backup\RaiDrive.Mount.exe.!STAMP!.bak"
copy /y "%TARGET%" "!BAK!" >nul
if errorlevel 1 (
    echo  [ERROR] Backup failed. Aborted, nothing was modified.
    pause
    exit /b 1
)
echo         Saved to backup folder

REM ---------- step 5: replace and re-verify ----------
echo  [5/5] Replacing and verifying...
copy /y "%PATCHED%" "%TARGET%" >nul
if errorlevel 1 (
    echo.
    echo  [ERROR] Replace failed. RaiDrive may still be running.
    echo          Please reboot and try again.
    echo.
    pause
    exit /b 1
)

call :GetHash "%TARGET%"
if /i not "!HASH!"=="%EXPECTED%" (
    echo.
    echo  [ERROR] Verification failed. Rolling back...
    copy /y "!BAK!" "%TARGET%" >nul
    echo         Rolled back to original.
    echo.
    pause
    exit /b 1
)

echo.
echo ============================================
echo   Done! Ad banner removed.
echo ============================================
echo.
echo  Start RaiDrive now.
echo.
echo  To revert: run "restore.bat"
echo.
pause
exit /b 0

REM ---------- sub: SHA256 of a file ----------
:GetHash
set "HASH="
for /f "skip=1 delims=" %%H in ('certutil -hashfile "%~1" SHA256') do (
    if not defined HASH (
        set "L=%%H"
        set "L=!L: =!"
        if "!L:~64!"=="" if not "!L:~63,1!"=="" set "HASH=!L!"
    )
)
exit /b
