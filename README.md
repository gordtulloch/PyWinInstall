# PyWinInstall - Python Environment Installer

A Windows WPF application that automates the installation of Python, cloning of Git repositories, and execution of PowerShell installer scripts.

## Features

- **Python Installation**: Downloads and installs specified Python versions from python.org
- **Git Repository Cloning**: Clones repositories using LibGit2Sharp
- **PowerShell Script Execution**: Runs installation scripts with proper execution policy handling
- **User-Friendly Interface**: Clean WPF interface with progress tracking and output logging
- **Automated Workflow**: "Install All" button for complete automation

## Requirements

- Windows 10/11
- .NET 8.0 SDK or later ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0))
- Administrator privileges (for Python installation)
- Git (optional, for repository cloning functionality)

## Installation

### Prerequisites
1. **Install .NET 8.0 SDK**:
   - Visit https://dotnet.microsoft.com/download/dotnet/8.0
   - Download and install the SDK (not just the runtime)
   - Verify installation by running `dotnet --version` in a terminal

2. **Install Git** (optional):
   - Visit https://git-scm.com/download/win
   - Download and install Git for Windows

## Building the Application

1. Ensure you have .NET 8.0 SDK installed
2. Open terminal in project directory
3. Run: `dotnet restore`
4. Run: `dotnet build`
5. Run: `dotnet run` (or build and run the executable)

## Usage

### Individual Operations

1. **Install Python**:
   - Select desired Python version (3.9.19 to 3.12.6)
   - Choose installation directory
   - Click "Install Python"

2. **Clone Repository**:
   - Enter Git repository URL
   - Select destination directory
   - Click "Clone Repository"

3. **Run PowerShell Script**:
   - Browse to or enter PowerShell script path
   - Click "Run Script"

### Automated Installation

1. Configure all three sections with your desired settings
2. Click "Install All" to run the complete process automatically

## Configuration Options

### Python Installation
- **Version**: Choose from supported Python versions
- **Install Path**: Directory where Python will be installed (default: C:\Python)

### Git Repository
- **Repository URL**: Full Git repository URL (https or ssh)
- **Clone Path**: Directory where repository will be cloned

### PowerShell Script
- **Script Path**: Path to the .ps1 installer script
- The application automatically looks for `install.ps1` in cloned repositories

## Sample PowerShell Script

The project includes `sample_install.ps1` which demonstrates:
- Python installation verification
- pip upgrade
- Package installation from requirements.txt
- Virtual environment creation
- Setup summary generation

## Dependencies

- **LibGit2Sharp**: Git operations
- **System.Management.Automation**: PowerShell execution
- **System.Windows.Forms**: File/folder dialogs
- **Newtonsoft.Json**: JSON handling

## Security Considerations

- The application requires administrator privileges for Python installation
- PowerShell execution policy is temporarily bypassed for script execution
- Always review PowerShell scripts before execution
- Repository cloning uses default Git credentials

## Troubleshooting

### Python Installation Issues
- Ensure you have administrator privileges
- Check if antivirus is blocking the installer
- Verify internet connection for download

### Git Clone Issues
- Verify repository URL is accessible
- Check Git credentials for private repositories
- Ensure destination directory has write permissions

### PowerShell Script Issues
- Verify script file exists and is accessible
- Check script syntax using PowerShell ISE
- Review execution policy settings

## Customization

### Adding New Python Versions
Edit the ComboBox items in `MainWindow.xaml`:
```xml
<ComboBoxItem Content="3.13.0"/>
```

### Modifying Download URLs
Update the `GetPythonDownloadUrl` method in `MainWindow.xaml.cs`

### Custom Script Parameters
Modify the PowerShell execution code to pass custom parameters to scripts

## License

This project is provided as-is for educational and development purposes.

## Contributing

Feel free to submit issues and enhancement requests!
