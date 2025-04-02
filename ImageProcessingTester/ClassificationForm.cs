using System.Data;
using System.Diagnostics;
using KtaneDefuserConnector;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace ImageProcessingTester;
public partial class ClassificationForm : Form {
	internal Image<Rgba32>? screenImage;

	public ClassificationForm() {
		InitializeComponent();
		LightsStateBox.SelectedIndex = 0;
	}

	private void OpenButton_Click(object sender, EventArgs e) {
		if (openFileDialog.ShowDialog(this) != DialogResult.OK) return;
		screenImage = Image.Load<Rgba32>(openFileDialog.FileName);
		PictureBox.Image = screenImage.ToWinFormsImage();
		ReadModeBox_SelectedIndexChanged(sender, EventArgs.Empty);
	}

	private void PasteButton_Click(object sender, EventArgs e) {
		var image = Clipboard.GetImage();
		if (image is null) {
			MessageBox.Show(this, "There is no usable image on the Clipboard.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}
		this.screenImage = image.ToImage<Rgba32>();
		PictureBox.Image = screenImage.ToWinFormsImage();
		ReadModeBox_SelectedIndexChanged(sender, EventArgs.Empty);
	}

	internal void SetLightsState(LightsState lightsState) {
		LightsStateBox.SelectedIndex = (int) lightsState;
		ReadModeBox_SelectedIndexChanged(ReadModeBox, EventArgs.Empty);
	}

	internal void ReadModeBox_SelectedIndexChanged(object sender, EventArgs e) {
		string? s = null;
		var stopwatch = Stopwatch.StartNew();
		try {
			var lightsState = (LightsState) LightsStateBox.SelectedIndex;
			if (screenImage == null) {
				return;
			}
			using var image = screenImage.Clone();
			//ImageUtils.ColourCorrect(bitmap, lightsState);
			PictureBox.Image = image.ToWinFormsImage();
			if (ReadModeBox.SelectedIndex == 0) {
				if (ImageUtils.CheckForBlankComponent(image)) {
					s = "Blank";
				} else {
					var probs = new Dictionary<string, float>();
					var frameType = ImageUtils.GetComponentFrame(image);

					foreach (var reader in ReadModeBox.Items.OfType<ComponentReader>()) {
						if (reader.FrameType == frameType) {
							var result = reader.IsModulePresent(image);
							probs[reader.Name] = result;
						}
					}

					var sorted = probs.OrderByDescending(e => e.Value).ToList();
					var max = sorted[0];
					s = $"Frame: {frameType}\r\nClassified as: {max.Key}\r\n({string.Join(", ", sorted.Take(3).Select(e => $"{e.Key} [{e.Value:0.000}]"))})";
				}
			} else if (ReadModeBox.SelectedIndex == 1) {
				var pixelCounts = WidgetReader.GetPixelCounts(image, 0);
				var probs = new Dictionary<string, float>();

				foreach (var reader in ReadModeBox.Items.OfType<WidgetReader>()) {
					var result = Math.Max(0, reader.IsWidgetPresent(image, 0, pixelCounts));
					probs[reader.Name] = result;
				}

				var sorted = probs.OrderByDescending(e => e.Value).ToList();
				var max = sorted[0];
				s = $"Classified as: {(max.Value < 0.25f ? "nothing" : max.Key)}\r\n(R: {pixelCounts.Red}, Y: {pixelCounts.Yellow}, E: {pixelCounts.Grey}, W: {pixelCounts.White})\r\n({string.Join(", ", sorted.Take(3).Select(e => $"{e.Key} [{e.Value:0.000}]"))})";
			} else if (ReadModeBox.SelectedIndex == 2) {
				var result = ImageUtils.GetLightState(image, image.Bounds);
				s = result.ToString();
			} else {
				using var bitmap2 = image.Clone();
				var debugImage = bitmap2;
				try {
					switch (ReadModeBox.SelectedItem) {
						case ComponentReader componentReader:
							var result = componentReader.ProcessNonGeneric(image, lightsState, ref debugImage);
							s = result.ToString();
							break;
						case WidgetReader widgetReader:
							result = widgetReader.ProcessNonGeneric(image, lightsState, ref debugImage);
							s = result.ToString();
							break;
					}
					PictureBox.Image = debugImage?.ToWinFormsImage();
				} finally {
					PictureBox.Image = debugImage?.ToWinFormsImage();
					debugImage?.Dispose();
				}
			}
		} catch (Exception ex) {
			s = ex.ToString();
		} finally {
			OutputBox.Text = $"Time: {stopwatch.ElapsedMilliseconds} ms\r\n{s}";
		}
	}

	private void CopyAnnotationsButton_Click(object sender, EventArgs e) => Clipboard.SetImage(PictureBox.Image);

	private void ClassificationForm_Load(object sender, EventArgs e) {
		foreach (var type in typeof(ComponentReader).Assembly.GetTypes()) {
			if (!type.IsAbstract && typeof(ComponentReader).IsAssignableFrom(type))
				ReadModeBox.Items.Add(Activator.CreateInstance(type)!);
			else if (!type.IsAbstract && typeof(WidgetReader).IsAssignableFrom(type))
				ReadModeBox.Items.Add(Activator.CreateInstance(type)!);
		}
	}

	private void ClassificationForm_DragEnter(object sender, DragEventArgs e) {
		if (e.AllowedEffect.HasFlag(DragDropEffects.Copy) && e.Data is DataObject dataObject) {
			var hasImage = dataObject.ContainsImage();
			var hasFile = dataObject.ContainsFileDropList();
			if (hasFile) {
				var list = dataObject.GetFileDropList();
				if (list.Count == 1 && Path.GetExtension(list[0]) is string ext && ext.ToLowerInvariant() is ".bmp" or ".jpg" or ".jpeg" or ".png" or ".webp")
					e.Effect = DragDropEffects.Copy;
			} else if (hasImage)
				e.Effect = DragDropEffects.Copy;
		}
	}

	private void ClassificationForm_DragDrop(object sender, DragEventArgs e) {
		if (e.AllowedEffect.HasFlag(DragDropEffects.Copy) && e.Data is DataObject dataObject) {
			var hasImage = dataObject.ContainsImage();
			var hasFile = dataObject.ContainsFileDropList();
			if (hasFile) {
				var list = dataObject.GetFileDropList();
				if (list.Count == 1 && Path.GetExtension(list[0]) is string ext && ext.ToLowerInvariant() is ".bmp" or ".jpg" or ".jpeg" or ".png" or ".webp") {
					e.Effect = DragDropEffects.Copy;
					screenImage = Image.Load<Rgba32>(list[0]!);
					PictureBox.Image = screenImage.ToWinFormsImage();
					ReadModeBox_SelectedIndexChanged(sender, EventArgs.Empty);
				}
			} else if (hasImage) {
				var image = dataObject.GetImage();
				if (image is not null) {
					this.screenImage = image.ToImage<Rgba32>();
					PictureBox.Image = screenImage.ToWinFormsImage();
					ReadModeBox_SelectedIndexChanged(sender, EventArgs.Empty);
					e.Effect = DragDropEffects.Copy;
				}
			}
		}
	}
}
