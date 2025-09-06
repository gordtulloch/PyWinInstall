# Sample PowerShell Installer Script
# This script demonstrates how to complete a Python environment setup

param(
    [string]$PythonPath = "C:\Python",
    [string]$ProjectPath = (Get-Location).Path
)

Write-Host "=== Python Environment Setup Script ===" -ForegroundColor Green
Write-Host "Python Path: $PythonPath" -ForegroundColor Yellow
Write-Host "Project Path: $ProjectPath" -ForegroundColor Yellow

try {
    # Verify Python installation
    Write-Host "`nVerifying Python installation..." -ForegroundColor Cyan
    $pythonExe = Join-Path $PythonPath "python.exe"
    
    if (Test-Path $pythonExe) {
        $pythonVersion = & $pythonExe --version
        Write-Host "✓ Python found: $pythonVersion" -ForegroundColor Green
    } else {
        Write-Host "✗ Python not found at $pythonExe" -ForegroundColor Red
        throw "Python installation not found"
    }

    # Upgrade pip
    Write-Host "`nUpgrading pip..." -ForegroundColor Cyan
    & $pythonExe -m pip install --upgrade pip
    Write-Host "✓ pip upgraded successfully" -ForegroundColor Green

    # Install requirements if requirements.txt exists
    $requirementsFile = Join-Path $ProjectPath "requirements.txt"
    if (Test-Path $requirementsFile) {
        Write-Host "`nInstalling Python packages from requirements.txt..." -ForegroundColor Cyan
        & $pythonExe -m pip install -r $requirementsFile
        Write-Host "✓ Requirements installed successfully" -ForegroundColor Green
    } else {
        Write-Host "`nNo requirements.txt found. Installing common packages..." -ForegroundColor Cyan
        $commonPackages = @("requests", "numpy", "pandas", "matplotlib")
        foreach ($package in $commonPackages) {
            Write-Host "Installing $package..." -ForegroundColor Yellow
            & $pythonExe -m pip install $package
        }
        Write-Host "✓ Common packages installed successfully" -ForegroundColor Green
    }

    # Create virtual environment
    Write-Host "`nCreating virtual environment..." -ForegroundColor Cyan
    $venvPath = Join-Path $ProjectPath "venv"
    & $pythonExe -m venv $venvPath
    Write-Host "✓ Virtual environment created at $venvPath" -ForegroundColor Green

    # Create activation script
    $activateScript = @"
@echo off
echo Activating Python virtual environment...
call "$venvPath\Scripts\activate.bat"
echo Virtual environment activated!
echo Python path: %VIRTUAL_ENV%
echo To deactivate, type: deactivate
cmd /k
"@

    $activateScriptPath = Join-Path $ProjectPath "activate_env.bat"
    $activateScript | Out-File -FilePath $activateScriptPath -Encoding ASCII
    Write-Host "✓ Activation script created: $activateScriptPath" -ForegroundColor Green

    # Create project setup summary
    $setupSummary = @"
=== Python Environment Setup Complete ===

Python Installation: $PythonPath
Project Directory: $ProjectPath
Virtual Environment: $venvPath

To activate the virtual environment:
1. Run: $activateScriptPath
2. Or manually: $venvPath\Scripts\activate.bat

Python Version: $pythonVersion
Pip Version: $(& $pythonExe -m pip --version)

Installation completed successfully at $(Get-Date)
"@

    $summaryPath = Join-Path $ProjectPath "setup_summary.txt"
    $setupSummary | Out-File -FilePath $summaryPath -Encoding UTF8
    Write-Host "`n✓ Setup summary saved to: $summaryPath" -ForegroundColor Green

    Write-Host "`n=== Installation Complete! ===" -ForegroundColor Green
    Write-Host "Check setup_summary.txt for details" -ForegroundColor Yellow

} catch {
    Write-Host "`n✗ Installation failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

exit 0
