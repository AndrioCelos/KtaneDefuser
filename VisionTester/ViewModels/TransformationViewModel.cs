﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using KtaneDefuserConnector;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using HsvColor = KtaneDefuserConnector.HsvColor;

namespace VisionTester.ViewModels;

public partial class TransformationViewModel : ViewModelBase {
	private const int ReferenceScreenWidth = 1920;
	private const int ReferenceScreenHeight = 1080;
	
	public MainWindowViewModel? MainViewModel;
	[ObservableProperty] public partial Image<Rgba32>? ScreenImage { get; set; }
	[ObservableProperty] public partial Image<Rgba32>? OutputImage { get; set; }
	[ObservableProperty] public partial Bitmap? OutputAvaloniaImage { get; set; }
	[ObservableProperty] public partial Quadrilateral Quadrilateral { get; set; }
	[ObservableProperty] public partial bool AutoClassify { get; set; }
	[ObservableProperty] public partial InterpolationMode InterpolationMode { get; set; }
	[ObservableProperty] public partial LightsState LightsState { get; private set; }
	[ObservableProperty] public partial LightsState LightsSimulation { get; set; }

	[ObservableProperty] internal partial string LoadImageLabel { get; set; } = "Load file...";
	[ObservableProperty] internal partial ICommand LoadImageCommand { get; set; }
	[ObservableProperty] internal partial string SaveImageLabel { get; set; } = "Save file...";
	[ObservableProperty] internal partial ICommand SaveImageCommand { get; set; }

	private Preset[] Presets { get; } = [
		new("Module 1", [new(572, 291), new(821, 291), new(560, 534), new(817, 534)], 224),
		new("Module 2", [new(852, 291), new(1096, 291), new(849, 534), new(1102, 534)], 224),
		new("Module 3", [new(1127, 292), new(1369, 292), new(1134, 533), new(1382, 533)], 224),
		new("Module 4", [new(558, 558), new(816, 558), new(544, 822), new(811, 822)], 224),
		new("Module 5", [new(848, 558), new(1099, 558), new(845, 821), new(1106, 821)], 224),
		new("Module 6", [new(1134, 558), new(1385, 558), new(1141, 821), new(1400, 821)], 224),

		new("Module -2 -1", [new(220, 100), new(496, 100), new(193, 359), new(479, 359)], 288),
		new("Module -1 -1", [new(535, 100), new(806, 101), new(522, 359), new(801, 360)], 288),
		new("Module  0 -1", [new(840, 101), new(1113, 101), new(836, 360), new(1119, 360)], 288),
		new("Module +1 -1", [new(1147, 101), new(1407, 101), new(1154, 360), new(1421, 360)], 288),
		new("Module +2 -1", [new(1456, 102), new(1718, 102), new(1474, 360), new(1745, 360)], 288),
		new("Module -2  0", [new(190, 392), new(477, 392), new(160, 678), new(459, 678)], 288),
		new("Module -1  0", [new(520, 392), new(800, 392), new(501, 677), new(794, 677)], 288),
		new("Module  0  0", [new(836, 390), new(1124,  390), new(836, 678), new(1124, 678)], 288),
		new("Module +1  0", [new(1155, 390), new(1425, 390), new(1163, 676), new(1442, 676)], 288),
		new("Module +2  0", [new(1476, 390), new(1748, 390), new(1497, 676), new(1779, 676)], 288),
		new("Module -2 +1", [new(157, 706), new(457, 705), new(124, 1019), new(436, 1018)], 288),
		new("Module -1 +1", [new(501, 705), new(794, 705), new(481, 1018), new(787, 1017)], 288),
		new("Module  0 +1", [new(829, 705), new(1125, 704), new(828, 1018), new(1134, 1016)], 288),
		new("Module +1 +1", [new(1164, 704), new(1444, 704), new(1173, 1016), new(1465, 1015)], 288),
		new("Module +2 +1", [new(1499, 704), new(1782, 703), new(1521, 1015), new(1816, 1014)], 288),

		.. from i in Enumerable.Range(1, 4) select new Preset($"Side widget {i}", new Point[4], ImageUtils.StandardResolution),

		new("Top widget 1", [new(588, 430), new(784, 430), new(587, 541), new(784, 541)], ImageUtils.StandardResolution),
		new("Top widget 2", [new(824, 430), new(1140, 430), new(824, 541), new(1140, 541)], ImageUtils.StandardResolution),
		new("Top widget 3", [new(1181, 430), new(1389, 430), new(1182, 540), new(1390, 541)], ImageUtils.StandardResolution),
		new("Top widget 4", [new(580, 566), new(783, 566), new(578, 678), new(782, 678)], ImageUtils.StandardResolution),
		new("Top widget 5", [new(821, 566), new(1140, 566), new(821, 678), new(1140, 678)], ImageUtils.StandardResolution),
		new("Top widget 6", [new(1181, 566), new(1390, 566), new(1182, 678), new(1392, 678)], ImageUtils.StandardResolution),

		new("Centurion -2 -1", [new( 180,   66), new( 479,   66), new( 168,  357), new( 469,  357)], 305),
		new("Centurion -1 -1", [new( 513,   66), new( 810,   66), new( 506,  357), new( 810,  357)], 305),
		new("Centurion  0 -1", [new( 847,   66), new(1148,   66), new( 847,  357), new(1149,  357)], 305),
		new("Centurion +1 -1", [new(1182,   66), new(1480,   66), new(1185,  357), new(1487,  357)], 305),
		new("Centurion +2 -1", [new(1517,   66), new(1820,   66), new(1525,  357), new(1824,  357)], 305),
		new("Centurion -2  0", [new( 167,  370), new( 469,  370), new( 155,  676), new( 460,  676)], 305),
		new("Centurion -1  0", [new( 503,  370), new( 811,  370), new( 499,  675), new( 807,  675)], 305),
		new("Centurion  0  0", [new( 844,  370), new(1149,  370), new( 844,  675), new(1149,  675)], 305),
		new("Centurion +1  0", [new(1189,  370), new(1492,  370), new(1190,  675), new(1498,  675)], 305),
		new("Centurion +2  0", [new(1532,  370), new(1827,  370), new(1536,  675), new(1831,  675)], 305),
		new("Centurion -2 +1", [new( 153,  688), new( 462,  688), new( 138, 1003), new( 452, 1003)], 305),
		new("Centurion -1 +1", [new( 501,  688), new( 806,  688), new( 495, 1002), new( 805, 1002)], 305),
		new("Centurion  0 +1", [new( 843,  688), new(1154,  688), new( 843, 1002), new(1155, 1002)], 305),
		new("Centurion +1 +1", [new(1194,  687), new(1499,  687), new(1195, 1001), new(1505, 1001)], 305),
		new("Centurion +2 +1", [new(1543,  687), new(1845,  687), new(1544, 1001), new(1855, 1001)], 305),

		new("Centurion -2.5 -1", [new(  17,   66), new( 317,   66), new(   0,  357), new( 299,  357)], 305),
		new("Centurion -1.5 -1", [new( 344,   66), new( 646,   66), new( 338,  357), new( 641,  357)], 305),
		new("Centurion -0.5 -1", [new( 681,   66), new( 980,   66), new( 681,  357), new( 980,  357)], 305),
		new("Centurion +0.5 -1", [new(1014,   66), new(1311,   66), new(1015,  357), new(1312,  357)], 305),
		new("Centurion +1.5 -1", [new(1353,   66), new(1642,   66), new(1351,  357), new(1654,  357)], 305),
		new("Centurion -1.5 +1", [new( 323,  688), new( 634,  688), new( 314, 1003), new( 626, 1003)], 305),
		new("Centurion -0.5 +1", [new( 670,  688), new( 982,  688), new( 670, 1002), new( 982, 1002)], 305),
		new("Centurion +0.5 +1", [new(1017,  688), new(1326,  688), new(1018, 1001), new(1329, 1001)], 305),
		new("Centurion +1.5 +1", [new(1368,  687), new(1671,  687), new(1369, 1001), new(1687, 1001)], 305),
		
		new("Centurion top/bottom widgets", [new( 550,  440), new(1440,  440), new( 550,  620), new(1440,  620)], ImageUtils.StandardResolution)
	];
	[ObservableProperty] public partial Preset? SelectedPreset { get; set; }
	[ObservableProperty] public partial int Resolution { get; set; } = ImageUtils.StandardResolution;

	public TransformationViewModel() {
		PropertyChanged += (_, e) => {
			switch (e.PropertyName) {
				case nameof(Quadrilateral):
				case nameof(InterpolationMode):
				case nameof(LightsSimulation):
				case nameof(Resolution):
					RedrawTransformedImage();
					break;
				case nameof(SelectedPreset):
					if (SelectedPreset is not { } preset) break;
					SetPreset(preset);
					RedrawTransformedImage();
					break;
				case nameof(ScreenImage):
					if (ScreenImage is null) break;
					Quadrilateral = new(from i in Enumerable.Range(0, 4) select Quadrilateral[i] into p select new Point(Math.Min(p.X, ScreenImage.Width), Math.Min(p.Y, ScreenImage.Height)));
					LightsState = ImageUtils.GetLightsState(ScreenImage);
					RecalculateSideWidgetPresets();
					RedrawTransformedImage();
					break;
			}
		};

		LoadImageCommand = LoadImageFileCommand;
		SaveImageCommand = SaveImageFileCommand;
	}

	[RelayCommand]
	private async Task LoadImageFile() {
		(LoadImageCommand, LoadImageLabel) = (LoadImageFileCommand, "Load file...");
		var message = new LoadImageFileMessage();
		var file = await WeakReferenceMessenger.Default.Send(message);
		if (file is null) return;

		await using var stream = await file.OpenReadAsync();
		ScreenImage = await Image.LoadAsync<Rgba32>(stream);
	}

	[RelayCommand]
	private async Task PasteImage() {
		(LoadImageCommand, LoadImageLabel) = (PasteImageCommand, "Paste");
		var message = new PasteImageMessage();
		var image = await WeakReferenceMessenger.Default.Send(message);
		if (image is null) return;
		ScreenImage = image;
	}

	[RelayCommand]
	private async Task LoadImageFromGame() {
		var stopwatch = Stopwatch.StartNew();
		(LoadImageCommand, LoadImageLabel) = (LoadImageFromGameCommand, "From game");
		Debug.WriteLine($"Set command: {stopwatch.Elapsed}");
		using var connector = new DefuserConnector();
		await connector.ConnectAsync(NullLoggerFactory.Instance);
		Debug.WriteLine($"Connected: {stopwatch.Elapsed}");
		var image = await connector.TakeScreenshotAsync();
		Debug.WriteLine($"Downloaded image: {stopwatch.Elapsed}");
		ScreenImage = image;
	}

	public async Task SaveInputImageFile() {
		if (ScreenImage is null) return;
		var message = new SaveImageFileMessage();
		var file = await WeakReferenceMessenger.Default.Send(message);
		if (file is null) return;

		var encoder = (ImageEncoder) (Path.GetExtension(file.Name).ToLowerInvariant() switch {
			".bmp" => new BmpEncoder(),
			".gif" => new GifEncoder(),
			".jpeg" or ".jpg" => new JpegEncoder(),
			".pbm" => new PbmEncoder(),
			".qoi" => new QoiEncoder(),
			".tga" => new TgaEncoder(),
			".tiff" => new TiffEncoder(),
			".webp" => new WebpEncoder(),
			_ => new PngEncoder()
		});
		await using var stream = await file.OpenWriteAsync();
		await ScreenImage.SaveAsync(stream, encoder);
	}

	[RelayCommand]
	private async Task SaveImageFile() {
		(SaveImageCommand, SaveImageLabel) = (SaveImageFileCommand, "Save file...");
		if (OutputImage is null) return;
		var message = new SaveImageFileMessage();
		var file = await WeakReferenceMessenger.Default.Send(message);
		if (file is null) return;

		var encoder = (ImageEncoder) (Path.GetExtension(file.Name).ToLowerInvariant() switch {
			".bmp" => new BmpEncoder(),
			".gif" => new GifEncoder(),
			".jpeg" or ".jpg" => new JpegEncoder(),
			".pbm" => new PbmEncoder(),
			".qoi" => new QoiEncoder(),
			".tga" => new TgaEncoder(),
			".tiff" => new TiffEncoder(),
			".webp" => new WebpEncoder(),
			_ => new PngEncoder()
		});
		await using var stream = await file.OpenWriteAsync();
		await OutputImage.SaveAsync(stream, encoder);
	}

	[RelayCommand]
	private void CopyImage() {
		(SaveImageCommand, SaveImageLabel) = (CopyImageCommand, "Copy");
		if (OutputImage is null) return;
		var message = new CopyImageMessage(OutputImage);
		WeakReferenceMessenger.Default.Send(message);
	}

	private void RedrawTransformedImage() {
		if (ScreenImage is null) {
			OutputImage = null;
			return;
		}

		OutputImage = ImageUtils.PerspectiveUndistort(ScreenImage, Quadrilateral, InterpolationMode, new(Resolution, Resolution));
		OutputImage.ColourUncorrect(LightsSimulation);

		using (var ms = new MemoryStream()) {
			OutputImage.Save(ms, new BmpEncoder());
			ms.Position = 0;
			OutputAvaloniaImage = new(ms);
		}

		if (AutoClassify) Analyse();
	}

	private void RecalculateSideWidgetPresets() {
		Array.Copy(new Point[] { new(813, 465), new(817, 228), new(988, 465), new(988, 228) }, Presets[21].Points, 4);
		Array.Copy(new Point[] { new(988, 465), new(988, 228), new(1163, 465), new(1158, 228) }, Presets[22].Points, 4);
		Array.Copy(new Point[] { new(808, 772), new(812, 515), new(988, 772), new(988, 515) }, Presets[23].Points, 4);
		Array.Copy(new Point[] { new(988, 772), new(988, 515), new(1168, 772), new(1164, 515) }, Presets[24].Points, 4);
		if (ScreenImage is null) return;

		// When turning the bomb to the side, it isn't possible to position it precisely, so find the bomb position and adjust the polygons.
		int left;
		for (left = 60; left < ScreenImage.Width - 60; left++) {
			if (IsBombBacking(HsvColor.FromColor(ScreenImage[left, ScreenImage.Height / 2])))
				break;
		}
		int right;
		for (right = ScreenImage.Width - 60; right >= 0; right--) {
			if (IsBombBacking(HsvColor.FromColor(ScreenImage[right, ScreenImage.Height / 2])))
				break;
		}
		var d = (left + right) / 2 - 988;
		for (var i = 0; i < 4; i++) {
			for (var j = 0; j < 4; j++) {
				Presets[21 + i].Points[j].X += d;
			}
		}

		return;

		static bool IsBombBacking(HsvColor hsv) => hsv is { H: >= 180 and < 225, S: < 0.35f, V: >= 0.35f };
	}

	private void SetPreset(Preset preset) {
		Quadrilateral = ScreenImage is null or { Width: ReferenceScreenWidth, Height: ReferenceScreenHeight }
			?  new(preset.Points)
			:  new(from p in preset.Points select new Point(p.X * ScreenImage.Width / ReferenceScreenWidth, p.Y * ScreenImage.Height / ReferenceScreenHeight));
		Resolution = preset.Resolution;
	}

	internal void SetPresetByName(string name) {
		var preset = Presets.First(p => p.Name == name);
		SetPreset(preset);
	}

	internal void CopyPresetCode() {
		if (ScreenImage is null) return;
		var text = $"new(\"\", [{string.Join(", ", from i in Enumerable.Range(0, 4) select Quadrilateral[i] into p
			select $"new({p.X * ReferenceScreenWidth / ScreenImage.Width,4}, {p.Y * ReferenceScreenHeight / ScreenImage.Height,4})")}]);";
		var message = new CopyTextMessage(text);
		WeakReferenceMessenger.Default.Send(message);
	}

	internal void Analyse() {
		if (ScreenImage is null || OutputImage is null || MainViewModel?.AnalysisViewModel is not { } vm) return;
		vm.InputImage = null;
		vm.LightsState = LightsSimulation != LightsState.On ? LightsSimulation : ImageUtils.GetLightsState(ScreenImage);
		vm.InputImage = OutputImage;
	}

	internal void ToggleAutoAnalyse() {
		AutoClassify = !AutoClassify;
		if (AutoClassify)
			Analyse();
	}

	public record struct Preset(string Name, Point[] Points, int Resolution);

	internal class LoadImageFileMessage : AsyncRequestMessage<IStorageFile?>;
	internal class PasteImageMessage : AsyncRequestMessage<Image<Rgba32>?>;
	internal class SaveImageFileMessage : AsyncRequestMessage<IStorageFile?>;
	internal record CopyTextMessage(string Text);
	internal record CopyImageMessage(Image Image);
}
