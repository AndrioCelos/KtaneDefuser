
using System.Configuration;
using BombDefuserConnector;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessingTester;
public partial class ColourRangeForm : Form {
	private Bitmap? originalImage;
	private Rectangle imageRectangle;
	private bool autoRedraw = true;

	public ColourRangeForm() {
		InitializeComponent();
	}

	public void RecalculateAverage() {
		if (originalImage is null) return;
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
		if (originalImage is null) return;

		var scaleX = (double) pictureBox1.Width / originalImage.Width;
		var scaleY = (double) pictureBox1.Height / originalImage.Height;
		if (scaleX < scaleY) {
			var height = (int) Math.Round(originalImage.Height * scaleX);
			imageRectangle = new(0, (pictureBox1.Height - height) / 2, pictureBox1.Width, height);
		} else {
			var width = (int) Math.Round(originalImage.Width * scaleY);
			imageRectangle = new((pictureBox1.Width - width) / 2, 0, width, pictureBox1.Height);
		}

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
		if (autoRedraw) Redraw();
	}

	private void ColourRangeForm_Load(object sender, EventArgs e) {
		comboBox1.SelectedIndex = 0;
	}

	private void button1_Click(object sender, EventArgs e) {
		if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
			var image = SixLabors.ImageSharp.Image.Load<Rgba32>(openFileDialog.FileName);
			originalImage = image.ToWinFormsImage();
			RecalculateAverage();
			Redraw();
		}
	}

	private void button2_Click(object sender, EventArgs e) {
		var image = Clipboard.GetImage();
		if (image is not null) {
			this.originalImage = new Bitmap(image);
			RecalculateAverage();
			Redraw();
		}
	}

	private void pictureBox1_MouseMove(object sender, MouseEventArgs e) {
		if (originalImage is null || (!e.Button.HasFlag(MouseButtons.Left) && !e.Button.HasFlag(MouseButtons.Right))) return;
		var x = (e.X - imageRectangle.X) * originalImage.Width / imageRectangle.Width;
		var y = (e.Y - imageRectangle.Y) * originalImage.Height / imageRectangle.Height;
		if (x >= 0 && x < originalImage.Width && y >= 0 && y < originalImage.Height) {
			var pixel = originalImage.GetPixel(x, y);
			if (e.Button == MouseButtons.Left) {
				this.label5.Text = this.comboBox1.SelectedIndex == 1 ? HsvColor.FromColor(pixel).ToString() : pixel.ToString();
			} else if (e.Button == MouseButtons.Right) {
				autoRedraw = false;
				switch (comboBox1.SelectedIndex) {
					case 0:
						numericUpDown1.Value = Math.Min(ModifierKeys.HasFlag(Keys.Alt) ? 255 : numericUpDown1.Value, pixel.R);
						numericUpDown2.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 0 : numericUpDown2.Value, pixel.R);
						numericUpDown3.Value = Math.Min(ModifierKeys.HasFlag(Keys.Alt) ? 255 : numericUpDown3.Value, pixel.G);
						numericUpDown4.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 0 : numericUpDown4.Value, pixel.G);
						numericUpDown5.Value = Math.Min(ModifierKeys.HasFlag(Keys.Alt) ? 255 : numericUpDown5.Value, pixel.B);
						numericUpDown6.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 0 : numericUpDown6.Value, pixel.B);
						break;
					case 1:
						var hsv = HsvColor.FromColor(pixel);
						if (ModifierKeys.HasFlag(Keys.Alt)) {
							numericUpDown1.Value = (decimal) Math.Floor(hsv.H);
							numericUpDown2.Value = (decimal) Math.Ceiling(hsv.H);
						} else if (numericUpDown2.Value < numericUpDown1.Value) {
							var midpoint = (numericUpDown1.Value + numericUpDown2.Value) / 2;
							if ((decimal) hsv.H < midpoint) numericUpDown2.Value = Math.Max(numericUpDown2.Value, (decimal) Math.Ceiling(hsv.H));
							else numericUpDown1.Value = Math.Min(numericUpDown1.Value, (decimal) Math.Floor(hsv.H));
						} else {
							numericUpDown1.Value = Math.Min(numericUpDown1.Value, (decimal) Math.Floor(hsv.H));
							numericUpDown2.Value = Math.Max(numericUpDown2.Value, (decimal) Math.Ceiling(hsv.H));
						}
						numericUpDown3.Value = Math.Min(ModifierKeys.HasFlag(Keys.Alt) ? 100 : numericUpDown3.Value, (decimal) Math.Floor(hsv.S * 100));
						numericUpDown4.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 0 : numericUpDown4.Value, (decimal) Math.Ceiling(hsv.S * 100));
						numericUpDown5.Value = Math.Min(ModifierKeys.HasFlag(Keys.Alt) ? 100 : numericUpDown5.Value, (decimal) Math.Floor(hsv.V * 100));
						numericUpDown6.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 0 : numericUpDown6.Value, (decimal) Math.Ceiling(hsv.V * 100));
						break;
					case 2:
						numericUpDown7.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 768 : numericUpDown7.Value, Math.Abs(pixel.R - (int) numericUpDown1.Value) + Math.Abs(pixel.G - (int) numericUpDown3.Value) + Math.Abs(pixel.B - (int) numericUpDown5.Value));
						break;
				}
				autoRedraw = true;
				Redraw();
			}
		}
	}

	private void pictureBox1_Resize(object sender, EventArgs e) {
		if (originalImage is null) return;
		var scaleX = (double) pictureBox1.Width / originalImage.Width;
		var scaleY = (double) pictureBox1.Height / originalImage.Height;
		if (scaleX < scaleY) {
			var height = (int) Math.Round(originalImage.Height * scaleX);
			imageRectangle = new(0, (pictureBox1.Height - height) / 2, pictureBox1.Width, height);
		} else {
			var width = (int) Math.Round(originalImage.Width * scaleY);
			imageRectangle = new((pictureBox1.Width - width) / 2, 0, width, pictureBox1.Height);
		}
	}
}
