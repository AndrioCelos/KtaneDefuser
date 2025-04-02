using KtaneDefuserConnector;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessingTester;
public partial class ColourRangeForm : Form {
	private Bitmap? originalImage;
	private Rectangle imageRectangle;
	private bool autoRedraw = true;

	public ColourRangeForm() => this.InitializeComponent();

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
		StatusLabel.Text = $"Average: ({r * 255 / a}, {g * 255 / a}, {b * 255 / a})";
	}

	public void Redraw() {
		if (originalImage is null) return;

		var scaleX = (double) PictureBox.Width / originalImage.Width;
		var scaleY = (double) PictureBox.Height / originalImage.Height;
		if (scaleX < scaleY) {
			var height = (int) Math.Round(originalImage.Height * scaleX);
			imageRectangle = new(0, (PictureBox.Height - height) / 2, PictureBox.Width, height);
		} else {
			var width = (int) Math.Round(originalImage.Width * scaleY);
			imageRectangle = new((PictureBox.Width - width) / 2, 0, width, PictureBox.Height);
		}

		var bitmap = new Bitmap(originalImage);
		if (ModeBox.SelectedIndex == 1) {
			for (int y = 0; y < bitmap.Height; y++) {
				for (int x = 0; x < bitmap.Width; x++) {
					var color = bitmap.GetPixel(x, y);
					var hsv = HsvColor.FromColor(color);
					if (!((RHMinBox.Value > RHMaxBox.Value ? (hsv.H >= (float) RHMinBox.Value || hsv.H <= (float) RHMaxBox.Value) : (hsv.H >= (float) RHMinBox.Value && hsv.H <= (float) RHMaxBox.Value))
						&& hsv.S * 100 >= (float) GSMinBox.Value && hsv.S * 100 <= (float) GSMaxBox.Value
						&& hsv.V * 100 >= (float) BVMinBox.Value && hsv.V * 100 <= (float) BVMaxBox.Value)) {
						bitmap.SetPixel(x, y, Color.FromArgb(color.R / 2, color.G / 2, color.B / 2));
					}
				}
			}
		} else if (ModeBox.SelectedIndex == 2) {
			for (int y = 0; y < bitmap.Height; y++) {
				for (int x = 0; x < bitmap.Width; x++) {
					var color = bitmap.GetPixel(x, y);
					if (Math.Abs(color.R - (int) RHMinBox.Value) + Math.Abs(color.G - (int) GSMinBox.Value) + Math.Abs(color.B - (int) BVMinBox.Value) > (int) DistanceBox.Value) {
						bitmap.SetPixel(x, y, Color.FromArgb(color.R / 2, color.G / 2, color.B / 2));
					}
				}
			}
		} else {
			for (int y = 0; y < bitmap.Height; y++) {
				for (int x = 0; x < bitmap.Width; x++) {
					var color = bitmap.GetPixel(x, y);
					if (!(color.R >= (float) RHMinBox.Value && color.R <= (float) RHMaxBox.Value
						&& color.G >= (float) GSMinBox.Value && color.G <= (float) GSMaxBox.Value
						&& color.B >= (float) BVMinBox.Value && color.B <= (float) BVMaxBox.Value)) {
						bitmap.SetPixel(x, y, Color.FromArgb(color.R / 2, color.G / 2, color.B / 2));
					}
				}
			}
		}
		PictureBox.Image = bitmap;
	}

	private void ModeBox_SelectedIndexChanged(object sender, EventArgs e) {
		label1.Text = ModeBox.SelectedIndex == 1 ? "H" : "R";
		label2.Text = ModeBox.SelectedIndex == 1 ? "S" : "G";
		label3.Text = ModeBox.SelectedIndex == 1 ? "V" : "B";
		RHMaxBox.Visible = GSMaxBox.Visible = BVMaxBox.Visible = ModeBox.SelectedIndex != 2;
		DistanceBox.Visible = ModeBox.SelectedIndex == 2;
		Redraw();
	}

	private void RhMinBox_ValueChanged(object sender, EventArgs e) {
		if (autoRedraw) Redraw();
	}

	private void ColourRangeForm_Load(object sender, EventArgs e) => ModeBox.SelectedIndex = 0;

	private void OpenButton_Click(object sender, EventArgs e) {
		if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
			var image = SixLabors.ImageSharp.Image.Load<Rgba32>(openFileDialog.FileName);
			originalImage = image.ToWinFormsImage();
			RecalculateAverage();
			Redraw();
		}
	}

	private void PasteButton_Click(object sender, EventArgs e) {
		var image = Clipboard.GetImage();
		if (image is null) {
			MessageBox.Show(this, "There is no usable image on the Clipboard.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}
		this.originalImage = new Bitmap(image);
		RecalculateAverage();
		Redraw();
	}

	private void PictureBox_MouseMove(object sender, MouseEventArgs e) {
		if (originalImage is null || (!e.Button.HasFlag(MouseButtons.Left) && !e.Button.HasFlag(MouseButtons.Right))) return;
		var x = (e.X - imageRectangle.X) * originalImage.Width / imageRectangle.Width;
		var y = (e.Y - imageRectangle.Y) * originalImage.Height / imageRectangle.Height;
		if (x >= 0 && x < originalImage.Width && y >= 0 && y < originalImage.Height) {
			var pixel = originalImage.GetPixel(x, y);
			if (e.Button == MouseButtons.Left) {
				this.StatusLabel.Text = this.ModeBox.SelectedIndex == 1 ? HsvColor.FromColor(pixel).ToString() : pixel.ToString();
			} else if (e.Button == MouseButtons.Right) {
				autoRedraw = false;
				switch (ModeBox.SelectedIndex) {
					case 0:
						RHMinBox.Value = Math.Min(ModifierKeys.HasFlag(Keys.Alt) ? 255 : RHMinBox.Value, pixel.R);
						RHMaxBox.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 0 : RHMaxBox.Value, pixel.R);
						GSMinBox.Value = Math.Min(ModifierKeys.HasFlag(Keys.Alt) ? 255 : GSMinBox.Value, pixel.G);
						GSMaxBox.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 0 : GSMaxBox.Value, pixel.G);
						BVMinBox.Value = Math.Min(ModifierKeys.HasFlag(Keys.Alt) ? 255 : BVMinBox.Value, pixel.B);
						BVMaxBox.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 0 : BVMaxBox.Value, pixel.B);
						break;
					case 1:
						var hsv = HsvColor.FromColor(pixel);
						if (ModifierKeys.HasFlag(Keys.Alt)) {
							RHMinBox.Value = (decimal) Math.Floor(hsv.H);
							RHMaxBox.Value = (decimal) Math.Ceiling(hsv.H);
						} else if (RHMaxBox.Value < RHMinBox.Value) {
							var midpoint = (RHMinBox.Value + RHMaxBox.Value) / 2;
							if ((decimal) hsv.H < midpoint) RHMaxBox.Value = Math.Max(RHMaxBox.Value, (decimal) Math.Ceiling(hsv.H));
							else RHMinBox.Value = Math.Min(RHMinBox.Value, (decimal) Math.Floor(hsv.H));
						} else {
							RHMinBox.Value = Math.Min(RHMinBox.Value, (decimal) Math.Floor(hsv.H));
							RHMaxBox.Value = Math.Max(RHMaxBox.Value, (decimal) Math.Ceiling(hsv.H));
						}
						GSMinBox.Value = Math.Min(ModifierKeys.HasFlag(Keys.Alt) ? 100 : GSMinBox.Value, (decimal) Math.Floor(hsv.S * 100));
						GSMaxBox.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 0 : GSMaxBox.Value, (decimal) Math.Ceiling(hsv.S * 100));
						BVMinBox.Value = Math.Min(ModifierKeys.HasFlag(Keys.Alt) ? 100 : BVMinBox.Value, (decimal) Math.Floor(hsv.V * 100));
						BVMaxBox.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 0 : BVMaxBox.Value, (decimal) Math.Ceiling(hsv.V * 100));
						break;
					case 2:
						DistanceBox.Value = Math.Max(ModifierKeys.HasFlag(Keys.Alt) ? 768 : DistanceBox.Value, Math.Abs(pixel.R - (int) RHMinBox.Value) + Math.Abs(pixel.G - (int) GSMinBox.Value) + Math.Abs(pixel.B - (int) BVMinBox.Value));
						break;
				}
				autoRedraw = true;
				Redraw();
			}
		}
	}

	private void PictureBox_Resize(object sender, EventArgs e) {
		if (originalImage is null) return;
		var scaleX = (double) PictureBox.Width / originalImage.Width;
		var scaleY = (double) PictureBox.Height / originalImage.Height;
		if (scaleX < scaleY) {
			var height = (int) Math.Round(originalImage.Height * scaleX);
			imageRectangle = new(0, (PictureBox.Height - height) / 2, PictureBox.Width, height);
		} else {
			var width = (int) Math.Round(originalImage.Width * scaleY);
			imageRectangle = new((PictureBox.Width - width) / 2, 0, width, PictureBox.Height);
		}
	}
}
