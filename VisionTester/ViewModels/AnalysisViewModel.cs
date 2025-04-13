using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using ImageProcessingTester;
using KtaneDefuserConnector;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
			new(AnalysisType.GetModuleLightState, "Light state"),
			.. from r in ComponentReaders select new AnalyserOption(AnalysisType.ComponentReader, r.Name) { ComponentReader = r },
			.. from r in WidgetReaders select new AnalyserOption(AnalysisType.WidgetReader, r.Name) { WidgetReader = r },
		];
		SelectedAnalyserOption = Analysers[0];
		LoadImageCommand = LoadImageFileCommand;
	}
	
	private void AnalysisViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(InputImage):
			case nameof(LightsState):
			case nameof(SelectedAnalyserOption):
				Analyse();
				break;
		}
	}

	[RelayCommand]
	public async Task LoadImageFile() {
		(LoadImageCommand, LoadImageLabel) = (LoadImageFileCommand, "Load file...");
		var message = new LoadImageFileMessage();
		var file = await WeakReferenceMessenger.Default.Send(message);
		if (file is null) return;

		using var stream = await file.OpenReadAsync();
		InputImage = await Image.LoadAsync<Rgba32>(stream);
	}

	[RelayCommand]
	public async Task PasteImage() {
		(LoadImageCommand, LoadImageLabel) = (PasteImageCommand, "Paste");
		var message = new PasteImageMessage();
		var image = await WeakReferenceMessenger.Default.Send(message);
		if (image is null) return;
		InputImage = image;
	}

	public void Analyse() {
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
			//ImageUtils.ColourCorrect(bitmap, lightsState);
			switch (SelectedAnalyserOption.Type) {
				case AnalysisType.IdentifyComponent: {
					if (ImageUtils.CheckForBlankComponent(InputImage)) {
						s = "Blank";
					} else {
						var probs = new Dictionary<string, float>();
						var frameType = ImageUtils.GetComponentFrame(InputImage);

						foreach (var reader in ComponentReaders) {
							if (reader.FrameType == frameType) {
								var result = reader.IsModulePresent(InputImage);
								probs[reader.Name] = result;
							}
						}

						var sorted = probs.OrderByDescending(e => e.Value).ToList();
						var max = sorted[0];
						s = $"Frame: {frameType}\nClassified as: {max.Key}\n({string.Join(", ", sorted.Take(3).Select(e => $"{e.Key} [{e.Value:0.000}]"))})";
					}
					break;
				}
				case AnalysisType.IdentifyWidget: {
					var pixelCounts = WidgetReader.GetPixelCounts(InputImage, 0);
					var probs = new Dictionary<string, float>();

					foreach (var reader in WidgetReaders) {
						var result = Math.Max(0, reader.IsWidgetPresent(InputImage, 0, pixelCounts));
						probs[reader.Name] = result;
					}

					var sorted = probs.OrderByDescending(e => e.Value).ToList();
					var max = sorted[0];
					s = $"Classified as: {(max.Value < 0.25f ? "nothing" : max.Key)}\n(R: {pixelCounts.Red}, Y: {pixelCounts.Yellow}, E: {pixelCounts.Grey}, W: {pixelCounts.White})\r\n({string.Join(", ", sorted.Take(3).Select(e => $"{e.Key} [{e.Value:0.000}]"))})";
					break;
				}
				case AnalysisType.GetModuleLightState: {
					var result = ImageUtils.GetLightState(InputImage, InputImage.Bounds);
					s = result.ToString();
					break;
				}
				default: {
					var image2 = InputImage.Clone();
					var debugImage = image2;
					if (SelectedAnalyserOption.ComponentReader is not null) {
						var result = SelectedAnalyserOption.ComponentReader.ProcessNonGeneric(InputImage, LightsState, ref debugImage);
						s = result.ToString();
					} else if (SelectedAnalyserOption.WidgetReader is not null) {
						var result = SelectedAnalyserOption.WidgetReader.ProcessNonGeneric(InputImage, LightsState, ref debugImage);
						s = result.ToString();
					}
					if (debugImage is not null) {
						OutputImage = debugImage;
						OutputAvaloniaImage = debugImage.ToAvaloniaImage();
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
		ComponentReader,
		WidgetReader
	}

	internal class LoadImageFileMessage : AsyncRequestMessage<IStorageFile?>;
	internal class PasteImageMessage : AsyncRequestMessage<Image<Rgba32>?>;
	internal record CopyImageMessage(Image Image);
}
