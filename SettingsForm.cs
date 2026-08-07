using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrosshairTool
{
    public class SettingsForm : Form
    {
        private readonly CrosshairForm _crosshairForm;
        private readonly Action? _onProfilesChanged;

        // Controls (assigned null! since they are initialized in InitializeComponent)
        private ComboBox cbProfile = null!;
        private Button btnNewProfile = null!;
        private Button btnDuplicateProfile = null!;
        private Button btnRenameProfile = null!;
        private Button btnDeleteProfile = null!;

        private readonly List<Button> _quickColorButtons = new();

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

        private TrackBar tbSquareCornerX = null!;
        private TextBox txtSquareCornerX = null!;
        private Label lblSquareCornerX = null!;
        private TrackBar tbSquareCornerY = null!;
        private TextBox txtSquareCornerY = null!;
        private Label lblSquareCornerY = null!;

        private CheckBox chkCenterDot = null!;
        private TrackBar tbCenterDotSize = null!;
        private TextBox txtCenterDotSize = null!;
        private Label lblCenterDotSize = null!;
        private ComboBox cbCenterDotShape = null!;
        private CheckBox chkCenterDotOutline = null!;
        private TrackBar tbCenterDotOutlineThickness = null!;
        private TextBox txtCenterDotOutlineThickness = null!;
        private Label lblCenterDotOutlineThickness = null!;
        private Panel pnlCenterDotOutlineColorPreview = null!;
        private Button btnChooseCenterDotOutlineColor = null!;
        private Label lblCenterDotOutlineColor = null!;

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
        private TextBox txtToggleHotkey = null!;
        private TextBox txtCycleHotkey = null!;

        private TrackBar tbOffsetX = null!;
        private TextBox txtOffsetX = null!;
        private Label lblOffsetX = null!;
        private TrackBar tbOffsetY = null!;
        private TextBox txtOffsetY = null!;
        private Label lblOffsetY = null!;

        // Debounce timer: avoids writing to disk on every scroll tick
        private readonly System.Windows.Forms.Timer _saveDebounceTimer;
        private const int SaveDebounceMs = 400;

        public SettingsForm(CrosshairForm crosshairForm, Action? onProfilesChanged = null)
        {
            _crosshairForm = crosshairForm;
            _onProfilesChanged = onProfilesChanged;

            _saveDebounceTimer = new System.Windows.Forms.Timer { Interval = SaveDebounceMs };
            _saveDebounceTimer.Tick += (s, e) =>
            {
                _saveDebounceTimer.Stop();
                SettingsManager.Save();
            };

            InitializeComponent();
            RefreshProfileList();
            LoadSettingsIntoUI();
            UpdateControlVisibility();
        }

        /// <summary>
        /// Reloads the UI from the current settings profile, e.g. after tray-menu profile switch.
        /// </summary>
        public void ReloadFromSettings()
        {
            RefreshProfileList();
            LoadSettingsIntoUI();
            UpdateControlVisibility();
        }

        private void InitializeComponent()
        {
            // Form setup (Dark Theme)
            this.Text = "屏幕准星设置 (Screen Crosshair Settings)";
            this.Size = new Size(475, 750);
            this.MinimumSize = new Size(475, 420);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 32);
            this.ForeColor = Color.FromArgb(230, 230, 235);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            // Create scrollable panel
            var scrollPanel = new Panel();
            scrollPanel.Location = new Point(0, 0);
            scrollPanel.Size = new Size(this.ClientSize.Width, this.ClientSize.Height);
            scrollPanel.AutoScroll = true;
            scrollPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(scrollPanel);

            int startY = 15;
            int labelX = 20;
            int controlX = 140;
            int width = 280;

            // ── Profile Management (Row 1: label + combobox) ──
            var lblProfile = new Label { Text = "配置 (Profile):", Location = new Point(labelX, startY), Size = new Size(110, 25), ForeColor = Color.FromArgb(180, 180, 185) };
            cbProfile = new ComboBox { Location = new Point(controlX, startY), Size = new Size(width, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cbProfile.SelectedIndexChanged += CbProfile_SelectedIndexChanged;
            scrollPanel.Controls.Add(lblProfile);
            scrollPanel.Controls.Add(cbProfile);

            // ── Profile Management (Row 2: action buttons) ──
            startY += 32;
            int btnX = controlX;
            btnNewProfile = CreateSmallButton("新建", btnX, startY);
            btnNewProfile.Click += BtnNewProfile_Click;
            scrollPanel.Controls.Add(btnNewProfile);
            btnX += 56;

            btnDuplicateProfile = CreateSmallButton("复制", btnX, startY);
            btnDuplicateProfile.Click += BtnDuplicateProfile_Click;
            scrollPanel.Controls.Add(btnDuplicateProfile);
            btnX += 56;

            btnRenameProfile = CreateSmallButton("重命名", btnX, startY, 60);
            btnRenameProfile.Click += BtnRenameProfile_Click;
            scrollPanel.Controls.Add(btnRenameProfile);
            btnX += 66;

            btnDeleteProfile = CreateSmallButton("删除", btnX, startY);
            btnDeleteProfile.Click += BtnDeleteProfile_Click;
            scrollPanel.Controls.Add(btnDeleteProfile);

            startY += 38;

            // 1. Style Selection
            var lblStyle = new Label { Text = "准星样式:", Location = new Point(labelX, startY), Size = new Size(110, 25), ForeColor = Color.FromArgb(180, 180, 185) };
            cbStyle = new ComboBox { Location = new Point(controlX, startY), Size = new Size(width, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cbStyle.Items.AddRange(new object[] { "Crosshair (十字形)", "Dot (圆点)", "Circle (圆形)", "Square (方框)" });
            cbStyle.SelectedIndexChanged += (s, e) => {
                if (_isLoadingUI) return;
                string[] styles = { "Crosshair", "Dot", "Circle", "Square" };
                SettingsManager.Current.Style = styles[cbStyle.SelectedIndex];
                UpdateControlVisibility();
                ApplyChanges();
            };
            scrollPanel.Controls.Add(lblStyle);
            scrollPanel.Controls.Add(cbStyle);

            // 2. Crosshair Color
            startY += 40;
            var lblColor = new Label { Text = "准星颜色:", Location = new Point(labelX, startY), Size = new Size(110, 25), ForeColor = Color.FromArgb(180, 180, 185) };
            pnlColorPreview = new Panel { Location = new Point(controlX, startY), Size = new Size(40, 25), BorderStyle = BorderStyle.FixedSingle };
            btnChooseColor = new Button { Text = "选择颜色", Location = new Point(controlX + 50, startY), Size = new Size(100, 25), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55), ForeColor = Color.White };
            btnChooseColor.FlatAppearance.BorderSize = 0;
            btnChooseColor.Click += ChooseColor_Click;

            scrollPanel.Controls.Add(lblColor);
            scrollPanel.Controls.Add(pnlColorPreview);
            scrollPanel.Controls.Add(btnChooseColor);

            // Quick color buttons on their own row
            startY += 35;
            RefreshQuickColorButtons(scrollPanel, startY);

            // Group Box for Dimensions
            startY += 40;
            var grpDim = new GroupBox { Text = "外观参数", Location = new Point(labelX, startY), Size = new Size(width + 120, 590), ForeColor = Color.FromArgb(0, 180, 255) };
            scrollPanel.Controls.Add(grpDim);

            int dimY = 28;
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
            dimY += 50;
            lblThickness = new Label { Text = "粗细 (Thickness):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbThickness = new TrackBar { Minimum = 1, Maximum = 20, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtThickness = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbThickness.Scroll += (s, e) => { SettingsManager.Current.Thickness = tbThickness.Value; txtThickness.Text = tbThickness.Value.ToString(); ApplyChanges(); };
            txtThickness.LostFocus += (s, e) => { UpdateFromTextBox(txtThickness, tbThickness, v => SettingsManager.Current.Thickness = v, SettingsManager.Current.Thickness); };
            txtThickness.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtThickness, tbThickness, v => SettingsManager.Current.Thickness = v, SettingsManager.Current.Thickness); txtThickness.Parent?.SelectNextControl(txtThickness, true, true, true, true); } };
            grpDim.Controls.Add(lblThickness); grpDim.Controls.Add(tbThickness); grpDim.Controls.Add(txtThickness);

            // Arm Count Slider
            dimY += 50;
            lblArmCount = new Label { Text = "臂数 (Arms):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbArmCount = new TrackBar { Minimum = 2, Maximum = 12, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtArmCount = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbArmCount.Scroll += (s, e) => { SettingsManager.Current.ArmCount = tbArmCount.Value; txtArmCount.Text = tbArmCount.Value.ToString(); ApplyChanges(); };
            txtArmCount.LostFocus += (s, e) => { UpdateFromTextBox(txtArmCount, tbArmCount, v => SettingsManager.Current.ArmCount = v, SettingsManager.Current.ArmCount); };
            txtArmCount.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtArmCount, tbArmCount, v => SettingsManager.Current.ArmCount = v, SettingsManager.Current.ArmCount); txtArmCount.Parent?.SelectNextControl(txtArmCount, true, true, true, true); } };
            grpDim.Controls.Add(lblArmCount); grpDim.Controls.Add(tbArmCount); grpDim.Controls.Add(txtArmCount);

            // Inner Gap Slider
            dimY += 50;
            lblInnerGap = new Label { Text = "内间距 (Gap):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbInnerGap = new TrackBar { Minimum = 0, Maximum = 50, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtInnerGap = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbInnerGap.Scroll += (s, e) => { SettingsManager.Current.InnerGap = tbInnerGap.Value; txtInnerGap.Text = tbInnerGap.Value.ToString(); ApplyChanges(); };
            txtInnerGap.LostFocus += (s, e) => { UpdateFromTextBox(txtInnerGap, tbInnerGap, v => SettingsManager.Current.InnerGap = v, SettingsManager.Current.InnerGap); };
            txtInnerGap.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtInnerGap, tbInnerGap, v => SettingsManager.Current.InnerGap = v, SettingsManager.Current.InnerGap); txtInnerGap.Parent?.SelectNextControl(txtInnerGap, true, true, true, true); } };
            grpDim.Controls.Add(lblInnerGap); grpDim.Controls.Add(tbInnerGap); grpDim.Controls.Add(txtInnerGap);

            // Arm Length Slider
            dimY += 50;
            lblArmLength = new Label { Text = "臂长 (Length):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbArmLength = new TrackBar { Minimum = 1, Maximum = 100, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtArmLength = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbArmLength.Scroll += (s, e) => { SettingsManager.Current.ArmLength = tbArmLength.Value; txtArmLength.Text = tbArmLength.Value.ToString(); ApplyChanges(); };
            txtArmLength.LostFocus += (s, e) => { UpdateFromTextBox(txtArmLength, tbArmLength, v => SettingsManager.Current.ArmLength = v, SettingsManager.Current.ArmLength); };
            txtArmLength.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtArmLength, tbArmLength, v => SettingsManager.Current.ArmLength = v, SettingsManager.Current.ArmLength); txtArmLength.Parent?.SelectNextControl(txtArmLength, true, true, true, true); } };
            grpDim.Controls.Add(lblArmLength); grpDim.Controls.Add(tbArmLength); grpDim.Controls.Add(txtArmLength);

            // Rotation Slider
            dimY += 50;
            lblRotation = new Label { Text = "旋转 (Rotation):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbRotation = new TrackBar { Minimum = 0, Maximum = 360, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtRotation = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbRotation.Scroll += (s, e) => { SettingsManager.Current.RotationAngle = tbRotation.Value; txtRotation.Text = tbRotation.Value.ToString(); ApplyChanges(); };
            txtRotation.LostFocus += (s, e) => { UpdateFromTextBox(txtRotation, tbRotation, v => SettingsManager.Current.RotationAngle = v, (int)SettingsManager.Current.RotationAngle); };
            txtRotation.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtRotation, tbRotation, v => SettingsManager.Current.RotationAngle = v, (int)SettingsManager.Current.RotationAngle); txtRotation.Parent?.SelectNextControl(txtRotation, true, true, true, true); } };
            grpDim.Controls.Add(lblRotation); grpDim.Controls.Add(tbRotation); grpDim.Controls.Add(txtRotation);

            // Square Width Slider
            dimY += 50;
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
            dimY += 50;
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
            dimY += 50;
            chkSquareFill = new CheckBox { Text = "方形填充", Checked = false, Location = new Point(15, dimY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            chkSquareFill.CheckedChanged += (s, e) => {
                if (_isLoadingUI) return;
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

            // Square Corner Length X Slider
            dimY += 45;
            lblSquareCornerX = new Label { Text = "角线长X (水平):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbSquareCornerX = new TrackBar { Minimum = 0, Maximum = 50, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtSquareCornerX = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbSquareCornerX.Scroll += (s, e) => {
                if (SettingsManager.Current.Style == "Square")
                {
                    SettingsManager.Current.SquareCornerLengthX = tbSquareCornerX.Value;
                    txtSquareCornerX.Text = tbSquareCornerX.Value.ToString();
                    ApplyChanges();
                }
                else
                {
                    tbSquareCornerX.Value = SettingsManager.Current.SquareCornerLengthX;
                }
            };
            txtSquareCornerX.LostFocus += (s, e) => {
                if (SettingsManager.Current.Style == "Square") {
                    if (int.TryParse(txtSquareCornerX.Text, out int val))
                    {
                        val = Constrain(val, tbSquareCornerX.Minimum, tbSquareCornerX.Maximum);
                        tbSquareCornerX.Value = val;
                        txtSquareCornerX.Text = val.ToString();
                        SettingsManager.Current.SquareCornerLengthX = val;
                        ApplyChanges();
                    }
                    else
                    {
                        txtSquareCornerX.Text = SettingsManager.Current.SquareCornerLengthX.ToString();
                    }
                }
            };
            txtSquareCornerX.KeyPress += (s, e) => {
                if (e.KeyChar == (char)Keys.Enter && SettingsManager.Current.Style == "Square") {
                    if (int.TryParse(txtSquareCornerX.Text, out int val))
                    {
                        val = Constrain(val, tbSquareCornerX.Minimum, tbSquareCornerX.Maximum);
                        tbSquareCornerX.Value = val;
                        txtSquareCornerX.Text = val.ToString();
                        SettingsManager.Current.SquareCornerLengthX = val;
                        ApplyChanges();
                    }
                    else
                    {
                        txtSquareCornerX.Text = SettingsManager.Current.SquareCornerLengthX.ToString();
                    }
                    txtSquareCornerX.Parent?.SelectNextControl(txtSquareCornerX, true, true, true, true);
                }
            };
            grpDim.Controls.Add(lblSquareCornerX); grpDim.Controls.Add(tbSquareCornerX); grpDim.Controls.Add(txtSquareCornerX);

            // Square Corner Length Y Slider
            dimY += 45;
            lblSquareCornerY = new Label { Text = "角线长Y (垂直):", Location = new Point(15, dimY), Size = new Size(110, 20) };
            tbSquareCornerY = new TrackBar { Minimum = 0, Maximum = 50, Location = new Point(130, dimY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtSquareCornerY = new TextBox { Location = new Point(valX, dimY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbSquareCornerY.Scroll += (s, e) => {
                if (SettingsManager.Current.Style == "Square")
                {
                    SettingsManager.Current.SquareCornerLengthY = tbSquareCornerY.Value;
                    txtSquareCornerY.Text = tbSquareCornerY.Value.ToString();
                    ApplyChanges();
                }
                else
                {
                    tbSquareCornerY.Value = SettingsManager.Current.SquareCornerLengthY;
                }
            };
            txtSquareCornerY.LostFocus += (s, e) => {
                if (SettingsManager.Current.Style == "Square") {
                    if (int.TryParse(txtSquareCornerY.Text, out int val))
                    {
                        val = Constrain(val, tbSquareCornerY.Minimum, tbSquareCornerY.Maximum);
                        tbSquareCornerY.Value = val;
                        txtSquareCornerY.Text = val.ToString();
                        SettingsManager.Current.SquareCornerLengthY = val;
                        ApplyChanges();
                    }
                    else
                    {
                        txtSquareCornerY.Text = SettingsManager.Current.SquareCornerLengthY.ToString();
                    }
                }
            };
            txtSquareCornerY.KeyPress += (s, e) => {
                if (e.KeyChar == (char)Keys.Enter && SettingsManager.Current.Style == "Square") {
                    if (int.TryParse(txtSquareCornerY.Text, out int val))
                    {
                        val = Constrain(val, tbSquareCornerY.Minimum, tbSquareCornerY.Maximum);
                        tbSquareCornerY.Value = val;
                        txtSquareCornerY.Text = val.ToString();
                        SettingsManager.Current.SquareCornerLengthY = val;
                        ApplyChanges();
                    }
                    else
                    {
                        txtSquareCornerY.Text = SettingsManager.Current.SquareCornerLengthY.ToString();
                    }
                    txtSquareCornerY.Parent?.SelectNextControl(txtSquareCornerY, true, true, true, true);
                }
            };
            grpDim.Controls.Add(lblSquareCornerY); grpDim.Controls.Add(tbSquareCornerY); grpDim.Controls.Add(txtSquareCornerY);

            // Group Box for Center Dot & Outline
            startY += 610;
            var grpEffects = new GroupBox { Text = "描边与中心点", Location = new Point(labelX, startY), Size = new Size(width + 120, 320), ForeColor = Color.FromArgb(0, 180, 255) };
            scrollPanel.Controls.Add(grpEffects);

            int effY = 28;

            // Center Dot Checkbox & Size Slider
            chkCenterDot = new CheckBox { Text = "显示中心点", Checked = true, Location = new Point(15, effY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            chkCenterDot.CheckedChanged += (s, e) => {
                if (_isLoadingUI) return;
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

            // Center Dot Shape ComboBox
            effY += 50;
            var lblCenterDotShape = new Label { Text = "中心点形状:", Location = new Point(15, effY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            cbCenterDotShape = new ComboBox { Location = new Point(130, effY - 3), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cbCenterDotShape.Items.AddRange(new object[] { "圆形 (Circle)", "方形 (Square)" });
            cbCenterDotShape.SelectedIndexChanged += (s, e) => {
                if (_isLoadingUI) return;
                string[] shapes = { "Circle", "Square" };
                SettingsManager.Current.CenterDotShape = shapes[cbCenterDotShape.SelectedIndex];
                ApplyChanges();
            };
            grpEffects.Controls.Add(lblCenterDotShape); grpEffects.Controls.Add(cbCenterDotShape);

            // Center Dot Outline Checkbox, Thickness & Color
            effY += 45;
            chkCenterDotOutline = new CheckBox { Text = "中心点描边", Checked = false, Location = new Point(15, effY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            chkCenterDotOutline.CheckedChanged += (s, e) => {
                if (_isLoadingUI) return;
                SettingsManager.Current.CenterDotEnableOutline = chkCenterDotOutline.Checked;
                UpdateControlVisibility();
                ApplyChanges();
            };
            tbCenterDotOutlineThickness = new TrackBar { Minimum = 1, Maximum = 10, Location = new Point(130, effY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtCenterDotOutlineThickness = new TextBox { Location = new Point(valX, effY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            lblCenterDotOutlineThickness = new Label { Text = "描边粗细:", Location = new Point(130, effY + 25), Size = new Size(100, 15), Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(200, 200, 205) };
            tbCenterDotOutlineThickness.Scroll += (s, e) => { SettingsManager.Current.CenterDotOutlineThickness = tbCenterDotOutlineThickness.Value; txtCenterDotOutlineThickness.Text = tbCenterDotOutlineThickness.Value.ToString(); ApplyChanges(); };
            txtCenterDotOutlineThickness.LostFocus += (s, e) => { UpdateFromTextBox(txtCenterDotOutlineThickness, tbCenterDotOutlineThickness, v => SettingsManager.Current.CenterDotOutlineThickness = v, SettingsManager.Current.CenterDotOutlineThickness); };
            txtCenterDotOutlineThickness.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtCenterDotOutlineThickness, tbCenterDotOutlineThickness, v => SettingsManager.Current.CenterDotOutlineThickness = v, SettingsManager.Current.CenterDotOutlineThickness); txtCenterDotOutlineThickness.Parent?.SelectNextControl(txtCenterDotOutlineThickness, true, true, true, true); } };
            grpEffects.Controls.Add(chkCenterDotOutline); grpEffects.Controls.Add(tbCenterDotOutlineThickness); grpEffects.Controls.Add(txtCenterDotOutlineThickness); grpEffects.Controls.Add(lblCenterDotOutlineThickness);

            effY += 50;
            lblCenterDotOutlineColor = new Label { Text = "描边颜色:", Location = new Point(15, effY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            pnlCenterDotOutlineColorPreview = new Panel { Location = new Point(135, effY - 3), Size = new Size(30, 20), BorderStyle = BorderStyle.FixedSingle };
            btnChooseCenterDotOutlineColor = new Button { Text = "颜色...", Location = new Point(175, effY - 5), Size = new Size(70, 24), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55), ForeColor = Color.White, Font = new Font("Segoe UI", 8.5F) };
            btnChooseCenterDotOutlineColor.FlatAppearance.BorderSize = 0;
            btnChooseCenterDotOutlineColor.Click += ChooseCenterDotOutlineColor_Click;
            grpEffects.Controls.Add(lblCenterDotOutlineColor); grpEffects.Controls.Add(pnlCenterDotOutlineColorPreview); grpEffects.Controls.Add(btnChooseCenterDotOutlineColor);

            // Outline Checkbox, Thickness Slider & Color
            effY += 50;
            chkOutline = new CheckBox { Text = "启用描边", Checked = true, Location = new Point(15, effY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            chkOutline.CheckedChanged += (s, e) => {
                if (_isLoadingUI) return;
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

            effY += 45;
            lblOutlineColor = new Label { Text = "描边颜色:", Location = new Point(15, effY), Size = new Size(110, 20), ForeColor = Color.FromArgb(230, 230, 235) };
            pnlOutlineColorPreview = new Panel { Location = new Point(135, effY - 3), Size = new Size(30, 20), BorderStyle = BorderStyle.FixedSingle };
            btnChooseOutlineColor = new Button { Text = "颜色...", Location = new Point(175, effY - 5), Size = new Size(70, 24), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55), ForeColor = Color.White, Font = new Font("Segoe UI", 8.5F) };
            btnChooseOutlineColor.FlatAppearance.BorderSize = 0;
            btnChooseOutlineColor.Click += ChooseOutlineColor_Click;
            grpEffects.Controls.Add(lblOutlineColor); grpEffects.Controls.Add(pnlOutlineColorPreview); grpEffects.Controls.Add(btnChooseOutlineColor);

            // Group Box for Position Offset
            startY += 340;
            var grpOffset = new GroupBox { Text = "位置偏移", Location = new Point(labelX, startY), Size = new Size(width + 120, 115), ForeColor = Color.FromArgb(0, 180, 255) };
            scrollPanel.Controls.Add(grpOffset);

            int offsetY = 25;

            lblOffsetX = new Label { Text = "水平偏移 (X):", Location = new Point(15, offsetY), Size = new Size(110, 20) };
            tbOffsetX = new TrackBar { Minimum = -200, Maximum = 200, Location = new Point(130, offsetY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtOffsetX = new TextBox { Location = new Point(valX, offsetY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbOffsetX.Scroll += (s, e) => { SettingsManager.Current.OffsetX = tbOffsetX.Value; txtOffsetX.Text = tbOffsetX.Value.ToString(); ApplyChanges(); };
            txtOffsetX.LostFocus += (s, e) => { UpdateFromTextBox(txtOffsetX, tbOffsetX, v => SettingsManager.Current.OffsetX = v, SettingsManager.Current.OffsetX); };
            txtOffsetX.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtOffsetX, tbOffsetX, v => SettingsManager.Current.OffsetX = v, SettingsManager.Current.OffsetX); txtOffsetX.Parent?.SelectNextControl(txtOffsetX, true, true, true, true); } };
            grpOffset.Controls.Add(lblOffsetX); grpOffset.Controls.Add(tbOffsetX); grpOffset.Controls.Add(txtOffsetX);

            offsetY += 45;
            lblOffsetY = new Label { Text = "垂直偏移 (Y):", Location = new Point(15, offsetY), Size = new Size(110, 20) };
            tbOffsetY = new TrackBar { Minimum = -200, Maximum = 200, Location = new Point(130, offsetY - 5), Size = new Size(trackWidth, 30), TickStyle = TickStyle.None };
            txtOffsetY = new TextBox { Location = new Point(valX, offsetY - 2), Size = new Size(50, 22), TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tbOffsetY.Scroll += (s, e) => { SettingsManager.Current.OffsetY = tbOffsetY.Value; txtOffsetY.Text = tbOffsetY.Value.ToString(); ApplyChanges(); };
            txtOffsetY.LostFocus += (s, e) => { UpdateFromTextBox(txtOffsetY, tbOffsetY, v => SettingsManager.Current.OffsetY = v, SettingsManager.Current.OffsetY); };
            txtOffsetY.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { UpdateFromTextBox(txtOffsetY, tbOffsetY, v => SettingsManager.Current.OffsetY = v, SettingsManager.Current.OffsetY); txtOffsetY.Parent?.SelectNextControl(txtOffsetY, true, true, true, true); } };
            grpOffset.Controls.Add(lblOffsetY); grpOffset.Controls.Add(tbOffsetY); grpOffset.Controls.Add(txtOffsetY);

            // Group Box for Global Settings (shared across all profiles)
            startY += 130;
            var grpGlobal = new GroupBox { Text = "全局设置 (Global)", Location = new Point(labelX, startY), Size = new Size(width + 120, 140), ForeColor = Color.FromArgb(255, 200, 50) };
            scrollPanel.Controls.Add(grpGlobal);

            int globalY = 25;

            chkAntiAliasing = new CheckBox { Text = "抗锯齿 (圆形/斜线)", Checked = true, Location = new Point(15, globalY), Size = new Size(250, 22), ForeColor = Color.FromArgb(200, 200, 200) };
            chkAntiAliasing.CheckedChanged += (s, e) => {
                if (_isLoadingUI) return;
                SettingsManager.Global.AntiAliasing = chkAntiAliasing.Checked;
                ApplyChanges();
            };
            grpGlobal.Controls.Add(chkAntiAliasing);

            globalY += 28;
            chkAutoStart = new CheckBox { Text = "开机自启动 (Auto-start)", Checked = false, Location = new Point(15, globalY), Size = new Size(250, 22), ForeColor = Color.FromArgb(200, 200, 200) };
            chkAutoStart.CheckedChanged += (s, e) => {
                if (_isLoadingUI) return;
                SettingsManager.Global.AutoStart = chkAutoStart.Checked;
                ApplyChanges();
            };
            grpGlobal.Controls.Add(chkAutoStart);

            globalY += 28;
            var lblToggleHotkey = new Label { Text = "切换快捷键:", Location = new Point(15, globalY), Size = new Size(95, 22), ForeColor = Color.FromArgb(180, 180, 185) };
            txtToggleHotkey = new TextBox { Text = SettingsManager.Global.ToggleHotkey, Location = new Point(110, globalY), Size = new Size(100, 22), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center };
            txtToggleHotkey.LostFocus += (s, e) => {
                SettingsManager.Global.ToggleHotkey = txtToggleHotkey.Text;
                ApplyChanges();
            };
            txtToggleHotkey.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { SettingsManager.Global.ToggleHotkey = txtToggleHotkey.Text; ApplyChanges(); txtToggleHotkey.Parent?.SelectNextControl(txtToggleHotkey, true, true, true, true); } };
            grpGlobal.Controls.Add(lblToggleHotkey);
            grpGlobal.Controls.Add(txtToggleHotkey);

            globalY += 28;
            var lblCycleHotkey = new Label { Text = "轮换配置键:", Location = new Point(15, globalY), Size = new Size(95, 22), ForeColor = Color.FromArgb(180, 180, 185) };
            txtCycleHotkey = new TextBox { Text = SettingsManager.Global.CycleProfileHotkey, Location = new Point(110, globalY), Size = new Size(100, 22), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center };
            txtCycleHotkey.LostFocus += (s, e) => {
                SettingsManager.Global.CycleProfileHotkey = txtCycleHotkey.Text;
                ApplyChanges();
            };
            txtCycleHotkey.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { SettingsManager.Global.CycleProfileHotkey = txtCycleHotkey.Text; ApplyChanges(); txtCycleHotkey.Parent?.SelectNextControl(txtCycleHotkey, true, true, true, true); } };
            grpGlobal.Controls.Add(lblCycleHotkey);
            grpGlobal.Controls.Add(txtCycleHotkey);

            startY += 155;

            btnClose = new Button { Text = "关闭 (后台运行)", Location = new Point(labelX + 150, startY), Size = new Size(100, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => {
                SettingsManager.Save();
                this.Hide();
            };
            scrollPanel.Controls.Add(btnClose);
            
            // Set scroll panel minimum size to fit all controls
            scrollPanel.AutoScrollMinSize = new Size(0, startY + 80);

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

        private void ChooseCenterDotOutlineColor_Click(object? sender, EventArgs e)
        {
            using (var cd = new ColorDialog())
            {
                cd.Color = ColorTranslator.FromHtml(SettingsManager.Current.CenterDotOutlineColorHex ?? "#000000");
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    SettingsManager.Current.CenterDotOutlineColorHex = ColorTranslator.ToHtml(cd.Color);
                    pnlCenterDotOutlineColorPreview.BackColor = cd.Color;
                    ApplyChanges();
                }
            }
        }

        private bool _isLoadingUI = false;

        private void LoadSettingsIntoUI()
        {
            _isLoadingUI = true;
            try
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

                // Square Corner Lengths
                tbSquareCornerX.Value = Constrain(settings.SquareCornerLengthX, tbSquareCornerX.Minimum, tbSquareCornerX.Maximum);
                txtSquareCornerX.Text = tbSquareCornerX.Value.ToString();

                tbSquareCornerY.Value = Constrain(settings.SquareCornerLengthY, tbSquareCornerY.Minimum, tbSquareCornerY.Maximum);
                txtSquareCornerY.Text = tbSquareCornerY.Value.ToString();

                // Center Dot
                chkCenterDot.Checked = settings.ShowCenterDot;
                tbCenterDotSize.Value = Constrain(settings.CenterDotSize, tbCenterDotSize.Minimum, tbCenterDotSize.Maximum);
                txtCenterDotSize.Text = tbCenterDotSize.Value.ToString();

                // Center Dot Shape
                string[] shapes = { "Circle", "Square" };
                int shapeIdx = Array.IndexOf(shapes, settings.CenterDotShape);
                cbCenterDotShape.SelectedIndex = shapeIdx >= 0 ? shapeIdx : 0;

                // Center Dot Outline
                chkCenterDotOutline.Checked = settings.CenterDotEnableOutline;
                tbCenterDotOutlineThickness.Value = Constrain(settings.CenterDotOutlineThickness, tbCenterDotOutlineThickness.Minimum, tbCenterDotOutlineThickness.Maximum);
                txtCenterDotOutlineThickness.Text = tbCenterDotOutlineThickness.Value.ToString();
                pnlCenterDotOutlineColorPreview.BackColor = ColorTranslator.FromHtml(settings.CenterDotOutlineColorHex ?? "#000000");

                // Outline
                chkOutline.Checked = settings.EnableOutline;
                tbOutlineThickness.Value = Constrain(settings.OutlineThickness, tbOutlineThickness.Minimum, tbOutlineThickness.Maximum);
                txtOutlineThickness.Text = tbOutlineThickness.Value.ToString();
                pnlOutlineColorPreview.BackColor = ColorTranslator.FromHtml(settings.OutlineColorHex ?? "#000000");

                // Position Offset
                tbOffsetX.Value = Constrain(settings.OffsetX, tbOffsetX.Minimum, tbOffsetX.Maximum);
                txtOffsetX.Text = tbOffsetX.Value.ToString();

                tbOffsetY.Value = Constrain(settings.OffsetY, tbOffsetY.Minimum, tbOffsetY.Maximum);
                txtOffsetY.Text = tbOffsetY.Value.ToString();

                // Anti-Aliasing
                chkAntiAliasing.Checked = SettingsManager.Global.AntiAliasing;

                // Auto start
                chkAutoStart.Checked = SettingsManager.Global.AutoStart;

                // Toggle hotkey
                txtToggleHotkey.Text = SettingsManager.Global.ToggleHotkey ?? "Ctrl+Q";
                txtCycleHotkey.Text = SettingsManager.Global.CycleProfileHotkey ?? "Ctrl+`";
            }
            finally
            {
                _isLoadingUI = false;
            }
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

            // Square Corner Length controls
            tbSquareCornerX.Enabled = true;
            lblSquareCornerX.ForeColor = isSquare ? activeColor : inactiveColor;
            txtSquareCornerX.ForeColor = isSquare ? activeValColor : inactiveValColor;

            tbSquareCornerY.Enabled = true;
            lblSquareCornerY.ForeColor = isSquare ? activeColor : inactiveColor;
            txtSquareCornerY.ForeColor = isSquare ? activeValColor : inactiveValColor;

            // Center Dot
            chkCenterDot.Enabled = true;
            bool centerDotSizeEnabled = chkCenterDot.Checked;
            tbCenterDotSize.Enabled = centerDotSizeEnabled;
            lblCenterDotSize.ForeColor = centerDotSizeEnabled ? Color.FromArgb(150, 150, 155) : inactiveColor;
            txtCenterDotSize.ForeColor = centerDotSizeEnabled ? activeValColor : inactiveValColor;
            
            // Center Dot Shape
            cbCenterDotShape.Enabled = centerDotSizeEnabled;
            cbCenterDotShape.ForeColor = centerDotSizeEnabled ? activeColor : inactiveColor;
            
            // Center Dot Outline
            chkCenterDotOutline.Enabled = centerDotSizeEnabled;
            chkCenterDotOutline.ForeColor = centerDotSizeEnabled ? Color.FromArgb(230, 230, 235) : inactiveColor;
            bool centerDotOutlineEnabled = centerDotSizeEnabled && chkCenterDotOutline.Checked;
            tbCenterDotOutlineThickness.Enabled = centerDotOutlineEnabled;
            lblCenterDotOutlineThickness.ForeColor = centerDotOutlineEnabled ? Color.FromArgb(150, 150, 155) : inactiveColor;
            txtCenterDotOutlineThickness.ForeColor = centerDotOutlineEnabled ? activeValColor : inactiveValColor;
            btnChooseCenterDotOutlineColor.Enabled = centerDotOutlineEnabled;
            lblCenterDotOutlineColor.ForeColor = centerDotOutlineEnabled ? activeColor : inactiveColor;

            // Outline
            bool outlineEnabled = chkOutline.Checked;
            tbOutlineThickness.Enabled = outlineEnabled;
            lblOutlineThickness.ForeColor = outlineEnabled ? Color.FromArgb(150, 150, 155) : inactiveColor;
            txtOutlineThickness.ForeColor = outlineEnabled ? activeValColor : inactiveValColor;
            btnChooseOutlineColor.Enabled = outlineEnabled;
            lblOutlineColor.ForeColor = outlineEnabled ? activeColor : inactiveColor;
        }

        // ── Quick Color Button Helpers ──

        private void RefreshQuickColorButtons(Panel scrollPanel, int startY)
        {
            // Remove old buttons
            foreach (var btn in _quickColorButtons)
                scrollPanel.Controls.Remove(btn);
            _quickColorButtons.Clear();

            int qx = 140; // Start aligned with other controls on their own row
            var quickColors = SettingsManager.Global.QuickColors;

            foreach (string hex in quickColors)
            {
                Color color;
                try { color = ColorTranslator.FromHtml(hex); }
                catch { continue; }

                var btn = new Button
                {
                    Size = new Size(20, 20),
                    Location = new Point(qx, startY + 2),
                    BackColor = color,
                    FlatStyle = FlatStyle.Flat
                };
                btn.FlatAppearance.BorderSize = 0;

                // Store hex in Tag for accessing in events
                btn.Tag = hex;

                // Left-click: apply this color
                btn.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is string h)
                    {
                        SettingsManager.Current.ColorHex = h;
                        pnlColorPreview.BackColor = b.BackColor;
                        ApplyChanges();
                    }
                };

                // Right-click: context menu to edit or delete
                btn.MouseUp += (s, e) =>
                {
                    if (e.Button != MouseButtons.Right) return;
                    if (s is Button b && b.Tag is string h)
                    {
                        var menu = new ContextMenuStrip();
                        var editItem = new ToolStripMenuItem("修改颜色...");
                        editItem.Click += (sender, args) =>
                        {
                            using (var cd = new ColorDialog { Color = b.BackColor })
                            {
                                if (cd.ShowDialog() == DialogResult.OK)
                                {
                                    string newHex = ColorTranslator.ToHtml(cd.Color);
                                    int idx = SettingsManager.Global.QuickColors.IndexOf(h);
                                    if (idx >= 0)
                                        SettingsManager.Global.QuickColors[idx] = newHex;
                                    SettingsManager.Save();
                                    RefreshQuickColorButtons(scrollPanel, startY);
                                }
                            }
                        };
                        var delItem = new ToolStripMenuItem("删除");
                        delItem.Click += (sender, args) =>
                        {
                            SettingsManager.Global.QuickColors.Remove(h);
                            SettingsManager.Save();
                            RefreshQuickColorButtons(scrollPanel, startY);
                        };
                        menu.Items.Add(editItem);
                        menu.Items.Add(delItem);
                        menu.Show(b, e.Location);
                    }
                };

                scrollPanel.Controls.Add(btn);
                _quickColorButtons.Add(btn);
                qx += 25;
            }

            // "+" add button
            var btnAdd = new Button
            {
                Text = "+",
                Size = new Size(20, 20),
                Location = new Point(qx, startY + 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.FromArgb(180, 180, 185),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) =>
            {
                using (var cd = new ColorDialog { Color = Color.Lime })
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        SettingsManager.Global.QuickColors.Add(ColorTranslator.ToHtml(cd.Color));
                        SettingsManager.Save();
                        RefreshQuickColorButtons(scrollPanel, startY);
                    }
                }
            };
            scrollPanel.Controls.Add(btnAdd);
            _quickColorButtons.Add(btnAdd); // Track it for cleanup, though it's also removed on refresh
        }

        private void ApplyChanges()
        {
            _crosshairForm.UpdatePositionAndSize();

            // Debounce disk writes — scroll events fire dozens of times per second.
            // Writing JSON synchronously on every tick blocks the UI thread,
            // causing the settings page to feel like it's constantly refreshing.
            _saveDebounceTimer.Stop();
            _saveDebounceTimer.Start();
        }

        /// <summary>
        /// Flushes any pending debounced save immediately (call before form close).
        /// </summary>
        public void FlushPendingSave()
        {
            _saveDebounceTimer.Stop();
            SettingsManager.Save();
        }

        // ── Profile Management Helpers ──

        private Button CreateSmallButton(string text, int x, int y, int btnWidth = 50)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(btnWidth, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F)
            };
        }

        private void RefreshProfileList()
        {
            cbProfile.SelectedIndexChanged -= CbProfile_SelectedIndexChanged;
            cbProfile.Items.Clear();
            cbProfile.Items.AddRange(SettingsManager.GetProfileNames().ToArray());
            cbProfile.SelectedItem = SettingsManager.ActiveProfileName;
            cbProfile.SelectedIndexChanged += CbProfile_SelectedIndexChanged;
        }

        private void CbProfile_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? selected = cbProfile.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected) || selected == SettingsManager.ActiveProfileName)
                return;

            SettingsManager.SwitchToProfile(selected);
            LoadSettingsIntoUI();
            UpdateControlVisibility();
            _crosshairForm.UpdatePositionAndSize();
            NotificationForm.ShowToast(selected);
        }

        private void BtnNewProfile_Click(object? sender, EventArgs e)
        {
            string? name = ShowInputDialog("新建配置", "请输入新配置名称:");
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                SettingsManager.CreateProfile(name.Trim());
                RefreshProfileList();
                LoadSettingsIntoUI();
                UpdateControlVisibility();
                _crosshairForm.UpdatePositionAndSize();
                _onProfilesChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDuplicateProfile_Click(object? sender, EventArgs e)
        {
            string? name = ShowInputDialog("复制配置", "请输入新配置名称:");
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                SettingsManager.DuplicateProfile(SettingsManager.ActiveProfileName, name.Trim());
                RefreshProfileList();
                LoadSettingsIntoUI();
                UpdateControlVisibility();
                _crosshairForm.UpdatePositionAndSize();
                _onProfilesChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnRenameProfile_Click(object? sender, EventArgs e)
        {
            string currentName = SettingsManager.ActiveProfileName;
            string? newName = ShowInputDialog("重命名配置", $"请输入新名称 (当前: {currentName}):");
            if (string.IsNullOrWhiteSpace(newName)) return;
            try
            {
                SettingsManager.RenameProfile(currentName, newName.Trim());
                RefreshProfileList();
                _onProfilesChanged?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDeleteProfile_Click(object? sender, EventArgs e)
        {
            if (SettingsManager.GetProfileNames().Count <= 1)
            {
                MessageBox.Show("无法删除最后一个配置。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string currentName = SettingsManager.ActiveProfileName;
            var result = MessageBox.Show(
                $"确定要删除配置 \"{currentName}\" 吗?\n此操作不可撤销。",
                "确认删除",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.OK)
            {
                try
                {
                    SettingsManager.DeleteProfile(currentName);
                    RefreshProfileList();
                    LoadSettingsIntoUI();
                    UpdateControlVisibility();
                    _crosshairForm.UpdatePositionAndSize();
                    _onProfilesChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private static string? ShowInputDialog(string title, string prompt)
        {
            // Simple input dialog using a small form
            using (var form = new Form { Text = title, Width = 350, Height = 150, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.FromArgb(30, 30, 32), ForeColor = Color.White })
            {
                var lbl = new Label { Text = prompt, Location = new Point(15, 15), Size = new Size(300, 20), ForeColor = Color.FromArgb(200, 200, 205) };
                var txt = new TextBox { Location = new Point(15, 40), Size = new Size(300, 25), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

                var btnOk = new Button { Text = "确定", Location = new Point(130, 75), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, DialogResult = DialogResult.OK };
                btnOk.FlatAppearance.BorderSize = 0;
                var btnCancel = new Button { Text = "取消", Location = new Point(220, 75), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55), ForeColor = Color.White, DialogResult = DialogResult.Cancel };
                btnCancel.FlatAppearance.BorderSize = 0;

                form.Controls.Add(lbl);
                form.Controls.Add(txt);
                form.Controls.Add(btnOk);
                form.Controls.Add(btnCancel);
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                return form.ShowDialog() == DialogResult.OK ? txt.Text : null;
            }
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
