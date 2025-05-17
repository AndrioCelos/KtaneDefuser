using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using KtaneDefuserConnector;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace VisionTester.ViewModels;

public partial class AnalysisViewModel : ViewModelBase {
	[ObservableProperty] public partial LightsState LightsState { get; set; }
	[ObservableProperty] public partial Image<Rgba32>? InputImage { get; set; }
	[ObservableProperty] public partial string? OutputText { get; set; }
	[ObservableProperty] public partial Image<Rgba32>? OutputImage { get; set; }
	[ObservableProperty] public partial Bitmap? OutputAvaloniaImage { get; set; }
	[ObservableProperty] public partial AnalyserOption SelectedAnalyserOption { get; set; }
	internal AnalyserOption[] Analysers { get; }

	[ObservableProperty] internal partial string LoadImageLabel { get; set; } = "Load file...";
	internal ICommand LoadImageCommand { get; set; }

	private static readonly ComponentReader[] ComponentReaders;
	private static readonly WidgetReader[] WidgetReaders;

	private readonly DefuserConnector _connector = new();

	static AnalysisViewModel() {
		var componentReaders = new List<ComponentReader>();
		var widgetReaders = new List<WidgetReader>();
		foreach (var type in typeof(ComponentReader).Assembly.GetTypes()) {
			if (!type.IsAbstract && typeof(ComponentReader).IsAssignableFrom(type))
				componentReaders.Add((ComponentReader) Activator.CreateInstance(type)!);
			else if (!type.IsAbstract && typeof(WidgetReader).IsAssignableFrom(type))
				widgetReaders.Add((WidgetReader) Activator.CreateInstance(type)!);
		}
		ComponentReaders = [.. componentReaders];
		WidgetReaders = [.. widgetReaders];
	}

	public AnalysisViewModel() {
		PropertyChanged += AnalysisViewModel_PropertyChanged;
		Analysers = [
			new(AnalysisType.IdentifyComponent, "Identify component"),
			new(AnalysisType.IdentifyWidget, "Identify widget"),
			new(AnalysisType.GetModuleLightState, "Module light state"),
			new(AnalysisType.GetRoomLightState, "Room light state"),
			new(AnalysisType.GetCenturionTopWidgetBoxes, "Centurion top widgets"),
			new(AnalysisType.GetCenturionSideWidgetBoxes, "Centurion side widgets"),
			.. from r in ComponentReaders select new AnalyserOption(AnalysisType.ComponentReader, r.Name) { ComponentReader = r },
			.. from r in WidgetReaders select new AnalyserOption(AnalysisType.WidgetReader, r.Name) { WidgetReader = r },
		];
		SelectedAnalyserOption = Analysers[0];
		LoadImageCommand = LoadImageFileCommand;
	}
	
	private void AnalysisViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(InputImage):
			case nameof(LightsState):
			case nameof(SelectedAnalyserOption):
				Analyse();
				break;
		}
	}

	[RelayCommand]
	private async Task LoadImageFile() {
		(LoadImageCommand, LoadImageLabel) = (LoadImageFileCommand, "Load file...");
		var message = new LoadImageFileMessage();
		var file = await WeakReferenceMessenger.Default.Send(message);
		if (file is null) return;

		await using var stream = await file.OpenReadAsync();
		InputImage = await Image.LoadAsync<Rgba32>(stream);
	}

	[RelayCommand]
	private async Task PasteImage() {
		(LoadImageCommand, LoadImageLabel) = (PasteImageCommand, "Paste");
		var message = new PasteImageMessage();
		var image = await WeakReferenceMessenger.Default.Send(message);
		if (image is null) return;
		InputImage = image;
	}

	private void Analyse() {
		string? s = null;
		var stopwatch = Stopwatch.StartNew();
		try {
			if (InputImage is null) {
				OutputImage = null;
				OutputText = null;
				return;
			}
			OutputImage = InputImage;
			OutputAvaloniaImage = InputImage.ToAvaloniaImage();
			switch (SelectedAnalyserOption.Type) {
				case AnalysisType.IdentifyComponent: {
					var reader = _connector.GetComponentReader(InputImage, InputImage.Bounds);
					s = $"Classified as: {reader?.Name ?? "null"}";
					break;
				}
				case AnalysisType.IdentifyWidget: {
					var pixelCounts = WidgetReader.GetPixelCounts(InputImage, 0);
					var reader = _connector.GetWidgetReader(InputImage, InputImage.Bounds);
					s = $"Classified as: {reader?.Name ?? "null"}\n(R: {pixelCounts.Red}, Y: {pixelCounts.Yellow}, E: {pixelCounts.Grey}, W: {pixelCounts.White})";
					break;
				}
				case AnalysisType.GetModuleLightState: {
					var result = ImageUtils.GetLightState(InputImage, InputImage.Bounds);
					s = result.ToString();
					break;
				}
				case AnalysisType.GetRoomLightState: {
					var result = ImageUtils.GetLightsState(InputImage);
					s = result.ToString();
					break;
				}
				case AnalysisType.GetCenturionTopWidgetBoxes: {
					var rectangles = ImageUtils.GetCenturionTopWidgetBounds(InputImage, InputImage.Width >= 1000 ? new(550, 440, 890, 180) : new(0, 0, InputImage.Width, InputImage.Height));
					var debugImage = InputImage.Clone();
					debugImage.Mutate(p => {
						foreach (var rect in rectangles) {
							p.Draw(Color.Cyan, 1, rect);
						}
					});
					OutputImage = debugImage;
					OutputAvaloniaImage = debugImage.ToAvaloniaImage();

					s = rectangles.Select(rect => _connector.GetWidgetReader(InputImage, rect)).Aggregate(s, (current, reader) => current + $"{reader?.Name ?? "null"}; ");
					break;
				}
				case AnalysisType.GetCenturionSideWidgetBoxes: {
					var bombTopY = 0;
					InputImage.ProcessPixelRows(a => {
						for (var y = 0; y < a.Height; y++) {
							var row = a.GetRowSpan(y);
							for (var x = 640 * a.Width / ImageUtils.ReferenceScreenWidth; x < 1280 * a.Width / ImageUtils.ReferenceScreenWidth; x++) {
								if (HsvColor.FromColor(row[x]) is not { S: < 0.11f, V: >= 0.35f and < 0.98f }) continue;
								bombTopY = y;
								return;
							}
						}
					});
					
					var (top, height) = bombTopY switch {
						< 25 => (20, 580),  // Looking at the top section.
						< 90 => (250, 500),  // Looking at the middle section.
						_ => (420, 650)  // Looking at the bottom section.
					};
					
					var debugImage = InputImage.Clone();
					var baseRectangle = ImageUtils.GetCenturionSideWidgetBaseRectangle(InputImage, top * InputImage.Height / ImageUtils.ReferenceScreenHeight, height * InputImage.Height / ImageUtils.ReferenceScreenHeight);
					debugImage.Mutate(p => p.Draw(Color.Lime, 1, baseRectangle));
					var rectangles = ImageUtils.GetCenturionSideWidgetBounds(InputImage, baseRectangle);
					debugImage.Mutate(p => {
						foreach (var rect in rectangles) {
							p.Draw(Color.Cyan, 1, rect);
						}
					});
					OutputImage = debugImage;
					OutputAvaloniaImage = debugImage.ToAvaloniaImage();

					s = rectangles.Select(rect => _connector.GetWidgetReader(InputImage, new(rect.Right, rect.Top, rect.Right, rect.Bottom, rect.Left, rect.Top, rect.Left, rect.Bottom))).Aggregate(s, (current, reader) => current + $"{reader?.Name ?? "null"}; ");
					break;
				}
				default: {
					var image2 = InputImage.Clone();
					var debugImage = image2;
					var args = new object[] { InputImage, LightsState, debugImage };
					try {
						var reader = (object?) SelectedAnalyserOption.ComponentReader ?? SelectedAnalyserOption.WidgetReader;
						if (reader is not null) {
							var result = reader.GetType().GetMethod(nameof(ComponentReader<>.Process), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(reader, args);
							s = result?.ToString();
						}
					} finally {
						debugImage = (Image<Rgba32>?) args[2];
						if (debugImage is not null) {
							OutputImage = debugImage;
							OutputAvaloniaImage = debugImage.ToAvaloniaImage();
						}
					}
					break;
				}
			}
		} catch (Exception ex) {
			s = ex.ToString();
		} finally {
			OutputText = $"Time: {stopwatch.ElapsedMilliseconds} ms\n{s}";
		}
	}

	public void CopyImage() {
		if (OutputImage is null) return;
		var message = new CopyImageMessage(OutputImage);
		WeakReferenceMessenger.Default.Send(message);
	}

	public class AnalyserOption(AnalysisType type, string label) {
		public AnalysisType Type { get; } = type;
		public string Label { get; } = label;
		public ComponentReader? ComponentReader { get; init; }
		public WidgetReader? WidgetReader { get; init; }
	}

	public enum AnalysisType {
		IdentifyComponent,
		IdentifyWidget,
		GetModuleLightState,
		GetRoomLightState,
		GetCenturionTopWidgetBoxes,
		GetCenturionSideWidgetBoxes,
		ComponentReader,
		WidgetReader
	}

	internal class LoadImageFileMessage : AsyncRequestMessage<IStorageFile?>;
	internal class PasteImageMessage : AsyncRequestMessage<Image<Rgba32>?>;
	internal record CopyImageMessage(Image Image);
}
