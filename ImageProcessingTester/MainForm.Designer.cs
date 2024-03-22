namespace ImageProcessingTester;

partial class MainForm {
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
		this.TransformationButton = new Button();
		this.ClassificationButton = new Button();
		this.ColourRangeButton = new Button();
		this.SuspendLayout();
		// 
		// TransformationButton
		// 
		this.TransformationButton.Anchor =  AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		this.TransformationButton.Location = new Point(12, 12);
		this.TransformationButton.Name = "TransformationButton";
		this.TransformationButton.Size = new Size(164, 23);
		this.TransformationButton.TabIndex = 0;
		this.TransformationButton.Text = "TransformationForm";
		this.TransformationButton.UseVisualStyleBackColor = true;
		this.TransformationButton.Click += this.TransformationButton_Click;
		// 
		// ClassificationButton
		// 
		this.ClassificationButton.Anchor =  AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		this.ClassificationButton.Location = new Point(12, 41);
		this.ClassificationButton.Name = "ClassificationButton";
		this.ClassificationButton.Size = new Size(164, 23);
		this.ClassificationButton.TabIndex = 1;
		this.ClassificationButton.Text = "ClassificationForm";
		this.ClassificationButton.UseVisualStyleBackColor = true;
		this.ClassificationButton.Click += this.ClassificationButton_Click;
		// 
		// ColourRangeButton
		// 
		this.ColourRangeButton.Location = new Point(12, 70);
		this.ColourRangeButton.Name = "ColourRangeButton";
		this.ColourRangeButton.Size = new Size(164, 23);
		this.ColourRangeButton.TabIndex = 2;
		this.ColourRangeButton.Text = "ColourRangeForm";
		this.ColourRangeButton.UseVisualStyleBackColor = true;
		this.ColourRangeButton.Click += this.ColourRangeButton_Click;
		// 
		// MainForm
		// 
		this.AutoScaleDimensions = new SizeF(7F, 15F);
		this.AutoScaleMode = AutoScaleMode.Font;
		this.ClientSize = new Size(188, 102);
		this.Controls.Add(this.ColourRangeButton);
		this.Controls.Add(this.ClassificationButton);
		this.Controls.Add(this.TransformationButton);
		this.MaximizeBox = false;
		this.Name = "MainForm";
		this.Text = "MainForm";
		this.ResumeLayout(false);
	}

	#endregion

	private Button TransformationButton;
	private Button ClassificationButton;
	private Button ColourRangeButton;
}