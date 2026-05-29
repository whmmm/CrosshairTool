using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CrosshairTool
{
    public class CrosshairForm : Form
    {
        private static readonly Color TransKey = Color.Magenta;

        public CrosshairForm()
        {
            // Set Form styles for overlay
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = TransKey;
            this.TransparencyKey = TransKey;
            this.StartPosition = FormStartPosition.Manual;
            this.DoubleBuffered = true;

            // Set styles to support transparency and prevent user interaction
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.SetStyle(ControlStyles.Selectable, false);

            UpdatePositionAndSize();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_TRANSPARENT (0x20) - click through
                // WS_EX_TOOLWINDOW (0x80) - hide from Alt-Tab
                // WS_EX_TOPMOST (0x8) - keep on top
                // WS_EX_NOACTIVATE (0x08000000) - don't steal focus
                cp.ExStyle |= 0x00000020 | 0x00000080 | 0x00000008 | 0x08000000;
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

            // Pad the radius for safety (thickness, outlines, anti-aliasing)
            int padding = settings.Thickness + settings.OutlineThickness + 15;
            int halfSize = maxRadius + padding;
            int size = halfSize * 2;

            // Set size
            if (this.Width != size || this.Height != size)
            {
                this.Width = size;
                this.Height = size;
            }

            // Center on primary screen
            Rectangle screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            int x = screenBounds.X + (screenBounds.Width - this.Width) / 2;
            int y = screenBounds.Y + (screenBounds.Height - this.Height) / 2;

            if (this.Left != x || this.Top != y)
            {
                this.Location = new Point(x, y);
            }

            this.Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Fill background with TransparencyKey color
            using (Brush b = new SolidBrush(TransKey))
            {
                e.Graphics.FillRectangle(b, ClientRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            var settings = SettingsManager.Current;
            if (settings == null) return;

            // Configure rendering quality based on settings
            if (settings.AntiAliasing)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.None;
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
            }
            else
            {
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.None;
                g.CompositingMode = CompositingMode.SourceOver;
            }

            Color mainColor = ColorTranslator.FromHtml(settings.ColorHex ?? "#00FF00");
            Color outlineColor = ColorTranslator.FromHtml(settings.OutlineColorHex ?? "#000000");

            // Avoid drawing transparency key as main color
            if (mainColor.ToArgb() == TransKey.ToArgb())
            {
                mainColor = Color.FromArgb(255, 255, 0, 254); // Visually identical Magenta
            }
            if (outlineColor.ToArgb() == TransKey.ToArgb())
            {
                outlineColor = Color.FromArgb(255, 255, 0, 254);
            }

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

        private void DrawCrosshair(Graphics g, float cx, float cy, Color mainColor, Color outlineColor, CrosshairSettings settings)
        {
            float gap = settings.InnerGap;
            float len = settings.ArmLength;
            int armCount = settings.ArmCount;
            float rotationRad = settings.RotationAngle * (float)Math.PI / 180f;

            // 1. Draw Optional Center Dot
            if (settings.ShowCenterDot)
            {
                float dotSize = settings.CenterDotSize;
                Color dotOutlineColor = ColorTranslator.FromHtml(settings.CenterDotOutlineColorHex ?? "#000000");
                
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

            // Draw center dot for square
            if (settings.ShowCenterDot)
            {
                float dotSize = settings.CenterDotSize;
                Color dotOutlineColor = ColorTranslator.FromHtml(settings.CenterDotOutlineColorHex ?? "#000000");
                
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
            }
        }
    }
}
