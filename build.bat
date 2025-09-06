@echo off
echo Building PyWinInstall...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo Failed to restore packages
    pause
    exit /b 1
)

dotnet build
if %ERRORLEVEL% neq 0 (
    echo Build failed
    pause
    exit /b 1
)

echo Build completed successfully!
echo.
echo To run the application:
echo   dotnet run
echo.
echo Or build release version:
echo   dotnet build --configuration Release
echo   dotnet run --configuration Release
echo.
pause
