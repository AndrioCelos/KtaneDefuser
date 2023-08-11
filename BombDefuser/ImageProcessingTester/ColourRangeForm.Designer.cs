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
		flowLayoutPanel1 = new FlowLayoutPanel();
		button1 = new Button();
		button2 = new Button();
		comboBox1 = new ComboBox();
		label1 = new Label();
		numericUpDown1 = new NumericUpDown();
		numericUpDown2 = new NumericUpDown();
		label2 = new Label();
		numericUpDown3 = new NumericUpDown();
		numericUpDown4 = new NumericUpDown();
		label3 = new Label();
		numericUpDown5 = new NumericUpDown();
		numericUpDown6 = new NumericUpDown();
		label4 = new Label();
		numericUpDown7 = new NumericUpDown();
		label5 = new Label();
		pictureBox1 = new PictureBox();
		openFileDialog = new OpenFileDialog();
		flowLayoutPanel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize) numericUpDown1).BeginInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown2).BeginInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown3).BeginInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown4).BeginInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown5).BeginInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown6).BeginInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown7).BeginInit();
		((System.ComponentModel.ISupportInitialize) pictureBox1).BeginInit();
		this.SuspendLayout();
		// 
		// flowLayoutPanel1
		// 
		flowLayoutPanel1.AutoSize = true;
		flowLayoutPanel1.Controls.Add(button1);
		flowLayoutPanel1.Controls.Add(button2);
		flowLayoutPanel1.Controls.Add(comboBox1);
		flowLayoutPanel1.Controls.Add(label1);
		flowLayoutPanel1.Controls.Add(numericUpDown1);
		flowLayoutPanel1.Controls.Add(numericUpDown2);
		flowLayoutPanel1.Controls.Add(label2);
		flowLayoutPanel1.Controls.Add(numericUpDown3);
		flowLayoutPanel1.Controls.Add(numericUpDown4);
		flowLayoutPanel1.Controls.Add(label3);
		flowLayoutPanel1.Controls.Add(numericUpDown5);
		flowLayoutPanel1.Controls.Add(numericUpDown6);
		flowLayoutPanel1.Controls.Add(label4);
		flowLayoutPanel1.Controls.Add(numericUpDown7);
		flowLayoutPanel1.Controls.Add(label5);
		flowLayoutPanel1.Dock = DockStyle.Top;
		flowLayoutPanel1.Location = new Point(0, 0);
		flowLayoutPanel1.Name = "flowLayoutPanel1";
		flowLayoutPanel1.Size = new Size(800, 29);
		flowLayoutPanel1.TabIndex = 0;
		// 
		// button1
		// 
		button1.ForeColor = Color.Black;
		button1.Location = new Point(3, 3);
		button1.Name = "button1";
		button1.Size = new Size(75, 23);
		button1.TabIndex = 0;
		button1.Text = "Open...";
		button1.UseVisualStyleBackColor = true;
		button1.Click += this.button1_Click;
		// 
		// button2
		// 
		button2.ForeColor = Color.Black;
		button2.Location = new Point(84, 3);
		button2.Name = "button2";
		button2.Size = new Size(75, 23);
		button2.TabIndex = 1;
		button2.Text = "Paste";
		button2.UseVisualStyleBackColor = true;
		button2.Click += this.button2_Click;
		// 
		// comboBox1
		// 
		comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
		comboBox1.FormattingEnabled = true;
		comboBox1.Items.AddRange(new object[] { "RGB", "HSV", "RGBD" });
		comboBox1.Location = new Point(165, 3);
		comboBox1.Name = "comboBox1";
		comboBox1.Size = new Size(75, 23);
		comboBox1.TabIndex = 2;
		comboBox1.SelectedIndexChanged += this.comboBox1_SelectedIndexChanged;
		// 
		// label1
		// 
		label1.AutoSize = true;
		label1.Location = new Point(246, 5);
		label1.Margin = new Padding(3, 5, 3, 0);
		label1.Name = "label1";
		label1.Size = new Size(14, 15);
		label1.TabIndex = 3;
		label1.Text = "R";
		// 
		// numericUpDown1
		// 
		numericUpDown1.Location = new Point(266, 3);
		numericUpDown1.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		numericUpDown1.Name = "numericUpDown1";
		numericUpDown1.Size = new Size(40, 23);
		numericUpDown1.TabIndex = 4;
		numericUpDown1.TextAlign = HorizontalAlignment.Right;
		numericUpDown1.ValueChanged += this.numericUpDown1_ValueChanged;
		// 
		// numericUpDown2
		// 
		numericUpDown2.Location = new Point(312, 3);
		numericUpDown2.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		numericUpDown2.Name = "numericUpDown2";
		numericUpDown2.Size = new Size(40, 23);
		numericUpDown2.TabIndex = 5;
		numericUpDown2.TextAlign = HorizontalAlignment.Right;
		numericUpDown2.ValueChanged += this.numericUpDown1_ValueChanged;
		// 
		// label2
		// 
		label2.AutoSize = true;
		label2.Location = new Point(358, 5);
		label2.Margin = new Padding(3, 5, 3, 0);
		label2.Name = "label2";
		label2.Size = new Size(15, 15);
		label2.TabIndex = 6;
		label2.Text = "G";
		// 
		// numericUpDown3
		// 
		numericUpDown3.Location = new Point(379, 3);
		numericUpDown3.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		numericUpDown3.Name = "numericUpDown3";
		numericUpDown3.Size = new Size(40, 23);
		numericUpDown3.TabIndex = 7;
		numericUpDown3.TextAlign = HorizontalAlignment.Right;
		numericUpDown3.ValueChanged += this.numericUpDown1_ValueChanged;
		// 
		// numericUpDown4
		// 
		numericUpDown4.Location = new Point(425, 3);
		numericUpDown4.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		numericUpDown4.Name = "numericUpDown4";
		numericUpDown4.Size = new Size(40, 23);
		numericUpDown4.TabIndex = 8;
		numericUpDown4.TextAlign = HorizontalAlignment.Right;
		numericUpDown4.ValueChanged += this.numericUpDown1_ValueChanged;
		// 
		// label3
		// 
		label3.AutoSize = true;
		label3.Location = new Point(471, 5);
		label3.Margin = new Padding(3, 5, 3, 0);
		label3.Name = "label3";
		label3.Size = new Size(14, 15);
		label3.TabIndex = 9;
		label3.Text = "B";
		// 
		// numericUpDown5
		// 
		numericUpDown5.Location = new Point(491, 3);
		numericUpDown5.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		numericUpDown5.Name = "numericUpDown5";
		numericUpDown5.Size = new Size(40, 23);
		numericUpDown5.TabIndex = 10;
		numericUpDown5.TextAlign = HorizontalAlignment.Right;
		numericUpDown5.ValueChanged += this.numericUpDown1_ValueChanged;
		// 
		// numericUpDown6
		// 
		numericUpDown6.Location = new Point(537, 3);
		numericUpDown6.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		numericUpDown6.Name = "numericUpDown6";
		numericUpDown6.Size = new Size(40, 23);
		numericUpDown6.TabIndex = 11;
		numericUpDown6.TextAlign = HorizontalAlignment.Right;
		numericUpDown6.ValueChanged += this.numericUpDown1_ValueChanged;
		// 
		// label4
		// 
		label4.AutoSize = true;
		label4.Location = new Point(583, 5);
		label4.Margin = new Padding(3, 5, 3, 0);
		label4.Name = "label4";
		label4.Size = new Size(27, 15);
		label4.TabIndex = 9;
		label4.Text = "Dist";
		// 
		// numericUpDown7
		// 
		numericUpDown7.Location = new Point(616, 3);
		numericUpDown7.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
		numericUpDown7.Name = "numericUpDown7";
		numericUpDown7.Size = new Size(40, 23);
		numericUpDown7.TabIndex = 11;
		numericUpDown7.TextAlign = HorizontalAlignment.Right;
		numericUpDown7.ValueChanged += this.numericUpDown1_ValueChanged;
		// 
		// label5
		// 
		label5.AutoSize = true;
		label5.Location = new Point(662, 5);
		label5.Margin = new Padding(3, 5, 3, 0);
		label5.Name = "label5";
		label5.Size = new Size(94, 15);
		label5.TabIndex = 9;
		label5.Text = "Average: (0, 0, 0)";
		// 
		// pictureBox1
		// 
		pictureBox1.Dock = DockStyle.Fill;
		pictureBox1.Location = new Point(0, 29);
		pictureBox1.Name = "pictureBox1";
		pictureBox1.Size = new Size(800, 421);
		pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
		pictureBox1.TabIndex = 1;
		pictureBox1.TabStop = false;
		pictureBox1.MouseDown += this.pictureBox1_MouseDown;
		// 
		// openFileDialog
		// 
		openFileDialog.Filter = "Images|*.bmp;*.jpg;*.jpeg;*.png";
		// 
		// ColourRangeForm
		// 
		this.AutoScaleDimensions = new SizeF(7F, 15F);
		this.AutoScaleMode = AutoScaleMode.Font;
		this.BackColor = Color.Black;
		this.ClientSize = new Size(800, 450);
		this.Controls.Add(pictureBox1);
		this.Controls.Add(flowLayoutPanel1);
		this.ForeColor = Color.White;
		this.Name = "ColourRangeForm";
		this.Text = "ColourRangeForm";
		this.Load += this.ColourRangeForm_Load;
		flowLayoutPanel1.ResumeLayout(false);
		flowLayoutPanel1.PerformLayout();
		((System.ComponentModel.ISupportInitialize) numericUpDown1).EndInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown2).EndInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown3).EndInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown4).EndInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown5).EndInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown6).EndInit();
		((System.ComponentModel.ISupportInitialize) numericUpDown7).EndInit();
		((System.ComponentModel.ISupportInitialize) pictureBox1).EndInit();
		this.ResumeLayout(false);
		this.PerformLayout();
	}

	#endregion

	private FlowLayoutPanel flowLayoutPanel1;
	private Button button1;
	private Button button2;
	private ComboBox comboBox1;
	private Label label1;
	private NumericUpDown numericUpDown1;
	private NumericUpDown numericUpDown2;
	private Label label2;
	private NumericUpDown numericUpDown3;
	private NumericUpDown numericUpDown4;
	private Label label3;
	private NumericUpDown numericUpDown5;
	private NumericUpDown numericUpDown6;
	private PictureBox pictureBox1;
	private OpenFileDialog openFileDialog;
	private Label label4;
	private NumericUpDown numericUpDown7;
	private Label label5;
}