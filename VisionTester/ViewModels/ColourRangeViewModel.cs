using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
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

public partial class ColourRangeViewModel : ViewModelBase {
	[ObservableProperty] public partial RangeType SelectedRangeType { get; set; }
	[ObservableProperty] public partial Image<Rgba32>? InputImage { get; set; }
	[ObservableProperty] public partial Bitmap? OutputAvaloniaImage { get; set; }
	[ObservableProperty] public partial string? Label1 { get; set; } = "R";
	[ObservableProperty] public partial int Min1 { get; set; } 
	[ObservableProperty] public partial int Max1 { get; set; }
	[ObservableProperty] public partial int Limit1 { get; set; } = byte.MaxValue;
	[ObservableProperty] public partial string? Label2 { get; set; } = "G";
	[ObservableProperty] public partial int Min2 { get; set; }
	[ObservableProperty] public partial int Max2 { get; set; }
	[ObservableProperty] public partial int Limit2 { get; set; } = byte.MaxValue;
	[ObservableProperty] public partial string? Label3 { get; set; } = "B";
	[ObservableProperty] public partial int Min3 { get; set; }
	[ObservableProperty] public partial int Max3 { get; set; }
	[ObservableProperty] public partial int Limit3 { get; set; } = byte.MaxValue;
	[ObservableProperty] public partial string? AverageLabel { get; set; } = "Average";

	[ObservableProperty] public partial bool UseDistance { get; set; }
	[ObservableProperty] public partial int MaxDistance { get; set; }

	[ObservableProperty] internal partial string LoadImageLabel { get; set; } = "Load file...";
	internal ICommand LoadImageCommand { get; set; }

	private bool autoRedraw = true;

	public ColourRangeViewModel() {
		PropertyChanged += AnalysisViewModel_PropertyChanged;
		LoadImageCommand = LoadImageFileCommand;
	}
	
	private void AnalysisViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(Min1) or nameof(Max1) or nameof(Min2) or nameof(Max2) or nameof(Min3) or nameof(Max3) or nameof(MaxDistance) when autoRedraw:
				Redraw();
				break;
			case nameof(SelectedRangeType):
				Label1 = SelectedRangeType == RangeType.HSV ? "H" : "R";
				Label2 = SelectedRangeType == RangeType.HSV ? "S" : "G";
				Label3 = SelectedRangeType == RangeType.HSV ? "V" : "B";
				Limit1 = SelectedRangeType == RangeType.HSV ? 360 : byte.MaxValue;
				Limit2 = SelectedRangeType == RangeType.HSV ? 100 : byte.MaxValue;
				Limit3 = SelectedRangeType == RangeType.HSV ? 100 : byte.MaxValue;
				UseDistance = SelectedRangeType == RangeType.RGBD;
				Redraw();
				break;
			case nameof(InputImage):
				WeakReferenceMessenger.Default.Send(ImageChangedMessage.Instance);
				Redraw();
				break;
		}
	}

	[RelayCommand]
	public async Task LoadImageFile() {
		(LoadImageCommand, LoadImageLabel) = (LoadImageFileCommand, "Load file...");
		var message = new LoadImageFileMessage();
		var file = await WeakReferenceMessenger.Default.Send(message);
		if (file is null) return;

		await using var stream = await file.OpenReadAsync();
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

	public void Redraw() {
		if (InputImage is null) return;
		
		using var outputImage = InputImage.Clone();
		switch (SelectedRangeType) {
			case RangeType.HSV:
				outputImage.ProcessPixelRows(p => {
					for (int y = 0; y < p.Height; y++) {
						var row = p.GetRowSpan(y);
						for (int x = 0; x < p.Width; x++) {
							ref var pixel = ref row[x];
							var hsv = HsvColor.FromColor(pixel);
							if (!((Min1 > Max1 ? hsv.H >= Min1 || hsv.H <= Max1 : hsv.H >= Min1 && hsv.H <= Max1)
								&& hsv.S * 100 >= Min2 && hsv.S * 100 <= Max2
								&& hsv.V * 100 >= Min3 && hsv.V * 100 <= Max3)) {
								pixel = pixel with { A = (byte) (pixel.A / 4) };
							}
						}
					}
				});
				break;
			case RangeType.RGBD:
				outputImage.ProcessPixelRows(p => {
					for (int y = 0; y < p.Height; y++) {
						var row = p.GetRowSpan(y);
						for (int x = 0; x < p.Width; x++) {
							ref var pixel = ref row[x];
							if (Math.Abs(pixel.R - (int) Min1) + Math.Abs(pixel.G - (int) Min2) + Math.Abs(pixel.B - (int) Min3) > (int) MaxDistance) {
								pixel = pixel with { A = (byte) (pixel.A / 4) };
							}
						}
					}
				});
				break;
			case RangeType.RGB:
				outputImage.ProcessPixelRows(p => {
					for (int y = 0; y < p.Height; y++) {
						var row = p.GetRowSpan(y);
						for (int x = 0; x < p.Width; x++) {
							ref var pixel = ref row[x];
							if (!(pixel.R >= (float) Min1 && pixel.R <= (float) Max1
								&& pixel.G >= (float) Min2 && pixel.G <= (float) Max2
								&& pixel.B >= (float) Min3 && pixel.B <= (float) Max3)) {
								pixel = pixel with { A = (byte) (pixel.A / 4) };
							}
						}
					}
				});
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
		
		OutputAvaloniaImage = outputImage.ToAvaloniaImage();
		RecalculateAverage();
	}

	public void MouseClick(PixelPoint point, bool rightButton, KeyModifiers keyModifiers) {
		if (InputImage is null) return;
		var x = point.X;
		var y = point.Y;
		if (x < 0 || x >= InputImage.Width || y < 0 || y >= InputImage.Height) return;

		var pixel = InputImage[x, y];
		if (!rightButton) {
			AverageLabel = SelectedRangeType == RangeType.HSV ? HsvColor.FromColor(pixel).ToString() : pixel.ToString();
		} else {
			autoRedraw = false;
			switch (SelectedRangeType) {
				case RangeType.RGB:
					Min1 = Math.Min(keyModifiers.HasFlag(KeyModifiers.Alt) ? 255 : Min1, pixel.R);
					Max1 = Math.Max(keyModifiers.HasFlag(KeyModifiers.Alt) ? 0 : Max1, pixel.R);
					Min2 = Math.Min(keyModifiers.HasFlag(KeyModifiers.Alt) ? 255 : Min2, pixel.G);
					Max2 = Math.Max(keyModifiers.HasFlag(KeyModifiers.Alt) ? 0 : Max2, pixel.G);
					Min3 = Math.Min(keyModifiers.HasFlag(KeyModifiers.Alt) ? 255 : Min3, pixel.B);
					Max3 = Math.Max(keyModifiers.HasFlag(KeyModifiers.Alt) ? 0 : Max3, pixel.B);
					break;
				case RangeType.HSV:
					var hsv = HsvColor.FromColor(pixel);
					if (keyModifiers.HasFlag(KeyModifiers.Alt)) {
						Min1 = (int) Math.Floor(hsv.H);
						Max1 = (int) Math.Ceiling(hsv.H);
					} else if (Max1 < Min1) {
						var midpoint = (Min1 + Max1) / 2f;
						if (hsv.H < midpoint) Max1 = Math.Max(Max1, (int) Math.Ceiling(hsv.H));
						else Min1 = Math.Min(Min1, (int) Math.Floor(hsv.H));
					} else {
						Min1 = Math.Min(Min1, (int) Math.Floor(hsv.H));
						Max1 = Math.Max(Max1, (int) Math.Ceiling(hsv.H));
					}
					Min2 = Math.Min(keyModifiers.HasFlag(KeyModifiers.Alt) ? 100 : Min2, (int) Math.Floor(hsv.S * 100));
					Max2 = Math.Max(keyModifiers.HasFlag(KeyModifiers.Alt) ? 0 : Max2, (int) Math.Ceiling(hsv.S * 100));
					Min3 = Math.Min(keyModifiers.HasFlag(KeyModifiers.Alt) ? 100 : Min3, (int) Math.Floor(hsv.V * 100));
					Max3 = Math.Max(keyModifiers.HasFlag(KeyModifiers.Alt) ? 0 : Max3, (int) Math.Ceiling(hsv.V * 100));
					break;
				case RangeType.RGBD:
					MaxDistance = Math.Max(keyModifiers.HasFlag(KeyModifiers.Alt) ? 768 : MaxDistance, Math.Abs(pixel.R - (int) Min1) + Math.Abs(pixel.G - (int) Min2) + Math.Abs(pixel.B - (int) Min3));
					break;
			}
			autoRedraw = true;
			Redraw();
		}
	}

	public void RecalculateAverage() {
		if (InputImage is null) return;
		long r = 0, g = 0, b = 0, a = 0;
		InputImage.ProcessPixelRows(p => {
			for (int y = 0; y < p.Height; y++) {
				var row = p.GetRowSpan(y);
				for (int x = 0; x < p.Width; x++) {
					var color = row[x];
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
		});
		AverageLabel = $"Average: ({r * 255 / a}, {g * 255 / a}, {b * 255 / a})";
	}

	public enum RangeType {
		RGB,
		HSV,
		RGBD
	}

	internal class LoadImageFileMessage : AsyncRequestMessage<IStorageFile?>;
	internal class PasteImageMessage : AsyncRequestMessage<Image<Rgba32>?>;

	internal class ImageChangedMessage {
		public static ImageChangedMessage Instance { get; } = new();
	}
}
