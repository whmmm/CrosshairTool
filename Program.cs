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

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

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
            private IntPtr trayIconHandle = IntPtr.Zero;

            public CrosshairApplicationContext()
            {
                // Create Crosshair Overlay Form
                crosshairForm = new CrosshairForm();
                crosshairForm.Show();

                // Setup Notify Icon (System Tray)
                notifyIcon = new NotifyIcon();
                notifyIcon.Text = "屏幕准星工具 (Screen Crosshair Tool)";
                
                // Draw a dynamic tray icon in memory
                try
                {
                    using (Bitmap bmp = new Bitmap(32, 32))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.Clear(Color.Transparent);
                            
                            // Draw nice green outer circle
                            using (Pen pen = new Pen(Color.Lime, 3))
                            {
                                g.DrawEllipse(pen, 4, 4, 24, 24);
                            }
                            
                            // Draw red center dot
                            g.FillEllipse(Brushes.Red, 13, 13, 6, 6);
                        }
                        trayIconHandle = bmp.GetHicon();
                        notifyIcon.Icon = Icon.FromHandle(trayIconHandle);
                    }
                }
                catch
                {
                    // Fallback to default application icon
                    notifyIcon.Icon = SystemIcons.Application;
                }

                // Add Double Click Event
                notifyIcon.DoubleClick += (s, e) => ShowSettings();

                // Create Context Menu
                var contextMenu = new ContextMenuStrip();
                
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

                // Clean up native icon handle to prevent leak
                if (trayIconHandle != IntPtr.Zero)
                {
                    DestroyIcon(trayIconHandle);
                }

                // Terminate application
                ExitThread();
            }
        }
    }
}