@echo off
setlocal

set "DOTNET_EXE=E:\DevTools\dotnet\dotnet.exe"
set "PROJECT_FILE=D:\program\ratepulse\src\RatePulse.Windows\RatePulse.Windows.csproj"
set "OUTPUT_DIR=D:\program\ratepulse\release\win-x64"

if not exist "%DOTNET_EXE%" (
    echo Publish failed.
    echo.
    echo Missing .NET SDK:
    echo %DOTNET_EXE%
    echo.
    pause
    exit /b 1
)

if not exist "%PROJECT_FILE%" (
    echo Publish failed.
    echo.
    echo Missing project file:
    echo %PROJECT_FILE%
    echo.
    pause
    exit /b 1
)

echo Publishing RatePulse for Windows x64...
echo Output: %OUTPUT_DIR%
echo.

"%DOTNET_EXE%" publish "%PROJECT_FILE%" ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "%OUTPUT_DIR%" ^
    /p:PublishSingleFile=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:EnableCompressionInSingleFile=true

if errorlevel 1 (
    echo.
    echo Publish failed.
    pause
    exit /b 1
)

echo.
echo Publish complete.
echo Run:
echo %OUTPUT_DIR%\RatePulse.Windows.exe
echo.
pause
endlocal
