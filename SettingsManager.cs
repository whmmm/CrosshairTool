using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Forms;

namespace CrosshairTool
{
    /// <summary>
    /// Application-wide settings shared across all profiles.
    /// </summary>
    public class GlobalSettings
    {
        public bool AntiAliasing { get; set; } = true;
        public bool AutoStart { get; set; } = false;
        public string ToggleHotkey { get; set; } = "Ctrl+Q";
    }

    /// <summary>
    /// Per-profile crosshair visual settings.
    /// </summary>
    public class CrosshairSettings
    {
        public string Style { get; set; } = "Crosshair"; // Crosshair, Dot, Circle, Square
        public string ColorHex { get; set; } = "#00FF00"; // Default: Neon Green
        public int Thickness { get; set; } = 2;
        public int Size { get; set; } = 30; // General size scale or radius
        public bool ShowCenterDot { get; set; } = true;
        public int CenterDotSize { get; set; } = 4;
        public string CenterDotShape { get; set; } = "Circle"; // Circle, Square
        public bool CenterDotEnableOutline { get; set; } = false;
        public string CenterDotOutlineColorHex { get; set; } = "#000000";
        public int CenterDotOutlineThickness { get; set; } = 1;
        public bool EnableOutline { get; set; } = true;
        public string OutlineColorHex { get; set; } = "#000000"; // Black outline
        public int OutlineThickness { get; set; } = 1;

        // Crosshair specific settings
        public int ArmCount { get; set; } = 4;
        public int InnerGap { get; set; } = 6;
        public int ArmLength { get; set; } = 12;
        public float RotationAngle { get; set; } = 0.0f; // in degrees

        // Square specific settings
        public int SquareWidth { get; set; } = 30;
        public int SquareHeight { get; set; } = 30;
        public bool SquareFillEnabled { get; set; } = false;
        public int SquareCornerLengthX { get; set; } = 0; // Horizontal segment length at each corner (0 = full square)
        public int SquareCornerLengthY { get; set; } = 0; // Vertical segment length at each corner (0 = full square)

        // Offset settings for positioning crosshair away from center
        public int OffsetX { get; set; } = 0;
        public int OffsetY { get; set; } = 0;
    }

    /// <summary>
    /// Stores global settings, all named profiles, and tracks which one is active.
    /// </summary>
    public class ProfilesData
    {
        public string ActiveProfile { get; set; } = "Default";
        public GlobalSettings Global { get; set; } = new GlobalSettings();
        public Dictionary<string, CrosshairSettings> Profiles { get; set; } = new();
    }

    public static class SettingsManager
    {
        private static readonly string FilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "settings.json"
        );
        private static readonly string ProfilesFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "profiles.json"
        );
        private const string RegistryKeyName = "ScreenCrosshairTool";

        private static ProfilesData _profiles = new ProfilesData();

        /// <summary>
        /// The current profile's crosshair visual settings.
        /// </summary>
        public static CrosshairSettings Current { get; private set; } = new CrosshairSettings();

        /// <summary>
        /// Global application settings shared across all profiles.
        /// </summary>
        public static GlobalSettings Global => _profiles.Global;

        /// <summary>
        /// Gets the name of the currently active profile.
        /// </summary>
        public static string ActiveProfileName => _profiles.ActiveProfile;

        /// <summary>
        /// Gets all profile names, sorted alphabetically.
        /// </summary>
        public static List<string> GetProfileNames()
        {
            return _profiles.Profiles.Keys.OrderBy(n => n).ToList();
        }

        /// <summary>
        /// Loads profiles.json (or migrates from old settings.json or old profiles.json without global).
        /// </summary>
        public static void Load()
        {
            try
            {
                // Normal path: profiles.json already exists
                if (File.Exists(ProfilesFilePath))
                {
                    string json = File.ReadAllText(ProfilesFilePath);
                    var loaded = JsonSerializer.Deserialize<ProfilesData>(json);
                    if (loaded != null && loaded.Profiles.Count > 0)
                    {
                        _profiles = loaded;

                        // Migration: old profiles.json without global settings
                        if (_profiles.Global == null)
                        {
                            MigrateGlobalFromLegacy();
                        }

                        // Ensure active profile name is valid (defensive)
                        if (!_profiles.Profiles.ContainsKey(_profiles.ActiveProfile))
                            _profiles.ActiveProfile = _profiles.Profiles.Keys.First();

                        Current = _profiles.Profiles[_profiles.ActiveProfile];
                        return;
                    }
                }

                // Migration path: old settings.json exists, no profiles.json yet
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var loaded = JsonSerializer.Deserialize<CrosshairSettings>(json);
                    if (loaded != null)
                    {
                        _profiles = new ProfilesData
                        {
                            ActiveProfile = "Default",
                            Global = new GlobalSettings(),  // Use defaults for global settings on migration
                            Profiles = new Dictionary<string, CrosshairSettings>
                            {
                                ["Default"] = loaded
                            }
                        };
                        Current = loaded;
                        Save(); // Write profiles.json, leaving old settings.json in place
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading profiles: {ex.Message}");
            }

            // Fallback: fresh start with a single Default profile
            _profiles = new ProfilesData
            {
                ActiveProfile = "Default",
                Global = new GlobalSettings(),
                Profiles = new Dictionary<string, CrosshairSettings>
                {
                    ["Default"] = new CrosshairSettings()
                }
            };
            Current = _profiles.Profiles["Default"];
            Save();
        }

        /// <summary>
        /// Extracts global settings from the first profile in the old profiles.json.
        /// Since CrosshairSettings no longer has these properties, we peek at the raw JSON.
        /// </summary>
        private static void MigrateGlobalFromLegacy()
        {
            _profiles.Global = new GlobalSettings();

            try
            {
                // Re-read the raw JSON to extract global settings from the first profile
                string json = File.ReadAllText(ProfilesFilePath);
                using (var doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("profiles", out var profilesEl))
                    {
                        // Use the first profile's global-ish values
                        foreach (var profile in profilesEl.EnumerateObject())
                        {
                            var p = profile.Value;
                            if (p.TryGetProperty("antiAliasing", out var aaEl))
                                _profiles.Global.AntiAliasing = aaEl.GetBoolean();
                            if (p.TryGetProperty("autoStart", out var asEl))
                                _profiles.Global.AutoStart = asEl.GetBoolean();
                            if (p.TryGetProperty("toggleHotkey", out var hkEl))
                                _profiles.Global.ToggleHotkey = hkEl.GetString() ?? "Ctrl+Q";
                            break; // Only need from first profile
                        }
                    }
                }
            }
            catch
            {
                // If raw JSON peek fails, keep defaults — not critical
            }
        }

        /// <summary>
        /// Saves current settings and global settings into profiles.json.
        /// </summary>
        public static void Save()
        {
            try
            {
                // Sync current visual settings back into the profiles dictionary
                _profiles.Profiles[_profiles.ActiveProfile] = Current;

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_profiles, options);
                File.WriteAllText(ProfilesFilePath, json);

                // Update AutoStart registry key
                ApplyAutoStart(_profiles.Global.AutoStart);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving profiles to {ProfilesFilePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Switches to a different profile by name.
        /// Only changes visual settings; global settings are unaffected.
        /// </summary>
        public static void SwitchToProfile(string name)
        {
            if (!_profiles.Profiles.TryGetValue(name, out var settings))
                throw new ArgumentException($"Profile '{name}' not found.");

            _profiles.ActiveProfile = name;
            Current = settings;
            Save();
        }

        /// <summary>
        /// Creates a new profile. If copyFrom is specified, clones that profile's visual settings;
        /// otherwise uses defaults. The new profile becomes the active one.
        /// </summary>
        public static void CreateProfile(string name, string? copyFrom = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Profile name cannot be empty.");
            if (_profiles.Profiles.ContainsKey(name))
                throw new ArgumentException($"Profile '{name}' already exists.");

            CrosshairSettings newSettings;
            if (copyFrom != null && _profiles.Profiles.TryGetValue(copyFrom, out var source))
            {
                // Deep-clone via JSON round-trip
                var cloneJson = JsonSerializer.Serialize(source);
                newSettings = JsonSerializer.Deserialize<CrosshairSettings>(cloneJson) ?? new CrosshairSettings();
            }
            else
            {
                newSettings = new CrosshairSettings();
            }

            _profiles.Profiles[name] = newSettings;
            _profiles.ActiveProfile = name;
            Current = newSettings;
            Save();
        }

        /// <summary>
        /// Deletes a profile. Refuses if it's the last remaining profile.
        /// If deleting the active profile, switches to the first remaining one.
        /// </summary>
        public static void DeleteProfile(string name)
        {
            if (_profiles.Profiles.Count <= 1)
                throw new InvalidOperationException("Cannot delete the last profile.");
            if (!_profiles.Profiles.ContainsKey(name))
                throw new ArgumentException($"Profile '{name}' not found.");

            _profiles.Profiles.Remove(name);

            if (_profiles.ActiveProfile == name)
            {
                _profiles.ActiveProfile = _profiles.Profiles.Keys.First();
                Current = _profiles.Profiles[_profiles.ActiveProfile];
            }

            Save();
        }

        /// <summary>
        /// Renames an existing profile.
        /// </summary>
        public static void RenameProfile(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New name cannot be empty.");
            if (!_profiles.Profiles.ContainsKey(oldName))
                throw new ArgumentException($"Profile '{oldName}' not found.");
            if (_profiles.Profiles.ContainsKey(newName))
                throw new ArgumentException($"Profile '{newName}' already exists.");

            var settings = _profiles.Profiles[oldName];
            _profiles.Profiles.Remove(oldName);
            _profiles.Profiles[newName] = settings;

            if (_profiles.ActiveProfile == oldName)
                _profiles.ActiveProfile = newName;

            Save();
        }

        /// <summary>
        /// Duplicates an existing profile under a new name. The new profile becomes active.
        /// </summary>
        public static void DuplicateProfile(string sourceName, string newName)
        {
            if (!_profiles.Profiles.ContainsKey(sourceName))
                throw new ArgumentException($"Source profile '{sourceName}' not found.");
            CreateProfile(newName, copyFrom: sourceName);
        }

        /// <summary>
        /// Updates the Windows registry to enable or disable auto-start.
        /// </summary>
        public static void ApplyAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey? rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (rk != null)
                    {
                        if (enable)
                        {
                            rk.SetValue(RegistryKeyName, Application.ExecutablePath);
                        }
                        else
                        {
                            rk.DeleteValue(RegistryKeyName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating registry for AutoStart: {ex.Message}");
            }
        }
    }
}
