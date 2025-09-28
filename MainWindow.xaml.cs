using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using LibGit2Sharp;
using Microsoft.Win32;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace PyWinInstall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HttpClient httpClient;
        private InstallationConfig? config;
        private bool isInitializing = true;

        public MainWindow()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            
            // Load configuration
            LoadConfiguration();
            
            LogOutput("PyWinInstall ready. Select options and click 'Complete Setup' or use individual buttons.");
            
            // Check for existing Python installation on startup
            if (config?.DefaultSettings?.Application?.AutoDetectPython == true)
            {
                CheckForExistingPython();
            }
            
            // Mark initialization as complete
            isInitializing = false;
        }

        private void LoadConfiguration()
        {
            try
            {
                config = InstallationConfig.Load();
                ApplyDefaultSettings();
                LogOutput("Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                LogOutput($"Error loading configuration: {ex.Message}");
                config = new InstallationConfig();
            }
        }

        private void ApplyDefaultSettings()
        {
            if (config?.DefaultSettings == null) return;
            
            // Apply Python settings
            PythonPathTextBox.Text = config.DefaultSettings.Python.InstallPath;
            
            // Set Python version in ComboBox
            foreach (ComboBoxItem item in PythonVersionComboBox.Items)
            {
                if (item.Content.ToString() == config.DefaultSettings.Python.Version)
                {
                    PythonVersionComboBox.SelectedItem = item;
                    break;
                }
            }

            // Apply Git settings
            RepoUrlTextBox.Text = config.DefaultSettings.Git.RepositoryUrl;
            ClonePathTextBox.Text = config.DefaultSettings.Git.ClonePath;

            // Apply Python already installed setting
            PythonAlreadyInstalledCheckBox.IsChecked = config.DefaultSettings.Python.AlreadyInstalled;
            if (config.DefaultSettings.Python.AlreadyInstalled)
            {
                UpdatePythonSectionState(false);
            }

            // Apply desktop shortcut setting
            CreateDesktopShortcutCheckBox.IsChecked = config.DefaultSettings.Application.CreateDesktopShortcut;
            
            // Apply target program setting
            TargetProgramTextBox.Text = config.DefaultSettings.Application.TargetProgram;
        }

        private void CheckForExistingPython()
        {
            string existingPython = CheckExistingPython(PythonPathTextBox.Text);
            if (!string.IsNullOrEmpty(existingPython))
            {
                LogOutput($"Detected existing Python installation: {existingPython}");
                if (config?.DefaultSettings?.Python?.AlreadyInstalled != true)
                {
                    PythonAlreadyInstalledCheckBox.IsChecked = true;
                    UpdatePythonSectionState(false);
                }
            }
        }

        private void PythonAlreadyInstalledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdatePythonSectionState(false);
            LogOutput("Python installation section disabled - using existing Python installation");
            if (!isInitializing)
                SaveCurrentSettings();
        }

        private void PythonAlreadyInstalledCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdatePythonSectionState(true);
            LogOutput("Python installation section enabled");
            if (!isInitializing)
                SaveCurrentSettings();
        }

        private void SaveCurrentSettings()
        {
            try
            {
                if (config?.DefaultSettings == null) return;
                
                // Update config with current UI values
                config.DefaultSettings.Python.InstallPath = PythonPathTextBox.Text;
                config.DefaultSettings.Python.AlreadyInstalled = PythonAlreadyInstalledCheckBox.IsChecked ?? false;
                
                if (PythonVersionComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    config.DefaultSettings.Python.Version = selectedItem.Content.ToString() ?? "3.12.6";
                }

                config.DefaultSettings.Git.RepositoryUrl = RepoUrlTextBox.Text;
                config.DefaultSettings.Git.ClonePath = ClonePathTextBox.Text;
                config.DefaultSettings.Application.CreateDesktopShortcut = CreateDesktopShortcutCheckBox.IsChecked ?? true;
                config.DefaultSettings.Application.TargetProgram = TargetProgramTextBox.Text;

                config.Save();
                LogOutput("Settings saved to setup.json");
            }
            catch (Exception ex)
            {
                LogOutput($"Warning: Could not save settings: {ex.Message}");
            }
        }

        private void UpdatePythonSectionState(bool isEnabled)
        {
            PythonInstallGrid.IsEnabled = isEnabled;
            PythonInstallGrid.Opacity = isEnabled ? 1.0 : 0.5;
            
            // Update individual controls
            PythonVersionComboBox.IsEnabled = isEnabled;
            PythonPathTextBox.IsEnabled = isEnabled;
            InstallPythonButton.IsEnabled = isEnabled;
            BrowsePythonPathButton.IsEnabled = isEnabled;
        }

        // Event handlers for auto-saving settings
        private void PythonPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (config != null && !isInitializing) // Only save if config is loaded and not initializing
                SaveCurrentSettings();
        }

        private void RepoUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (config != null && !isInitializing)
                SaveCurrentSettings();
        }

        private void ClonePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (config != null && !isInitializing)
                SaveCurrentSettings();
        }

        private void TargetProgramTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (config != null && !isInitializing)
                SaveCurrentSettings();
        }

        private void PythonVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (config != null && !isInitializing)
                SaveCurrentSettings();
        }

        private void LogOutput(string message)
        {
            Dispatcher.Invoke(() =>
            {
                OutputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                OutputTextBox.ScrollToEnd();
            });
        }

        private void SetProgress(bool isVisible, double value = 0)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                ProgressBar.Value = value;
            });
        }

        private async void InstallPythonButton_Click(object sender, RoutedEventArgs e)
        {
            await InstallPython();
        }

        private async Task<bool> InstallPython()
        {
            try
            {
                // Check if Python installation is skipped
                if (PythonAlreadyInstalledCheckBox.IsChecked == true)
                {
                    LogOutput("Skipping Python installation - using existing installation");
                    
                    // Verify existing Python installation
                    string verifyPython = CheckExistingPython(PythonPathTextBox.Text);
                    if (!string.IsNullOrEmpty(verifyPython))
                    {
                        LogOutput($"Verified existing Python: {verifyPython}");
                        return true;
                    }
                    else
                    {
                        LogOutput("WARNING: Could not verify existing Python installation");
                        var result = MessageBox.Show(
                            "Could not verify existing Python installation.\n\nDo you want to proceed anyway?",
                            "Python Verification Failed",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        
                        return result == MessageBoxResult.Yes;
                    }
                }

                SetProgress(true, 0);
                LogOutput("Starting Python installation...");

                string version = ((ComboBoxItem)PythonVersionComboBox.SelectedItem).Content.ToString()!;
                string installPath = PythonPathTextBox.Text.Trim();

                if (string.IsNullOrEmpty(installPath))
                {
                    LogOutput("ERROR: Please specify a Python installation path.");
                    return false;
                }

                // Create directory if it doesn't exist
                Directory.CreateDirectory(installPath);

                // Download Python installer
                string downloadUrl = GetPythonDownloadUrl(version);
                string installerPath = Path.Combine(Path.GetTempPath(), $"python-{version}-installer.exe");

                LogOutput($"Downloading Python {version} from {downloadUrl}...");
                SetProgress(true, 25);

                using (var response = await httpClient.GetAsync(downloadUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        LogOutput($"ERROR: Failed to download Python installer. Status: {response.StatusCode}");
                        return false;
                    }

                    await using (var fileStream = File.Create(installerPath))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                LogOutput("Download completed. Starting installation...");
                SetProgress(true, 50);

                // Check if Python is already installed
                string existingPython = CheckExistingPython(installPath);
                if (!string.IsNullOrEmpty(existingPython))
                {
                    LogOutput($"Python already found at {existingPython}");
                    var result = MessageBox.Show(
                        $"Python is already installed at {existingPython}.\n\nDo you want to:\n" +
                        "• YES: Proceed with existing installation\n" +
                        "• NO: Continue with new installation (may upgrade/reinstall)\n" +
                        "• CANCEL: Skip Python installation",
                        "Python Already Installed",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        LogOutput($"Using existing Python installation at {existingPython}");
                        SetProgress(true, 100);
                        try { File.Delete(installerPath); } catch { }
                        return true;
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        LogOutput("Python installation skipped by user");
                        try { File.Delete(installerPath); } catch { }
                        return false;
                    }
                    // If NO, continue with installation
                }

                // Run Python installer
                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = $"/quiet InstallAllUsers=1 PrependPath=1 TargetDir=\"{installPath}\" SimpleInstall=1",
                    UseShellExecute = true,
                    Verb = "runas" // Run as administrator
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        
                        if (process.ExitCode == 0)
                        {
                            LogOutput($"Python {version} installed successfully to {installPath}");
                            SetProgress(true, 100);
                            
                            // Clean up installer
                            try { File.Delete(installerPath); } catch { }
                            
                            return true;
                        }
                        else if (process.ExitCode == 1638)
                        {
                            LogOutput($"Python installation completed (exit code 1638 - another version was already installed)");
                            LogOutput($"Verifying Python installation at {installPath}...");
                            
                            // Verify the installation worked despite the error code
                            string verifyResult = CheckExistingPython(installPath);
                            if (!string.IsNullOrEmpty(verifyResult))
                            {
                                LogOutput($"Python verification successful: {verifyResult}");
                                SetProgress(true, 100);
                                try { File.Delete(installerPath); } catch { }
                                return true;
                            }
                            else
                            {
                                LogOutput("Python verification failed - installation may not have completed properly");
                            }
                        }
                        else
                        {
                            LogOutput($"ERROR: Python installation failed with exit code {process.ExitCode}");
                            LogOutput("Common exit codes:");
                            LogOutput("  1602: User cancelled installation");
                            LogOutput("  1603: Fatal error during installation");
                            LogOutput("  1638: Another version already installed");
                            LogOutput("  3010: Success, restart required");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogOutput($"ERROR during Python installation: {ex.Message}");
            }
            finally
            {
                SetProgress(false);
            }

            return false;
        }

        private string GetPythonDownloadUrl(string version)
        {
            // For x64 Windows
            return $"https://www.python.org/ftp/python/{version}/python-{version}-amd64.exe";
        }

        private string CheckExistingPython(string targetPath)
        {
            try
            {
                // Check target path first
                string pythonExe = Path.Combine(targetPath, "python.exe");
                if (File.Exists(pythonExe))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();
                            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                            {
                                return $"{pythonExe} ({output.Trim()})";
                            }
                        }
                    }
                }

                // Check common Python installation locations
                string[] commonPaths = {
                    @"C:\Python312",
                    @"C:\Python311", 
                    @"C:\Python310",
                    @"C:\Python39",
                    @"C:\Program Files\Python312",
                    @"C:\Program Files\Python311",
                    @"C:\Program Files\Python310",
                    @"C:\Program Files\Python39",
                    @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python\Python312",
                    @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python\Python311",
                    @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python\Python310",
                    @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python\Python39"
                };

                foreach (string path in commonPaths)
                {
                    pythonExe = Path.Combine(path, "python.exe");
                    if (File.Exists(pythonExe))
                    {
                        try
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = pythonExe,
                                Arguments = "--version",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                CreateNoWindow = true
                            };

                            using (var process = Process.Start(startInfo))
                            {
                                if (process != null)
                                {
                                    string output = process.StandardOutput.ReadToEnd();
                                    process.WaitForExit();
                                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                                    {
                                        return $"{pythonExe} ({output.Trim()})";
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Continue checking other paths
                        }
                    }
                }

                // Try to find Python in PATH
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();
                            
                            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                            {
                                return $"python (in PATH) ({output.Trim()})";
                            }
                        }
                    }
                }
                catch
                {
                    // Python not in PATH
                }
            }
            catch (Exception ex)
            {
                LogOutput($"Error checking existing Python: {ex.Message}");
            }

            return string.Empty;
        }

        private async void CloneRepoButton_Click(object sender, RoutedEventArgs e)
        {
            await CloneRepository();
        }

        private async Task<bool> CloneRepository()
        {
            try
            {
                SetProgress(true, 0);
                LogOutput("Starting repository clone...");

                string repoUrl = RepoUrlTextBox.Text.Trim();
                string clonePath = ClonePathTextBox.Text.Trim();

                if (string.IsNullOrEmpty(repoUrl) || string.IsNullOrEmpty(clonePath))
                {
                    LogOutput("ERROR: Please specify both repository URL and clone path.");
                    return false;
                }

                // Extract repository name from URL
                string repoName = Path.GetFileNameWithoutExtension(repoUrl.Split('/').Last());
                string fullClonePath = Path.Combine(clonePath, repoName);

                // Create directory if it doesn't exist
                Directory.CreateDirectory(clonePath);

                SetProgress(true, 25);

                await Task.Run(() =>
                {
                    try
                    {
                        LogOutput($"Cloning {repoUrl} to {fullClonePath}...");
                        
                        var cloneOptions = new CloneOptions();

                        Repository.Clone(repoUrl, fullClonePath, cloneOptions);
                        
                        LogOutput($"Repository cloned successfully to {fullClonePath}");
                        SetProgress(true, 100);
                    }
                    catch (Exception ex)
                    {
                        LogOutput($"ERROR during repository clone: {ex.Message}");
                        throw;
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                LogOutput($"ERROR during repository clone: {ex.Message}");
                return false;
            }
            finally
            {
                SetProgress(false);
            }
        }

        private async void SetupEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            await SetupPythonEnvironment();
        }

        private async Task<bool> SetupPythonEnvironment()
        {
            try
            {
                SetProgress(true, 0);
                LogOutput("Starting Python environment setup...");

                string projectPath = ClonePathTextBox.Text.Trim();
                if (string.IsNullOrEmpty(projectPath))
                {
                    LogOutput("ERROR: Please specify a project directory.");
                    return false;
                }

                // Extract repository name from URL for the project directory
                string repoUrl = RepoUrlTextBox.Text.Trim();
                string repoName = Path.GetFileNameWithoutExtension(repoUrl.Split('/').Last());
                string fullProjectPath = Path.Combine(projectPath, repoName);

                if (!Directory.Exists(fullProjectPath))
                {
                    LogOutput($"ERROR: Project directory does not exist: {fullProjectPath}");
                    LogOutput("Please clone the repository first.");
                    return false;
                }

                SetProgress(true, 25);

                // Check if virtual environment should be created
                if (CreateVenvCheckBox.IsChecked == true)
                {
                    await CreateVirtualEnvironment(fullProjectPath);
                }

                SetProgress(true, 60);

                // Install packages if requested
                if (InstallPackagesCheckBox.IsChecked == true)
                {
                    await InstallPythonPackages(fullProjectPath);
                }

                SetProgress(true, 100);
                LogOutput("Python environment setup completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                LogOutput($"ERROR during Python environment setup: {ex.Message}");
                return false;
            }
            finally
            {
                SetProgress(false);
            }
        }

        private async Task<bool> CreateVirtualEnvironment(string projectPath)
        {
            try
            {
                LogOutput("Creating virtual environment...");
                
                string venvPath = Path.Combine(projectPath, ".venv");
                
                // Remove existing venv if it exists
                if (Directory.Exists(venvPath))
                {
                    LogOutput("Removing existing virtual environment...");
                    Directory.Delete(venvPath, true);
                }

                await Task.Run(() =>
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = "-m venv .venv",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = projectPath
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            
                            process.WaitForExit();

                            if (process.ExitCode == 0)
                            {
                                LogOutput("Virtual environment created successfully.");
                                return true;
                            }
                            else
                            {
                                LogOutput($"ERROR creating virtual environment: {error}");
                                return false;
                            }
                        }
                    }
                    return false;
                });

                return true;
            }
            catch (Exception ex)
            {
                LogOutput($"ERROR creating virtual environment: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> InstallPythonPackages(string projectPath)
        {
            try
            {
                LogOutput("Installing Python packages...");
                SetProgress(true, 0);

                string venvPython = Path.Combine(projectPath, ".venv", "Scripts", "python.exe");
                
                if (!File.Exists(venvPython))
                {
                    LogOutput("ERROR: Virtual environment not found. Please create it first.");
                    SetProgress(false);
                    return false;
                }

                // Upgrade pip first
                LogOutput("Upgrading pip...");
                SetProgress(true, 10);
                bool pipUpgradeSuccess = await RunPipCommandWithLiveOutput(venvPython, "-m pip install --upgrade pip", projectPath);
                
                if (!pipUpgradeSuccess)
                {
                    LogOutput("Warning: Failed to upgrade pip, but continuing...");
                }

                // Check for requirements.txt
                string requirementsPath = Path.Combine(projectPath, "requirements.txt");
                
                if (File.Exists(requirementsPath))
                {
                    LogOutput("Installing packages from requirements.txt...");
                    SetProgress(true, 30);
                    bool reqSuccess = await RunPipCommandWithLiveOutput(venvPython, "-m pip install -r requirements.txt", projectPath);
                    
                    if (!reqSuccess)
                    {
                        LogOutput("ERROR: Failed to install packages from requirements.txt");
                        SetProgress(false);
                        return false;
                    }
                    SetProgress(true, 90);
                }
                else
                {
                    LogOutput("No requirements.txt found. Installing common packages...");
                    string[] packages = { "astropy", "peewee", "numpy", "matplotlib", "pytz", "PySide6" };
                    
                    for (int i = 0; i < packages.Length; i++)
                    {
                        string package = packages[i];
                        LogOutput($"Installing {package}...");
                        SetProgress(true, 30 + (50 * (i + 1) / packages.Length)); // Progress from 30% to 80%
                        
                        bool packageSuccess = await RunPipCommandWithLiveOutput(venvPython, $"-m pip install {package}", projectPath);
                        
                        if (!packageSuccess)
                        {
                            LogOutput($"Warning: Failed to install {package}, but continuing...");
                        }
                    }
                }

                LogOutput("Package installation completed.");
                SetProgress(true, 100);
                await Task.Delay(1000); // Show 100% briefly
                SetProgress(false);
                return true;
            }
            catch (Exception ex)
            {
                LogOutput($"ERROR installing packages: {ex.Message}");
                SetProgress(false);
                return false;
            }
        }

        private async Task<bool> RunPipCommandWithLiveOutput(string pythonPath, string arguments, string workingDirectory)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null) return false;

                    // Read output in real-time
                    var outputTask = Task.Run(async () =>
                    {
                        while (!process.StandardOutput.EndOfStream)
                        {
                            string? line = await process.StandardOutput.ReadLineAsync();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                // Filter and format pip output for better readability
                                if (line.Contains("Collecting") || 
                                    line.Contains("Downloading") || 
                                    line.Contains("Installing") ||
                                    line.Contains("Successfully installed") ||
                                    line.Contains("Requirement already satisfied") ||
                                    line.Contains("Using cached"))
                                {
                                    // Use Dispatcher to update UI from background thread
                                    Dispatcher.Invoke(() => LogOutput($"  {line.Trim()}"));
                                }
                                else if (line.Contains("ERROR") || line.Contains("Error"))
                                {
                                    Dispatcher.Invoke(() => LogOutput($"  ERROR: {line.Trim()}"));
                                }
                            }
                        }
                    });

                    var errorTask = Task.Run(async () =>
                    {
                        while (!process.StandardError.EndOfStream)
                        {
                            string? line = await process.StandardError.ReadLineAsync();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                // Use Dispatcher to update UI from background thread
                                Dispatcher.Invoke(() => LogOutput($"  WARNING: {line.Trim()}"));
                            }
                        }
                    });

                    // Wait for process to complete
                    await process.WaitForExitAsync();
                    
                    // Wait for all output to be read
                    await Task.WhenAll(outputTask, errorTask);

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                LogOutput($"ERROR running pip command: {ex.Message}");
                return false;
            }
        }

        private async void CreateRunScriptsButton_Click(object sender, RoutedEventArgs e)
        {
            await CreateRunScripts();
        }

        private async Task<bool> CreateRunScripts()
        {
            try
            {
                LogOutput("Creating run scripts...");

                string projectPath = ClonePathTextBox.Text.Trim();
                if (string.IsNullOrEmpty(projectPath))
                {
                    LogOutput("ERROR: Please specify a project directory.");
                    return false;
                }

                // Extract repository name from URL for the project directory
                string repoUrl = RepoUrlTextBox.Text.Trim();
                string repoName = Path.GetFileNameWithoutExtension(repoUrl.Split('/').Last());
                string fullProjectPath = Path.Combine(projectPath, repoName);

                if (!Directory.Exists(fullProjectPath))
                {
                    LogOutput($"ERROR: Project directory does not exist: {fullProjectPath}");
                    return false;
                }

                await Task.Run(() =>
                {
                    // Create PowerShell run script
                    string psScript = @"# AstroFiler Runner Script
Set-Location -Path $PSScriptRoot
& "".\\.venv\\Scripts\\Activate.ps1""
python astrofiler.py
Read-Host ""Press Enter to exit""";

                    File.WriteAllText(Path.Combine(fullProjectPath, "run_astrofiler.ps1"), psScript);

                    // Create Batch run script
                    string batScript = @"@echo off
cd /d ""%~dp0""
call .venv\Scripts\activate.bat
python astrofiler.py
pause";

                    File.WriteAllText(Path.Combine(fullProjectPath, "run_astrofiler.bat"), batScript);

                    LogOutput("Run scripts created successfully:");
                    LogOutput("  - run_astrofiler.ps1 (PowerShell)");
                    LogOutput("  - run_astrofiler.bat (Command Prompt)");
                });

                return true;
            }
            catch (Exception ex)
            {
                LogOutput($"ERROR creating run scripts: {ex.Message}");
                return false;
            }
        }

        private async void InstallAllButton_Click(object sender, RoutedEventArgs e)
        {
            LogOutput("Starting complete setup process...");
            
            bool pythonSuccess = await InstallPython();
            if (!pythonSuccess)
            {
                LogOutput("Python installation failed. Stopping process.");
                return;
            }

            bool cloneSuccess = await CloneRepository();
            if (!cloneSuccess)
            {
                LogOutput("Repository clone failed. Stopping process.");
                return;
            }

            bool environmentSuccess = await SetupPythonEnvironment();
            if (!environmentSuccess)
            {
                LogOutput("Python environment setup failed. Stopping process.");
                return;
            }

            bool scriptsSuccess = await CreateRunScripts();
            if (!scriptsSuccess)
            {
                LogOutput("Run script creation failed.");
                return;
            }

            // Create desktop shortcut as the final step
            CreateDesktopShortcut();

            LogOutput("Complete setup process finished successfully!");
            MessageBox.Show("Setup completed successfully! Your Python environment is ready to use.", "Success", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BrowsePythonPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Python installation directory",
                SelectedPath = PythonPathTextBox.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PythonPathTextBox.Text = dialog.SelectedPath;
                SaveCurrentSettings(); // Explicitly save when user browses
            }
        }

        private void BrowseClonePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select directory for repository clone",
                SelectedPath = ClonePathTextBox.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ClonePathTextBox.Text = dialog.SelectedPath;
                SaveCurrentSettings(); // Explicitly save when user browses
            }
        }

        private void CreateDesktopShortcut()
        {
            try
            {
                if (CreateDesktopShortcutCheckBox.IsChecked != true)
                {
                    LogOutput("Skipping desktop shortcut creation");
                    return;
                }

                string targetProgram = TargetProgramTextBox.Text.Trim();
                if (string.IsNullOrEmpty(targetProgram))
                {
                    LogOutput("Error: No target program specified for shortcut");
                    return;
                }

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string clonePath = ClonePathTextBox.Text.Trim();
                string projectPath = Path.Combine(clonePath, Path.GetFileNameWithoutExtension(RepoUrlTextBox.Text.Split('/').Last().Replace(".git", "")));
                string venvPython = Path.Combine(projectPath, ".venv", "Scripts", "python.exe");
                string targetProgramPath = Path.Combine(projectPath, targetProgram);

                if (!File.Exists(venvPython))
                {
                    LogOutput("Error: Virtual environment not found. Cannot create shortcut.");
                    return;
                }

                if (!File.Exists(targetProgramPath))
                {
                    LogOutput($"Warning: Target program '{targetProgram}' not found at {targetProgramPath}. Creating shortcut anyway.");
                }

                // Create program name from target program file
                string programName = Path.GetFileNameWithoutExtension(targetProgram);
                string shortcutName = char.ToUpper(programName[0]) + programName.Substring(1); // Capitalize first letter
                string shortcutPath = Path.Combine(desktopPath, $"{shortcutName}.lnk");

                // Look for .ico file in the project directory
                string iconPath = FindIconFile(projectPath, programName);

                // Create a VBS script that runs the Python program without showing console
                string vbsContent = $@"Set objShell = CreateObject(""WScript.Shell"")
objShell.CurrentDirectory = ""{projectPath}""
pythonPath = ""{venvPython}""
programPath = ""{targetProgram}""
objShell.Run pythonPath & "" "" & programPath, 0, False";
                string vbsPath = Path.Combine(projectPath, $"run_{programName}.vbs");
                
                File.WriteAllText(vbsPath, vbsContent);

                // Use PowerShell to create the shortcut pointing to the VBS script
                string iconScript = !string.IsNullOrEmpty(iconPath) ? $"$Shortcut.IconLocation = '{iconPath}'" : "";
                string powershellScript = $@"
                    $WshShell = New-Object -comObject WScript.Shell
                    $Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
                    $Shortcut.TargetPath = 'wscript.exe'
                    $Shortcut.Arguments = '\""{vbsPath}\""'
                    $Shortcut.WorkingDirectory = '{projectPath}'
                    $Shortcut.Description = '{shortcutName} - Python Application'
                    {iconScript}
                    $Shortcut.Save()
                ";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{powershellScript}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process? process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit(10000);
                        if (process.ExitCode == 0)
                        {
                            LogOutput($"Desktop shortcut created: {shortcutPath}");
                            LogOutput($"Created hidden launcher: run_{programName}.vbs");
                            if (!string.IsNullOrEmpty(iconPath))
                            {
                                LogOutput($"Using icon: {iconPath}");
                            }
                        }
                        else
                        {
                            string error = process.StandardError.ReadToEnd();
                            LogOutput($"Failed to create desktop shortcut: {error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogOutput($"Error creating desktop shortcut: {ex.Message}");
            }
        }

        private string FindIconFile(string projectPath, string programName)
        {
            try
            {
                // Look for icon files in common locations
                string[] iconSearchPaths = {
                    Path.Combine(projectPath, $"{programName}.ico"),
                    Path.Combine(projectPath, "icon.ico"),
                    Path.Combine(projectPath, "app.ico"),
                    Path.Combine(projectPath, "assets", $"{programName}.ico"),
                    Path.Combine(projectPath, "icons", $"{programName}.ico"),
                    Path.Combine(projectPath, "resources", $"{programName}.ico")
                };

                foreach (string iconPath in iconSearchPaths)
                {
                    if (File.Exists(iconPath))
                    {
                        return iconPath;
                    }
                }

                // Look for any .ico file in the project directory
                var icoFiles = Directory.GetFiles(projectPath, "*.ico", SearchOption.AllDirectories);
                if (icoFiles.Length > 0)
                {
                    return icoFiles[0]; // Return the first .ico file found
                }
            }
            catch (Exception ex)
            {
                LogOutput($"Warning: Error searching for icon file: {ex.Message}");
            }

            return string.Empty;
        }

        protected override void OnClosed(EventArgs e)
        {
            httpClient?.Dispose();
            base.OnClosed(e);
        }
    }
}
