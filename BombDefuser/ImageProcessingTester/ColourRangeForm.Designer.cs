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
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
			this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
			this.label2 = new System.Windows.Forms.Label();
			this.numericUpDown3 = new System.Windows.Forms.NumericUpDown();
			this.numericUpDown4 = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.numericUpDown5 = new System.Windows.Forms.NumericUpDown();
			this.numericUpDown6 = new System.Windows.Forms.NumericUpDown();
			this.label4 = new System.Windows.Forms.Label();
			this.numericUpDown7 = new System.Windows.Forms.NumericUpDown();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.label5 = new System.Windows.Forms.Label();
			this.flowLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown4)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown5)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown6)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown7)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.Controls.Add(this.button1);
			this.flowLayoutPanel1.Controls.Add(this.button2);
			this.flowLayoutPanel1.Controls.Add(this.comboBox1);
			this.flowLayoutPanel1.Controls.Add(this.label1);
			this.flowLayoutPanel1.Controls.Add(this.numericUpDown1);
			this.flowLayoutPanel1.Controls.Add(this.numericUpDown2);
			this.flowLayoutPanel1.Controls.Add(this.label2);
			this.flowLayoutPanel1.Controls.Add(this.numericUpDown3);
			this.flowLayoutPanel1.Controls.Add(this.numericUpDown4);
			this.flowLayoutPanel1.Controls.Add(this.label3);
			this.flowLayoutPanel1.Controls.Add(this.numericUpDown5);
			this.flowLayoutPanel1.Controls.Add(this.numericUpDown6);
			this.flowLayoutPanel1.Controls.Add(this.label4);
			this.flowLayoutPanel1.Controls.Add(this.numericUpDown7);
			this.flowLayoutPanel1.Controls.Add(this.label5);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(800, 29);
			this.flowLayoutPanel1.TabIndex = 0;
			// 
			// button1
			// 
			this.button1.ForeColor = System.Drawing.Color.Black;
			this.button1.Location = new System.Drawing.Point(3, 3);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 0;
			this.button1.Text = "Open...";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// button2
			// 
			this.button2.ForeColor = System.Drawing.Color.Black;
			this.button2.Location = new System.Drawing.Point(84, 3);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 1;
			this.button2.Text = "Paste";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// comboBox1
			// 
			this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.FormattingEnabled = true;
			this.comboBox1.Items.AddRange(new object[] {
            "RGB",
            "HSV",
            "RGBD"});
			this.comboBox1.Location = new System.Drawing.Point(165, 3);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(75, 23);
			this.comboBox1.TabIndex = 2;
			this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(246, 5);
			this.label1.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(14, 15);
			this.label1.TabIndex = 3;
			this.label1.Text = "R";
			// 
			// numericUpDown1
			// 
			this.numericUpDown1.Location = new System.Drawing.Point(266, 3);
			this.numericUpDown1.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
			this.numericUpDown1.Name = "numericUpDown1";
			this.numericUpDown1.Size = new System.Drawing.Size(40, 23);
			this.numericUpDown1.TabIndex = 4;
			this.numericUpDown1.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// numericUpDown2
			// 
			this.numericUpDown2.Location = new System.Drawing.Point(312, 3);
			this.numericUpDown2.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
			this.numericUpDown2.Name = "numericUpDown2";
			this.numericUpDown2.Size = new System.Drawing.Size(40, 23);
			this.numericUpDown2.TabIndex = 5;
			this.numericUpDown2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.numericUpDown2.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(358, 5);
			this.label2.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(15, 15);
			this.label2.TabIndex = 6;
			this.label2.Text = "G";
			// 
			// numericUpDown3
			// 
			this.numericUpDown3.Location = new System.Drawing.Point(379, 3);
			this.numericUpDown3.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
			this.numericUpDown3.Name = "numericUpDown3";
			this.numericUpDown3.Size = new System.Drawing.Size(40, 23);
			this.numericUpDown3.TabIndex = 7;
			this.numericUpDown3.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.numericUpDown3.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// numericUpDown4
			// 
			this.numericUpDown4.Location = new System.Drawing.Point(425, 3);
			this.numericUpDown4.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
			this.numericUpDown4.Name = "numericUpDown4";
			this.numericUpDown4.Size = new System.Drawing.Size(40, 23);
			this.numericUpDown4.TabIndex = 8;
			this.numericUpDown4.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.numericUpDown4.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(471, 5);
			this.label3.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(14, 15);
			this.label3.TabIndex = 9;
			this.label3.Text = "B";
			// 
			// numericUpDown5
			// 
			this.numericUpDown5.Location = new System.Drawing.Point(491, 3);
			this.numericUpDown5.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
			this.numericUpDown5.Name = "numericUpDown5";
			this.numericUpDown5.Size = new System.Drawing.Size(40, 23);
			this.numericUpDown5.TabIndex = 10;
			this.numericUpDown5.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.numericUpDown5.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// numericUpDown6
			// 
			this.numericUpDown6.Location = new System.Drawing.Point(537, 3);
			this.numericUpDown6.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
			this.numericUpDown6.Name = "numericUpDown6";
			this.numericUpDown6.Size = new System.Drawing.Size(40, 23);
			this.numericUpDown6.TabIndex = 11;
			this.numericUpDown6.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.numericUpDown6.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(583, 5);
			this.label4.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(27, 15);
			this.label4.TabIndex = 9;
			this.label4.Text = "Dist";
			// 
			// numericUpDown7
			// 
			this.numericUpDown7.Location = new System.Drawing.Point(616, 3);
			this.numericUpDown7.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
			this.numericUpDown7.Name = "numericUpDown7";
			this.numericUpDown7.Size = new System.Drawing.Size(40, 23);
			this.numericUpDown7.TabIndex = 11;
			this.numericUpDown7.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.numericUpDown7.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBox1.Location = new System.Drawing.Point(0, 29);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(800, 421);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pictureBox1.TabIndex = 1;
			this.pictureBox1.TabStop = false;
			// 
			// openFileDialog
			// 
			this.openFileDialog.Filter = "Images|*.bmp;*.jpg;*.jpeg;*.png";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(662, 5);
			this.label5.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(94, 15);
			this.label5.TabIndex = 9;
			this.label5.Text = "Average: (0, 0, 0)";
			// 
			// ColourRangeForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Black;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.flowLayoutPanel1);
			this.ForeColor = System.Drawing.Color.White;
			this.Name = "ColourRangeForm";
			this.Text = "ColourRangeForm";
			this.Load += new System.EventHandler(this.ColourRangeForm_Load);
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown4)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown5)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown6)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown7)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
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