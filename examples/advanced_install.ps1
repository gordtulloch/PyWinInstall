# Advanced PowerShell Installer Script
# This script demonstrates more advanced setup capabilities

param(
    [string]$PythonPath = "C:\Python",
    [string]$ProjectPath = (Get-Location).Path,
    [string]$ProjectName = "MyPythonProject",
    [switch]$CreateDesktopShortcut,
    [switch]$AddToPath
)

Write-Host "=== Advanced Python Environment Setup ===" -ForegroundColor Green
Write-Host "Project: $ProjectName" -ForegroundColor Yellow
Write-Host "Python Path: $PythonPath" -ForegroundColor Yellow
Write-Host "Project Path: $ProjectPath" -ForegroundColor Yellow

try {
    # Function to check if running as administrator
    function Test-Administrator {
        $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
        return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }

    # Check administrator privileges
    if (-not (Test-Administrator)) {
        Write-Host "⚠️  Warning: Not running as administrator. Some features may not work." -ForegroundColor Yellow
    }

    # Verify Python installation
    Write-Host "`n🔍 Verifying Python installation..." -ForegroundColor Cyan
    $pythonExe = Join-Path $PythonPath "python.exe"
    
    if (Test-Path $pythonExe) {
        $pythonVersion = & $pythonExe --version 2>&1
        Write-Host "✅ Python found: $pythonVersion" -ForegroundColor Green
        
        $pipVersion = & $pythonExe -m pip --version 2>&1
        Write-Host "✅ Pip version: $pipVersion" -ForegroundColor Green
    } else {
        Write-Host "❌ Python not found at $pythonExe" -ForegroundColor Red
        throw "Python installation not found"
    }

    # Create project structure
    Write-Host "`n📁 Creating project structure..." -ForegroundColor Cyan
    $projectStructure = @(
        "src",
        "tests",
        "docs",
        "data\raw",
        "data\processed",
        "notebooks",
        "scripts",
        "config"
    )

    foreach ($folder in $projectStructure) {
        $folderPath = Join-Path $ProjectPath $folder
        if (-not (Test-Path $folderPath)) {
            New-Item -ItemType Directory -Path $folderPath -Force | Out-Null
            Write-Host "  Created: $folder" -ForegroundColor Gray
        }
    }

    # Create project files
    Write-Host "`n📄 Creating project files..." -ForegroundColor Cyan
    
    # .gitignore
    $gitignoreContent = @"
# Python
__pycache__/
*.py[cod]
*$py.class
*.so
.Python
build/
develop-eggs/
dist/
downloads/
eggs/
.eggs/
lib/
lib64/
parts/
sdist/
var/
wheels/
*.egg-info/
.installed.cfg
*.egg

# Virtual environments
venv/
env/
ENV/

# IDE
.vscode/
.idea/
*.swp
*.swo

# Data
*.csv
*.xlsx
*.db
*.sqlite

# Logs
*.log

# OS
.DS_Store
Thumbs.db
"@
    $gitignoreContent | Out-File -FilePath (Join-Path $ProjectPath ".gitignore") -Encoding UTF8

    # Create main.py
    $mainPyContent = @"
#!/usr/bin/env python3
"""
$ProjectName - Main Application Entry Point
"""

import sys
import os
from pathlib import Path

def main():
    """Main application entry point."""
    print(f"Welcome to {sys.argv[0] if len(sys.argv) > 0 else '$ProjectName'}!")
    print(f"Python version: {sys.version}")
    print(f"Current directory: {os.getcwd()}")
    print(f"Project root: {Path(__file__).parent}")
    
    # Add your application logic here
    
if __name__ == "__main__":
    main()
"@
    $mainPyContent | Out-File -FilePath (Join-Path $ProjectPath "src\main.py") -Encoding UTF8

    # Create setup.py
    $setupPyContent = @"
from setuptools import setup, find_packages

setup(
    name="$ProjectName",
    version="1.0.0",
    description="A Python project created with PyWinInstall",
    packages=find_packages(where="src"),
    package_dir={"": "src"},
    python_requires=">=3.9",
    install_requires=[
        # Add your dependencies here
    ],
    entry_points={
        "console_scripts": [
            "$ProjectName=main:main",
        ],
    },
)
"@
    $setupPyContent | Out-File -FilePath (Join-Path $ProjectPath "setup.py") -Encoding UTF8

    # Upgrade pip
    Write-Host "`n⬆️  Upgrading pip..." -ForegroundColor Cyan
    & $pythonExe -m pip install --upgrade pip | Out-Host

    # Install requirements
    $requirementsFile = Join-Path $ProjectPath "requirements.txt"
    if (Test-Path $requirementsFile) {
        Write-Host "`n📦 Installing packages from requirements.txt..." -ForegroundColor Cyan
        & $pythonExe -m pip install -r $requirementsFile | Out-Host
    }

    # Create virtual environment
    Write-Host "`n🌍 Creating virtual environment..." -ForegroundColor Cyan
    $venvPath = Join-Path $ProjectPath "venv"
    & $pythonExe -m venv $venvPath | Out-Host

    # Install packages in virtual environment
    if (Test-Path $requirementsFile) {
        Write-Host "`n📦 Installing packages in virtual environment..." -ForegroundColor Cyan
        $venvPython = Join-Path $venvPath "Scripts\python.exe"
        & $venvPython -m pip install --upgrade pip | Out-Host
        & $venvPython -m pip install -r $requirementsFile | Out-Host
    }

    # Add Python to PATH if requested
    if ($AddToPath) {
        Write-Host "`n🛣️  Adding Python to system PATH..." -ForegroundColor Cyan
        $currentPath = [Environment]::GetEnvironmentVariable("PATH", [EnvironmentVariableTarget]::User)
        if ($currentPath -notlike "*$PythonPath*") {
            $newPath = "$PythonPath;$currentPath"
            [Environment]::SetEnvironmentVariable("PATH", $newPath, [EnvironmentVariableTarget]::User)
            Write-Host "✅ Python added to user PATH" -ForegroundColor Green
        } else {
            Write-Host "ℹ️  Python already in PATH" -ForegroundColor Blue
        }
    }

    # Create desktop shortcut if requested
    if ($CreateDesktopShortcut) {
        Write-Host "`n🖥️  Creating desktop shortcut..." -ForegroundColor Cyan
        $desktopPath = [Environment]::GetFolderPath("Desktop")
        $shortcutPath = Join-Path $desktopPath "$ProjectName.lnk"
        
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = Join-Path $venvPath "Scripts\python.exe"
        $shortcut.Arguments = Join-Path $ProjectPath "src\main.py"
        $shortcut.WorkingDirectory = $ProjectPath
        $shortcut.Description = "$ProjectName Python Application"
        $shortcut.Save()
        
        Write-Host "✅ Desktop shortcut created: $shortcutPath" -ForegroundColor Green
    }

    # Create comprehensive summary
    $setupSummary = @"
=== $ProjectName Setup Complete ===

📅 Installation Date: $(Get-Date)
🐍 Python Installation: $PythonPath
📂 Project Directory: $ProjectPath
🌍 Virtual Environment: $venvPath
👤 User: $env:USERNAME
💻 Computer: $env:COMPUTERNAME

📋 Project Structure:
$(Get-ChildItem $ProjectPath -Directory | ForEach-Object { "  📁 $($_.Name)" } | Out-String)

🔧 Installed Packages:
$(if (Test-Path $requirementsFile) { Get-Content $requirementsFile | Where-Object { $_ -notmatch "^#" -and $_ -ne "" } | ForEach-Object { "  📦 $_" } | Out-String } else { "  No requirements.txt found" })

🚀 Quick Start Commands:
  Activate environment: $venvPath\Scripts\activate.bat
  Run application: python src\main.py
  Install in development mode: pip install -e .
  Run tests: python -m pytest tests/

📝 Files Created:
  ✅ Project structure
  ✅ src\main.py
  ✅ setup.py  
  ✅ .gitignore
  ✅ Virtual environment
$(if ($CreateDesktopShortcut) { "  ✅ Desktop shortcut" } else { "" })

🔗 Useful Links:
  Python Documentation: https://docs.python.org/
  Virtual Environments: https://docs.python.org/3/tutorial/venv.html
  Package Installation: https://packaging.python.org/tutorials/installing-packages/

Happy coding! 🎉
"@

    $summaryPath = Join-Path $ProjectPath "SETUP_SUMMARY.txt"
    $setupSummary | Out-File -FilePath $summaryPath -Encoding UTF8
    
    Write-Host "`n✅ Setup summary saved to: $summaryPath" -ForegroundColor Green
    Write-Host "`n🎉 === Installation Complete! ===" -ForegroundColor Green
    Write-Host "📖 Check SETUP_SUMMARY.txt for details and next steps" -ForegroundColor Yellow
    
    # Open summary file
    Start-Process notepad.exe $summaryPath

} catch {
    Write-Host "`n❌ Installation failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "🔍 Error details: $($_.Exception.GetType().FullName)" -ForegroundColor Red
    exit 1
}

exit 0
