namespace ImageProcessingTester;
public partial class MainForm : Form {
	public MainForm() => this.InitializeComponent();

	private void TransformationButton_Click(object sender, EventArgs e) => new TransformationForm().Show();

	private void ClassificationButton_Click(object sender, EventArgs e) => new ClassificationForm().Show();

	private void ColourRangeButton_Click(object sender, EventArgs e) => new ColourRangeForm().Show();
}
