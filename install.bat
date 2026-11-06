@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion
title RaiDrive Ad Remover - Install

REM ============================================================
REM  Double-click this file. No commands to type.
REM  It will ask for admin rights (UAC -> click Yes).
REM
REM  You do NOT need to download RaiDrive.Mount.noads.v4.exe
REM  separately. If it is missing, this script fetches it from
REM  GitHub automatically.
REM
REM  NOTE: This file is intentionally ASCII-only.
REM  cmd.exe parses .bat in the system ANSI codepage (GBK on
REM  Chinese Windows), so non-ASCII comments break parsing.
REM ============================================================

set "SCRIPT=%~dp0"
set "TARGET=C:\Program Files\OpenBoxLab\RaiDrive Mount\RaiDrive.Mount.exe"
set "EXPECTED=0acccd3ee5f914d1aaba87d8f6bc38e19374379f296a55505937ad06a1a259fa"

set "EXENAME=RaiDrive.Mount.noads.v4.exe"
set "DLURL=https://github.com/eeetechen/raidrive-no-ads/releases/download/v2026.10.0/RaiDrive.Mount.noads.v4.exe"

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

echo  Target : %TARGET%
echo.

REM ============================================================
REM  Step 1: get the patched exe (local, or download)
REM ============================================================
echo  [1/5] Locating patch file...

set "PATCHED="
if exist "%SCRIPT%%EXENAME%"        set "PATCHED=%SCRIPT%%EXENAME%"
if not defined PATCHED if exist "%SCRIPT%bin\%EXENAME%"    set "PATCHED=%SCRIPT%bin\%EXENAME%"
if not defined PATCHED if exist "%SCRIPT%..\bin\%EXENAME%" set "PATCHED=%SCRIPT%..\bin\%EXENAME%"

set "NEED_DOWNLOAD=0"
if not defined PATCHED (
    echo         Not found locally.
    set "NEED_DOWNLOAD=1"
) else (
    call :GetHash "!PATCHED!"
    if /i "!HASH!"=="%EXPECTED%" (
        echo         Found: !PATCHED!
        echo         SHA256 OK.
    ) else (
        echo         Found but hash mismatch - will download a fresh copy.
        set "NEED_DOWNLOAD=1"
    )
)

if "!NEED_DOWNLOAD!"=="1" (
    set "PATCHED=%SCRIPT%%EXENAME%"
    echo.
    echo         Downloading from GitHub...
    echo         !DLURL!
    echo.
    call :Download "!DLURL!" "!PATCHED!"
    if errorlevel 1 (
        echo.
        echo  [ERROR] Download failed.
        echo.
        echo  Possible reasons:
        echo    - No internet connection
        echo    - github.com is blocked or slow in your network
        echo.
        echo  Workaround: download the file manually in a browser
        echo    %DLURL%
        echo  and put it in the SAME folder as this .bat, then
        echo  run this script again.
        echo.
        pause
        exit /b 1
    )
    call :GetHash "!PATCHED!"
    if /i not "!HASH!"=="%EXPECTED%" (
        echo.
        echo  [ERROR] Downloaded file failed verification.
        echo          Expected: %EXPECTED%
        echo          Got     : !HASH!
        echo.
        echo  The download may be incomplete or tampered with.
        echo  Delete the file and try again.
        echo.
        pause
        exit /b 1
    )
    echo         Downloaded and verified OK.
)

REM ---------- step 2: already patched? ----------
echo.
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
copy /y "!PATCHED!" "%TARGET%" >nul
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

REM ============================================================
REM  Sub: SHA256 of a file  ->  !HASH!
REM ============================================================
:GetHash
set "HASH="
for /f "skip=1 delims=" %%H in ('certutil -hashfile "%~1" SHA256 2^>nul') do (
    if not defined HASH (
        set "L=%%H"
        set "L=!L: =!"
        if "!L:~64!"=="" if not "!L:~63,1!"=="" set "HASH=!L!"
    )
)
exit /b

REM ============================================================
REM  Sub: Download %1 -> %2
REM  Tries curl.exe first (built into Win10 1803+), then
REM  PowerShell Invoke-WebRequest. Returns errorlevel 1 on failure.
REM ============================================================
:Download
if exist "%~2" del /f /q "%~2" >nul 2>&1

REM --- curl ---
where curl >nul 2>&1
if not errorlevel 1 (
    curl -L --fail --silent --show-error --retry 3 --retry-delay 2 --connect-timeout 30 --max-time 900 -o "%~2" "%~1" 2>nul
    call :SizeOK "%~2"
    if not errorlevel 1 exit /b 0
)

REM --- PowerShell fallback ---
if exist "%~2" del /f /q "%~2" >nul 2>&1
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; try { Invoke-WebRequest -Uri '%~1' -OutFile '%~2' -UseBasicParsing -TimeoutSec 900 } catch { exit 1 }" >nul 2>&1
call :SizeOK "%~2"
if not errorlevel 1 exit /b 0

if exist "%~2" del /f /q "%~2" >nul 2>&1
exit /b 1

REM ============================================================
REM  Sub: is %1 a plausibly complete file? (>1000 bytes)
REM  A truncated download is the most common failure, so a
REM  small file is treated as "not downloaded".
REM ============================================================
:SizeOK
if not exist "%~1" exit /b 1
for %%A in ("%~1") do if %%~zA LSS 1000 exit /b 1
exit /b 0
