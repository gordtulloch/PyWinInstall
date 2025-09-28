using System;
using System.IO;
using Newtonsoft.Json;

namespace PyWinInstall
{
    public class InstallationConfig
    {
        public DefaultSettings DefaultSettings { get; set; } = new DefaultSettings();

        public static InstallationConfig Load(string configPath = "setup.json")
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<InstallationConfig>(json);
                    return config ?? new InstallationConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
            }

            return new InstallationConfig();
        }

        public void Save(string configPath = "setup.json")
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }
    }

    public class DefaultSettings
    {
        public PythonSettings Python { get; set; } = new PythonSettings();
        public GitSettings Git { get; set; } = new GitSettings();
        public ApplicationSettings Application { get; set; } = new ApplicationSettings();
    }

    public class PythonSettings
    {
        public string Version { get; set; } = "3.12.6";
        public string InstallPath { get; set; } = "C:\\Python";
        public bool AlreadyInstalled { get; set; } = false;
    }

    public class GitSettings
    {
        public string RepositoryUrl { get; set; } = "https://github.com/gordtulloch/astrofiler-gui.git";
        public string ClonePath { get; set; } = "C:\\";
    }

    public class ApplicationSettings
    {
        public bool AutoDetectPython { get; set; } = true;
        public bool CreateDesktopShortcut { get; set; } = true;
        public bool AddPythonToPath { get; set; } = true;
        public string TargetProgram { get; set; } = "astrofiler.py";
    }
}
