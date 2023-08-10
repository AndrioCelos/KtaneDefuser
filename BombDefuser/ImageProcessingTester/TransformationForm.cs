using BombDefuserConnector;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = System.Drawing.Color;
using Image = SixLabors.ImageSharp.Image;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using SizeF = System.Drawing.SizeF;

namespace ImageProcessingTester;

public partial class TransformationForm : Form {
	private Image<Rgba32>? screenImage;
	private Image<Rgba32>? distortedImage;
	private Bitmap? screenBitmap;
	private Point[] points = new Point[4];
	private int? draggingPoint;

	private void SetScreenImage(Image<Rgba32>? value) {
		this.screenImage = value;
		if (value != null) {
			screenBitmap = value.ToWinFormsImage();
			points[1].X = Math.Min(points[1].X, value.Width);
			points[2].Y = Math.Min(points[2].Y, value.Height);
			points[3].X = Math.Min(points[3].X, value.Width);
			points[3].Y = Math.Min(points[3].Y, value.Height);
			lightsLabel.Text = value.Height >= 250 ? $"Lights: {ImageUtils.GetLightsState(value)}" : "";
			screenshotPanel_Resize(this, EventArgs.Empty);
			RecalculateSideWidgetPresets();
			RedrawTransformedImage();
		}
	}

	public TransformationForm() {
		InitializeComponent();
		interpolationBox.SelectedIndex = 0;
		lightsSimulateBox.SelectedIndex = 0;
		RecalculateSideWidgetPresets();
	}

	private void screenshotLoadButton_Click(object sender, EventArgs e) {
		if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
			SetScreenImage(Image.Load<Rgba32>(openFileDialog.FileName));
		}
	}

	private void screenPictureBox_Paint(object sender, PaintEventArgs e) {
		if (screenImage != null && screenBitmap != null) {
			e.Graphics.DrawImage(screenBitmap, 0, 0, screenshotPictureBox.ClientSize.Width, screenshotPictureBox.ClientSize.Height);
			e.Graphics.DrawLine(Pens.LightGray, ToDisplayPoint(points[0]), ToDisplayPoint(points[1]));
			e.Graphics.DrawLine(Pens.LightGray, ToDisplayPoint(points[1]), ToDisplayPoint(points[3]));
			e.Graphics.DrawLine(Pens.LightGray, ToDisplayPoint(points[3]), ToDisplayPoint(points[2]));
			e.Graphics.DrawLine(Pens.LightGray, ToDisplayPoint(points[2]), ToDisplayPoint(points[0]));
			for (int i = 0; i < 4; i++) {
				var brush = i switch { 0 => Brushes.Red, 1 => Brushes.Yellow, 2 => Brushes.Lime, _ => Brushes.RoyalBlue };
				var point = ToDisplayPoint(points[i]);
				e.Graphics.FillEllipse(brush, new(point.X - 5, point.Y - 5, 11, 11));
			}
			if (draggingPoint != null) {
				var point = this.points[this.draggingPoint.Value];
				var displayPoint = ToDisplayPoint(point);
				e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				var destRect = new Rectangle(displayPoint.X + 16, displayPoint.Y + 16, 144, 144);
				for (int y = 0; y < 9; y++) {
					for (int x = 0; x < 9; x++) {
						var x2 = point.X + x - 4;
						var y2 = point.Y + y - 4;
						if (x2 >= 0 && y2 >= 0 && x2 < this.screenImage.Width && y2 < this.screenImage.Height) {
							var pixel = this.screenImage[x2, y2];
							e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(pixel.R, pixel.G, pixel.B)), new Rectangle(displayPoint.X + 16 + x * 16, displayPoint.Y + 16 + y * 16, 16, 16));
						}
					}
				}
				e.Graphics.DrawRectangle(Pens.LightGray, destRect);
				e.Graphics.FillRectangle(Brushes.White, new Rectangle(displayPoint.X + 16 + 72, displayPoint.Y + 16 + 72, 1, 1));
				var rectangle = new Rectangle(destRect.Left, destRect.Bottom, 72, 24);
				e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(192, 0, 0, 0)), rectangle);
				e.Graphics.DrawString($"({point.X}, {point.Y})", this.Font, Brushes.Magenta, rectangle.Location);
			}
		}
	}

	private void screenPictureBox_MouseDown(object sender, MouseEventArgs e) {
		presetBox.Select();
		var ds = new int[4];
		var index = 0;
		for (int i = 0; i < 4; i++) {
			var point = ToDisplayPoint(points[i]);
			ds[i] = (e.X - point.X) * (e.X - point.X) + (e.Y - point.Y) * (e.Y - point.Y);
			if (i != 0 && ds[i] < ds[index]) index = i;
		}
		if (ds[index] > 100) return;
		draggingPoint = index;
		screenshotPictureBox.Refresh();
	}

	private void screenPictureBox_MouseMove(object sender, MouseEventArgs e) {
		if (draggingPoint != null && screenImage != null) {
			var point = FromDisplayPoint(e.Location);
			point.X = Math.Min(Math.Max(point.X, 0), screenImage.Width);
			point.Y = Math.Min(Math.Max(point.Y, 0), screenImage.Height);
			this.points[this.draggingPoint.Value] = point;
			presetBox.SelectedIndex = -1;
			screenshotPictureBox.Refresh();
			RedrawTransformedImage();
		}
	}

	private void screenPictureBox_MouseUp(object sender, MouseEventArgs e) {
		draggingPoint = null;
		screenshotPictureBox.Refresh();
	}

	private Point ToDisplayPoint(Point bitmapPoint) => screenImage == null ? bitmapPoint : new(bitmapPoint.X * screenshotPictureBox.ClientSize.Height / screenImage.Height, bitmapPoint.Y * screenshotPictureBox.ClientSize.Height / screenImage.Height);
	private Point FromDisplayPoint(Point displayPoint) => screenImage == null ? displayPoint : new(displayPoint.X * screenImage.Height / screenshotPictureBox.ClientSize.Height, displayPoint.Y * screenImage.Height / screenshotPictureBox.ClientSize.Height);

	private void screenshotPanel_Resize(object sender, EventArgs e) {
		if (screenImage == null) return;
		var size = new SizeF(panel1.ClientSize.Width, (float) panel1.ClientSize.Width * screenImage.Height / screenImage.Width);
		if (size.Height > panel1.ClientSize.Height)
			size = new SizeF((float) panel1.ClientSize.Height * screenImage.Width / screenImage.Height, panel1.ClientSize.Height);
		screenshotPictureBox.Size = new((int) size.Width, (int) size.Height);
		screenshotPictureBox.Location = new((panel1.Width - screenshotPictureBox.Width) / 2, (panel1.Height - screenshotPictureBox.Height) / 2);
		screenshotPictureBox.Refresh();
	}

	private void RedrawTransformedImage() {
		if (screenImage == null) {
			pictureBox1.Image = null;
			return;
		}

		distortedImage = ImageUtils.PerspectiveUndistort(screenImage, points.Select(p => new SixLabors.ImageSharp.Point(p.X, p.Y)).ToArray(), (InterpolationMode) interpolationBox.SelectedIndex);
		ImageUtils.ColourUncorrect(distortedImage, (LightsState) lightsSimulateBox.SelectedIndex);

		pictureBox1.Image = distortedImage.ToWinFormsImage();
		pictureBox1.Refresh();

		if (autoClassifyBox.Checked && Application.OpenForms.OfType<ClassificationForm>().FirstOrDefault() is ClassificationForm form) {
			form.screenBitmap = distortedImage;
			form.SetLightsState(ImageUtils.GetLightsState(screenImage));
		}
	}

	private void outputCopyButton_Click(object sender, EventArgs e) {
		Clipboard.SetImage(pictureBox1.Image);
	}

	private void outputSaveButton_Click(object sender, EventArgs e) {
		if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
			SaveImage(distortedImage!);
		}
	}

	private void interpolationBox_SelectedIndexChanged(object sender, EventArgs e) {
		RedrawTransformedImage();
	}

	private void screenshotPasteButton_Click(object sender, EventArgs e) {
		if (Clipboard.ContainsImage()) {
			SetScreenImage(new Bitmap(Clipboard.GetImage()!).ToImage<Rgba32>());
		}
	}

	private void outputClassifyButton_Click(object sender, EventArgs e) {
		if (Application.OpenForms.OfType<ClassificationForm>().FirstOrDefault() is ClassificationForm form) {
			form.screenBitmap = distortedImage;
			if (screenImage is not null) form.SetLightsState(ImageUtils.GetLightsState(screenImage));
		}
	}

	private void setPreset(Point[] points) {
		if (screenImage == null || (screenImage.Width == 1920 && screenImage.Height == 1080))
			points.CopyTo(this.points, 0);
		else {
			for (int i = 0; i < points.Length; i++) {
				var point = points[i];
				point.X = point.X * screenImage.Width / 1920;
				point.Y = point.Y * screenImage.Height / 1080;
				this.points[i] = point;
			}
		}
	}

	private void PresetBox_SelectedIndexChanged(object sender, EventArgs e) {
		switch (presetBox.SelectedItem) {
			case Preset preset:
				setPreset(preset.Points);
				screenshotPictureBox.Refresh();
				RedrawTransformedImage();
				break;
			case "Copy current":
				Clipboard.SetText($"presetBox.Items.Add(new Preset(\"\", new Point[] {{ {string.Join(", ", points.Select((p, i) => $"new({p.X,4}, {p.Y,4})"))} }}));");
				break;
		}
	}

	private readonly Preset[] sidePresets = Enumerable.Range(1, 4).Select(i => new Preset($"Side widget {i}", new Point[4])).ToArray();

	private void TransformationForm_Load(object sender, EventArgs e) {
		presetBox.Items.Add(new Preset("Focus", new Point[] { new(836, 390), new(1120, 390), new(832, 678), new(1124, 678) }));

		presetBox.Items.Add(new Preset("Module 1", new Point[] { new(572, 291), new(821, 291), new(560, 534), new(817, 534) }));
		presetBox.Items.Add(new Preset("Module 2", new Point[] { new(852, 291), new(1096, 291), new(849, 534), new(1102, 534) }));
		presetBox.Items.Add(new Preset("Module 3", new Point[] { new(1127, 292), new(1369, 292), new(1134, 533), new(1382, 533) }));
		presetBox.Items.Add(new Preset("Module 4", new Point[] { new(558, 558), new(816, 558), new(544, 822), new(811, 822) }));
		presetBox.Items.Add(new Preset("Module 5", new Point[] { new(848, 558), new(1099, 558), new(845, 821), new(1106, 821) }));
		presetBox.Items.Add(new Preset("Module 6", new Point[] { new(1134, 558), new(1385, 558), new(1141, 821), new(1400, 821) }));

		presetBox.Items.Add(new Preset("Module (–2, –1)", new Point[] { new(220, 100), new(496, 100), new(193, 359), new(479, 359) }));
		presetBox.Items.Add(new Preset("Module (–1, –1)", new Point[] { new(535, 100), new(806, 101), new(522, 359), new(801, 360) }));
		presetBox.Items.Add(new Preset("Module ( 0, –1)", new Point[] { new(840, 101), new(1113, 101), new(836, 360), new(1119, 360) }));
		presetBox.Items.Add(new Preset("Module (+1, –1)", new Point[] { new(1147, 101), new(1407, 101), new(1154, 360), new(1421, 360) }));
		presetBox.Items.Add(new Preset("Module (+2, –1)", new Point[] { new(1456, 102), new(1718, 102), new(1474, 360), new(1745, 360) }));
		presetBox.Items.Add(new Preset("Module (–2,  0)", new Point[] { new(190, 392), new(477, 392), new(160, 678), new(459, 678) }));
		presetBox.Items.Add(new Preset("Module (–1,  0)", new Point[] { new(520, 392), new(800, 392), new(501, 677), new(794, 677) }));
		presetBox.Items.Add(new Preset("Module (+1,  0)", new Point[] { new(1155, 390), new(1425, 390), new(1163, 676), new(1442, 676) }));
		presetBox.Items.Add(new Preset("Module (+2,  0)", new Point[] { new(1476, 390), new(1748, 390), new(1497, 676), new(1779, 676) }));
		presetBox.Items.Add(new Preset("Module (–2, +1)", new Point[] { new(157, 706), new(457, 705), new(124, 1019), new(436, 1018) }));
		presetBox.Items.Add(new Preset("Module (–1, +1)", new Point[] { new(501, 705), new(794, 705), new(481, 1018), new(787, 1017) }));
		presetBox.Items.Add(new Preset("Module ( 0, +1)", new Point[] { new(829, 705), new(1125, 704), new(828, 1018), new(1134, 1016) }));
		presetBox.Items.Add(new Preset("Module (+1, +1)", new Point[] { new(1164, 704), new(1444, 704), new(1173, 1016), new(1465, 1015) }));
		presetBox.Items.Add(new Preset("Module (+2, +1)", new Point[] { new(1499, 704), new(1782, 703), new(1521, 1015), new(1816, 1014) }));

		foreach (var preset in sidePresets) presetBox.Items.Add(preset);

		presetBox.Items.Add(new Preset("Top widget 1", new Point[] { new(588, 430), new(784, 430), new(587, 541), new(784, 541) }));
		presetBox.Items.Add(new Preset("Top widget 2", new Point[] { new(824, 430), new(1140, 430), new(824, 541), new(1140, 541) }));
		presetBox.Items.Add(new Preset("Top widget 3", new Point[] { new(1181, 430), new(1389, 430), new(1182, 540), new(1390, 541) }));
		presetBox.Items.Add(new Preset("Top widget 4", new Point[] { new(580, 566), new(783, 566), new(578, 678), new(782, 678) }));
		presetBox.Items.Add(new Preset("Top widget 5", new Point[] { new(821, 566), new(1140, 566), new(821, 678), new(1140, 678) }));
		presetBox.Items.Add(new Preset("Top widget 6", new Point[] { new(1181, 566), new(1390, 566), new(1182, 678), new(1392, 678) }));

		presetBox.Items.Add("Copy current");
	}

	private void RecalculateSideWidgetPresets() {
		Array.Copy(new Point[] { new(813, 465), new(817, 228), new(988, 465), new(988, 228) }, sidePresets[0].Points, 4);
		Array.Copy(new Point[] { new(988, 465), new(988, 228), new(1163, 465), new(1158, 228) }, sidePresets[1].Points, 4);
		Array.Copy(new Point[] { new(808, 772), new(812, 515), new(988, 772), new(988, 515) }, sidePresets[2].Points, 4);
		Array.Copy(new Point[] { new(988, 772), new(988, 515), new(1168, 772), new(1164, 515) }, sidePresets[3].Points, 4);
		if (screenImage is not null) {
			// When turning the bomb to the side, it isn't possible to position it precisely, so find the bomb position and adjust the polygons.
			static bool isBombBacking(HsvColor hsv) => hsv.H is >= 180 and < 225 && hsv.S < 0.35f && hsv.V >= 0.35f;
			int left;
			for (left = 60; left < screenImage.Width - 60; left++) {
				if (isBombBacking(HsvColor.FromColor(screenImage[left, screenImage.Height / 2])))
					break;
			}
			int right;
			for (right = screenImage.Width - 60; right >= 0; right--) {
				if (isBombBacking(HsvColor.FromColor(screenImage[right, screenImage.Height / 2])))
					break;
			}
			var d = (left + right) / 2 - 988;
			for (var i = 0; i < 4; i++) {
				for (var j = 0; j < 4; j++) {
					sidePresets[i].Points[j].X += d;
				}
			}
		}
	}

	public struct Preset {
		public string Name;
		public Point[] Points;

		public Preset(string name, Point[] points) {
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Points = points ?? throw new ArgumentNullException(nameof(points));
		}
	}

	private void PresetBox_Format(object sender, ListControlConvertEventArgs e) {
		switch (e.ListItem) {
			case Preset preset:
				e.Value = preset.Name;
				break;
			case string s:
				e.Value = s;
				break;
		}
	}

	private async void getFromGameButton_Click(object sender, EventArgs e) {
		using var connector = new DefuserConnector();
		await connector.ConnectAsync(false);
		var image = await connector.TakeScreenshotAsync();
		SetScreenImage(image);
	}

	private void screenshotSaveButton_Click(object sender, EventArgs e) {
		if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
			SaveImage(screenImage!);
		}
	}

	private void SaveImage(Image image) {
		image.Save(saveFileDialog1.FileName, saveFileDialog1.FilterIndex switch {
			1 => new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder(),
			2 => new SixLabors.ImageSharp.Formats.Gif.GifEncoder(),
			3 => new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder(),
			4 => new SixLabors.ImageSharp.Formats.Png.PngEncoder(),
			5 => new SixLabors.ImageSharp.Formats.Tiff.TiffEncoder(),
			_ => new SixLabors.ImageSharp.Formats.Webp.WebpEncoder()
		});
	}

	private void presetBox_KeyDown(object sender, KeyEventArgs e) {
		switch (e.KeyCode) {
			case Keys.W:
				if (this.draggingPoint.HasValue) {
					this.points[this.draggingPoint.Value].Y--;
					presetBox.SelectedIndex = -1;
					screenshotPictureBox.Refresh();
					RedrawTransformedImage();
					e.Handled = true;
					e.SuppressKeyPress = true;
				}
				break;
			case Keys.S:
				if (this.draggingPoint.HasValue) {
					this.points[this.draggingPoint.Value].Y++;
					presetBox.SelectedIndex = -1;
					screenshotPictureBox.Refresh();
					RedrawTransformedImage();
					e.Handled = true;
					e.SuppressKeyPress = true;
				}
				break;
			case Keys.A:
				if (this.draggingPoint.HasValue) {
					this.points[this.draggingPoint.Value].X--;
					presetBox.SelectedIndex = -1;
					screenshotPictureBox.Refresh();
					RedrawTransformedImage();
					e.Handled = true;
					e.SuppressKeyPress = true;
				}
				break;
			case Keys.D:
				if (this.draggingPoint.HasValue) {
					this.points[this.draggingPoint.Value].X++;
					presetBox.SelectedIndex = -1;
					screenshotPictureBox.Refresh();
					RedrawTransformedImage();
					e.Handled = true;
					e.SuppressKeyPress = true;
				}
				break;
		}
	}

	private void lightsSimulateBox_SelectedIndexChanged(object sender, EventArgs e) {
		RedrawTransformedImage();
	}
}
