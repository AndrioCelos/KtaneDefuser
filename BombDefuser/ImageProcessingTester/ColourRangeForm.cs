
using BombDefuserConnector;

namespace ImageProcessingTester;
public partial class ColourRangeForm : Form {
	private Bitmap? originalImage;

	public ColourRangeForm() {
		InitializeComponent();
	}

	public void RecalculateAverage() {
		long r = 0, g = 0, b = 0, a = 0;
		for (int y = 0; y < originalImage.Height; y++) {
			for (int x = 0; x < originalImage.Width; x++) {
				var color = originalImage.GetPixel(x, y);
				if (color.A > 0 && (color.R > 0 || color.G > 0 || color.B > 0)) {
					if (color.A == 255) {
						r += color.R;
						g += color.G;
						b += color.B;
						a += 255;
					} else {
						r += color.R * 255 / color.A;
						g += color.G * 255 / color.A;
						b += color.B * 255 / color.A;
						a += color.A;
					}
				}
			}
		}
		label5.Text = $"Average: ({r * 255 / a}, {g * 255 / a}, {b * 255 / a})";
	}

	public void Redraw() {
		if (originalImage == null)
			return;
		var bitmap = new Bitmap(originalImage);
		if (comboBox1.SelectedIndex == 1) {
			for (int y = 0; y < bitmap.Height; y++) {
				for (int x = 0; x < bitmap.Width; x++) {
					var color = bitmap.GetPixel(x, y);
					var hsv = HsvColor.FromColor(color);
					if (!((numericUpDown1.Value > numericUpDown2.Value ? (hsv.H >= (float) numericUpDown1.Value || hsv.H <= (float) numericUpDown2.Value) : (hsv.H >= (float) numericUpDown1.Value && hsv.H <= (float) numericUpDown2.Value))
						&& hsv.S * 100 >= (float) numericUpDown3.Value && hsv.S * 100 <= (float) numericUpDown4.Value
						&& hsv.V * 100 >= (float) numericUpDown5.Value && hsv.V * 100 <= (float) numericUpDown6.Value)) {
						bitmap.SetPixel(x, y, Color.FromArgb(color.R / 2, color.G / 2, color.B / 2));
					}
				}
			}
		} else if (comboBox1.SelectedIndex == 2) {
			for (int y = 0; y < bitmap.Height; y++) {
				for (int x = 0; x < bitmap.Width; x++) {
					var color = bitmap.GetPixel(x, y);
					if (Math.Abs(color.R - (int) numericUpDown1.Value) + Math.Abs(color.G - (int) numericUpDown3.Value) + Math.Abs(color.B - (int) numericUpDown5.Value) > (int) numericUpDown7.Value) {
						bitmap.SetPixel(x, y, Color.FromArgb(color.R / 2, color.G / 2, color.B / 2));
					}
				}
			}
		} else {
			for (int y = 0; y < bitmap.Height; y++) {
				for (int x = 0; x < bitmap.Width; x++) {
					var color = bitmap.GetPixel(x, y);
					if (!(color.R >= (float) numericUpDown1.Value && color.R <= (float) numericUpDown2.Value
						&& color.G >= (float) numericUpDown3.Value && color.G <= (float) numericUpDown4.Value
						&& color.B >= (float) numericUpDown5.Value && color.B <= (float) numericUpDown6.Value)) {
						bitmap.SetPixel(x, y, Color.FromArgb(color.R / 2, color.G / 2, color.B / 2));
					}
				}
			}
		}
		pictureBox1.Image = bitmap;
	}

	private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
		label1.Text = comboBox1.SelectedIndex == 1 ? "H" : "R";
		label2.Text = comboBox1.SelectedIndex == 1 ? "S" : "G";
		label3.Text = comboBox1.SelectedIndex == 1 ? "V" : "B";
		numericUpDown2.Visible = numericUpDown4.Visible = numericUpDown6.Visible = comboBox1.SelectedIndex != 2;
		numericUpDown7.Visible = comboBox1.SelectedIndex == 2;
		Redraw();
	}

	private void numericUpDown1_ValueChanged(object sender, EventArgs e) {
		Redraw();
	}

	private void ColourRangeForm_Load(object sender, EventArgs e) {
		comboBox1.SelectedIndex = 0;
	}

	private void button1_Click(object sender, EventArgs e) {
		if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
			originalImage = new Bitmap(openFileDialog.FileName);
			RecalculateAverage();
			Redraw();
		}
	}

	private void button2_Click(object sender, EventArgs e) {
		if (Clipboard.ContainsImage()) {
			originalImage = new Bitmap(Clipboard.GetImage());
			RecalculateAverage();
			Redraw();
		}
	}
}
