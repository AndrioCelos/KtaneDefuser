using System.Data;
using System.Diagnostics;
using BombDefuserConnector;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using Point = SixLabors.ImageSharp.Point;

namespace ImageProcessingTester;
public partial class ClassificationForm : Form {
	internal Image<Rgba32>? screenBitmap;

	public ClassificationForm() {
		InitializeComponent();
		comboBox2.SelectedIndex = 0;
	}

	private void button1_Click(object sender, EventArgs e) {
		if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
			screenBitmap = Image.Load<Rgba32>(openFileDialog.FileName);
			pictureBox1.Image = screenBitmap.ToWinFormsImage();
			comboBox1_SelectedIndexChanged(sender, EventArgs.Empty);
		}
	}

	private void button2_Click(object sender, EventArgs e) {
		if (Clipboard.ContainsImage()) {
			screenBitmap = Clipboard.GetImage()!.ToImage<Rgba32>();
			pictureBox1.Image = screenBitmap.ToWinFormsImage();
			comboBox1_SelectedIndexChanged(sender, EventArgs.Empty);
		} else
			MessageBox.Show(this, "There is no usable image on the Clipboard.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
	}

	internal void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
		string? s = null;
		var stopwatch = Stopwatch.StartNew();
		try {
			var lightsState = (LightsState) comboBox2.SelectedIndex;
			if (screenBitmap == null) {
				return;
			}
			using var bitmap = screenBitmap.Clone();
			ImageUtils.ColourCorrect(bitmap, lightsState);
			pictureBox1.Image = bitmap.ToWinFormsImage();
			if (comboBox1.SelectedIndex == 0) {
				if (ImageUtils.CheckForBlankComponent(bitmap)) {
					s = "Blank";
				} else {
					var probs = new Dictionary<string, float>();
					var needyRating = ImageUtils.CheckForNeedyFrame(bitmap);

					var looksLikeANeedyModule = needyRating >= 0.5f;

					foreach (var reader in comboBox1.Items.OfType<ComponentReader>()) {
						if (reader.UsesNeedyFrame == looksLikeANeedyModule) {
							var result = reader.IsModulePresent(bitmap);
							probs[reader.Name] = result;
						}
					}

					var sorted = probs.OrderByDescending(e => e.Value).ToList();
					var max = sorted[0];
					s = $"Needy frame: {needyRating}\r\nClassified as: {max.Key}\r\n({string.Join(", ", sorted.Take(3).Select(e => $"{e.Key} [{e.Value:0.000}]"))})";
				}
			} else if (comboBox1.SelectedIndex == 1) {
				var pixelCounts = WidgetReader.GetPixelCounts(bitmap, 0);
				var probs = new Dictionary<string, float>();

				foreach (var reader in comboBox1.Items.OfType<WidgetReader>()) {
					var result = Math.Max(0, reader.IsWidgetPresent(bitmap, 0, pixelCounts));
					probs[reader.Name] = result;
				}

				var sorted = probs.OrderByDescending(e => e.Value).ToList();
				var max = sorted[0];
				s = $"Classified as: {(max.Value < 0.25f ? "nothing" : max.Key)}\r\n(R: {pixelCounts.Red}, Y: {pixelCounts.Yellow}, E: {pixelCounts.Grey}, W: {pixelCounts.White})\r\n({string.Join(", ", sorted.Take(3).Select(e => $"{e.Key} [{e.Value:0.000}]"))})";
			} else if (comboBox1.SelectedIndex == 2) {
				var result = ImageUtils.GetLightState(bitmap, new Point[] { new(0, 0), new(256, 0), new(0, 256), new(256, 256) });
				s = result.ToString();
			} else {
				using var bitmap2 = bitmap.Clone();
				var debugImage = bitmap2;
				try {
					switch (comboBox1.SelectedItem) {
						case ComponentReader componentReader:
							var result = componentReader.ProcessNonGeneric(bitmap, ref debugImage);
							s = result.ToString();
							break;
						case WidgetReader widgetReader:
							result = widgetReader.ProcessNonGeneric(bitmap, 0, ref debugImage);
							s = result.ToString();
							break;
					}
					pictureBox1.Image = debugImage?.ToWinFormsImage();
				} finally {
					pictureBox1.Image = debugImage?.ToWinFormsImage();
					debugImage?.Dispose();
				}
			}
		} catch (Exception ex) {
			s = ex.ToString();
		} finally {
			textBox1.Text = $"Time: {stopwatch.ElapsedMilliseconds} ms\r\n{s}";
		}
	}

	private void button3_Click(object sender, EventArgs e) {
		Clipboard.SetImage(pictureBox1.Image);
	}

	private void ClassificationForm_Load(object sender, EventArgs e) {
		foreach (var type in typeof(ComponentReader).Assembly.GetTypes()) {
			if (!type.IsAbstract && typeof(ComponentReader).IsAssignableFrom(type))
				comboBox1.Items.Add(Activator.CreateInstance(type));
			else if (!type.IsAbstract && typeof(WidgetReader).IsAssignableFrom(type))
				comboBox1.Items.Add(Activator.CreateInstance(type));
		}
	}
}
