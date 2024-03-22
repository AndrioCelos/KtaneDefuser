namespace ImageProcessingTester;

partial class TransformationForm {
	/// <summary>
	///  Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	///  Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing) {
		if (disposing && (components != null)) {
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Windows Form Designer generated code

	/// <summary>
	///  Required method for Designer support - do not modify
	///  the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
		screenshotPictureBox = new PictureBox();
		screenshotLoadButton = new Button();
		openFileDialog = new OpenFileDialog();
		OutputPictureBox = new PictureBox();
		splitContainer = new SplitContainer();
		tableLayoutPanel1 = new TableLayoutPanel();
		panel1 = new Panel();
		flowLayoutPanel2 = new FlowLayoutPanel();
		screenshotPasteButton = new Button();
		screenshotFromGameButton = new Button();
		screenshotSaveButton = new Button();
		label2 = new Label();
		presetBox = new ComboBox();
		label1 = new Label();
		interpolationBox = new ComboBox();
		tableLayoutPanel2 = new TableLayoutPanel();
		flowLayoutPanel1 = new FlowLayoutPanel();
		outputCopyButton = new Button();
		outputSaveButton = new Button();
		outputClassifyButton = new Button();
		autoClassifyBox = new CheckBox();
		label3 = new Label();
		lightsSimulateBox = new ComboBox();
		saveFileDialog1 = new SaveFileDialog();
		lightsLabel = new Label();
		((System.ComponentModel.ISupportInitialize) screenshotPictureBox).BeginInit();
		((System.ComponentModel.ISupportInitialize) OutputPictureBox).BeginInit();
		((System.ComponentModel.ISupportInitialize) splitContainer).BeginInit();
		splitContainer.Panel1.SuspendLayout();
		splitContainer.Panel2.SuspendLayout();
		splitContainer.SuspendLayout();
		tableLayoutPanel1.SuspendLayout();
		panel1.SuspendLayout();
		flowLayoutPanel2.SuspendLayout();
		tableLayoutPanel2.SuspendLayout();
		flowLayoutPanel1.SuspendLayout();
		this.SuspendLayout();
		// 
		// screenshotPictureBox
		// 
		screenshotPictureBox.Location = new Point(21, 13);
		screenshotPictureBox.Name = "screenshotPictureBox";
		screenshotPictureBox.Size = new Size(400, 225);
		screenshotPictureBox.TabIndex = 0;
		screenshotPictureBox.TabStop = false;
		screenshotPictureBox.Paint += this.ScreenPictureBox_Paint;
		screenshotPictureBox.MouseDown += this.ScreenPictureBox_MouseDown;
		screenshotPictureBox.MouseMove += this.ScreenPictureBox_MouseMove;
		screenshotPictureBox.MouseUp += this.ScreenPictureBox_MouseUp;
		// 
		// screenshotLoadButton
		// 
		screenshotLoadButton.ForeColor = SystemColors.ControlText;
		screenshotLoadButton.Location = new Point(3, 3);
		screenshotLoadButton.Name = "screenshotLoadButton";
		screenshotLoadButton.Size = new Size(75, 23);
		screenshotLoadButton.TabIndex = 1;
		screenshotLoadButton.Text = "Load...";
		screenshotLoadButton.UseVisualStyleBackColor = true;
		screenshotLoadButton.Click += this.ScreenshotLoadButton_Click;
		// 
		// openFileDialog
		// 
		openFileDialog.Filter = "Images|*.bmp;*.jpg;*.jpeg;*.png";
		// 
		// pictureBox1
		// 
		OutputPictureBox.Dock = DockStyle.Fill;
		OutputPictureBox.Location = new Point(3, 38);
		OutputPictureBox.Name = "pictureBox1";
		OutputPictureBox.Size = new Size(477, 424);
		OutputPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
		OutputPictureBox.TabIndex = 2;
		OutputPictureBox.TabStop = false;
		// 
		// splitContainer
		// 
		splitContainer.BackColor = Color.Black;
		splitContainer.Dock = DockStyle.Fill;
		splitContainer.Location = new Point(0, 0);
		splitContainer.Name = "splitContainer";
		// 
		// splitContainer.Panel1
		// 
		splitContainer.Panel1.Controls.Add(tableLayoutPanel1);
		// 
		// splitContainer.Panel2
		// 
		splitContainer.Panel2.Controls.Add(tableLayoutPanel2);
		splitContainer.Size = new Size(1212, 465);
		splitContainer.SplitterDistance = 725;
		splitContainer.TabIndex = 3;
		// 
		// tableLayoutPanel1
		// 
		tableLayoutPanel1.ColumnCount = 1;
		tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel1.Controls.Add(panel1, 0, 1);
		tableLayoutPanel1.Controls.Add(flowLayoutPanel2, 0, 0);
		tableLayoutPanel1.Dock = DockStyle.Fill;
		tableLayoutPanel1.Location = new Point(0, 0);
		tableLayoutPanel1.Name = "tableLayoutPanel1";
		tableLayoutPanel1.RowCount = 2;
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		tableLayoutPanel1.Size = new Size(725, 465);
		tableLayoutPanel1.TabIndex = 0;
		// 
		// panel1
		// 
		panel1.Controls.Add(screenshotPictureBox);
		panel1.Dock = DockStyle.Fill;
		panel1.Location = new Point(3, 59);
		panel1.Name = "panel1";
		panel1.Size = new Size(719, 403);
		panel1.TabIndex = 2;
		panel1.Resize += this.ScreenshotPanel_Resize;
		// 
		// flowLayoutPanel2
		// 
		flowLayoutPanel2.AutoSize = true;
		flowLayoutPanel2.Controls.Add(screenshotLoadButton);
		flowLayoutPanel2.Controls.Add(screenshotPasteButton);
		flowLayoutPanel2.Controls.Add(screenshotFromGameButton);
		flowLayoutPanel2.Controls.Add(screenshotSaveButton);
		flowLayoutPanel2.Controls.Add(label2);
		flowLayoutPanel2.Controls.Add(presetBox);
		flowLayoutPanel2.Controls.Add(label1);
		flowLayoutPanel2.Controls.Add(interpolationBox);
		flowLayoutPanel2.Controls.Add(lightsLabel);
		flowLayoutPanel2.Location = new Point(3, 3);
		flowLayoutPanel2.Name = "flowLayoutPanel2";
		flowLayoutPanel2.Size = new Size(715, 50);
		flowLayoutPanel2.TabIndex = 3;
		// 
		// screenshotPasteButton
		// 
		screenshotPasteButton.ForeColor = Color.Black;
		screenshotPasteButton.Location = new Point(84, 3);
		screenshotPasteButton.Name = "screenshotPasteButton";
		screenshotPasteButton.Size = new Size(75, 23);
		screenshotPasteButton.TabIndex = 4;
		screenshotPasteButton.Text = "Paste";
		screenshotPasteButton.UseVisualStyleBackColor = true;
		screenshotPasteButton.Click += this.ScreenshotPasteButton_Click;
		// 
		// screenshotFromGameButton
		// 
		screenshotFromGameButton.ForeColor = SystemColors.ControlText;
		screenshotFromGameButton.Location = new Point(165, 3);
		screenshotFromGameButton.Name = "screenshotFromGameButton";
		screenshotFromGameButton.Size = new Size(84, 23);
		screenshotFromGameButton.TabIndex = 6;
		screenshotFromGameButton.Text = "From game";
		screenshotFromGameButton.UseVisualStyleBackColor = true;
		screenshotFromGameButton.Click += this.GetFromGameButton_Click;
		// 
		// screenshotSaveButton
		// 
		screenshotSaveButton.ForeColor = SystemColors.ControlText;
		screenshotSaveButton.Location = new Point(255, 3);
		screenshotSaveButton.Name = "screenshotSaveButton";
		screenshotSaveButton.Size = new Size(75, 23);
		screenshotSaveButton.TabIndex = 7;
		screenshotSaveButton.Text = "Save...";
		screenshotSaveButton.UseVisualStyleBackColor = true;
		screenshotSaveButton.Click += this.ScreenshotSaveButton_Click;
		// 
		// label2
		// 
		label2.AutoSize = true;
		label2.Location = new Point(336, 6);
		label2.Margin = new Padding(3, 6, 3, 0);
		label2.Name = "label2";
		label2.Size = new Size(42, 15);
		label2.TabIndex = 2;
		label2.Text = "Preset:";
		// 
		// presetBox
		// 
		presetBox.DropDownStyle = ComboBoxStyle.DropDownList;
		presetBox.FormattingEnabled = true;
		presetBox.Location = new Point(384, 3);
		presetBox.Name = "presetBox";
		presetBox.Size = new Size(103, 23);
		presetBox.TabIndex = 5;
		presetBox.SelectedIndexChanged += this.PresetBox_SelectedIndexChanged;
		presetBox.Format += this.PresetBox_Format;
		presetBox.KeyDown += this.PresetBox_KeyDown;
		// 
		// label1
		// 
		label1.AutoSize = true;
		label1.Location = new Point(493, 6);
		label1.Margin = new Padding(3, 6, 3, 0);
		label1.Name = "label1";
		label1.Size = new Size(78, 15);
		label1.TabIndex = 2;
		label1.Text = "Interpolation:";
		label1.Visible = false;
		// 
		// interpolationBox
		// 
		interpolationBox.DropDownStyle = ComboBoxStyle.DropDownList;
		interpolationBox.FormattingEnabled = true;
		interpolationBox.Items.AddRange(new object[] { "Nearest neighbour", "Bilinear" });
		interpolationBox.Location = new Point(577, 3);
		interpolationBox.Name = "interpolationBox";
		interpolationBox.Size = new Size(135, 23);
		interpolationBox.TabIndex = 3;
		interpolationBox.Visible = false;
		interpolationBox.SelectedIndexChanged += this.InterpolationBox_SelectedIndexChanged;
		// 
		// tableLayoutPanel2
		// 
		tableLayoutPanel2.ColumnCount = 1;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		tableLayoutPanel2.Controls.Add(OutputPictureBox, 0, 1);
		tableLayoutPanel2.Controls.Add(flowLayoutPanel1, 0, 0);
		tableLayoutPanel2.Dock = DockStyle.Fill;
		tableLayoutPanel2.Location = new Point(0, 0);
		tableLayoutPanel2.Name = "tableLayoutPanel2";
		tableLayoutPanel2.RowCount = 2;
		tableLayoutPanel2.RowStyles.Add(new RowStyle());
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
		tableLayoutPanel2.Size = new Size(483, 465);
		tableLayoutPanel2.TabIndex = 3;
		// 
		// flowLayoutPanel1
		// 
		flowLayoutPanel1.AutoSize = true;
		flowLayoutPanel1.Controls.Add(outputCopyButton);
		flowLayoutPanel1.Controls.Add(outputSaveButton);
		flowLayoutPanel1.Controls.Add(outputClassifyButton);
		flowLayoutPanel1.Controls.Add(autoClassifyBox);
		flowLayoutPanel1.Controls.Add(label3);
		flowLayoutPanel1.Controls.Add(lightsSimulateBox);
		flowLayoutPanel1.Location = new Point(3, 3);
		flowLayoutPanel1.Name = "flowLayoutPanel1";
		flowLayoutPanel1.Size = new Size(477, 29);
		flowLayoutPanel1.TabIndex = 3;
		// 
		// outputCopyButton
		// 
		outputCopyButton.ForeColor = SystemColors.ControlText;
		outputCopyButton.Location = new Point(3, 3);
		outputCopyButton.Name = "outputCopyButton";
		outputCopyButton.Size = new Size(75, 23);
		outputCopyButton.TabIndex = 0;
		outputCopyButton.Text = "Copy";
		outputCopyButton.UseVisualStyleBackColor = true;
		outputCopyButton.Click += this.OutputCopyButton_Click;
		// 
		// outputSaveButton
		// 
		outputSaveButton.ForeColor = SystemColors.ControlText;
		outputSaveButton.Location = new Point(84, 3);
		outputSaveButton.Name = "outputSaveButton";
		outputSaveButton.Size = new Size(75, 23);
		outputSaveButton.TabIndex = 1;
		outputSaveButton.Text = "Save...";
		outputSaveButton.UseVisualStyleBackColor = true;
		outputSaveButton.Click += this.OutputSaveButton_Click;
		// 
		// outputClassifyButton
		// 
		outputClassifyButton.ForeColor = SystemColors.ControlText;
		outputClassifyButton.Location = new Point(165, 3);
		outputClassifyButton.Name = "outputClassifyButton";
		outputClassifyButton.Size = new Size(75, 23);
		outputClassifyButton.TabIndex = 3;
		outputClassifyButton.Text = "Classify";
		outputClassifyButton.UseVisualStyleBackColor = true;
		outputClassifyButton.Click += this.OutputClassifyButton_Click;
		// 
		// autoClassifyBox
		// 
		autoClassifyBox.AutoSize = true;
		autoClassifyBox.Location = new Point(246, 6);
		autoClassifyBox.Margin = new Padding(3, 6, 3, 3);
		autoClassifyBox.Name = "autoClassifyBox";
		autoClassifyBox.Size = new Size(52, 19);
		autoClassifyBox.TabIndex = 2;
		autoClassifyBox.Text = "Auto";
		autoClassifyBox.UseVisualStyleBackColor = true;
		// 
		// label3
		// 
		label3.AutoSize = true;
		label3.Location = new Point(304, 6);
		label3.Margin = new Padding(3, 6, 3, 0);
		label3.Name = "label3";
		label3.Size = new Size(56, 15);
		label3.TabIndex = 2;
		label3.Text = "Simulate:";
		// 
		// lightsSimulateBox
		// 
		lightsSimulateBox.DropDownStyle = ComboBoxStyle.DropDownList;
		lightsSimulateBox.FormattingEnabled = true;
		lightsSimulateBox.Items.AddRange(new object[] { "On", "Buzz", "Off", "Emergency" });
		lightsSimulateBox.Location = new Point(366, 3);
		lightsSimulateBox.Name = "lightsSimulateBox";
		lightsSimulateBox.Size = new Size(108, 23);
		lightsSimulateBox.TabIndex = 8;
		lightsSimulateBox.SelectedIndexChanged += this.LightsSimulateBox_SelectedIndexChanged;
		// 
		// saveFileDialog1
		// 
		saveFileDialog1.Filter = "Bitmap|*.bmp|GIF image|*.gif|JPEG image|*.jpeg;*.jpg|PNG image|*.png|TIFF image|*.tiff|WebP image|*.webp";
		saveFileDialog1.FilterIndex = 4;
		// 
		// lightsLabel
		// 
		lightsLabel.AutoSize = true;
		lightsLabel.Location = new Point(3, 35);
		lightsLabel.Margin = new Padding(3, 6, 3, 0);
		lightsLabel.Name = "lightsLabel";
		lightsLabel.Size = new Size(61, 15);
		lightsLabel.TabIndex = 2;
		lightsLabel.Text = "Lights: On";
		// 
		// TransformationForm
		// 
		this.AutoScaleDimensions = new SizeF(7F, 15F);
		this.AutoScaleMode = AutoScaleMode.Font;
		this.BackColor = Color.Black;
		this.ClientSize = new Size(1212, 465);
		this.Controls.Add(splitContainer);
		this.ForeColor = Color.White;
		this.Name = "TransformationForm";
		this.Text = "Transformation";
		this.Load += this.TransformationForm_Load;
		((System.ComponentModel.ISupportInitialize) screenshotPictureBox).EndInit();
		((System.ComponentModel.ISupportInitialize) OutputPictureBox).EndInit();
		splitContainer.Panel1.ResumeLayout(false);
		splitContainer.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize) splitContainer).EndInit();
		splitContainer.ResumeLayout(false);
		tableLayoutPanel1.ResumeLayout(false);
		tableLayoutPanel1.PerformLayout();
		panel1.ResumeLayout(false);
		flowLayoutPanel2.ResumeLayout(false);
		flowLayoutPanel2.PerformLayout();
		tableLayoutPanel2.ResumeLayout(false);
		tableLayoutPanel2.PerformLayout();
		flowLayoutPanel1.ResumeLayout(false);
		flowLayoutPanel1.PerformLayout();
		this.ResumeLayout(false);
	}

	#endregion

	private PictureBox screenshotPictureBox;
	private Button screenshotLoadButton;
	private OpenFileDialog openFileDialog;
	private PictureBox OutputPictureBox;
	private SplitContainer splitContainer;
	private TableLayoutPanel tableLayoutPanel1;
	private Panel panel1;
	private TableLayoutPanel tableLayoutPanel2;
	private FlowLayoutPanel flowLayoutPanel1;
	private Button outputCopyButton;
	private Button outputSaveButton;
	private SaveFileDialog saveFileDialog1;
	private FlowLayoutPanel flowLayoutPanel2;
	private Label label1;
	private ComboBox interpolationBox;
	private Button screenshotPasteButton;
	private CheckBox autoClassifyBox;
	private Button outputClassifyButton;
	private Label label2;
	private ComboBox presetBox;
	private Button screenshotFromGameButton;
	private Button screenshotSaveButton;
	private Label label3;
	private ComboBox lightsSimulateBox;
	private Label lightsLabel;
}
