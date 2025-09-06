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
            
            LogOutput("PyWinInstall ready. Select options and click 'Install All' or use individual buttons.");
            
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

            // Apply PowerShell settings
            ScriptPathTextBox.Text = config.DefaultSettings.PowerShell.ScriptPath;

            // Apply Python already installed setting
            PythonAlreadyInstalledCheckBox.IsChecked = config.DefaultSettings.Python.AlreadyInstalled;
            if (config.DefaultSettings.Python.AlreadyInstalled)
            {
                UpdatePythonSectionState(false);
            }
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
                config.DefaultSettings.PowerShell.ScriptPath = ScriptPathTextBox.Text;

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

        private void ScriptPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
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
                        
                        // Update script path if it's in the default location
                        Dispatcher.Invoke(() =>
                        {
                            if (ScriptPathTextBox.Text.Contains("install.ps1"))
                            {
                                string possibleScriptPath = Path.Combine(fullClonePath, "install.ps1");
                                if (File.Exists(possibleScriptPath))
                                {
                                    ScriptPathTextBox.Text = possibleScriptPath;
                                    LogOutput($"Found install.ps1 script at {possibleScriptPath}");
                                }
                            }
                        });
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

        private async void RunScriptButton_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellScript();
        }

        private async Task<bool> RunPowerShellScript()
        {
            try
            {
                SetProgress(true, 0);
                LogOutput("Starting PowerShell script execution...");

                string scriptPath = ScriptPathTextBox.Text.Trim();

                if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
                {
                    LogOutput("ERROR: Please specify a valid PowerShell script path.");
                    return false;
                }

                SetProgress(true, 25);

                bool success = await Task.Run(() =>
                {
                    try
                    {
                        LogOutput($"Executing PowerShell script: {scriptPath}");

                        // Use PowerShell.exe directly instead of the managed API
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory
                        };

                        using (var process = Process.Start(startInfo))
                        {
                            if (process != null)
                            {
                                SetProgress(true, 50);

                                // Read output and error streams
                                string output = process.StandardOutput.ReadToEnd();
                                string error = process.StandardError.ReadToEnd();
                                
                                process.WaitForExit();

                                SetProgress(true, 75);

                                // Log output
                                if (!string.IsNullOrEmpty(output))
                                {
                                    LogOutput("SCRIPT OUTPUT:");
                                    foreach (string line in output.Split('\n'))
                                    {
                                        if (!string.IsNullOrWhiteSpace(line))
                                        {
                                            LogOutput($"  {line.TrimEnd()}");
                                        }
                                    }
                                }

                                // Log errors
                                if (!string.IsNullOrEmpty(error))
                                {
                                    LogOutput("SCRIPT ERRORS:");
                                    foreach (string line in error.Split('\n'))
                                    {
                                        if (!string.IsNullOrWhiteSpace(line))
                                        {
                                            LogOutput($"  ERROR: {line.TrimEnd()}");
                                        }
                                    }
                                }

                                if (process.ExitCode == 0)
                                {
                                    LogOutput("PowerShell script execution completed successfully.");
                                    SetProgress(true, 100);
                                    return true;
                                }
                                else
                                {
                                    LogOutput($"PowerShell script execution failed with exit code: {process.ExitCode}");
                                    return false;
                                }
                            }
                        }

                        return false;
                    }
                    catch (Exception ex)
                    {
                        LogOutput($"ERROR during PowerShell script execution: {ex.Message}");
                        return false;
                    }
                });

                return success;
            }
            catch (Exception ex)
            {
                LogOutput($"ERROR during PowerShell script execution: {ex.Message}");
                return false;
            }
            finally
            {
                SetProgress(false);
            }
        }

        private async void InstallAllButton_Click(object sender, RoutedEventArgs e)
        {
            LogOutput("Starting complete installation process...");
            
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

            bool scriptSuccess = await RunPowerShellScript();
            if (!scriptSuccess)
            {
                LogOutput("PowerShell script execution failed.");
                return;
            }

            LogOutput("Complete installation process finished successfully!");
            MessageBox.Show("Installation completed successfully!", "Success", 
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

        private void BrowseScriptPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select PowerShell Script",
                Filter = "PowerShell Scripts (*.ps1)|*.ps1|All Files (*.*)|*.*",
                InitialDirectory = Path.GetDirectoryName(ScriptPathTextBox.Text) ?? @"C:\"
            };

            if (dialog.ShowDialog() == true)
            {
                ScriptPathTextBox.Text = dialog.FileName;
                SaveCurrentSettings(); // Explicitly save when user browses
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            httpClient?.Dispose();
            base.OnClosed(e);
        }
    }
}
