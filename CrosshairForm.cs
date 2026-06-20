using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CrosshairTool
{
    public class CrosshairForm : Form
    {
        // Win32 API declarations for layered windows
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y) { X = x; Y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        private const int WS_EX_LAYERED = 0x00080000;
        private const int ULW_ALPHA = 0x00000002;
        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pprSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);
        
        // Win32 API for getting foreground window process
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private Bitmap? _bufferBitmap;
        private System.Windows.Forms.Timer? _processCheckTimer;
        private bool _lastProcessMatch = true; // Default to true (show crosshair)

        public CrosshairForm()
        {
            // Set Form styles for overlay
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            
            // Set styles to prevent user interaction
            this.SetStyle(ControlStyles.Selectable, false);

            // Initialize process check timer (check every 500ms)
            _processCheckTimer = new System.Windows.Forms.Timer();
            _processCheckTimer.Interval = 2000; // 500ms
            _processCheckTimer.Tick += ProcessCheckTimer_Tick;
            _processCheckTimer.Start();

            UpdatePositionAndSize();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_LAYERED (0x80000) - enable layered window
                // WS_EX_TRANSPARENT (0x20) - click through
                // WS_EX_TOOLWINDOW (0x80) - hide from Alt-Tab
                // WS_EX_TOPMOST (0x8) - keep on top
                // WS_EX_NOACTIVATE (0x08000000) - don't steal focus
                cp.ExStyle |= WS_EX_LAYERED | 0x00000020 | 0x00000080 | 0x00000008 | 0x08000000;
                return cp;
            }
        }

        public void UpdatePositionAndSize()
        {
            var settings = SettingsManager.Current;
            if (settings == null) return;

            // Calculate the maximum radius needed for drawing
            int maxRadius = 10;
            switch (settings.Style)
            {
                case "Crosshair":
                    maxRadius = settings.InnerGap + settings.ArmLength;
                    if (settings.ShowCenterDot)
                    {
                        maxRadius = Math.Max(maxRadius, settings.CenterDotSize);
                    }
                    break;
                case "Dot":
                    maxRadius = settings.Size / 2;
                    break;
                case "Circle":
                case "Square":
                    maxRadius = Math.Max(settings.SquareWidth, settings.SquareHeight) / 2;
                    break;
            }

            // Pad the radius for safety (thickness, outlines)
            int padding = settings.Thickness + settings.OutlineThickness + 15;
            
            // Include offset in the halfSize calculation to prevent clipping
            int offsetPadding = Math.Max(Math.Abs(settings.OffsetX), Math.Abs(settings.OffsetY));
            int halfSize = maxRadius + padding + offsetPadding;
            int size = halfSize * 2;

            // Set size
            if (this.Width != size || this.Height != size)
            {
                this.Width = size;
                this.Height = size;
            }

            // Center on primary screen with offset
            Rectangle screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            int x = screenBounds.X + (screenBounds.Width - this.Width) / 2 - settings.OffsetX;
            int y = screenBounds.Y + (screenBounds.Height - this.Height) / 2 - settings.OffsetY;

            if (this.Left != x || this.Top != y)
            {
                this.Location = new Point(x, y);
            }

            Redraw();
        }
        
        private void ProcessCheckTimer_Tick(object? sender, EventArgs e)
        {
            var settings = SettingsManager.Current;
            if (settings == null) return;
            
            // If process filter is disabled, always show crosshair
            if (!settings.EnableProcessFilter)
            {
                if (!_lastProcessMatch)
                {
                    _lastProcessMatch = true;
                    this.Visible = true;
                }
                return;
            }
            
            // Get foreground window process
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                // No foreground window, hide crosshair
                if (_lastProcessMatch)
                {
                    _lastProcessMatch = false;
                    this.Visible = false;
                }
                return;
            }
            
            // Get process ID from window handle
            GetWindowThreadProcessId(foregroundWindow, out uint processId);
            if (processId == 0)
            {
                // Could not get process ID, hide crosshair
                if (_lastProcessMatch)
                {
                    _lastProcessMatch = false;
                    this.Visible = false;
                }
                return;
            }
            
            // Get process name
            try
            {
                using (System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById((int)processId))
                {
                    string processName = process.ProcessName.ToLowerInvariant();
                    
                    // Check if process name is in the allowed list
                    string[] allowedProcesses = (settings.ProcessList ?? "")
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim().ToLowerInvariant())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToArray();
                    
                    // Also check with .exe extension
                    bool isMatch = allowedProcesses.Any(p => 
                        p == processName || 
                        p == processName + ".exe" ||
                        processName.EndsWith(p.Replace(".exe", "")));
                    
                    if (isMatch != _lastProcessMatch)
                    {
                        _lastProcessMatch = isMatch;
                        this.Visible = isMatch;
                    }
                }
            }
            catch (Exception)
            {
                // Process might have terminated or access denied, hide crosshair
                if (_lastProcessMatch)
                {
                    _lastProcessMatch = false;
                    this.Visible = false;
                }
            }
        }

        public void Redraw()
        {
            if (this.Width == 0 || this.Height == 0) return;

            // Create or resize buffer bitmap
            if (_bufferBitmap == null || _bufferBitmap.Width != this.Width || _bufferBitmap.Height != this.Height)
            {
                _bufferBitmap?.Dispose();
                _bufferBitmap = new Bitmap(this.Width, this.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }

            using (Graphics g = Graphics.FromImage(_bufferBitmap))
            {
                // Clear with transparent background
                g.Clear(Color.Transparent);

                var settings = SettingsManager.Current;
                if (settings == null) return;

                // Smart anti-aliasing: only enable for circles and diagonal lines
                bool needsAntiAliasing = settings.AntiAliasing && NeedsAntiAliasing(settings);
                
                if (needsAntiAliasing)
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                }
                else
                {
                    g.SmoothingMode = SmoothingMode.None;
                    g.PixelOffsetMode = PixelOffsetMode.None;
                    g.CompositingMode = CompositingMode.SourceOver;
                }

                Color mainColor = ColorTranslator.FromHtml(settings.ColorHex ?? "#00FF00");
                Color outlineColor = ColorTranslator.FromHtml(settings.OutlineColorHex ?? "#000000");

                float cx = this.Width / 2f;
                float cy = this.Height / 2f;

                switch (settings.Style)
                {
                    case "Crosshair":
                        DrawCrosshair(g, cx, cy, mainColor, outlineColor, settings);
                        break;
                    case "Dot":
                        DrawDot(g, cx, cy, mainColor, outlineColor, settings);
                        break;
                    case "Circle":
                        DrawCircle(g, cx, cy, mainColor, outlineColor, settings);
                        break;
                    case "Square":
                        DrawSquare(g, cx, cy, mainColor, outlineColor, settings);
                        break;
                }
            }

            UpdateLayeredWindowWithBitmap();
        }

        private bool NeedsAntiAliasing(CrosshairSettings settings)
        {
            // Circle and Dot always need anti-aliasing
            if (settings.Style == "Circle" || settings.Style == "Dot")
                return true;

            // Crosshair needs anti-aliasing if rotated (diagonal lines)
            if (settings.Style == "Crosshair")
            {
                // Check if rotation is not a multiple of 90 degrees
                double normalizedAngle = settings.RotationAngle % 90;
                if (normalizedAngle < 0) normalizedAngle += 90;
                // Allow small tolerance for floating point
                return normalizedAngle > 0.1 && normalizedAngle < 89.9;
            }

            // Square doesn't need anti-aliasing (unless rotated, but we don't support square rotation)
            return false;
        }

        private void UpdateLayeredWindowWithBitmap()
        {
            if (_bufferBitmap == null) return;

            IntPtr hdcScreen = CreateCompatibleDC(IntPtr.Zero);
            IntPtr hdcBitmap = CreateCompatibleDC(IntPtr.Zero);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr hOldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = _bufferBitmap.GetHbitmap(Color.FromArgb(0));
                hOldBitmap = SelectObject(hdcBitmap, hBitmap);

                POINT ptDst = new POINT(this.Left, this.Top);
                SIZE size = new SIZE(_bufferBitmap.Width, _bufferBitmap.Height);
                POINT ptSrc = new POINT(0, 0);

                BLENDFUNCTION blend = new BLENDFUNCTION
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = AC_SRC_ALPHA
                };

                UpdateLayeredWindow(this.Handle, hdcScreen, ref ptDst, ref size, hdcBitmap, ref ptSrc, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                if (hOldBitmap != IntPtr.Zero) SelectObject(hdcBitmap, hOldBitmap);
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                DeleteDC(hdcBitmap);
                DeleteDC(hdcScreen);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Override to prevent default painting
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Override to prevent default background painting
        }

        private void DrawCrosshair(Graphics g, float cx, float cy, Color mainColor, Color outlineColor, CrosshairSettings settings)
        {
            float gap = settings.InnerGap;
            float len = settings.ArmLength;
            int armCount = settings.ArmCount;
            float rotationRad = settings.RotationAngle * (float)Math.PI / 180f;

            // 1. Draw Optional Center Dot (with separate AA handling)
            if (settings.ShowCenterDot)
            {
                DrawCenterDot(g, cx, cy, mainColor, settings);
            }

            // 2. Draw Arms Outline
            if (settings.EnableOutline)
            {
                using (Pen pen = new Pen(outlineColor, settings.Thickness + settings.OutlineThickness * 2))
                {
                    pen.StartCap = LineCap.Square;
                    pen.EndCap = LineCap.Square;
                    for (int i = 0; i < armCount; i++)
                    {
                        float angle = rotationRad + i * (2f * (float)Math.PI / armCount);
                        float x1 = cx + gap * (float)Math.Cos(angle);
                        float y1 = cy + gap * (float)Math.Sin(angle);
                        float x2 = cx + (gap + len) * (float)Math.Cos(angle);
                        float y2 = cy + (gap + len) * (float)Math.Sin(angle);
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
            }

            // 3. Draw Arms Main Line
            using (Pen pen = new Pen(mainColor, settings.Thickness))
            {
                pen.StartCap = LineCap.Square;
                pen.EndCap = LineCap.Square;
                for (int i = 0; i < armCount; i++)
                {
                    float angle = rotationRad + i * (2f * (float)Math.PI / armCount);
                    float x1 = cx + gap * (float)Math.Cos(angle);
                    float y1 = cy + gap * (float)Math.Sin(angle);
                    float x2 = cx + (gap + len) * (float)Math.Cos(angle);
                    float y2 = cy + (gap + len) * (float)Math.Sin(angle);
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }
        }

        private void DrawDot(Graphics g, float cx, float cy, Color mainColor, Color outlineColor, CrosshairSettings settings)
        {
            float size = settings.Size;

            if (settings.EnableOutline)
            {
                float outSize = size + settings.OutlineThickness * 2;
                using (Brush b = new SolidBrush(outlineColor))
                {
                    g.FillEllipse(b, cx - outSize / 2f, cy - outSize / 2f, outSize, outSize);
                }
            }

            using (Brush b = new SolidBrush(mainColor))
            {
                g.FillEllipse(b, cx - size / 2f, cy - size / 2f, size, size);
            }
        }

        private void DrawCircle(Graphics g, float cx, float cy, Color mainColor, Color outlineColor, CrosshairSettings settings)
        {
            float size = settings.Size;

            if (settings.EnableOutline)
            {
                using (Pen pen = new Pen(outlineColor, settings.Thickness + settings.OutlineThickness * 2))
                {
                    g.DrawEllipse(pen, cx - size / 2f, cy - size / 2f, size, size);
                }
            }

            using (Pen pen = new Pen(mainColor, settings.Thickness))
            {
                g.DrawEllipse(pen, cx - size / 2f, cy - size / 2f, size, size);
            }
        }

        private void DrawSquare(Graphics g, float cx, float cy, Color mainColor, Color outlineColor, CrosshairSettings settings)
        {
            float width = settings.SquareWidth;
            float height = settings.SquareHeight;

            if (settings.SquareFillEnabled)
            {
                if (settings.EnableOutline)
                {
                    float outlinePadding = settings.OutlineThickness;
                    using (Brush b = new SolidBrush(outlineColor))
                    {
                        g.FillRectangle(b, cx - width / 2f - outlinePadding, cy - height / 2f - outlinePadding, width + outlinePadding * 2, height + outlinePadding * 2);
                    }
                }
                using (Brush b = new SolidBrush(mainColor))
                {
                    g.FillRectangle(b, cx - width / 2f, cy - height / 2f, width, height);
                }
            }
            else
            {
                if (settings.EnableOutline)
                {
                    using (Pen pen = new Pen(outlineColor, settings.Thickness + settings.OutlineThickness * 2))
                    {
                        g.DrawRectangle(pen, cx - width / 2f, cy - height / 2f, width, height);
                    }
                }

                using (Pen pen = new Pen(mainColor, settings.Thickness))
                {
                    g.DrawRectangle(pen, cx - width / 2f, cy - height / 2f, width, height);
                }
            }

            // Draw center dot for square (with separate AA handling)
            if (settings.ShowCenterDot)
            {
                DrawCenterDot(g, cx, cy, mainColor, settings);
            }
        }

        /// <summary>
        /// Draws the center dot with separate anti-aliasing handling based on dot shape.
        /// Circle dots need anti-aliasing, square dots don't.
        /// </summary>
        private void DrawCenterDot(Graphics g, float cx, float cy, Color mainColor, CrosshairSettings settings)
        {
            float dotSize = settings.CenterDotSize;
            Color dotOutlineColor = ColorTranslator.FromHtml(settings.CenterDotOutlineColorHex ?? "#000000");
            
            // Save current graphics state
            var oldSmoothingMode = g.SmoothingMode;
            var oldPixelOffsetMode = g.PixelOffsetMode;
            
            // Enable anti-aliasing only for circular center dots when AA is enabled globally
            bool needsDotAA = settings.AntiAliasing && settings.CenterDotShape == "Circle";
            if (needsDotAA)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.Half;
            }
            
            // Draw center dot outline
            if (settings.CenterDotEnableOutline)
            {
                float outSize = dotSize + settings.CenterDotOutlineThickness * 2;
                using (Brush b = new SolidBrush(dotOutlineColor))
                {
                    if (settings.CenterDotShape == "Circle")
                    {
                        g.FillEllipse(b, cx - outSize / 2f, cy - outSize / 2f, outSize, outSize);
                    }
                    else
                    {
                        g.FillRectangle(b, cx - outSize / 2f, cy - outSize / 2f, outSize, outSize);
                    }
                }
            }
            
            // Draw center dot
            using (Brush b = new SolidBrush(mainColor))
            {
                if (settings.CenterDotShape == "Circle")
                {
                    g.FillEllipse(b, cx - dotSize / 2f, cy - dotSize / 2f, dotSize, dotSize);
                }
                else
                {
                    g.FillRectangle(b, cx - dotSize / 2f, cy - dotSize / 2f, dotSize, dotSize);
                }
            }
            
            // Restore graphics state
            g.SmoothingMode = oldSmoothingMode;
            g.PixelOffsetMode = oldPixelOffsetMode;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bufferBitmap?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
