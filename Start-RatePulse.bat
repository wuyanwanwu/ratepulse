@echo off
setlocal

set "DOTNET_EXE=E:\DevTools\dotnet\dotnet.exe"
set "PROJECT_FILE=D:\program\ratepulse\src\RatePulse.Windows\RatePulse.Windows.csproj"
set "PUBLISHED_EXE=D:\program\ratepulse\release\win-x64\RatePulse.Windows.exe"

if exist "%PUBLISHED_EXE%" (
    start "" "%PUBLISHED_EXE%"
    exit /b 0
)

if not exist "%DOTNET_EXE%" (
    echo RatePulse could not start.
    echo.
    echo Missing .NET SDK:
    echo %DOTNET_EXE%
    echo.
    pause
    exit /b 1
)

if not exist "%PROJECT_FILE%" (
    echo RatePulse could not start.
    echo.
    echo Missing project file:
    echo %PROJECT_FILE%
    echo.
    pause
    exit /b 1
)

cd /d "D:\program\ratepulse"
"%DOTNET_EXE%" run --project "%PROJECT_FILE%"

if errorlevel 1 (
    echo.
    echo RatePulse exited with an error.
    pause
    exit /b 1
)

endlocal
