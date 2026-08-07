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

        // WinEvent hook for detecting foreground window changes
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y,
            int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int GWL_STYLE = -16;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

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
            private readonly ContextMenuStrip contextMenu;
            private readonly List<ToolStripMenuItem> profileMenuItems = new();
            private SettingsForm? settingsForm;
            private ToolStripMenuItem? toggleMenuItem;
            private IntPtr _winEventHook = IntPtr.Zero;
            private WinEventDelegate? _winEventProc;
            private System.Windows.Forms.Timer? _reassertTimer;

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

                // Create Context Menu — profiles at top, only update Checked on open
                contextMenu = new ContextMenuStrip();

                // Fixed menu items (reused — never destroyed)
                toggleMenuItem = new ToolStripMenuItem("隐藏准星 (Hide)");
                toggleMenuItem.Click += (s, e) => ToggleCrosshairVisibility();

                var itemSettings = new ToolStripMenuItem("设置 (Settings)...");
                itemSettings.Click += (s, e) => ShowSettings();

                var itemExit = new ToolStripMenuItem("退出 (Exit)");
                itemExit.Click += (s, e) => ExitApplication();

                // Build profile items at the top
                RebuildProfileMenu();

                // Hook foreground window changes to re-assert topmost position
                // This fixes the issue where games (e.g., Cyberpunk 2077) steal topmost
                // when the user alt-tabs back into the game
                _winEventProc = OnForegroundChanged;
                _winEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

                // Timer to re-show crosshair with a short delay after foreground change.
                // An immediate SetWindowPos isn't enough — full-screen games cause DWM
                // to bypass composition of layered windows; only a full Hide/Show cycle
                // (like the manual toggle) forces DWM to re-evaluate and re-composite.
                _reassertTimer = new System.Windows.Forms.Timer { Interval = 300 };
                _reassertTimer.Tick += (s, e) =>
                {
                    _reassertTimer.Stop();
                    try
                    {
                        if (!crosshairForm.IsDisposed && crosshairForm.IsHandleCreated && crosshairForm.Visible)
                        {
                            crosshairForm.Hide();
                            crosshairForm.Show();
                            crosshairForm.Redraw();
                        }
                    }
                    catch
                    {
                        // Suppress errors in timer tick
                    }
                };

                // Add separator then fixed items (never removed on rebuild)
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add(toggleMenuItem);
                contextMenu.Items.Add(itemSettings);
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add(itemExit);

                // On open, just update check marks (lightweight, no Clear/Add)
                contextMenu.Opening += (s, e) =>
                {
                    string activeName = SettingsManager.ActiveProfileName;
                    foreach (var item in profileMenuItems)
                    {
                        item.Checked = (item.Text == activeName);
                    }
                };

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

                // Check toggle hotkey
                var (hotCtrl, hotShift, hotAlt, hotKey) = ParseHotkey(SettingsManager.Global.ToggleHotkey ?? "Ctrl+Q");
                if (ctrl == hotCtrl && shift == hotShift && alt == hotAlt && vk == hotKey)
                {
                    ToggleCrosshairVisibility();
                    return;
                }

                // Check cycle profile hotkey
                var (cycCtrl, cycShift, cycAlt, cycKey) = ParseHotkey(SettingsManager.Global.CycleProfileHotkey ?? "Ctrl+`");
                if (ctrl == cycCtrl && shift == cycShift && alt == cycAlt && vk == cycKey)
                {
                    CycleNextProfile();
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
                                else if (trimmed[0] == '`' || trimmed[0] == '~')
                                    key = 0xC0; // VK_OEM_3 (backtick/tilde key)
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

            /// <summary>
            /// Returns true if the given window covers the entire screen and has no caption bar,
            /// indicating it's a full-screen game (the only scenario where we need to re-assert).
            /// </summary>
            private static bool IsFullScreenWindow(IntPtr hwnd)
            {
                if (hwnd == IntPtr.Zero) return false;

                if (!GetWindowRect(hwnd, out RECT rect)) return false;

                // Must cover the entire primary screen
                Rectangle screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                bool coversScreen = rect.Left <= screen.Left && rect.Top <= screen.Top
                    && rect.Right >= screen.Right && rect.Bottom >= screen.Bottom;
                if (!coversScreen) return false;

                // Full-screen games typically lack WS_CAPTION (no title bar / window chrome)
                int style = GetWindowLong(hwnd, GWL_STYLE);
                return (style & (int)WS_CAPTION) == 0;
            }

            private void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
                int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
            {
                try
                {
                    if (crosshairForm.IsDisposed || !crosshairForm.IsHandleCreated || !crosshairForm.Visible)
                        return;

                    // Only react when the user switches to a full-screen game window.
                    // Other foreground changes (tray menu, color dialog, desktop, etc.)
                    // must NOT trigger the Hide/Show cycle, or they cause focus loss
                    // and popup-menu dismissal.
                    if (!IsFullScreenWindow(hwnd))
                        return;

                    // Marshal to UI thread, then start a short delay timer.
                    // The delay lets the game finish its full-screen setup before we
                    // do the Hide/Show cycle to force DWM to re-composite our window.
                    crosshairForm.BeginInvoke(() =>
                    {
                        try
                        {
                            _reassertTimer?.Stop();
                            _reassertTimer?.Start();
                        }
                        catch
                        {
                            // Suppress
                        }
                    });
                }
                catch
                {
                    // Silently ignore errors from the WinEvent callback thread
                }
            }

            private void CycleNextProfile()
            {
                string newProfile = SettingsManager.CycleNextProfile();
                crosshairForm.UpdatePositionAndSize();
                NotificationForm.ShowToast(newProfile);
                if (settingsForm != null && !settingsForm.IsDisposed && settingsForm.Visible)
                {
                    settingsForm.ReloadFromSettings();
                }
            }

            private void ShowSettings()
            {
                if (settingsForm == null || settingsForm.IsDisposed)
                {
                    settingsForm = new SettingsForm(crosshairForm, RebuildProfileMenu);
                }

                if (!settingsForm.Visible)
                {
                    settingsForm.Show();
                }
                settingsForm.Activate();
            }

            /// <summary>
            /// Rebuilds the profile items at the top of the tray context menu.
            /// Call this after creating, deleting, or renaming profiles.
            /// </summary>
            public void RebuildProfileMenu()
            {
                // Remove old profile items only (fixed items stay put)
                foreach (var item in profileMenuItems)
                {
                    contextMenu.Items.Remove(item);
                }
                profileMenuItems.Clear();

                // Insert new profile items at the top (before the separator + fixed items)
                string activeName = SettingsManager.ActiveProfileName;
                int insertIdx = 0;

                foreach (string profileName in SettingsManager.GetProfileNames())
                {
                    var item = new ToolStripMenuItem(profileName);
                    item.Checked = (profileName == activeName);
                    item.Click += (sender, args) =>
                    {
                        if (profileName != SettingsManager.ActiveProfileName)
                        {
                            SettingsManager.SwitchToProfile(profileName);
                            crosshairForm.UpdatePositionAndSize();
                            NotificationForm.ShowToast(profileName);
                            if (settingsForm != null && !settingsForm.IsDisposed && settingsForm.Visible)
                            {
                                settingsForm.ReloadFromSettings();
                            }
                        }
                    };
                    contextMenu.Items.Insert(insertIdx++, item);
                    profileMenuItems.Add(item);
                }
            }

            private void ExitApplication()
            {
                // Stop keyboard hook
                hook.Stop();

                // Stop foreground change hook
                if (_winEventHook != IntPtr.Zero)
                {
                    UnhookWinEvent(_winEventHook);
                    _winEventHook = IntPtr.Zero;
                }

                // Stop reassert timer
                _reassertTimer?.Stop();
                _reassertTimer?.Dispose();

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