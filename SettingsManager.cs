using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Forms;

namespace CrosshairTool
{
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

        // Rendering options
        public bool AntiAliasing { get; set; } = false; // Default false for razor-sharp pixel look
        
        // System
        public bool AutoStart { get; set; } = false;
    }

    public static class SettingsManager
    {
        private static readonly string FilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "settings.json"
        );
        private const string RegistryKeyName = "ScreenCrosshairTool";

        public static CrosshairSettings Current { get; private set; } = new CrosshairSettings();

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var loaded = JsonSerializer.Deserialize<CrosshairSettings>(json);
                    if (loaded != null)
                    {
                        Current = loaded;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings from {FilePath}: {ex.Message}");
            }
            Current = new CrosshairSettings();
        }

        public static void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Current, options);
                File.WriteAllText(FilePath, json);
                
                // Update AutoStart registry key
                ApplyAutoStart(Current.AutoStart);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings to {FilePath}: {ex.Message}");
            }
        }

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
