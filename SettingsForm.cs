using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrosshairTool
{
    public class SettingsForm : Form
    {
        private readonly CrosshairForm _crosshairForm;

        // Controls (assigned null! since they are initialized in InitializeComponent)
        private ComboBox cbStyle = null!;
        private Panel pnlColorPreview = null!;
        private Button btnChooseColor = null!;
        
        // Trackbars & Value Labels
        private TrackBar tbSize = null!;
        private TextBox txtSize = null!;
        private Label lblSize = null!;

        private TrackBar tbThickness = null!;
        private TextBox txtThickness = null!;
        private Label lblThickness = null!;

        private TrackBar tbArmCount = null!;
        private TextBox txtArmCount = null!;
        private Label lblArmCount = null!;

        private TrackBar tbInnerGap = null!;
        private TextBox txtInnerGap = null!;
        private Label lblInnerGap = null!;

        private TrackBar tbArmLength = null!;
        private TextBox txtArmLength = null!;
        private Label lblArmLength = null!;

        private TrackBar tbRotation = null!;
        private TextBox txtRotation = null!;
        private Label lblRotation = null!;

        private TrackBar tbSquareWidth = null!;
        private TextBox txtSquareWidth = null!;
        private Label lblSquareWidth = null!;

        private TrackBar tbSquareHeight = null!;
        private TextBox txtSquareHeight = null!;
        private Label lblSquareHeight = null!;

        private CheckBox chkSquareFill = null!;

        private CheckBox chkCenterDot = null!;
        private TrackBar tbCenterDotSize = null!;
        private TextBox txtCenterDotSize = null!;
        private Label lblCenterDotSize = null!;

        private CheckBox chkOutline = null!;
        private TrackBar tbOutlineThickness = null!;
        private TextBox txtOutlineThickness = null!;
        private Label lblOutlineThickness = null!;
        private Panel pnlOutlineColorPreview = null!;
        private Button btnChooseOutlineColor = null!;
        private Label lblOutlineColor = null!;

        private CheckBox chkAntiAliasing = null!;
        private CheckBox chkAutoStart = null!;
        private Button btnClose = null!;

        public SettingsForm(CrosshairForm crosshairForm)
        {
            _crosshairForm = crosshairForm;
            InitializeComponent();
            LoadSettingsIntoUI();
            UpdateControlVisibility();
        }

        private void InitializeComponent()
        {
            // Form setup (Dark Theme)
            this.Text = "屏幕准星设置 (Screen Crosshair Settings)";
            this.Size = new Size(460, 835);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 32);
            this.ForeColor = Color.FromArgb(230, 230, 235);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            int startY = 15;
            int labelX = 20;
            int controlX = 140;
            int width = 280;

            // 1. Style Selection
            var lblStyle = new Label { Text = "准星样式:", Location = new Point(labelX, startY), Size = new Size(110, 25), ForeColor = Color.FromArgb(180, 180, 185) };
            cbStyle = new ComboBox { Location = new Point(controlX, startY), Size = new Size(width, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cbStyle.Items.AddRange(new object[] { "Crosshair (十字形)", "Dot (圆点)", "Circle (圆形)", "Square (方框)" });
            cbStyle.SelectedIndexChanged += (s, e) => {
                string[] styles = { "Crosshair", "Dot", "Circle", "Square" };
                SettingsManager.Current.Style = styles[cbStyle.SelectedIndex];
                UpdateControlVisibility();
                ApplyChanges();
            };
            this.Controls.Add(lblStyle);
            this.Controls.Add(cbStyle);

            // 2. Crosshair Color
            startY += 40;
            var lblColor = new Label { Text = "准星颜色:", Location = new Point(labelX, startY), Size = new Size(110, 25), ForeColor = Color.FromArgb(180, 180, 185) };
            pnlColorPreview = new Panel { Location = new Point(controlX, startY), Size = new Size(40, 25), BorderStyle = BorderStyle.FixedSingle };
            btnChooseColor = new Button { Text = "选择颜色...", Location = new Point(controlX + 50, startY), Size = new Size(120, 25), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55), ForeColor = Color.White };
            btnChooseColor.FlatAppearance.BorderSize = 0;
            btnChooseColor.Click += ChooseColor_Click;
            
            // Add quick color buttons
            int qx = controlX + 180;
            Color[] quickColors = { Color.Lime, Color.Red, Color.Cyan, Color.Yellow };
            foreach (var qc in quickColors)
            {
                var btnQuick = new Button { Size = new Size(20, 20), Location = new Point(qx, startY + 2), BackColor = qc, FlatStyle = FlatStyle.Flat };
                btnQuick.FlatAppearance.BorderSize = 0;
                btnQuick.Click += (s, e) => {
                    SettingsManager.Current.ColorHex = ColorTranslator.ToHtml(qc);
                    pnlColorPreview.BackColor = qc;
                    ApplyChanges();
                };
                this.Controls.Add(btnQuick);
                qx += 25;
            }

            this.Controls.Add(lblColor);
            this.Controls.Add(pnlColorPreview);
            this.Controls.Add(btnChooseColor);

            // Group Box for Dimensions
            startY += 45;
            var grpDim = new GroupBox { Text = "外观参数", Location = new Point(labelX, startY), Size = new Size(width + 120, 445), ForeColor = Color.FromArgb(0, 180, 255) };
            this.Controls.Add(grpDim);

            int dimY = 25;
            int trackWidth = 200;
            int valX = 340;

            // Size Slider
            lblSize = new Label { Text = "大小 (Size):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbSize = new TrackBar { Minimum = 2, Maximum = 100, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtSize = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbSize.Scroll += (s, e) => { SettingsManager.Current.Size = tbSize.Value; txtSize.Text = tbSize.Value.ToString(); ApplyChanges(); };
            txtSize.LostFocus += (s, e) => { UpdateFromTextBox(txtSize, tbSize, v => SettingsManager.Current.Size = v, SettingsManager.Current.Size); };
            txtSize.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtSize, tbSize, v => SettingsManager.Current.Size = v, SettingsManager.Current.Size); txtSize.Parent?.SelectNextControl(txtSize, true, true, true, true); } };
            grpDim.Controls.Add(lblSize); grpDim.Controls.Add(tbSize); grpDim.Controls.Add(txtSize);

            // Thickness Slider
            dimY += 45;
            lblThickness = new Label { Text = "粗细 (Thickness):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbThickness = new TrackBar { Minimum = 1, Maximum = 20, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtThickness = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbThickness.Scroll += (s, e) => { SettingsManager.Current.Thickness = tbThickness.Value; txtThickness.Text = tbThickness.Value.ToString(); ApplyChanges(); };
            txtThickness.LostFocus += (s, e) => { UpdateFromTextBox(txtThickness, tbThickness, v => SettingsManager.Current.Thickness = v, SettingsManager.Current.Thickness); };
            txtThickness.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtThickness, tbThickness, v => SettingsManager.Current.Thickness = v, SettingsManager.Current.Thickness); txtThickness.Parent?.SelectNextControl(txtThickness, true, true, true, true); } };
            grpDim.Controls.Add(lblThickness); grpDim.Controls.Add(tbThickness); grpDim.Controls.Add(txtThickness);

            // Arm Count Slider
            dimY += 45;
            lblArmCount = new Label { Text = "臂数 (Arms):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbArmCount = new TrackBar { Minimum = 2, Maximum = 12, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtArmCount = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbArmCount.Scroll += (s, e) => { SettingsManager.Current.ArmCount = tbArmCount.Value; txtArmCount.Text = tbArmCount.Value.ToString(); ApplyChanges(); };
            txtArmCount.LostFocus += (s, e) => { UpdateFromTextBox(txtArmCount, tbArmCount, v => SettingsManager.Current.ArmCount = v, SettingsManager.Current.ArmCount); };
            txtArmCount.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtArmCount, tbArmCount, v => SettingsManager.Current.ArmCount = v, SettingsManager.Current.ArmCount); txtArmCount.Parent?.SelectNextControl(txtArmCount, true, true, true, true); } };
            grpDim.Controls.Add(lblArmCount); grpDim.Controls.Add(tbArmCount); grpDim.Controls.Add(txtArmCount);

            // Inner Gap Slider
            dimY += 45;
            lblInnerGap = new Label { Text = "内间距 (Gap):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbInnerGap = new TrackBar { Minimum = 0, Maximum = 50, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtInnerGap = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbInnerGap.Scroll += (s, e) => { SettingsManager.Current.InnerGap = tbInnerGap.Value; txtInnerGap.Text = tbInnerGap.Value.ToString(); ApplyChanges(); };
            txtInnerGap.LostFocus += (s, e) => { UpdateFromTextBox(txtInnerGap, tbInnerGap, v => SettingsManager.Current.InnerGap = v, SettingsManager.Current.InnerGap); };
            txtInnerGap.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtInnerGap, tbInnerGap, v => SettingsManager.Current.InnerGap = v, SettingsManager.Current.InnerGap); txtInnerGap.Parent?.SelectNextControl(txtInnerGap, true, true, true, true); } };
            grpDim.Controls.Add(lblInnerGap); grpDim.Controls.Add(tbInnerGap); grpDim.Controls.Add(txtInnerGap);

            // Arm Length Slider
            dimY += 45;
            lblArmLength = new Label { Text = "臂长 (Length):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbArmLength = new TrackBar { Minimum = 1, Maximum = 100, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtArmLength = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbArmLength.Scroll += (s, e) => { SettingsManager.Current.ArmLength = tbArmLength.Value; txtArmLength.Text = tbArmLength.Value.ToString(); ApplyChanges(); };
            txtArmLength.LostFocus += (s, e) => { UpdateFromTextBox(txtArmLength, tbArmLength, v => SettingsManager.Current.ArmLength = v, SettingsManager.Current.ArmLength); };
            txtArmLength.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtArmLength, tbArmLength, v => SettingsManager.Current.ArmLength = v, SettingsManager.Current.ArmLength); txtArmLength.Parent?.SelectNextControl(txtArmLength, true, true, true, true); } };
            grpDim.Controls.Add(lblArmLength); grpDim.Controls.Add(tbArmLength); grpDim.Controls.Add(txtArmLength);

            // Rotation Slider
            dimY += 45;
            lblRotation = new Label { Text = "旋转 (Rotation):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbRotation = new TrackBar { Minimum = 0, Maximum = 360, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtRotation = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbRotation.Scroll += (s, e) => { SettingsManager.Current.RotationAngle = tbRotation.Value; txtRotation.Text = tbRotation.Value.ToString(); ApplyChanges(); };
            txtRotation.LostFocus += (s, e) => { UpdateFromTextBox(txtRotation, tbRotation, v => SettingsManager.Current.RotationAngle = v, (int)SettingsManager.Current.RotationAngle); };
            txtRotation.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtRotation, tbRotation, v => SettingsManager.Current.RotationAngle = v, (int)SettingsManager.Current.RotationAngle); txtRotation.Parent?.SelectNextControl(txtRotation, true, true, true, true); } };
            grpDim.Controls.Add(lblRotation); grpDim.Controls.Add(tbRotation); grpDim.Controls.Add(txtRotation);

            // Square Width Slider
            dimY += 45;
            lblSquareWidth = new Label { Text = "方形宽度:", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbSquareWidth = new TrackBar { Minimum = 2, Maximum = 100, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtSquareWidth = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbSquareWidth.Scroll += (s, e) => { 
                if (SettingsManager.Current.Style == "Square")
                {
                    SettingsManager.Current.SquareWidth = tbSquareWidth.Value; 
                    txtSquareWidth.Text = tbSquareWidth.Value.ToString(); 
                    ApplyChanges(); 
                }
                else
                {
                    tbSquareWidth.Value = SettingsManager.Current.SquareWidth;
                }
            };
            txtSquareWidth.LostFocus += (s, e) => { 
                if (SettingsManager.Current.Style == "Square") { 
                    if (int.TryParse(txtSquareWidth.Text, out int val))
                    {
                        val = Constrain(val, tbSquareWidth.Minimum, tbSquareWidth.Maximum);
                        tbSquareWidth.Value = val;
                        txtSquareWidth.Text = val.ToString();
                        SettingsManager.Current.SquareWidth = val;
                        ApplyChanges();
                    }
                    else
                    {
                        txtSquareWidth.Text = SettingsManager.Current.SquareWidth.ToString();
                    }
                } 
            };
            txtSquareWidth.KeyPress += (s, e) => { 
                if (e.KeyChar == (char)Keys.Enter && SettingsManager.Current.Style == "Square") { 
                    if (int.TryParse(txtSquareWidth.Text, out int val))
                    {
                        val = Constrain(val, tbSquareWidth.Minimum, tbSquareWidth.Maximum);
                        tbSquareWidth.Value = val;
                        txtSquareWidth.Text = val.ToString();
                        SettingsManager.Current.SquareWidth = val;
                        ApplyChanges();
                    }
                    else
                    {
                        txtSquareWidth.Text = SettingsManager.Current.SquareWidth.ToString();
                    }
                    txtSquareWidth.Parent?.SelectNextControl(txtSquareWidth, true, true, true, true); 
                } 
            };
            grpDim.Controls.Add(lblSquareWidth); grpDim.Controls.Add(tbSquareWidth); grpDim.Controls.Add(txtSquareWidth);

            // Square Height Slider
            dimY += 45;
            lblSquareHeight = new Label { Text = "方形高度:", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbSquareHeight = new TrackBar { Minimum = 2, Maximum = 100, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtSquareHeight = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbSquareHeight.Scroll += (s, e) => { 
                if (SettingsManager.Current.Style == "Square")
                {
                    SettingsManager.Current.SquareHeight = tbSquareHeight.Value; 
                    txtSquareHeight.Text = tbSquareHeight.Value.ToString(); 
                    ApplyChanges(); 
                }
                else
                {
                    tbSquareHeight.Value = SettingsManager.Current.SquareHeight;
                }
            };
            txtSquareHeight.LostFocus += (s, e) => { 
                if (SettingsManager.Current.Style == "Square") { 
                    if (int.TryParse(txtSquareHeight.Text, out int val))
                    {
                        val = Constrain(val, tbSquareHeight.Minimum, tbSquareHeight.Maximum);
                        tbSquareHeight.Value = val;
                        txtSquareHeight.Text = val.ToString();
                        SettingsManager.Current.SquareHeight = val;
                        ApplyChanges();
                    }
                    else
                    {
                        txtSquareHeight.Text = SettingsManager.Current.SquareHeight.ToString();
                    }
                } 
            };
            txtSquareHeight.KeyPress += (s, e) => { 
                if (e.KeyChar == (char)Keys.Enter && SettingsManager.Current.Style == "Square") { 
                    if (int.TryParse(txtSquareHeight.Text, out int val))
                    {
                        val = Constrain(val, tbSquareHeight.Minimum, tbSquareHeight.Maximum);
                        tbSquareHeight.Value = val;
                        txtSquareHeight.Text = val.ToString();
                        SettingsManager.Current.SquareHeight = val;
                        ApplyChanges();
                    }
                    else
                    {
                        txtSquareHeight.Text = SettingsManager.Current.SquareHeight.ToString();
                    }
                    txtSquareHeight.Parent?.SelectNextControl(txtSquareHeight, true, true, true, true); 
                } 
            };
            grpDim.Controls.Add(lblSquareHeight); grpDim.Controls.Add(tbSquareHeight); grpDim.Controls.Add(txtSquareHeight);

            // Square Fill Checkbox
            dimY += 45;
            chkSquareFill = new CheckBox { Text = "方形填充", Checked = false, Location = new Point(15, dimY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            chkSquareFill.CheckedChanged += (s, e) => {
                if (SettingsManager.Current.Style == "Square")
                {
                    SettingsManager.Current.SquareFillEnabled = chkSquareFill.Checked;
                    ApplyChanges();
                }
                else
                {
                    chkSquareFill.Checked = SettingsManager.Current.SquareFillEnabled;
                }
            };
            grpDim.Controls.Add(chkSquareFill);

            // Group Box for Center Dot & Outline
            startY += 455;
            var grpEffects = new GroupBox { Text = "描边与中心点", Location = new Point(labelX, startY), Size = new Size(width + 120, 160), ForeColor = Color.FromArgb(0, 180, 255) };
            this.Controls.Add(grpEffects);

            int effY = 25;

            // Center Dot Checkbox & Size Slider
            chkCenterDot = new CheckBox { Text = "显示中心点", Checked = true, Location = new Point(15, effY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            chkCenterDot.CheckedChanged += (s, e) => {
                SettingsManager.Current.ShowCenterDot = chkCenterDot.Checked;
                UpdateControlVisibility();
                ApplyChanges();
            };
            tbCenterDotSize = new TrackBar { Minimum = 1, Maximum = 30, Location = new Point(130, effY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtCenterDotSize = new TextBox { Location = new Point(valX, effY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            lblCenterDotSize = new Label { Text = "中心点大小:", Location = new Point(130, effY + 25), Size = new Size(100, 15), Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(200, 200, 205) };
            tbCenterDotSize.Scroll += (s, e) => { SettingsManager.Current.CenterDotSize = tbCenterDotSize.Value; txtCenterDotSize.Text = tbCenterDotSize.Value.ToString(); ApplyChanges(); };
            txtCenterDotSize.LostFocus += (s, e) => { UpdateFromTextBox(txtCenterDotSize, tbCenterDotSize, v => SettingsManager.Current.CenterDotSize = v, SettingsManager.Current.CenterDotSize); };
            txtCenterDotSize.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtCenterDotSize, tbCenterDotSize, v => SettingsManager.Current.CenterDotSize = v, SettingsManager.Current.CenterDotSize); txtCenterDotSize.Parent?.SelectNextControl(txtCenterDotSize, true, true, true, true); } };
            grpEffects.Controls.Add(chkCenterDot); grpEffects.Controls.Add(tbCenterDotSize); grpEffects.Controls.Add(txtCenterDotSize); grpEffects.Controls.Add(lblCenterDotSize);

            // Outline Checkbox, Thickness Slider & Color
            effY += 45;
            chkOutline = new CheckBox { Text = "启用描边", Checked = true, Location = new Point(15, effY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            chkOutline.CheckedChanged += (s, e) => {
                SettingsManager.Current.EnableOutline = chkOutline.Checked;
                UpdateControlVisibility();
                ApplyChanges();
            };
            tbOutlineThickness = new TrackBar { Minimum = 1, Maximum = 10, Location = new Point(130, effY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtOutlineThickness = new TextBox { Location = new Point(valX, effY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            lblOutlineThickness = new Label { Text = "描边粗细:", Location = new Point(130, effY + 25), Size = new Size(100, 15), Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(200, 200, 205) };
            tbOutlineThickness.Scroll += (s, e) => { SettingsManager.Current.OutlineThickness = tbOutlineThickness.Value; txtOutlineThickness.Text = tbOutlineThickness.Value.ToString(); ApplyChanges(); };
            txtOutlineThickness.LostFocus += (s, e) => { UpdateFromTextBox(txtOutlineThickness, tbOutlineThickness, v => SettingsManager.Current.OutlineThickness = v, SettingsManager.Current.OutlineThickness); };
            txtOutlineThickness.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtOutlineThickness, tbOutlineThickness, v => SettingsManager.Current.OutlineThickness = v, SettingsManager.Current.OutlineThickness); txtOutlineThickness.Parent?.SelectNextControl(txtOutlineThickness, true, true, true, true); } };
            grpEffects.Controls.Add(chkOutline); grpEffects.Controls.Add(tbOutlineThickness); grpEffects.Controls.Add(txtOutlineThickness); grpEffects.Controls.Add(lblOutlineThickness);

            effY += 40;
            lblOutlineColor = new Label { Text = "描边颜色:", Location = new Point(15, effY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            pnlOutlineColorPreview = new Panel { Location = new Point(135, effY - 3), Size = new Size(30, 20), BorderStyle = BorderStyle.FixedSingle };
            btnChooseOutlineColor = new Button { Text = "颜色...", Location = new Point(175, effY - 5), Size = new Size(70, 24), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55), ForeColor = Color.White, Font = new Font("Segoe UI", 8.5F) };
            btnChooseOutlineColor.FlatAppearance.BorderSize = 0;
            btnChooseOutlineColor.Click += ChooseOutlineColor_Click;
            grpEffects.Controls.Add(lblOutlineColor); grpEffects.Controls.Add(pnlOutlineColorPreview); grpEffects.Controls.Add(btnChooseOutlineColor);

            // 4. Anti-Aliasing & Auto Start & Close
            startY += 175;
            chkAntiAliasing = new CheckBox { Text = "开启抗锯齿 (Enable Anti-Aliasing)", Location = new Point(labelX, startY), Size = new Size(250, 25), ForeColor = Color.FromArgb(200, 200, 200) };
            chkAntiAliasing.CheckedChanged += (s, e) => {
                SettingsManager.Current.AntiAliasing = chkAntiAliasing.Checked;
                ApplyChanges();
            };
            this.Controls.Add(chkAntiAliasing);

            startY += 30;
            chkAutoStart = new CheckBox { Text = "开机自启动 (Auto-start on boot)", Location = new Point(labelX, startY), Size = new Size(250, 25), ForeColor = Color.FromArgb(200, 200, 200) };
            chkAutoStart.CheckedChanged += (s, e) => {
                SettingsManager.Current.AutoStart = chkAutoStart.Checked;
                ApplyChanges();
            };
            this.Controls.Add(chkAutoStart);

            btnClose = new Button { Text = "关闭 (后台运行)", Location = new Point(width + 20, startY - 15), Size = new Size(100, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => {
                SettingsManager.Save();
                this.Hide();
            };
            this.Controls.Add(btnClose);

            // Handle Form Closing (Hide it instead of destroying, except when application exits)
            this.FormClosing += (s, e) => {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    SettingsManager.Save();
                    this.Hide();
                }
            };
        }

        private void ChooseColor_Click(object? sender, EventArgs e)
        {
            using (var cd = new ColorDialog())
            {
                cd.Color = ColorTranslator.FromHtml(SettingsManager.Current.ColorHex ?? "#00FF00");
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    SettingsManager.Current.ColorHex = ColorTranslator.ToHtml(cd.Color);
                    pnlColorPreview.BackColor = cd.Color;
                    ApplyChanges();
                }
            }
        }

        private void ChooseOutlineColor_Click(object? sender, EventArgs e)
        {
            using (var cd = new ColorDialog())
            {
                cd.Color = ColorTranslator.FromHtml(SettingsManager.Current.OutlineColorHex ?? "#000000");
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    SettingsManager.Current.OutlineColorHex = ColorTranslator.ToHtml(cd.Color);
                    pnlOutlineColorPreview.BackColor = cd.Color;
                    ApplyChanges();
                }
            }
        }

        private void LoadSettingsIntoUI()
        {
            var settings = SettingsManager.Current;

            // Style
            string[] styles = { "Crosshair", "Dot", "Circle", "Square" };
            int idx = Array.IndexOf(styles, settings.Style);
            cbStyle.SelectedIndex = idx >= 0 ? idx : 0;

            // Color
            Color mainColor = ColorTranslator.FromHtml(settings.ColorHex ?? "#00FF00");
            pnlColorPreview.BackColor = mainColor;

            // Sliders
            tbSize.Value = Constrain(settings.Size, tbSize.Minimum, tbSize.Maximum);
            txtSize.Text = tbSize.Value.ToString();

            tbThickness.Value = Constrain(settings.Thickness, tbThickness.Minimum, tbThickness.Maximum);
            txtThickness.Text = tbThickness.Value.ToString();

            tbArmCount.Value = Constrain(settings.ArmCount, tbArmCount.Minimum, tbArmCount.Maximum);
            txtArmCount.Text = tbArmCount.Value.ToString();

            tbInnerGap.Value = Constrain(settings.InnerGap, tbInnerGap.Minimum, tbInnerGap.Maximum);
            txtInnerGap.Text = tbInnerGap.Value.ToString();

            tbArmLength.Value = Constrain(settings.ArmLength, tbArmLength.Minimum, tbArmLength.Maximum);
            txtArmLength.Text = tbArmLength.Value.ToString();

            // Rotation
            tbRotation.Value = Constrain((int)settings.RotationAngle, tbRotation.Minimum, tbRotation.Maximum);
            txtRotation.Text = tbRotation.Value.ToString();

            // Square
            tbSquareWidth.Value = Constrain(settings.SquareWidth, tbSquareWidth.Minimum, tbSquareWidth.Maximum);
            txtSquareWidth.Text = tbSquareWidth.Value.ToString();

            tbSquareHeight.Value = Constrain(settings.SquareHeight, tbSquareHeight.Minimum, tbSquareHeight.Maximum);
            txtSquareHeight.Text = tbSquareHeight.Value.ToString();

            chkSquareFill.Checked = settings.SquareFillEnabled;

            // Center Dot
            chkCenterDot.Checked = settings.ShowCenterDot;
            tbCenterDotSize.Value = Constrain(settings.CenterDotSize, tbCenterDotSize.Minimum, tbCenterDotSize.Maximum);
            txtCenterDotSize.Text = tbCenterDotSize.Value.ToString();

            // Outline
            chkOutline.Checked = settings.EnableOutline;
            tbOutlineThickness.Value = Constrain(settings.OutlineThickness, tbOutlineThickness.Minimum, tbOutlineThickness.Maximum);
            txtOutlineThickness.Text = tbOutlineThickness.Value.ToString();
            pnlOutlineColorPreview.BackColor = ColorTranslator.FromHtml(settings.OutlineColorHex ?? "#000000");

            // Anti-Aliasing
            chkAntiAliasing.Checked = settings.AntiAliasing;

            // Auto start
            chkAutoStart.Checked = settings.AutoStart;
        }

        private void UpdateControlVisibility()
        {
            string style = SettingsManager.Current.Style;
            bool isCross = (style == "Crosshair");

            Color activeColor = Color.White;
            Color inactiveColor = Color.FromArgb(128, 128, 128);
            Color activeValColor = Color.White;
            Color inactiveValColor = Color.FromArgb(128, 128, 128);

            // Size Slider
            bool sizeEnabled = (style != "Crosshair");
            tbSize.Enabled = sizeEnabled;
            lblSize.ForeColor = sizeEnabled ? activeColor : inactiveColor;
            txtSize.ForeColor = sizeEnabled ? activeValColor : inactiveValColor;

            // Thickness Slider
            bool thicknessEnabled = (style != "Dot");
            tbThickness.Enabled = thicknessEnabled;
            lblThickness.ForeColor = thicknessEnabled ? activeColor : inactiveColor;
            txtThickness.ForeColor = thicknessEnabled ? activeValColor : inactiveValColor;

            // Arm Count Slider
            tbArmCount.Enabled = isCross;
            lblArmCount.ForeColor = isCross ? activeColor : inactiveColor;
            txtArmCount.ForeColor = isCross ? activeValColor : inactiveValColor;

            // Inner Gap Slider
            tbInnerGap.Enabled = isCross;
            lblInnerGap.ForeColor = isCross ? activeColor : inactiveColor;
            txtInnerGap.ForeColor = isCross ? activeValColor : inactiveValColor;

            // Arm Length Slider
            tbArmLength.Enabled = isCross;
            lblArmLength.ForeColor = isCross ? activeColor : inactiveColor;
            txtArmLength.ForeColor = isCross ? activeValColor : inactiveValColor;

            // Rotation Slider
            tbRotation.Enabled = isCross;
            lblRotation.ForeColor = isCross ? activeColor : inactiveColor;
            txtRotation.ForeColor = isCross ? activeValColor : inactiveValColor;

            // Square controls
            bool isSquare = (style == "Square");
            tbSquareWidth.Enabled = true;
            lblSquareWidth.ForeColor = isSquare ? activeColor : inactiveColor;
            txtSquareWidth.ForeColor = isSquare ? activeValColor : inactiveValColor;

            tbSquareHeight.Enabled = true;
            lblSquareHeight.ForeColor = isSquare ? activeColor : inactiveColor;
            txtSquareHeight.ForeColor = isSquare ? activeValColor : inactiveValColor;

            chkSquareFill.Enabled = true;
            chkSquareFill.ForeColor = isSquare ? Color.FromArgb(230, 230, 235) : inactiveColor;

            // Center Dot
            chkCenterDot.Enabled = true;
            bool centerDotSizeEnabled = isCross && chkCenterDot.Checked;
            tbCenterDotSize.Enabled = true;
            lblCenterDotSize.ForeColor = centerDotSizeEnabled ? Color.FromArgb(150, 150, 155) : inactiveColor;
            txtCenterDotSize.ForeColor = centerDotSizeEnabled ? activeValColor : inactiveValColor;

            // Outline
            bool outlineEnabled = chkOutline.Checked;
            tbOutlineThickness.Enabled = outlineEnabled;
            lblOutlineThickness.ForeColor = outlineEnabled ? Color.FromArgb(150, 150, 155) : inactiveColor;
            txtOutlineThickness.ForeColor = outlineEnabled ? activeValColor : inactiveValColor;
            btnChooseOutlineColor.Enabled = outlineEnabled;
            lblOutlineColor.ForeColor = outlineEnabled ? activeColor : inactiveColor;
        }

        private void ApplyChanges()
        {
            _crosshairForm.UpdatePositionAndSize();
            SettingsManager.Save();
        }

        private int Constrain(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        private void UpdateFromTextBox(TextBox txt, TrackBar tb, Action<int> setter, int defaultValue)
        {
            if (int.TryParse(txt.Text, out int val))
            {
                val = Constrain(val, tb.Minimum, tb.Maximum);
                tb.Value = val;
                txt.Text = val.ToString();
                setter(val);
                ApplyChanges();
            }
            else
            {
                txt.Text = defaultValue.ToString();
            }
        }
    }
}
