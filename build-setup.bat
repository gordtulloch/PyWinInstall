@echo off
echo Building PyWinInstall as setup.exe...

echo.
echo Step 1: Building Release version...
dotnet build --configuration Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Step 2: Publishing self-contained executable...
dotnet publish --configuration Release --self-contained true --runtime win-x64 --output ./dist
if %ERRORLEVEL% neq 0 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo Step 3: Copying configuration file...
copy "setup.json" "dist\" >nul
if %ERRORLEVEL% neq 0 (
    echo Warning: Could not copy setup.json
)

echo.
echo Step 4: Creating distribution package...
if exist "PyWinInstall-Setup.zip" del "PyWinInstall-Setup.zip"
powershell -Command "Compress-Archive -Path 'dist\*' -DestinationPath 'PyWinInstall-Setup.zip'"

echo.
echo ===== BUILD COMPLETE =====
echo.
echo Files created:
echo   - dist\setup.exe (main executable)
echo   - dist\setup.json (configuration file)
echo   - PyWinInstall-Setup.zip (distribution package)
echo.
echo To run: Navigate to 'dist' folder and run setup.exe
echo The setup.json file contains default configuration settings.
echo.
pause
