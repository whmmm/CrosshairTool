using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CrosshairTool
{
    /// <summary>
    /// A topmost, auto-dismissing toast that shows which profile was activated.
    /// </summary>
    public class NotificationForm : Form
    {
        private readonly System.Windows.Forms.Timer _closeTimer;

        /// <summary>
        /// Shows a notification for the given profile name. Non-blocking, auto-closes after 3 seconds.
        /// </summary>
        public static void ShowToast(string profileName)
        {
            var form = new NotificationForm(profileName);
            form.Show(); // Modeless — doesn't block the caller
        }

        private NotificationForm(string profileName)
        {
            // Form setup
            this.Text = "";
            this.StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Width = 320;
            this.Height = 55;

            // Center horizontally, 5px from top
            var screen = Screen.PrimaryScreen;
            if (screen != null)
            {
                this.Left = (screen.WorkingArea.Width - this.Width) / 2;
                this.Top = 5;
            }

            this.BackColor = Color.FromArgb(30, 30, 32);
            this.ForeColor = Color.White;

            // Round corners via region
            using (var path = new GraphicsPath())
            {
                int r = 12;
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(this.Width - r * 2 - 1, 0, r * 2, r * 2, 270, 90);
                path.AddArc(this.Width - r * 2 - 1, this.Height - r * 2 - 1, r * 2, r * 2, 0, 90);
                path.AddArc(0, this.Height - r * 2 - 1, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                this.Region = new Region(path);
            }

            // Icon / label area
            var iconLabel = new Label
            {
                // Text = "⚙",
                Text = "+",
                Font = new Font("Segoe UI", 18F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0, 200, 100),
                Location = new Point(20, 7),
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Prefix label (regular weight)
            var prefixLabel = new Label
            {
                Text = "已切换至配置: ",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = Color.White,
                Location = new Point(58, 15),
                Size = new Size(115, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Profile name label (bold, green)
            var nameLabel = new Label
            {
                Text = profileName,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 200, 100),
                Location = new Point(173, 15),
                Size = new Size(130, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Hint text
            var hintLabel = new Label
            {
                // Text = "3 秒后自动关闭",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(140, 140, 145),
                Location = new Point(58, 40),
                Size = new Size(240, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };

            this.Controls.Add(iconLabel);
            this.Controls.Add(prefixLabel);
            this.Controls.Add(nameLabel);
            this.Controls.Add(hintLabel);

            // Auto-close timer
            _closeTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _closeTimer.Tick += (s, e) =>
            {
                _closeTimer.Stop();
                this.Close();
            };
            _closeTimer.Start();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE: don't steal focus
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW: hide from Alt+Tab
                return cp;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _closeTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
