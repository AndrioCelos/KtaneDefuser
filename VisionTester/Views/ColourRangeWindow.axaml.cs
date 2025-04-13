using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VisionTester.ViewModels;

namespace VisionTester.Views;

public partial class ColourRangeWindow : Window {
	private Rect imageRectangle;

	public ColourRangeWindow() {
		InitializeComponent();
		WeakReferenceMessenger.Default.Register<ColourRangeViewModel.LoadImageFileMessage>(this, (_, m) => m.Reply(LoadImageFileAsync()));
		WeakReferenceMessenger.Default.Register<ColourRangeViewModel.PasteImageMessage>(this, (_, m) => m.Reply(PasteImageAsync()));
		WeakReferenceMessenger.Default.Register<ColourRangeViewModel.ImageChangedMessage>(this, (_, _) => UpdateImageRectangle());
		ModeBox.ItemsSource = new[] {
			ColourRangeViewModel.RangeType.RGB,
			ColourRangeViewModel.RangeType.HSV,
			ColourRangeViewModel.RangeType.RGBD
		};
		
		ImageControl.SizeChanged += ImageControl_SizeChanged;
		ImageControl.PointerMoved += ImageControl_PointerMoved;
	}

	private async Task<IStorageFile?> LoadImageFileAsync() {
		var result = await GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(new() { FileTypeFilter = [FilePickerFileTypes.ImageAll] });
		return result.Count == 0 ? null : result[0];
	}

	private async Task<Image<Rgba32>?> PasteImageAsync() {
		var clipboard = GetTopLevel(this)!.Clipboard!;
		var data = await clipboard.GetDataAsync("PNG");
		if (data is null) {
			Debug.WriteLine($"Could not get image data; formats are {string.Join(", ", await clipboard.GetFormatsAsync())}");
			return null;
		}
		using var ms = new MemoryStream((byte[]) data);
		return SixLabors.ImageSharp.Image.Load<Rgba32>(ms);
	}

	private void ImageControl_PointerMoved(object? sender, PointerEventArgs e) {
		if (imageRectangle.Width == 0 || imageRectangle.Height == 0 || DataContext is not ColourRangeViewModel { InputImage: { } image } vm) return;
		var point = e.GetCurrentPoint(ImageControl);
		var x = (int) Math.Round((point.Position.X - imageRectangle.X) * image.Width / imageRectangle.Width);
		var y = (int) Math.Round((point.Position.Y - imageRectangle.Y) * image.Height / imageRectangle.Height);
		if (point.Properties is { IsLeftButtonPressed: false, IsRightButtonPressed: false }) return;
		vm.MouseClick(new(x, y), point.Properties.IsRightButtonPressed, e.KeyModifiers);
	}

	private void ImageControl_SizeChanged(object? sender, SizeChangedEventArgs e) {
		UpdateImageRectangle();
	}

	private void UpdateImageRectangle() {
		if (DataContext is not ColourRangeViewModel { InputImage: { } image }) return;

		var scaleX = ImageControl.Bounds.Width / image.Width;
		var scaleY = ImageControl.Bounds.Height / image.Height;
		if (scaleX < scaleY) {
			var height = (int) Math.Round(image.Height * scaleX);
			imageRectangle = new(0, (ImageControl.Bounds.Height - height) / 2, ImageControl.Bounds.Width, height);
		} else {
			var width = (int) Math.Round(image.Width * scaleY);
			imageRectangle = new((ImageControl.Bounds.Width - width) / 2, 0, width, ImageControl.Bounds.Height);
		}
	}
}
