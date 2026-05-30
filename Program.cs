using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CrosshairTool
{
    static class Program
    {
        private static Mutex? mutex = null;
        
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);
        
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [STAThread]
        static void Main()
        {
            // Ensure single-instance execution
            const string mutexName = "Global\\ScreenCrosshairToolMutex_2026";
            mutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                MessageBox.Show(
                    "屏幕准星工具已经在后台运行中。\n请在屏幕右下角系统托盘中查找并设置。",
                    "提示",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            try
            {
                ApplicationConfiguration.Initialize();
                
                // Load settings
                SettingsManager.Load();

                // Run Application Context
                using (var context = new CrosshairApplicationContext())
                {
                    Application.Run(context);
                }
            }
            finally
            {
                mutex.ReleaseMutex();
                mutex.Dispose();
            }
        }

        private class CrosshairApplicationContext : ApplicationContext
        {
            private readonly NotifyIcon notifyIcon;
            private readonly CrosshairForm crosshairForm;
            private readonly KeyboardHook hook;
            private SettingsForm? settingsForm;
            private ToolStripMenuItem? toggleMenuItem;

            public CrosshairApplicationContext()
            {
                // Create Crosshair Overlay Form
                crosshairForm = new CrosshairForm();
                crosshairForm.Show();

                // Setup keyboard hook for global hotkey
                hook = new KeyboardHook(this);
                hook.Start();

                // Setup Notify Icon (System Tray)
                notifyIcon = new NotifyIcon();
                notifyIcon.Text = "屏幕准星工具 (Screen Crosshair Tool)";
                
                // Load tray icon from resources
                try
                {
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "crosshair.ico");
                    if (File.Exists(iconPath))
                    {
                        notifyIcon.Icon = new Icon(iconPath);
                    }
                    else
                    {
                        notifyIcon.Icon = SystemIcons.Application;
                    }
                }
                catch
                {
                    notifyIcon.Icon = SystemIcons.Application;
                }

                // Add Double Click Event
                notifyIcon.DoubleClick += (s, e) => ShowSettings();

                // Create Context Menu
                var contextMenu = new ContextMenuStrip();
                
                toggleMenuItem = new ToolStripMenuItem("隐藏准星 (Hide)");
                toggleMenuItem.Click += (s, e) => ToggleCrosshairVisibility();
                contextMenu.Items.Add(toggleMenuItem);

                var itemSettings = new ToolStripMenuItem("设置 (Settings)...");
                itemSettings.Click += (s, e) => ShowSettings();
                contextMenu.Items.Add(itemSettings);

                contextMenu.Items.Add(new ToolStripSeparator());

                var itemExit = new ToolStripMenuItem("退出 (Exit)");
                itemExit.Click += (s, e) => ExitApplication();
                contextMenu.Items.Add(itemExit);

                notifyIcon.ContextMenuStrip = contextMenu;
                notifyIcon.Visible = true;
            }

            private (bool ctrl, bool shift, bool alt, uint key) GetCurrentKeyState()
            {
                bool ctrl = (GetAsyncKeyState(0x11) & 0x8000) != 0;  // VK_CONTROL
                bool shift = (GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT
                bool alt = (GetAsyncKeyState(0x12) & 0x8000) != 0;   // VK_MENU
                return (ctrl, shift, alt, 0);
            }

            public void OnKeyPressed(uint vk)
            {
                var (ctrl, shift, alt, _) = GetCurrentKeyState();
                var (hotCtrl, hotShift, hotAlt, hotKey) = ParseHotkey(SettingsManager.Current.ToggleHotkey ?? "Ctrl+Q");
                
                if (ctrl == hotCtrl && shift == hotShift && alt == hotAlt && vk == hotKey)
                {
                    ToggleCrosshairVisibility();
                }
            }

            private (bool ctrl, bool shift, bool alt, uint key) ParseHotkey(string hotkeyStr)
            {
                bool ctrl = false, shift = false, alt = false;
                uint key = 0;
                
                string[] parts = hotkeyStr.Split('+');
                foreach (string part in parts)
                {
                    string trimmed = part.Trim().ToLower();
                    switch (trimmed)
                    {
                        case "ctrl":
                        case "control":
                            ctrl = true;
                            break;
                        case "alt":
                            alt = true;
                            break;
                        case "shift":
                            shift = true;
                            break;
                        default:
                            if (trimmed.Length == 1)
                            {
                                char c = char.ToUpper(trimmed[0]);
                                if (c >= 'A' && c <= 'Z')
                                    key = (uint)c;
                                else if (c >= '0' && c <= '9')
                                    key = (uint)c;
                            }
                            else if (trimmed.StartsWith("f") && int.TryParse(trimmed.Substring(1), out int fkey))
                            {
                                if (fkey >= 1 && fkey <= 24)
                                    key = (uint)(0x70 + fkey - 1);
                            }
                            break;
                    }
                }
                return (ctrl, shift, alt, key);
            }

            public void OnHotkeyPressed()
            {
                ToggleCrosshairVisibility();
            }

            private void ToggleCrosshairVisibility()
            {
                if (crosshairForm.Visible)
                {
                    crosshairForm.Hide();
                    if (toggleMenuItem != null)
                        toggleMenuItem.Text = "显示准星 (Show)";
                }
                else
                {
                    crosshairForm.Show();
                    if (toggleMenuItem != null)
                        toggleMenuItem.Text = "隐藏准星 (Hide)";
                }
            }

            private void ShowSettings()
            {
                if (settingsForm == null || settingsForm.IsDisposed)
                {
                    settingsForm = new SettingsForm(crosshairForm);
                }
                
                if (!settingsForm.Visible)
                {
                    settingsForm.Show();
                }
                settingsForm.Activate();
            }

            private void ExitApplication()
            {
                // Stop keyboard hook
                hook.Stop();

                // Clean up forms
                if (settingsForm != null && !settingsForm.IsDisposed)
                {
                    settingsForm.Dispose();
                }
                if (crosshairForm != null && !crosshairForm.IsDisposed)
                {
                    crosshairForm.Close();
                    crosshairForm.Dispose();
                }

                // Clean up tray icon
                notifyIcon.Visible = false;
                notifyIcon.Dispose();

                // Terminate application
                ExitThread();
            }
        }

        private class KeyboardHook
        {
            private readonly CrosshairApplicationContext context;
            private IntPtr hookId = IntPtr.Zero;
            private LowLevelKeyboardProc? proc;

            public KeyboardHook(CrosshairApplicationContext context)
            {
                this.context = context;
            }

            public void Start()
            {
                proc = HookCallback;
                using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    if (curModule != null)
                    {
                        hook_id = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                    }
                }
            }

            public void Stop()
            {
                if (hook_id != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(hook_id);
                    hook_id = IntPtr.Zero;
                }
            }

            private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
                {
                    int vk = Marshal.ReadInt32(lParam);
                    context.OnKeyPressed((uint)vk);
                }
                return CallNextHookEx(hook_id, nCode, wParam, lParam);
            }

            private IntPtr hook_id;
        }
    }
}