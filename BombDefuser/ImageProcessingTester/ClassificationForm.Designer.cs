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
		this.openFileDialog = new OpenFileDialog();
		this.OpenButton = new Button();
		this.flowLayoutPanel1 = new FlowLayoutPanel();
		this.PasteButton = new Button();
		this.CopyAnnotationsButton = new Button();
		this.label2 = new Label();
		this.LightsStateBox = new ComboBox();
		this.PictureBox = new PictureBox();
		this.OutputBox = new TextBox();
		this.label1 = new Label();
		this.ReadModeBox = new ComboBox();
		this.flowLayoutPanel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize) this.PictureBox).BeginInit();
		this.SuspendLayout();
		// 
		// openFileDialog
		// 
		this.openFileDialog.Filter = "Images|*.bmp;*.jpg;*.jpeg;*.png;*.webp";
		// 
		// OpenButton
		// 
		this.OpenButton.ForeColor = SystemColors.ControlText;
		this.OpenButton.Location = new Point(3, 3);
		this.OpenButton.Name = "OpenButton";
		this.OpenButton.Size = new Size(75, 23);
		this.OpenButton.TabIndex = 2;
		this.OpenButton.Text = "Open...";
		this.OpenButton.UseVisualStyleBackColor = true;
		this.OpenButton.Click += this.OpenButton_Click;
		// 
		// flowLayoutPanel1
		// 
		this.flowLayoutPanel1.AutoSize = true;
		this.flowLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		this.flowLayoutPanel1.Controls.Add(this.OpenButton);
		this.flowLayoutPanel1.Controls.Add(this.PasteButton);
		this.flowLayoutPanel1.Controls.Add(this.CopyAnnotationsButton);
		this.flowLayoutPanel1.Controls.Add(this.label2);
		this.flowLayoutPanel1.Controls.Add(this.LightsStateBox);
		this.flowLayoutPanel1.Location = new Point(12, 9);
		this.flowLayoutPanel1.Name = "flowLayoutPanel1";
		this.flowLayoutPanel1.Size = new Size(441, 29);
		this.flowLayoutPanel1.TabIndex = 3;
		// 
		// PasteButton
		// 
		this.PasteButton.ForeColor = SystemColors.ControlText;
		this.PasteButton.Location = new Point(84, 3);
		this.PasteButton.Name = "PasteButton";
		this.PasteButton.Size = new Size(75, 23);
		this.PasteButton.TabIndex = 2;
		this.PasteButton.Text = "Paste";
		this.PasteButton.UseVisualStyleBackColor = true;
		this.PasteButton.Click += this.PasteButton_Click;
		// 
		// CopyAnnotationsButton
		// 
		this.CopyAnnotationsButton.ForeColor = SystemColors.ControlText;
		this.CopyAnnotationsButton.Location = new Point(165, 3);
		this.CopyAnnotationsButton.Name = "CopyAnnotationsButton";
		this.CopyAnnotationsButton.Size = new Size(111, 23);
		this.CopyAnnotationsButton.TabIndex = 2;
		this.CopyAnnotationsButton.Text = "Copy annotations";
		this.CopyAnnotationsButton.UseVisualStyleBackColor = true;
		this.CopyAnnotationsButton.Click += this.CopyAnnotationsButton_Click;
		// 
		// label2
		// 
		this.label2.AutoSize = true;
		this.label2.Location = new Point(282, 6);
		this.label2.Margin = new Padding(3, 6, 3, 0);
		this.label2.Name = "label2";
		this.label2.Size = new Size(42, 15);
		this.label2.TabIndex = 6;
		this.label2.Text = "Lights:";
		// 
		// LightsStateBox
		// 
		this.LightsStateBox.DropDownStyle = ComboBoxStyle.DropDownList;
		this.LightsStateBox.FormattingEnabled = true;
		this.LightsStateBox.Items.AddRange(new object[] { "On", "Buzz", "Off", "Emergency" });
		this.LightsStateBox.Location = new Point(330, 3);
		this.LightsStateBox.Name = "LightsStateBox";
		this.LightsStateBox.Size = new Size(108, 23);
		this.LightsStateBox.TabIndex = 7;
		this.LightsStateBox.SelectedIndexChanged += this.ReadModeBox_SelectedIndexChanged;
		// 
		// PictureBox
		// 
		this.PictureBox.Anchor =  AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		this.PictureBox.Location = new Point(12, 44);
		this.PictureBox.Name = "PictureBox";
		this.PictureBox.Size = new Size(512, 512);
		this.PictureBox.SizeMode = PictureBoxSizeMode.Zoom;
		this.PictureBox.TabIndex = 4;
		this.PictureBox.TabStop = false;
		// 
		// OutputBox
		// 
		this.OutputBox.Anchor =  AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		this.OutputBox.BackColor = Color.FromArgb(  16,   16,   16);
		this.OutputBox.ForeColor = Color.White;
		this.OutputBox.Location = new Point(12, 591);
		this.OutputBox.Multiline = true;
		this.OutputBox.Name = "OutputBox";
		this.OutputBox.ReadOnly = true;
		this.OutputBox.Size = new Size(512, 87);
		this.OutputBox.TabIndex = 5;
		// 
		// label1
		// 
		this.label1.Anchor =  AnchorStyles.Bottom | AnchorStyles.Left;
		this.label1.AutoSize = true;
		this.label1.Location = new Point(12, 565);
		this.label1.Name = "label1";
		this.label1.Size = new Size(36, 15);
		this.label1.TabIndex = 6;
		this.label1.Text = "Read:";
		// 
		// ReadModeBox
		// 
		this.ReadModeBox.Anchor =  AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		this.ReadModeBox.DisplayMember = "Name";
		this.ReadModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
		this.ReadModeBox.FormattingEnabled = true;
		this.ReadModeBox.Items.AddRange(new object[] { "Module information", "Widget information", "Module light state" });
		this.ReadModeBox.Location = new Point(54, 562);
		this.ReadModeBox.Name = "ReadModeBox";
		this.ReadModeBox.Size = new Size(470, 23);
		this.ReadModeBox.TabIndex = 7;
		this.ReadModeBox.SelectedIndexChanged += this.ReadModeBox_SelectedIndexChanged;
		// 
		// ClassificationForm
		// 
		this.AllowDrop = true;
		this.AutoScaleDimensions = new SizeF(7F, 15F);
		this.AutoScaleMode = AutoScaleMode.Font;
		this.BackColor = Color.Black;
		this.ClientSize = new Size(532, 689);
		this.Controls.Add(this.ReadModeBox);
		this.Controls.Add(this.label1);
		this.Controls.Add(this.OutputBox);
		this.Controls.Add(this.PictureBox);
		this.Controls.Add(this.flowLayoutPanel1);
		this.ForeColor = Color.White;
		this.Name = "ClassificationForm";
		this.Text = "ClassificationForm";
		this.Load += this.ClassificationForm_Load;
		this.DragDrop += this.ClassificationForm_DragDrop;
		this.DragEnter += this.ClassificationForm_DragEnter;
		this.flowLayoutPanel1.ResumeLayout(false);
		this.flowLayoutPanel1.PerformLayout();
		((System.ComponentModel.ISupportInitialize) this.PictureBox).EndInit();
		this.ResumeLayout(false);
		this.PerformLayout();
	}

	#endregion

	private OpenFileDialog openFileDialog;
	private Button OpenButton;
	private FlowLayoutPanel flowLayoutPanel1;
	private Button PasteButton;
	private PictureBox PictureBox;
	private TextBox OutputBox;
	private Label label1;
	private ComboBox ReadModeBox;
	private Button CopyAnnotationsButton;
	private Label label2;
	private ComboBox LightsStateBox;
}