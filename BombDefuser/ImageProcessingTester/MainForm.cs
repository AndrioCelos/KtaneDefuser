namespace ImageProcessingTester;
public partial class MainForm : Form {
	public MainForm() {
		InitializeComponent();
	}

	private void button1_Click(object sender, EventArgs e) {
		new TransformationForm().Show();
	}

	private void button2_Click(object sender, EventArgs e) {
		new ClassificationForm().Show();
	}

	private void button3_Click(object sender, EventArgs e) {
		new ColourRangeForm().Show();
	}
}
