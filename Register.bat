@echo off
:: Check for administrative privileges
net session >nul 2>&1
if %errorlevel% == 0 (
    echo Running with administrative privileges
) else (
    echo Requesting administrative privileges...
    goto UACPrompt
)

goto Start

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    exit /B

:Start
cd /d "%~dp0"

:: Find DLL files ending with comhost.dll
for /f "delims=" %%a in ('dir /b *comhost.dll') do set "DLLFile=%%a"
if defined DLLFile goto Found

echo No DLL file ending with comhost.dll found, please enter the DLL name:
SET /P DLLName=Enter the DLL name to register (include .dll extension):
set DLLFile=%DLLName%

:Found
if not exist "%DLLFile%" (
    echo The specified DLL file does not exist.
    pause
    exit /b
)

regsvr32 /s "%DLLFile%"
echo %DLLFile% registered.