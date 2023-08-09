namespace ImageProcessingTester;

partial class ClassificationForm {
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
		openFileDialog = new OpenFileDialog();
		button1 = new Button();
		flowLayoutPanel1 = new FlowLayoutPanel();
		button2 = new Button();
		button3 = new Button();
		label2 = new Label();
		comboBox2 = new ComboBox();
		pictureBox1 = new PictureBox();
		textBox1 = new TextBox();
		label1 = new Label();
		comboBox1 = new ComboBox();
		flowLayoutPanel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize) pictureBox1).BeginInit();
		this.SuspendLayout();
		// 
		// openFileDialog
		// 
		openFileDialog.Filter = "Images|*.bmp;*.jpg;*.jpeg;*.png;*.webp";
		// 
		// button1
		// 
		button1.ForeColor = SystemColors.ControlText;
		button1.Location = new Point(3, 3);
		button1.Name = "button1";
		button1.Size = new Size(75, 23);
		button1.TabIndex = 2;
		button1.Text = "Open...";
		button1.UseVisualStyleBackColor = true;
		button1.Click += this.button1_Click;
		// 
		// flowLayoutPanel1
		// 
		flowLayoutPanel1.AutoSize = true;
		flowLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		flowLayoutPanel1.Controls.Add(button1);
		flowLayoutPanel1.Controls.Add(button2);
		flowLayoutPanel1.Controls.Add(button3);
		flowLayoutPanel1.Controls.Add(label2);
		flowLayoutPanel1.Controls.Add(comboBox2);
		flowLayoutPanel1.Location = new Point(12, 9);
		flowLayoutPanel1.Name = "flowLayoutPanel1";
		flowLayoutPanel1.Size = new Size(441, 29);
		flowLayoutPanel1.TabIndex = 3;
		// 
		// button2
		// 
		button2.ForeColor = SystemColors.ControlText;
		button2.Location = new Point(84, 3);
		button2.Name = "button2";
		button2.Size = new Size(75, 23);
		button2.TabIndex = 2;
		button2.Text = "Paste";
		button2.UseVisualStyleBackColor = true;
		button2.Click += this.button2_Click;
		// 
		// button3
		// 
		button3.ForeColor = SystemColors.ControlText;
		button3.Location = new Point(165, 3);
		button3.Name = "button3";
		button3.Size = new Size(111, 23);
		button3.TabIndex = 2;
		button3.Text = "Copy annotations";
		button3.UseVisualStyleBackColor = true;
		button3.Click += this.button3_Click;
		// 
		// label2
		// 
		label2.AutoSize = true;
		label2.Location = new Point(282, 6);
		label2.Margin = new Padding(3, 6, 3, 0);
		label2.Name = "label2";
		label2.Size = new Size(42, 15);
		label2.TabIndex = 6;
		label2.Text = "Lights:";
		// 
		// comboBox2
		// 
		comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
		comboBox2.FormattingEnabled = true;
		comboBox2.Items.AddRange(new object[] { "On", "Buzz", "Off", "Emergency" });
		comboBox2.Location = new Point(330, 3);
		comboBox2.Name = "comboBox2";
		comboBox2.Size = new Size(108, 23);
		comboBox2.TabIndex = 7;
		comboBox2.SelectedIndexChanged += this.comboBox1_SelectedIndexChanged;
		// 
		// pictureBox1
		// 
		pictureBox1.Anchor =  AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		pictureBox1.Location = new Point(12, 44);
		pictureBox1.Name = "pictureBox1";
		pictureBox1.Size = new Size(512, 512);
		pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
		pictureBox1.TabIndex = 4;
		pictureBox1.TabStop = false;
		// 
		// textBox1
		// 
		textBox1.Anchor =  AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		textBox1.BackColor = Color.FromArgb(  16,   16,   16);
		textBox1.ForeColor = Color.White;
		textBox1.Location = new Point(12, 591);
		textBox1.Multiline = true;
		textBox1.Name = "textBox1";
		textBox1.ReadOnly = true;
		textBox1.Size = new Size(512, 87);
		textBox1.TabIndex = 5;
		// 
		// label1
		// 
		label1.Anchor =  AnchorStyles.Bottom | AnchorStyles.Left;
		label1.AutoSize = true;
		label1.Location = new Point(12, 565);
		label1.Name = "label1";
		label1.Size = new Size(36, 15);
		label1.TabIndex = 6;
		label1.Text = "Read:";
		// 
		// comboBox1
		// 
		comboBox1.Anchor =  AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		comboBox1.DisplayMember = "Name";
		comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
		comboBox1.FormattingEnabled = true;
		comboBox1.Items.AddRange(new object[] { "Module information", "Widget information", "Module light state" });
		comboBox1.Location = new Point(54, 562);
		comboBox1.Name = "comboBox1";
		comboBox1.Size = new Size(470, 23);
		comboBox1.TabIndex = 7;
		comboBox1.SelectedIndexChanged += this.comboBox1_SelectedIndexChanged;
		// 
		// ClassificationForm
		// 
		this.AllowDrop = true;
		this.AutoScaleDimensions = new SizeF(7F, 15F);
		this.AutoScaleMode = AutoScaleMode.Font;
		this.BackColor = Color.Black;
		this.ClientSize = new Size(532, 689);
		this.Controls.Add(comboBox1);
		this.Controls.Add(label1);
		this.Controls.Add(textBox1);
		this.Controls.Add(pictureBox1);
		this.Controls.Add(flowLayoutPanel1);
		this.ForeColor = Color.White;
		this.Name = "ClassificationForm";
		this.Text = "ClassificationForm";
		this.Load += this.ClassificationForm_Load;
		this.DragDrop += this.ClassificationForm_DragDrop;
		this.DragEnter += this.ClassificationForm_DragEnter;
		flowLayoutPanel1.ResumeLayout(false);
		flowLayoutPanel1.PerformLayout();
		((System.ComponentModel.ISupportInitialize) pictureBox1).EndInit();
		this.ResumeLayout(false);
		this.PerformLayout();
	}

	#endregion

	private OpenFileDialog openFileDialog;
	private Button button1;
	private FlowLayoutPanel flowLayoutPanel1;
	private Button button2;
	private PictureBox pictureBox1;
	private TextBox textBox1;
	private Label label1;
	private ComboBox comboBox1;
	private Button button3;
	private Label label2;
	private ComboBox comboBox2;
}