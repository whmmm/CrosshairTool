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
            private SettingsForm? settingsForm;

            public CrosshairApplicationContext()
            {
                // Create Crosshair Overlay Form
                crosshairForm = new CrosshairForm();
                crosshairForm.Show();

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
                
                var itemToggleVisibility = new ToolStripMenuItem("隐藏准星 (Hide)");
                itemToggleVisibility.Click += (s, e) => ToggleCrosshairVisibility(itemToggleVisibility);
                contextMenu.Items.Add(itemToggleVisibility);

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

            private void ToggleCrosshairVisibility(ToolStripMenuItem menuItem)
            {
                if (crosshairForm.Visible)
                {
                    crosshairForm.Hide();
                    menuItem.Text = "显示准星 (Show)";
                }
                else
                {
                    crosshairForm.Show();
                    menuItem.Text = "隐藏准星 (Hide)";
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
    }
}