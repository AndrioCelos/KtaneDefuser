namespace ImageProcessingTester;

partial class ColourRangeForm {
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	/// Clean up any resources being used.
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
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
		this.flowLayoutPanel1 = new FlowLayoutPanel();
		this.OpenButton = new Button();
		this.PasteButton = new Button();
		this.ModeBox = new ComboBox();
		this.label1 = new Label();
		this.RHMinBox = new NumericUpDown();
		this.RHMaxBox = new NumericUpDown();
		this.label2 = new Label();
		this.GSMinBox = new NumericUpDown();
		this.GSMaxBox = new NumericUpDown();
		this.label3 = new Label();
		this.BVMinBox = new NumericUpDown();
		this.BVMaxBox = new NumericUpDown();
		this.label4 = new Label();
		this.DistanceBox = new NumericUpDown();
		this.StatusLabel = new Label();
		this.PictureBox = new PictureBox();
		this.openFileDialog = new OpenFileDialog();
		this.flowLayoutPanel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize) this.RHMinBox).BeginInit();
		((System.ComponentModel.ISupportInitialize) this.RHMaxBox).BeginInit();
		((System.ComponentModel.ISupportInitialize) this.GSMinBox).BeginInit();
		((System.ComponentModel.ISupportInitialize) this.GSMaxBox).BeginInit();
		((System.ComponentModel.ISupportInitialize) this.BVMinBox).BeginInit();
		((System.ComponentModel.ISupportInitialize) this.BVMaxBox).BeginInit();
		((System.ComponentModel.ISupportInitialize) this.DistanceBox).BeginInit();
		((System.ComponentModel.ISupportInitialize) this.PictureBox).BeginInit();
		this.SuspendLayout();
		// 
		// flowLayoutPanel1
		// 
		this.flowLayoutPanel1.AutoSize = true;
		this.flowLayoutPanel1.Controls.Add(this.OpenButton);
		this.flowLayoutPanel1.Controls.Add(this.PasteButton);
		this.flowLayoutPanel1.Controls.Add(this.ModeBox);
		this.flowLayoutPanel1.Controls.Add(this.label1);
		this.flowLayoutPanel1.Controls.Add(this.RHMinBox);
		this.flowLayoutPanel1.Controls.Add(this.RHMaxBox);
		this.flowLayoutPanel1.Controls.Add(this.label2);
		this.flowLayoutPanel1.Controls.Add(this.GSMinBox);
		this.flowLayoutPanel1.Controls.Add(this.GSMaxBox);
		this.flowLayoutPanel1.Controls.Add(this.label3);
		this.flowLayoutPanel1.Controls.Add(this.BVMinBox);
		this.flowLayoutPanel1.Controls.Add(this.BVMaxBox);
		this.flowLayoutPanel1.Controls.Add(this.label4);
		this.flowLayoutPanel1.Controls.Add(this.DistanceBox);
		this.flowLayoutPanel1.Controls.Add(this.StatusLabel);
		this.flowLayoutPanel1.Dock = DockStyle.Top;
		this.flowLayoutPanel1.Location = new Point(0, 0);
		this.flowLayoutPanel1.Name = "flowLayoutPanel1";
		this.flowLayoutPanel1.Size = new Size(1008, 29);
		this.flowLayoutPanel1.TabIndex = 0;
		// 
		// OpenButton
		// 
		this.OpenButton.ForeColor = Color.Black;
		this.OpenButton.Location = new Point(3, 3);
		this.OpenButton.Name = "OpenButton";
		this.OpenButton.Size = new Size(75, 23);
		this.OpenButton.TabIndex = 0;
		this.OpenButton.Text = "Open...";
		this.OpenButton.UseVisualStyleBackColor = true;
		this.OpenButton.Click += this.OpenButton_Click;
		// 
		// PasteButton
		// 
		this.PasteButton.ForeColor = Color.Black;
		this.PasteButton.Location = new Point(84, 3);
		this.PasteButton.Name = "PasteButton";
		this.PasteButton.Size = new Size(75, 23);
		this.PasteButton.TabIndex = 1;
		this.PasteButton.Text = "Paste";
		this.PasteButton.UseVisualStyleBackColor = true;
		this.PasteButton.Click += this.PasteButton_Click;
		// 
		// ModeBox
		// 
		this.ModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
		this.ModeBox.FormattingEnabled = true;
		this.ModeBox.Items.AddRange(new object[] { "RGB", "HSV", "RGBD" });
		this.ModeBox.Location = new Point(165, 3);
		this.ModeBox.Name = "ModeBox";
		this.ModeBox.Size = new Size(75, 23);
		this.ModeBox.TabIndex = 2;
		this.ModeBox.SelectedIndexChanged += this.ModeBox_SelectedIndexChanged;
		// 
		// label1
		// 
		this.label1.AutoSize = true;
		this.label1.Location = new Point(246, 5);
		this.label1.Margin = new Padding(3, 5, 3, 0);
		this.label1.Name = "label1";
		this.label1.Size = new Size(14, 15);
		this.label1.TabIndex = 3;
		this.label1.Text = "R";
		// 
		// RHMinBox
		// 
		this.RHMinBox.Location = new Point(266, 3);
		this.RHMinBox.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		this.RHMinBox.Name = "RHMinBox";
		this.RHMinBox.Size = new Size(40, 23);
		this.RHMinBox.TabIndex = 4;
		this.RHMinBox.TextAlign = HorizontalAlignment.Right;
		this.RHMinBox.ValueChanged += this.RhMinBox_ValueChanged;
		// 
		// RHMaxBox
		// 
		this.RHMaxBox.Location = new Point(312, 3);
		this.RHMaxBox.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		this.RHMaxBox.Name = "RHMaxBox";
		this.RHMaxBox.Size = new Size(40, 23);
		this.RHMaxBox.TabIndex = 5;
		this.RHMaxBox.TextAlign = HorizontalAlignment.Right;
		this.RHMaxBox.ValueChanged += this.RhMinBox_ValueChanged;
		// 
		// label2
		// 
		this.label2.AutoSize = true;
		this.label2.Location = new Point(358, 5);
		this.label2.Margin = new Padding(3, 5, 3, 0);
		this.label2.Name = "label2";
		this.label2.Size = new Size(15, 15);
		this.label2.TabIndex = 6;
		this.label2.Text = "G";
		// 
		// GSMinBox
		// 
		this.GSMinBox.Location = new Point(379, 3);
		this.GSMinBox.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		this.GSMinBox.Name = "GSMinBox";
		this.GSMinBox.Size = new Size(40, 23);
		this.GSMinBox.TabIndex = 7;
		this.GSMinBox.TextAlign = HorizontalAlignment.Right;
		this.GSMinBox.ValueChanged += this.RhMinBox_ValueChanged;
		// 
		// GSMaxBox
		// 
		this.GSMaxBox.Location = new Point(425, 3);
		this.GSMaxBox.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		this.GSMaxBox.Name = "GSMaxBox";
		this.GSMaxBox.Size = new Size(40, 23);
		this.GSMaxBox.TabIndex = 8;
		this.GSMaxBox.TextAlign = HorizontalAlignment.Right;
		this.GSMaxBox.ValueChanged += this.RhMinBox_ValueChanged;
		// 
		// label3
		// 
		this.label3.AutoSize = true;
		this.label3.Location = new Point(471, 5);
		this.label3.Margin = new Padding(3, 5, 3, 0);
		this.label3.Name = "label3";
		this.label3.Size = new Size(14, 15);
		this.label3.TabIndex = 9;
		this.label3.Text = "B";
		// 
		// BVMinBox
		// 
		this.BVMinBox.Location = new Point(491, 3);
		this.BVMinBox.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		this.BVMinBox.Name = "BVMinBox";
		this.BVMinBox.Size = new Size(40, 23);
		this.BVMinBox.TabIndex = 10;
		this.BVMinBox.TextAlign = HorizontalAlignment.Right;
		this.BVMinBox.ValueChanged += this.RhMinBox_ValueChanged;
		// 
		// BVMaxBox
		// 
		this.BVMaxBox.Location = new Point(537, 3);
		this.BVMaxBox.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		this.BVMaxBox.Name = "BVMaxBox";
		this.BVMaxBox.Size = new Size(40, 23);
		this.BVMaxBox.TabIndex = 11;
		this.BVMaxBox.TextAlign = HorizontalAlignment.Right;
		this.BVMaxBox.ValueChanged += this.RhMinBox_ValueChanged;
		// 
		// label4
		// 
		this.label4.AutoSize = true;
		this.label4.Location = new Point(583, 5);
		this.label4.Margin = new Padding(3, 5, 3, 0);
		this.label4.Name = "label4";
		this.label4.Size = new Size(27, 15);
		this.label4.TabIndex = 9;
		this.label4.Text = "Dist";
		// 
		// DistanceBox
		// 
		this.DistanceBox.Location = new Point(616, 3);
		this.DistanceBox.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		this.DistanceBox.Name = "DistanceBox";
		this.DistanceBox.Size = new Size(40, 23);
		this.DistanceBox.TabIndex = 11;
		this.DistanceBox.TextAlign = HorizontalAlignment.Right;
		this.DistanceBox.ValueChanged += this.RhMinBox_ValueChanged;
		// 
		// StatusLabel
		// 
		this.StatusLabel.AutoSize = true;
		this.StatusLabel.Location = new Point(662, 5);
		this.StatusLabel.Margin = new Padding(3, 5, 3, 0);
		this.StatusLabel.Name = "StatusLabel";
		this.StatusLabel.Size = new Size(94, 15);
		this.StatusLabel.TabIndex = 9;
		this.StatusLabel.Text = "Average: (0, 0, 0)";
		// 
		// PictureBox
		// 
		this.PictureBox.Dock = DockStyle.Fill;
		this.PictureBox.Location = new Point(0, 29);
		this.PictureBox.Name = "PictureBox";
		this.PictureBox.Size = new Size(1008, 532);
		this.PictureBox.SizeMode = PictureBoxSizeMode.Zoom;
		this.PictureBox.TabIndex = 1;
		this.PictureBox.TabStop = false;
		this.PictureBox.MouseDown += this.PictureBox_MouseMove;
		this.PictureBox.MouseMove += this.PictureBox_MouseMove;
		this.PictureBox.Resize += this.PictureBox_Resize;
		// 
		// openFileDialog
		// 
		this.openFileDialog.Filter = "Images|*.bmp;*.jpg;*.jpeg;*.png;*.webp";
		// 
		// ColourRangeForm
		// 
		this.AutoScaleDimensions = new SizeF(7F, 15F);
		this.AutoScaleMode = AutoScaleMode.Font;
		this.BackColor = Color.Black;
		this.ClientSize = new Size(1008, 561);
		this.Controls.Add(this.PictureBox);
		this.Controls.Add(this.flowLayoutPanel1);
		this.ForeColor = Color.White;
		this.Name = "ColourRangeForm";
		this.Text = "ColourRangeForm";
		this.Load += this.ColourRangeForm_Load;
		this.flowLayoutPanel1.ResumeLayout(false);
		this.flowLayoutPanel1.PerformLayout();
		((System.ComponentModel.ISupportInitialize) this.RHMinBox).EndInit();
		((System.ComponentModel.ISupportInitialize) this.RHMaxBox).EndInit();
		((System.ComponentModel.ISupportInitialize) this.GSMinBox).EndInit();
		((System.ComponentModel.ISupportInitialize) this.GSMaxBox).EndInit();
		((System.ComponentModel.ISupportInitialize) this.BVMinBox).EndInit();
		((System.ComponentModel.ISupportInitialize) this.BVMaxBox).EndInit();
		((System.ComponentModel.ISupportInitialize) this.DistanceBox).EndInit();
		((System.ComponentModel.ISupportInitialize) this.PictureBox).EndInit();
		this.ResumeLayout(false);
		this.PerformLayout();
	}

	#endregion

	private FlowLayoutPanel flowLayoutPanel1;
	private Button OpenButton;
	private Button PasteButton;
	private ComboBox ModeBox;
	private Label label1;
	private NumericUpDown RHMinBox;
	private NumericUpDown RHMaxBox;
	private Label label2;
	private NumericUpDown GSMinBox;
	private NumericUpDown GSMaxBox;
	private Label label3;
	private NumericUpDown BVMinBox;
	private NumericUpDown BVMaxBox;
	private PictureBox PictureBox;
	private OpenFileDialog openFileDialog;
	private Label label4;
	private NumericUpDown DistanceBox;
	private Label StatusLabel;
}